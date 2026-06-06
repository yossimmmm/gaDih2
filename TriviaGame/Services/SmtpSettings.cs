namespace TriviaGame.Services;

// הגדרות SMTP שמוזנות מקובצי config או ממשתני סביבה.
public sealed class SmtpSettings
{
    // כתובת שרת ה-SMTP.
    public string Host { get; set; } = "";
    // הפורט לשליחת מיילים.
    public int Port { get; set; } = 465;
    // האם להשתמש בהצפנה.
    public bool Secure { get; set; } = true;
    // שם משתמש להתחברות ל-SMTP.
    public string User { get; set; } = "";
    // סיסמה להתחברות ל-SMTP.
    public string Pass { get; set; } = "";
    // כתובת השולח שמופיעה במיילים היוצאים.
    public string From { get; set; } = "";
}
