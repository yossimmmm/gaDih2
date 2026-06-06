namespace TriviaGame.Services;

public sealed class SmtpSettings
{
    // כתובת שרת ה-SMTP
    public string Host { get; set; } = "";
    // פורט לשליחת מיילים
    public int Port { get; set; } = 465;
    // האם להשתמש ב-SSL/TLS
    public bool Secure { get; set; } = true;
    // שם משתמש להתחברות ל-SMTP
    public string User { get; set; } = "";
    // סיסמה להתחברות ל-SMTP
    public string Pass { get; set; } = "";
    // כתובת השולח שמופיעה לנמען
    public string From { get; set; } = "";
}
