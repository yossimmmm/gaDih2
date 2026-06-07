using TriviaGame.Components;
using TriviaGame.Hubs;
using DBL;
using TriviaGame.Services;
using Models;

var builder = WebApplication.CreateBuilder(args);

// רישום שירותי UI אינטראקטיבי (Razor + SignalR).
// זה החלק שמרים את ה-web app עצמו, לפני כל endpoint או middleware.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// שירותי תשתית כלליים לאפליקציה:
// SignalR, HttpClient, גישה ל-HttpContext, ניהול session, AI, דוא"ל ו-logging.
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserSessionState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GeminiAdviceService>();
builder.Services.AddSingleton<AuthAuditDispatcher>();
// #room-cleanup #disconnect #heartbeat #last-seen
// שירות רקע שמנקה חדרים/שחקנים שנשארו בגלל ניתוק לא מסודר.
// בלי זה חדר יכול להישאר במסד עד שמישהו יפתח שוב את רשימת החדרים הציבוריים.
builder.Services.AddHostedService<RoomCleanupService>();
builder.Services.AddScoped(sp =>
{
    // בונים את הגדרות ה-SMTP מתוך הקונפיגורציה של הסביבה.
    var cfg = sp.GetRequiredService<IConfiguration>();
    return BuildSmtpSettings(cfg);
});
builder.Services.AddScoped<EmailService>();
var app = builder.Build();

// מאזין לאירועי audit של auth כדי לכתוב אותם ללוגים של השרת.
var authAudit = app.Services.GetRequiredService<AuthAuditDispatcher>();
var authAuditLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("AuthAudit");
authAudit.OnAuditAsync += async auditEvent =>
{
    authAuditLogger.LogInformation("AUTH_AUDIT action={Action} userId={UserId} email={Email} outcome={Outcome} at={AtUtc}",
        auditEvent.Action, auditEvent.UserId, auditEvent.Email, auditEvent.Outcome, auditEvent.OccurredAtUtc);
    await Task.CompletedTask;
};

// זריעת נתוני שאלות התחלתיים בזמן עליית האפליקציה.
// אם אין שאלות בסיסיות במסד, המשחק לא יכול להתחיל כראוי.
try
{
    await SeedData.EnsureSeedQuestionsAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to seed trivia questions.");
}

// קונפיגורציית middleware בהתאם לסביבה.
// כאן קובעים איך השרת מתנהג ב-Development מול Production.
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

// רשימת נתיבים שדורשים משתמש מחובר.
// כל מה שמופיע כאן הוא מסך פנימי ולא אמור להיפתח בלי session תקף.
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

    // דילוג על בדיקת התחברות עבור נתיבים ציבוריים.
    // דפים כמו login/register/reset-password צריכים להישאר פתוחים.
    if (IsPublicPath(path))
    {
        await next();
        return;
    }

    if (protectedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
    {
        // אם אין טוקן סשן - מעבירים ללוגין.
        // זה מגן על כל מסכי המשחק והניהול.
        // #cookie #session_token #login #auth
        if (!context.Request.Cookies.TryGetValue("session_token", out var token) || string.IsNullOrWhiteSpace(token))
        {
            context.Response.Redirect("/login");
            return;
        }

        // בדיקה שהטוקן אכן קיים במסד ושייך למשתמש תקף.
        // אם הטוקן לא קיים או פג תוקף, אנחנו מוחקים אותו ומחזירים ל-login.
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

// endpoint התחברות: אימות פרטי משתמש ויצירת סשן.
// זהו הצעד הראשון בזרימת auth: בדיקת אימייל/סיסמה, יצירת token, וכתיבת cookie.
app.MapPost("/api/auth/login", async (HttpContext http, LoginRequest req, AuthAuditDispatcher audit) =>
{
    // #login-validation #email-validation #password-validation #validation
    // קלט חסר = בקשה לא תקינה.
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { ok = false, message = "Email and password are required." });

    // מאתרים את המשתמש לפי אימייל ומשווים hash של הסיסמה.
    var userDb = new UserDB();
    var user = await userDb.GetByEmailAsync(req.Email.Trim().ToLowerInvariant());
    if (user is null || !PasswordHelper.Verify(req.Password, user.PasswordHash))
    {
        await audit.PublishAsync(new AuthAuditEvent("Login", null, req.Email.Trim(), "Failed", DateTime.UtcNow));
        return Results.Unauthorized();
    }

    // אם האימות הצליח, יוצרים session token חדש לתוקף קבוע.
    var sessionDb = new SessionDB();
    var token = await sessionDb.CreateSessionAsync(user.UserID, TimeSpan.FromDays(7));

    // כאן נכתבת העוגייה שהדפדפן ישלח אוטומטית בכל בקשה עתידית.
    // #cookie #session_token #login
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

// endpoint התנתקות: מחיקת סשן מהשרת ומהעוגיות.
// logout אמיתי הוא גם מחיקה של הטוקן ב-DB וגם ניקוי ה-cookie בדפדפן.
// #logout #cookie #session_token #sign-out
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

// endpoint זיהוי משתמש מחובר לפי session cookie.
// זה endpoint עזר ל-UI כדי לדעת מי מחובר בלי לבצע login מחדש.
// #cookie #session_token #auth-me #current-user
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

// endpoint שכחתי סיסמה: יצירת טוקן איפוס ושליחת מייל.
// כאן מתחיל flow של forgot password: token -> link -> email.
// #forgot-password #email #reset-token #reset-link
app.MapPost("/api/auth/forgot-password", async (HttpContext http, ForgotPasswordRequest req, EmailService emailService, AuthAuditDispatcher audit) =>
{
    // #email-validation #forgot-password-validation #validation
    // ולידציה בסיסית לקלט.
    // בלי אימייל אי אפשר לזהות את המשתמש או לשלוח קישור איפוס.
    if (string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new { ok = false, message = "Email is required." });

    // מנרמלים את האימייל כדי שהחיפוש במסד יהיה עקבי.
    var normalizedEmail = req.Email.Trim().ToLowerInvariant();
    // #email-validation #forgot-password-validation #validation
    if (!ValidationHelper.IsValidEmail(normalizedEmail))
        return Results.BadRequest(new { ok = false, message = "Please enter a valid email address." });

    // אם אין משתמש כזה, לא ממשיכים לשלב של שליחת מייל.
    var userDb = new UserDB();
    var user = await userDb.GetByEmailAsync(normalizedEmail);
    if (user is null)
    {
        await audit.PublishAsync(new AuthAuditEvent("ForgotPassword", null, normalizedEmail, "EmailNotFound", DateTime.UtcNow));
        return Results.BadRequest(new { ok = false, message = "No account found for this email." });
    }

    // בדיקת קונפיג SMTP לפני שליחה.
    // אם השרת לא מוגדר לשלוח דואר, עוצרים כאן במקום לייצר token סתם.
    var smtpFrom = Environment.GetEnvironmentVariable("SMTP_FROM") ?? builder.Configuration["Smtp:From"];
    var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? builder.Configuration["Smtp:Host"];
    var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? builder.Configuration["Smtp:User"];
    var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? builder.Configuration["Smtp:Pass"];

    // #smtp-validation #email-validation #forgot-password-validation #validation
    if (string.IsNullOrWhiteSpace(smtpFrom) ||
        string.IsNullOrWhiteSpace(smtpHost) ||
        string.IsNullOrWhiteSpace(smtpUser) ||
        string.IsNullOrWhiteSpace(smtpPass))
    {
        return Results.Json(new { ok = false, message = "SMTP is not configured correctly on the server." }, statusCode: 500);
    }

    // קודם יוצרים טוקן חד-פעמי בתוקף קצר ושומרים במסד רק את ה-hash שלו.
    // הטוקן הגולמי נשלח למייל, אבל במסד לא שומרים אותו כטקסט גלוי.
    var token = await userDb.CreatePasswordResetTokenAsync(user.UserID, TimeSpan.FromMinutes(30));
    // את הטוקן הגולמי מקודדים ל-URL כדי שלא ישבור את ה-link אם יש תווים מיוחדים.
    var encodedToken = Uri.EscapeDataString(token);
    // כאן נבנה קישור האיפוס המלא שנשלח במייל למשתמש.
    // ה-link מחזיר למסך reset-password עם הטוקן כ-query string.
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

// endpoint איפוס סיסמה בפועל לפי טוקן.
// זה השלב השני ב-flow: המשתמש חוזר מהקישור ומוסר token + סיסמה חדשה.
// #reset-password #token #email
app.MapPost("/api/auth/reset-password", async (ResetPasswordRequest req, AuthAuditDispatcher audit) =>
{
    // #token-validation #password-validation #reset-password-validation #validation
    // בדיקות בסיס לקלט.
    // אם אין token או סיסמה חדשה, אין בקשה תקינה.
    if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
        return Results.BadRequest(new { ok = false, message = "Invalid reset request." });

    // #password-validation #reset-password-validation #validation
    // בדיקת מדיניות סיסמה קיימת במערכת.
    // אנחנו רוצים לוודא שהסיסמה החדשה עומדת באותו סטנדרט של הרשמה.
    var (valid, passwordError) = ValidationHelper.ValidatePassword(req.NewPassword);
    if (!valid)
        return Results.BadRequest(new { ok = false, message = passwordError });

    // עדכון סיסמה לפי טוקן תקף.
    // אם הטוקן חוקי, ה-hash החדש נכנס למסד והטוקן מסומן כלא-שמיש.
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

// מסך admin של משתמשים: רק Admin יכול לקרוא רשימה מלאה של כל המשתמשים.
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

// עדכון role של משתמש דרך admin.
// זה endpoint נפרד כי שינוי role הוא פעולה רגישה שדורשת בדיקת הרשאות.
app.MapPost("/api/admin/users/{userId:int}/role", async (HttpContext http, int userId, RoleUpdateRequest req) =>
{
    var current = await GetCurrentUserAsync(http);
    if (current is null || current.Role != UserRole.Admin)
        return Results.Unauthorized();

    // #role-validation #admin-validation #validation
    if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var parsedRole))
        return Results.BadRequest(new { ok = false, message = "Role must be User or Admin." });

    var userDb = new UserDB();
    var ok = await userDb.UpdateUserRoleAsync(userId, parsedRole);
    if (!ok)
        return Results.BadRequest(new { ok = false, message = "User role update failed." });

    return Results.Ok(new { ok = true });
});

// עדכון נתוני משתמש מלאים דרך admin.
// כאן admin יכול לתקן username/fullName/email/role במקום edit עצמי.
app.MapPut("/api/admin/users/{userId:int}", async (HttpContext http, int userId, AdminUserUpdateRequest req) =>
{
    var current = await GetCurrentUserAsync(http);
    if (current is null || current.Role != UserRole.Admin)
        return Results.Unauthorized();

    // #user-id-validation #admin-validation #validation
    if (userId <= 0)
        return Results.BadRequest(new { ok = false, message = "Invalid user id." });

    // #email-validation #admin-validation #validation
    if (string.IsNullOrWhiteSpace(req.Email) || !ValidationHelper.IsValidEmail(req.Email.Trim()))
        return Results.BadRequest(new { ok = false, message = "Invalid email." });

    // #role-validation #admin-validation #validation
    if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var parsedRole))
        return Results.BadRequest(new { ok = false, message = "Role must be User or Admin." });

    var userDb = new UserDB();
    var ok = await userDb.UpdateUserByAdminAsync(userId, req.Username ?? "", req.FullName ?? "", req.Email, parsedRole);
    if (!ok)
        return Results.BadRequest(new { ok = false, message = "User update failed." });

    return Results.Ok(new { ok = true });
});

// מחיקת משתמש דרך admin.
// גם כאן בודקים שלא ימחק את עצמו בטעות.
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

// הפעלת האפליקציה.
// מהרגע הזה השרת מתחיל להאזין לבקשות HTTP, ל-views, ול-hub של המשחק.
app.Run();

// DTO-ים לבקשות auth מהלקוח.
// אלו record-ים קטנים שמייצגים את גוף הבקשה שנכנס ל-endpoints שלמעלה.

// בניית קונפיג SMTP ממקורות שונים עם ברירות מחדל.
// פה מאחדים env vars, appsettings, וערכי ברירת מחדל ל-SMTP אחד.
static SmtpSettings BuildSmtpSettings(IConfiguration cfg)
{
    // תמיכה גם במפתח השגוי SMTP_POR למקרה שכבר הוגדר כך בסביבה.
    // זה נותן backward compatibility בלי לשבור deployים ישנים.
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

// בדיקה האם הנתיב ציבורי ולא דורש session.
// פונקציה זו קובעת אילו paths עוקפים את middleware של auth.
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

// החזרת המשתמש המחובר מתוך ה-cookie של הבקשה הנוכחית.
// helper משותף לכל מסכי admin/auth שצריכים לדעת מי הבעלים של ה-request.
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
