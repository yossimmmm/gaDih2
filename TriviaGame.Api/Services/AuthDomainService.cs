using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

public sealed class AuthDomainService
{
    private readonly EmailService emailService;
    private readonly IConfiguration configuration;
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

    // התחברות + יצירת session token שמוחזר למובייל.
    public async Task<AuthResultResponse> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return new(false, "Email and password are required.", "", 0, "", "User");

        var userDb = new UserDB();
        var user = await userDb.GetByEmailAsync(req.Email.Trim().ToLowerInvariant());
        if (user is null || !PasswordHelper.Verify(req.Password, user.PasswordHash))
            return new(false, "Invalid email or password.", "", 0, "", "User");

        var sessionDb = new SessionDB();
        var token = await sessionDb.CreateSessionAsync(user.UserID, TimeSpan.FromDays(7));
        return new(true, "Login succeeded.", token, user.UserID, user.Username, user.Role.ToString());
    }

    // רישום משתמש חדש עם אותן בדיקות כמו מסך register ב-web.
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

        var username = ValidationHelper.SanitizeInput(req.Username, 50);
        var fullName = ValidationHelper.SanitizeInput(req.FullName, 100);
        var email = ValidationHelper.SanitizeInput(req.Email, 100).ToLowerInvariant().Trim();
        var password = req.Password.Trim();

        var userDb = new UserDB();
        if (await userDb.GetByEmailAsync(email) is not null)
            return (false, "Email already registered.");
        if (await userDb.GetByUsernameAsync(username) is not null)
            return (false, "Username already taken.");

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

    // סיום session לפי token נוכחי.
    public async Task LogoutAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        var sessionDb = new SessionDB();
        await sessionDb.DeleteSessionAsync(token);
    }

    // תהליך forgot-password כולל יצירת token ושליחת מייל.
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
            // מייצרים token חד-פעמי ומרכיבים קישור מלא למסך reset.
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

    // איפוס סיסמה לפי token.
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

    // בדיקה שהגדרות SMTP מינימליות קיימות.
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
