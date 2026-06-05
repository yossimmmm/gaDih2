using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TriviaGame.Api.Services;

// שירות ששולח מיילי איפוס סיסמה.
public sealed class EmailService
{
    // הגדרות SMTP שמגיעות מהקונפיגורציה.
    private readonly SmtpSettings settings;

    // לוג להצלחה או לכישלון בשליחת המייל.
    private readonly ILogger<EmailService> logger;

    public EmailService(SmtpSettings settings, ILogger<EmailService> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    // בונה ושולח מייל איפוס סיסמה.
    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        // אם אין כתובת יעד, אין מה לשלוח ולא זורקים שגיאה.
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

        // מייל טקסט פשוט כדי שיהיה קריא בכל לקוח.
        var message = new MimeMessage();

        // From מגיע מהגדרות SMTP.
        message.From.Add(MailboxAddress.Parse(settings.From));

        // To הוא האימייל של המשתמש שביקש איפוס.
        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
        message.Subject = "Trivia Game - Password Reset";

        // גוף ההודעה כולל את קישור האיפוס שהשרת בנה.
        message.Body = new TextPart("plain")
        {
            Text = $"We received a password reset request.\n\nOpen this link:\n{resetLink}\n\nIf this wasn't you, ignore this email."
        };

        using var client = new SmtpClient();

        // timeout מונע מצב שבו שליחת מייל תיתקע לזמן ארוך מדי.
        client.Timeout = 15000;

        // בוחרים מצב אבטחה לפי הפורט.
        var secureMode = settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : (settings.Secure ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        // מתחברים לשרת SMTP.
        await client.ConnectAsync(settings.Host, settings.Port, secureMode);

        // מזדהים מול שרת המייל.
        await client.AuthenticateAsync(settings.User, settings.Pass);

        // שולחים את ההודעה בפועל.
        await client.SendAsync(message);

        // סוגרים את החיבור בצורה מסודרת.
        await client.DisconnectAsync(true);

        logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
}
