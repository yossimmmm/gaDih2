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
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

        // מייל טקסט פשוט כדי שיהיה קריא בכל לקוח.
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
        message.Subject = "Trivia Game - Password Reset";
        message.Body = new TextPart("plain")
        {
            Text = $"We received a password reset request.\n\nOpen this link:\n{resetLink}\n\nIf this wasn't you, ignore this email."
        };

        using var client = new SmtpClient();
        client.Timeout = 15000;

        // בוחרים מצב אבטחה לפי הפורט.
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
