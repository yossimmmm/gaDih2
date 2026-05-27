namespace TriviaGame.Api.Services;

public sealed class SmtpSettings
{
    // כתובת שרת SMTP.
    public string Host { get; set; } = "";
    // פורט SMTP.
    public int Port { get; set; } = 465;
    // האם להשתמש ב-TLS/SSL.
    public bool Secure { get; set; } = true;
    // שם משתמש לחשבון SMTP.
    public string User { get; set; } = "";
    // סיסמת חשבון SMTP.
    public string Pass { get; set; } = "";
    // כתובת שולח.
    public string From { get; set; } = "";
}

public static class SmtpSettingsFactory
{
    // בניית אובייקט SMTP אחיד מה-Configuration/Environment.
    public static SmtpSettings Build(IConfiguration cfg)
    {
        var smtpPortRaw = Environment.GetEnvironmentVariable("SMTP_PORT")
            ?? Environment.GetEnvironmentVariable("SMTP_POR")
            ?? cfg["Smtp:Port"]
            ?? "465";

        var secureRaw = Environment.GetEnvironmentVariable("SMTP_SECURE")
            ?? cfg["Smtp:Secure"]
            ?? "true";

        _ = int.TryParse(smtpPortRaw, out var smtpPort);
        var smtpSecure = !bool.TryParse(secureRaw, out var parsedSecure) || parsedSecure;

        return new SmtpSettings
        {
            From = Environment.GetEnvironmentVariable("SMTP_FROM") ?? cfg["Smtp:From"] ?? "",
            Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? cfg["Smtp:Host"] ?? "",
            User = Environment.GetEnvironmentVariable("SMTP_USER") ?? cfg["Smtp:User"] ?? "",
            Pass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? cfg["Smtp:Pass"] ?? "",
            Port = smtpPort <= 0 ? 465 : smtpPort,
            Secure = smtpSecure
        };
    }
}
