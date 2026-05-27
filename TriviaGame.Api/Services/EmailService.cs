using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TriviaGame.Api.Services;

public sealed class EmailService
{
    // קונפיגורציית SMTP.
    private readonly SmtpSettings settings;
    // לוג תקלות/שליחה.
    private readonly ILogger<EmailService> logger;

    public EmailService(SmtpSettings settings, ILogger<EmailService> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    // שולח מייל איפוס סיסמה עם קישור חד-פעמי.
    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

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
