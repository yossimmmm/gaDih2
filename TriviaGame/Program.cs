using TriviaGame.Components;
using TriviaGame.Hubs;
using DBL;
using TriviaGame.Services;
using Models;

var builder = WebApplication.CreateBuilder(args);

// רישום שירותי UI אינטראקטיבי (Razor + SignalR)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// שירותי תשתית כלליים לאפליקציה
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserSessionState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GeminiAdviceService>();
builder.Services.AddSingleton<AuthAuditDispatcher>();
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return BuildSmtpSettings(cfg);
});
builder.Services.AddScoped<EmailService>();
var app = builder.Build();
var authAudit = app.Services.GetRequiredService<AuthAuditDispatcher>();
var authAuditLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AuthAudit");
authAudit.OnAuditAsync += async auditEvent =>
{
    authAuditLogger.LogInformation("AUTH_AUDIT action={Action} userId={UserId} email={Email} outcome={Outcome} at={AtUtc}",
        auditEvent.Action, auditEvent.UserId, auditEvent.Email, auditEvent.Outcome, auditEvent.OccurredAtUtc);
    await Task.CompletedTask;
};

// זריעת נתוני שאלות התחלתיים בזמן עליית האפליקציה
try
{
    await SeedData.EnsureSeedQuestionsAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to seed trivia questions.");
}

// קונפיגורציית middleware בהתאם לסביבה
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAntiforgery();

// רשימת נתיבים שדורשים משתמש מחובר
var protectedPrefixes = new[]
{
    "/menu",
    "/rooms",
    "/create-room",
    "/lobby",
    "/play",
    "/results",
    "/stats",
    "/assistant",
    "/user",
    "/top-players",
    "/admin-users"
};

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // דילוג על בדיקת התחברות עבור נתיבים ציבוריים
    if (IsPublicPath(path))
    {
        await next();
        return;
    }

    if (protectedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
    {
        // אם אין טוקן סשן - מעבירים ללוגין
        if (!context.Request.Cookies.TryGetValue("session_token", out var token) || string.IsNullOrWhiteSpace(token))
        {
            context.Response.Redirect("/login");
            return;
        }

        // בדיקה שהטוקן אכן קיים במסד ושייך למשתמש תקף
        var sessionDb = new SessionDB();
        var userId = await sessionDb.GetUserIdByTokenAsync(token);
        if (userId is null)
        {
            context.Response.Cookies.Delete("session_token");
            context.Response.Redirect("/login");
            return;
        }
    }

    await next();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// endpoint התחברות: אימות פרטי משתמש ויצירת סשן
app.MapPost("/api/auth/login", async (HttpContext http, LoginRequest req, AuthAuditDispatcher audit) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { ok = false, message = "Email and password are required." });

    var userDb = new UserDB();
    var user = await userDb.GetByEmailAsync(req.Email.Trim().ToLowerInvariant());
    if (user is null || !PasswordHelper.Verify(req.Password, user.PasswordHash))
    {
        await audit.PublishAsync(new AuthAuditEvent("Login", null, req.Email.Trim(), "Failed", DateTime.UtcNow));
        return Results.Unauthorized();
    }

    var sessionDb = new SessionDB();
    var token = await sessionDb.CreateSessionAsync(user.UserID, TimeSpan.FromDays(7));

    http.Response.Cookies.Append("session_token", token, new CookieOptions
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    });

    await audit.PublishAsync(new AuthAuditEvent("Login", user.UserID, user.Email, "Success", DateTime.UtcNow));
    return Results.Ok(new { ok = true, userId = user.UserID, username = user.Username, role = user.Role.ToString() });
});

// endpoint התנתקות: מחיקת סשן מהשרת ומהעוגיות
app.MapPost("/api/auth/logout", async (HttpContext http) =>
{
    if (http.Request.Cookies.TryGetValue("session_token", out var token) && !string.IsNullOrWhiteSpace(token))
    {
        var sessionDb = new SessionDB();
        await sessionDb.DeleteSessionAsync(token);
    }

    http.Response.Cookies.Delete("session_token");
    return Results.Ok(new { ok = true });
});

// endpoint זיהוי משתמש מחובר לפי session cookie
app.MapGet("/api/auth/me", async (HttpContext http) =>
{
    if (!http.Request.Cookies.TryGetValue("session_token", out var token) || string.IsNullOrWhiteSpace(token))
        return Results.Ok(new { userId = 0, role = "User" });

    var sessionDb = new SessionDB();
    var userId = await sessionDb.GetUserIdByTokenAsync(token);
    if (!userId.HasValue)
        return Results.Ok(new { userId = 0, role = "User" });

    var userDb = new UserDB();
    var user = await userDb.GetByIdAsync(userId.Value);
    return Results.Ok(new { userId = userId.Value, role = (user?.Role ?? UserRole.User).ToString() });
});

// endpoint שכחתי סיסמה: יצירת טוקן איפוס ושליחת מייל
app.MapPost("/api/auth/forgot-password", async (HttpContext http, ForgotPasswordRequest req, EmailService emailService, AuthAuditDispatcher audit) =>
{
    // ולידציה בסיסית לקלט
    if (string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { ok = false, message = "Email is required." });

    var normalizedEmail = req.Email.Trim().ToLowerInvariant();
    if (!ValidationHelper.IsValidEmail(normalizedEmail))
        return Results.BadRequest(new { ok = false, message = "Please enter a valid email address." });

    var userDb = new UserDB();
    var user = await userDb.GetByEmailAsync(normalizedEmail);
    if (user is null)
    {
        await audit.PublishAsync(new AuthAuditEvent("ForgotPassword", null, normalizedEmail, "EmailNotFound", DateTime.UtcNow));
        return Results.BadRequest(new { ok = false, message = "No account found for this email." });
    }

    // בדיקת קונפיג SMTP לפני שליחה
    var smtpFrom = Environment.GetEnvironmentVariable("SMTP_FROM") ?? builder.Configuration["Smtp:From"];
    var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? builder.Configuration["Smtp:Host"];
    var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? builder.Configuration["Smtp:User"];
    var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? builder.Configuration["Smtp:Pass"];

    if (string.IsNullOrWhiteSpace(smtpFrom) ||
        string.IsNullOrWhiteSpace(smtpHost) ||
        string.IsNullOrWhiteSpace(smtpUser) ||
        string.IsNullOrWhiteSpace(smtpPass))
    {
        return Results.Json(new { ok = false, message = "SMTP is not configured correctly on the server." }, statusCode: 500);
    }

    // יצירת טוקן מאובטח וקישור איפוס עם query token
    var token = await userDb.CreatePasswordResetTokenAsync(user.UserID, TimeSpan.FromMinutes(30));
    var encodedToken = Uri.EscapeDataString(token);
    var link = $"{http.Request.Scheme}://{http.Request.Host}/reset-password?token={encodedToken}";

    try
    {
        await emailService.SendPasswordResetAsync(user.Email, link);
        await audit.PublishAsync(new AuthAuditEvent("ForgotPassword", user.UserID, user.Email, "Sent", DateTime.UtcNow));
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed sending reset email for {Email}", user.Email);
        await audit.PublishAsync(new AuthAuditEvent("ForgotPassword", user.UserID, user.Email, "SendFailed", DateTime.UtcNow));
        return Results.Json(new { ok = false, message = "Failed to send reset email. Please try again." }, statusCode: 500);
    }

    return Results.Ok(new { ok = true, message = "Reset email was sent successfully." });
});

// endpoint איפוס סיסמה בפועל לפי טוקן
app.MapPost("/api/auth/reset-password", async (ResetPasswordRequest req, AuthAuditDispatcher audit) =>
{
    // בדיקות בסיס לקלט
    if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
        return Results.BadRequest(new { ok = false, message = "Invalid reset request." });

    // בדיקת מדיניות סיסמה קיימת במערכת
    var (valid, passwordError) = ValidationHelper.ValidatePassword(req.NewPassword);
    if (!valid)
        return Results.BadRequest(new { ok = false, message = passwordError });

    // עדכון סיסמה לפי טוקן תקף
    var newHash = PasswordHelper.Hash(req.NewPassword.Trim());
    var userDb = new UserDB();
    var ok = await userDb.ResetPasswordByTokenAsync(req.Token.Trim(), newHash);

    if (!ok)
    {
        await audit.PublishAsync(new AuthAuditEvent("ResetPassword", null, "", "InvalidToken", DateTime.UtcNow));
        return Results.BadRequest(new { ok = false, message = "Invalid or expired reset link." });
    }

    await audit.PublishAsync(new AuthAuditEvent("ResetPassword", null, "", "Success", DateTime.UtcNow));
    return Results.Ok(new { ok = true });
});

app.MapGet("/api/health", () => Results.Ok(new { ok = true, utc = DateTime.UtcNow }));

app.MapGet("/api/admin/users", async (HttpContext http) =>
{
    var current = await GetCurrentUserAsync(http);
    if (current is null || current.Role != UserRole.Admin)
        return Results.Unauthorized();

    var userDb = new UserDB();
    var users = await userDb.GetAllUsersAsync();
    var payload = users.Select(u => new
    {
        userId = u.UserID,
        username = u.Username,
        fullName = u.FullName,
        email = u.Email,
        role = u.Role.ToString()
    });

    return Results.Ok(payload);
});

app.MapPost("/api/admin/users/{userId:int}/role", async (HttpContext http, int userId, RoleUpdateRequest req) =>
{
    var current = await GetCurrentUserAsync(http);
    if (current is null || current.Role != UserRole.Admin)
        return Results.Unauthorized();

    if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var parsedRole))
        return Results.BadRequest(new { ok = false, message = "Role must be User or Admin." });

    var userDb = new UserDB();
    var ok = await userDb.UpdateUserRoleAsync(userId, parsedRole);
    if (!ok)
        return Results.BadRequest(new { ok = false, message = "User role update failed." });

    return Results.Ok(new { ok = true });
});

app.MapPut("/api/admin/users/{userId:int}", async (HttpContext http, int userId, AdminUserUpdateRequest req) =>
{
    var current = await GetCurrentUserAsync(http);
    if (current is null || current.Role != UserRole.Admin)
        return Results.Unauthorized();

    if (userId <= 0)
        return Results.BadRequest(new { ok = false, message = "Invalid user id." });

    if (string.IsNullOrWhiteSpace(req.Email) || !ValidationHelper.IsValidEmail(req.Email.Trim()))
        return Results.BadRequest(new { ok = false, message = "Invalid email." });

    if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var parsedRole))
        return Results.BadRequest(new { ok = false, message = "Role must be User or Admin." });

    var userDb = new UserDB();
    var ok = await userDb.UpdateUserByAdminAsync(userId, req.Username ?? "", req.FullName ?? "", req.Email, parsedRole);
    if (!ok)
        return Results.BadRequest(new { ok = false, message = "User update failed." });

    return Results.Ok(new { ok = true });
});

app.MapDelete("/api/admin/users/{userId:int}", async (HttpContext http, int userId) =>
{
    var current = await GetCurrentUserAsync(http);
    if (current is null || current.Role != UserRole.Admin)
        return Results.Unauthorized();

    if (userId <= 0)
        return Results.BadRequest(new { ok = false, message = "Invalid user id." });

    if (current.UserID == userId)
        return Results.BadRequest(new { ok = false, message = "Admin cannot delete own account." });

    var userDb = new UserDB();
    var ok = await userDb.DeleteUserAsync(userId);
    if (!ok)
        return Results.BadRequest(new { ok = false, message = "Delete failed." });

    return Results.Ok(new { ok = true });
});

app.MapHub<GameHub>("/hubs/game");

// הפעלת האפליקציה
app.Run();

// DTO-ים לבקשות auth מהלקוח

// בניית קונפיג SMTP ממקורות שונים עם ברירות מחדל
static SmtpSettings BuildSmtpSettings(IConfiguration cfg)
{
    // תמיכה גם במפתח השגוי SMTP_POR למקרה שכבר הוגדר כך בסביבה
    var smtpPortRaw = Environment.GetEnvironmentVariable("SMTP_PORT")
        ?? Environment.GetEnvironmentVariable("SMTP_POR")
        ?? cfg["Smtp:Port"]
        ?? "465";

    var secureRaw = Environment.GetEnvironmentVariable("SMTP_SECURE")
        ?? cfg["Smtp:Secure"]
        ?? "true";

    _ = int.TryParse(smtpPortRaw, out var smtpPort);
    var smtpSecure = !bool.TryParse(secureRaw, out var parsedSecure) || parsedSecure;

    return new SmtpSettings
    {
        From = Environment.GetEnvironmentVariable("SMTP_FROM") ?? cfg["Smtp:From"] ?? "",
        Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? cfg["Smtp:Host"] ?? "",
        User = Environment.GetEnvironmentVariable("SMTP_USER") ?? cfg["Smtp:User"] ?? "",
        Pass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? cfg["Smtp:Pass"] ?? "",
        Port = smtpPort <= 0 ? 465 : smtpPort,
        Secure = smtpSecure
    };
}

// בדיקה האם הנתיב ציבורי ולא דורש session
static bool IsPublicPath(string path)
{
    if (path == "/")
        return true;

    return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/login", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/register", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/forgot-password", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/reset-password", StringComparison.OrdinalIgnoreCase);
}

// DTO-ים לבקשות auth מהלקוח
static async Task<User?> GetCurrentUserAsync(HttpContext http)
{
    if (!http.Request.Cookies.TryGetValue("session_token", out var token) || string.IsNullOrWhiteSpace(token))
        return null;

    var sessionDb = new SessionDB();
    var userId = await sessionDb.GetUserIdByTokenAsync(token);
    if (!userId.HasValue)
        return null;

    var userDb = new UserDB();
    return await userDb.GetByIdAsync(userId.Value);
}

internal sealed record LoginRequest(string Email, string Password);
internal sealed record ForgotPasswordRequest(string Email);
internal sealed record ResetPasswordRequest(string Token, string NewPassword);
internal sealed record RoleUpdateRequest(string Role);
internal sealed record AdminUserUpdateRequest(string Email, string Username, string FullName, string Role);
