using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TriviaGame.Services;

// KEYWORDS: email, send reset email, smtp, forgot password
// שירות ששולח מיילים דרך SMTP, בעיקר לאיפוס סיסמה.
public sealed class EmailService
{
    // הגדרות שרת ה-SMTP.
    private readonly SmtpSettings _settings;
    // לוג פנימי למעקב אחרי שליחות ותקלות.
    private readonly ILogger<EmailService> _logger;

    public EmailService(SmtpSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        // אם אין כתובת יעד, אין טעם לשלוח מייל.
        if (string.IsNullOrWhiteSpace(toEmail))
            return;

        // בונים את מבנה המייל עצמו.
        var message = new MimeMessage();
        // ה-From חייב להיות כתובת תקינה שהשרת מורשה לשלוח ממנה.
        message.From.Add(MailboxAddress.Parse(_settings.From));
        // ה-To הוא המשתמש שמקבל את קישור האיפוס.
        message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
        // Subject קצר וברור כדי שהמשתמש יבין מיד מה זה.
        message.Subject = "Trivia Game - Password Reset";
        // טקסט פשוט מספיק כאן; אין צורך ב-HTML מורכב למסך איפוס.
        message.Body = new TextPart("plain")
        {
            Text = $"We received a password reset request for your account.\n\nOpen this link to set a new password:\n{resetLink}\n\nIf you did not request this, you can ignore this email."
        };

        // SmtpClient הוא הלקוח שמדבר עם שרת המייל.
        using var client = new SmtpClient();
        // מונע מצב שבו שליחת המייל נתקעת לנצח.
        client.Timeout = 15000;

        // פורט 465 בדרך כלל משתמש ב-SSL ישיר; אחרת אפשר להשתמש ב-STARTTLS.
        var secureMode = _settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : (_settings.Secure ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        // מתחברים, מזדהים מול ה-SMTP, שולחים את המייל ומתנתקים.
        // אם אחד השלבים האלה נכשל, הקריאה זורקת חריגה למעלה.
        await client.ConnectAsync(_settings.Host, _settings.Port, secureMode);
        await client.AuthenticateAsync(_settings.User, _settings.Pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        // לוג זה עוזר להבין שהשליחה הושלמה בהצלחה.
        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
}
