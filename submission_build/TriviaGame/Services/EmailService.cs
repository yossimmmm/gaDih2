using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TriviaGame.Services;

public sealed class EmailService
{
    // הגדרות שרת המייל
    private readonly SmtpSettings _settings;
    // לוג פנימי למעקב תקלות/שליחות
    private readonly ILogger<EmailService> _logger;

    public EmailService(SmtpSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        // אם אין כתובת יעד לא שולחים כלום
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
        message.Subject = "Trivia Game - Password Reset";
        message.Body = new TextPart("plain")
        {
            Text = $"We received a password reset request for your account.\n\nOpen this link to set a new password:\n{resetLink}\n\nIf you did not request this, you can ignore this email."
        };

        using var client = new SmtpClient();
        client.Timeout = 15000;

        // פורט 465 דורש לרוב SSL מיידי; פורטים אחרים לרוב STARTTLS
        var secureMode = _settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : (_settings.Secure ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        await client.ConnectAsync(_settings.Host, _settings.Port, secureMode);
        await client.AuthenticateAsync(_settings.User, _settings.Pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
}
