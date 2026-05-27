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

        // בניית אובייקט מייל לשחזור סיסמה
        var message = new MimeMessage();
        // כתובת השולח כפי שמוגדרת ב-SMTP_FROM.
        message.From.Add(MailboxAddress.Parse(_settings.From));
        // כתובת הנמען (האימייל שהמשתמש הזין במסך forgot password).
        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
        // נושא המייל שמופיע בתיבת הדואר.
        message.Subject = "Trivia Game - Password Reset";
        // גוף המייל בפורמט טקסט פשוט עם קישור איפוס אישי.
        message.Body = new TextPart("plain")
        {
            // הקישור מכיל token חד-פעמי לאיפוס הסיסמה.
            Text = $"We received a password reset request for your account.\n\nOpen this link to set a new password:\n{resetLink}\n\nIf you did not request this, you can ignore this email."
        };

        using var client = new SmtpClient();
        client.Timeout = 15000;

        // פורט 465 דורש לרוב SSL מיידי; פורטים אחרים לרוב STARTTLS
        var secureMode = _settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : (_settings.Secure ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        // פתיחת חיבור SMTP לפי ההגדרות
        await client.ConnectAsync(_settings.Host, _settings.Port, secureMode);
        // אימות משתמש SMTP
        await client.AuthenticateAsync(_settings.User, _settings.Pass);
        // שליחת המייל בפועל
        await client.SendAsync(message);
        // ניתוק מסודר מהשרת
        await client.DisconnectAsync(true);
        // רישום הצלחה בלוג
        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
}
