namespace TriviaGame.Api.Services;

// מחלקת הגדרות SMTP פשוטה.
// השדות האלה מוזנים מ-appsettings או ממשתני סביבה.
public sealed class SmtpSettings
{
    // כתובת שרת ה-SMTP.
    public string Host { get; set; } = "";

    // הפורט שבו מתחברים לשרת.
    public int Port { get; set; } = 465;

    // האם להשתמש ב-TLS/SSL.
    public bool Secure { get; set; } = true;

    // שם משתמש לשרת SMTP.
    public string User { get; set; } = "";

    // סיסמה לשרת SMTP.
    public string Pass { get; set; } = "";

    // כתובת שולח המייל.
    public string From { get; set; } = "";
}

public static class SmtpSettingsFactory
{
    // בונה אובייקט הגדרות אחד מתוך environment + configuration.
    public static SmtpSettings Build(IConfiguration cfg)
    {
        // פורט SMTP יכול להגיע ממשתנה סביבה או מ-appsettings.
        // SMTP_POR נשאר כאן כדי לתמוך בטעות כתיב קיימת אם הוגדרה בסביבה.
        var smtpPortRaw = Environment.GetEnvironmentVariable("SMTP_PORT")
            ?? Environment.GetEnvironmentVariable("SMTP_POR")
            ?? cfg["Smtp:Port"]
            ?? "465";

        // Secure קובע אם נשתמש בחיבור מאובטח.
        var secureRaw = Environment.GetEnvironmentVariable("SMTP_SECURE")
            ?? cfg["Smtp:Secure"]
            ?? "true";

        // אם הפורט לא מספר תקין, נשתמש בהמשך בברירת מחדל 465.
        _ = int.TryParse(smtpPortRaw, out var smtpPort);

        // אם secure לא הוגדר בצורה תקינה, ברירת המחדל היא true.
        var smtpSecure = !bool.TryParse(secureRaw, out var parsedSecure) || parsedSecure;

        // בונים אובייקט הגדרות אחד שמוזרק ל-EmailService.
        return new SmtpSettings
        {
            // משתני סביבה מקבלים עדיפות על appsettings כדי לא לשמור סודות בקוד.
            From = Environment.GetEnvironmentVariable("SMTP_FROM") ?? cfg["Smtp:From"] ?? "",
            Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? cfg["Smtp:Host"] ?? "",
            User = Environment.GetEnvironmentVariable("SMTP_USER") ?? cfg["Smtp:User"] ?? "",
            Pass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? cfg["Smtp:Pass"] ?? "",
            Port = smtpPort <= 0 ? 465 : smtpPort,
            Secure = smtpSecure
        };
    }
}
