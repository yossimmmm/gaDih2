using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TriviaGame.Api.Services;

// השירות הזה שולח מיילים דרך SMTP.
// כרגע השימוש העיקרי הוא עבור password reset.
public sealed class EmailService
{
    // הגדרות SMTP שנקראות מהקונפיגורציה.
    private readonly SmtpSettings settings;

    // לוגים של שליחה או כשל.
    private readonly ILogger<EmailService> logger;

    public EmailService(SmtpSettings settings, ILogger<EmailService> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    // שולח מייל איפוס סיסמה.
    // ה-link כבר נבנה לפני הקריאה, והמתודה רק שולחת אותו לכתובת המבוקשת.
    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

        // בניית מייל טקסטואלי פשוט.
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
        message.Subject = "Trivia Game - Password Reset";
        message.Body = new TextPart("plain")
        {
            Text = $"We received a password reset request.\n\nOpen this link:\n{resetLink}\n\nIf this wasn't you, ignore this email."
        };

        // SMTP client זמני שנפתח, שולח, ומתנתק.
        using var client = new SmtpClient();
        client.Timeout = 15000;

        // בוחרים מצב אבטחת חיבור לפי פורט והגדרות.
        var secureMode = settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : (settings.Secure ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        await client.ConnectAsync(settings.Host, settings.Port, secureMode);
        await client.AuthenticateAsync(settings.User, settings.Pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
}
