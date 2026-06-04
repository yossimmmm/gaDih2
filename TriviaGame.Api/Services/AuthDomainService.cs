using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// השירות הזה מרכז את כל פעולות האותנטיקציה:
// login, register, forgot password, reset password.
// אין כאן session/cookie; ה-client שומר את המשתמש שקיבל מה-API.
public sealed class AuthDomainService
{
    // שירות שליחת המיילים לאיפוס סיסמה.
    private readonly EmailService emailService;

    // גישה לקונפיגורציה של SMTP ושאר ערכים.
    private readonly IConfiguration configuration;

    // לוגים לכשלים אמיתיים.
    private readonly ILogger<AuthDomainService> logger;

    public AuthDomainService(
        EmailService emailService,
        IConfiguration configuration,
        ILogger<AuthDomainService> logger)
    {
        this.emailService = emailService;
        this.configuration = configuration;
        this.logger = logger;
    }

    // Login לא יוצר session ולא token.
    // הוא רק בודק את הסיסמה ומחזיר ל-client את פרטי המשתמש שיש לשמור בזיכרון.
    public async Task<AuthResultResponse> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return new(false, "Email and password are required.", "", 0, "", "User");

        // מחפשים את המשתמש לפי email מנורמל.
        var userDb = new UserDB();
        var user = await userDb.GetByEmailAsync(req.Email.Trim().ToLowerInvariant());
        if (user is null || !PasswordHelper.Verify(req.Password, user.PasswordHash))
            return new(false, "Invalid email or password.", "", 0, "", "User");

        // אין token ואין session.
        // האפליקציה שומרת את המשתמש שקיבלה ומעבירה userId לכל בקשה שדורשת הקשר.
        return new(true, "Login succeeded.", "", user.UserID, user.Username, user.Role.ToString());
    }

    // יצירת משתמש חדש לפי אותם כללי תקינות של המערכת.
    public async Task<(bool Ok, string Message)> RegisterAsync(RegisterRequest req)
    {
        var (usernameValid, usernameError) = ValidationHelper.ValidateUsername(req.Username);
        if (!usernameValid)
            return (false, usernameError);

        var (fullNameValid, fullNameError) = ValidationHelper.ValidateFullName(req.FullName);
        if (!fullNameValid)
            return (false, fullNameError);

        if (!ValidationHelper.IsValidEmail(req.Email))
            return (false, "Please enter a valid email address.");

        var (passwordValid, passwordError) = ValidationHelper.ValidatePassword(req.Password);
        if (!passwordValid)
            return (false, passwordError);

        // מנרמלים את השדות לפני שמכניסים למסד.
        var username = ValidationHelper.SanitizeInput(req.Username, 50);
        var fullName = ValidationHelper.SanitizeInput(req.FullName, 100);
        var email = ValidationHelper.SanitizeInput(req.Email, 100).ToLowerInvariant().Trim();
        var password = req.Password.Trim();

        var userDb = new UserDB();
        if (await userDb.GetByEmailAsync(email) is not null)
            return (false, "Email already registered.");
        if (await userDb.GetByUsernameAsync(username) is not null)
            return (false, "Username already taken.");

        // הסיסמה נשמרת כ-hash בלבד.
        var created = await userDb.InsertUserAsync(new User
        {
            Username = username,
            FullName = fullName,
            Email = email,
            PasswordHash = PasswordHelper.Hash(password),
            Role = UserRole.User
        });

        return created is null
            ? (false, "Failed to create user.")
            : (true, "Registered successfully.");
    }

    // שחזור סיסמה דרך email.
    // זה עדיין משתמש ב-link מבוסס token כדי לשמור על ה-flow הקיים.
    public async Task<(bool Ok, string Message)> ForgotPasswordAsync(ForgotPasswordRequest req, string requestBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return (false, "Email is required.");

        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        if (!ValidationHelper.IsValidEmail(normalizedEmail))
            return (false, "Please enter a valid email address.");

        if (!IsSmtpConfigured())
            return (false, "SMTP is not configured correctly on the server.");

        var userDb = new UserDB();
        var user = await userDb.GetByEmailAsync(normalizedEmail);
        if (user is null)
            return (false, "No account found for this email.");

        try
        {
            // הטוקן נשמר במסד הנתונים, והקישור נבנה על בסיס כתובת השרת הנוכחית.
            var token = await userDb.CreatePasswordResetTokenAsync(user.UserID, TimeSpan.FromMinutes(30));
            var link = $"{requestBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
            await emailService.SendPasswordResetAsync(user.Email, link);
            return (true, "Reset email was sent successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send reset email for {Email}", normalizedEmail);
            return (false, "Failed to send reset email. Please try again.");
        }
    }

    // איפוס סיסמה דרך token.
    public async Task<(bool Ok, string Message)> ResetPasswordAsync(ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
            return (false, "Invalid reset request.");

        var (valid, passwordError) = ValidationHelper.ValidatePassword(req.NewPassword);
        if (!valid)
            return (false, passwordError);

        var userDb = new UserDB();
        var ok = await userDb.ResetPasswordByTokenAsync(req.Token.Trim(), PasswordHelper.Hash(req.NewPassword.Trim()));
        return ok
            ? (true, "Password reset succeeded.")
            : (false, "Invalid or expired reset link.");
    }

    // בודק אם כל פרטי ה-SMTP קיימים.
    // אם חסר ערך, המערכת לא תוכל לשלוח מייל.
    private bool IsSmtpConfigured()
    {
        var smtpFrom = Environment.GetEnvironmentVariable("SMTP_FROM") ?? configuration["Smtp:From"];
        var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? configuration["Smtp:Host"];
        var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? configuration["Smtp:User"];
        var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? configuration["Smtp:Pass"];

        return !string.IsNullOrWhiteSpace(smtpFrom)
               && !string.IsNullOrWhiteSpace(smtpHost)
               && !string.IsNullOrWhiteSpace(smtpUser)
               && !string.IsNullOrWhiteSpace(smtpPass);
    }
}
