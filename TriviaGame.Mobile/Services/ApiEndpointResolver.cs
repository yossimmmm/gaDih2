using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace TriviaGame.Mobile.Services;

// המחלקה הזאת מחליטה לאיזה API ה־MAUI ידבר.
// היא יודעת לבחור בין localhost, emulator, device LAN, או override ידני.
public sealed class ApiEndpointResolver
{
    private const string EnvKey = "api_env";
    private const string OverrideUrlKey = "api_override_url";
    private const string DeviceUrlKey = "api_device_url";
    private const string AppCodeKey = "api_app_code";

    private readonly IConfiguration configuration;

    public ApiEndpointResolver(IConfiguration configuration)
    {
        // configuration נטען מ-appsettings של הלקוח.
        this.configuration = configuration;
    }

    // מחזירה את כתובת ה־base הנוכחית.
    // קודם בודקים override ידני, ואז בוחרים לפי platform/device.
    public string GetBaseUrl()
    {
        // override ידני מקבל עדיפות על כל דבר אחר.
        // זה שימושי כשבודקים מול שרת אחר בלי לשנות appsettings.
        var overrideUrl = Preferences.Default.Get(OverrideUrlKey, "");
        if (TryNormalizeHttpUrl(overrideUrl, out var normalizedOverride))
            return normalizedOverride;

        // הסביבה קובעת מאיזה בלוק בקונפיגורציה לקרוא כתובות.
        var env = Preferences.Default.Get(EnvKey, configuration["Api:Environment"] ?? "Development");
        var envPath = $"Api:{env}";

        // על desktop בדרך כלל משתמשים ב־localhost.
        var desktopUrl = configuration[$"{envPath}:DesktopBaseUrl"] ?? "http://localhost:5040";

        // ב־Android emulator צריך להשתמש ב־10.0.2.2 כדי להגיע למחשב המארח.
        var emulatorUrl = configuration[$"{envPath}:AndroidEmulatorBaseUrl"] ?? "http://10.0.2.2:5040";

        // במכשיר אמיתי משתמשים בכתובת LAN של המחשב המקומי.
        var configuredDeviceUrl = Preferences.Default.Get(
            DeviceUrlKey,
            configuration[$"{envPath}:DeviceBaseUrl"] ?? "http://192.168.1.23:5040");

        var isAndroid = DeviceInfo.Platform == DevicePlatform.Android;
        var isVirtualDevice = DeviceInfo.DeviceType == DeviceType.Virtual;

        // Android emulator לא יכול להשתמש ב-localhost של המחשב.
        // לכן במכשיר וירטואלי משתמשים ב-10.0.2.2, ובמכשיר אמיתי בכתובת LAN.
        var selected = isAndroid
            ? (isVirtualDevice ? emulatorUrl : configuredDeviceUrl)
            : desktopUrl;

        // אם הכתובת שנבחרה לא תקינה, חוזרים ל-localhost כברירת מחדל בטוחה לפיתוח.
        return TryNormalizeHttpUrl(selected, out var normalizedSelected)
            ? normalizedSelected
            : "http://localhost:5040";
    }

    // מחזירה את שם הסביבה שנבחרה כרגע.
    public string GetCurrentEnvironment() =>
        Preferences.Default.Get(EnvKey, configuration["Api:Environment"] ?? "Development");

    // שומרת את הסביבה שנבחרה מקומית.
    public void SetEnvironment(string environmentName)
    {
        // לא שומרים ערך ריק כדי לא לשבור את בחירת הסביבה.
        if (!string.IsNullOrWhiteSpace(environmentName))
            Preferences.Default.Set(EnvKey, environmentName.Trim());
    }

    // מחזירה את כתובת ה־LAN השמורה עבור device אמיתי.
    public string GetDeviceBaseUrl() =>
        Preferences.Default.Get(DeviceUrlKey, configuration["Api:Development:DeviceBaseUrl"] ?? "http://192.168.1.23:5040");

    // שומרת כתובת device רק אם היא URL תקין.
    public void SetDeviceBaseUrl(string value)
    {
        // שומרים רק URL חוקי כדי שלא נתקע את כל קריאות ה-API.
        if (TryNormalizeHttpUrl(value, out var normalized))
            Preferences.Default.Set(DeviceUrlKey, normalized);
    }

    // שומרת override זמני לכל ה־API.
    public void SetOverrideBaseUrl(string value)
    {
        // אם המשתמש הזין URL תקין, הוא הופך להיות היעד לכל הקריאות.
        if (TryNormalizeHttpUrl(value, out var normalized))
        {
            Preferences.Default.Set(OverrideUrlKey, normalized);
            return;
        }

        // אם הערך לא תקין או ריק, מסירים override וחוזרים לבחירה לפי סביבה/מכשיר.
        Preferences.Default.Remove(OverrideUrlKey);
    }

    // מחזיר את ה-override הנוכחי כדי להציג אותו במסך ההגדרות.
    public string GetOverrideBaseUrl() => Preferences.Default.Get(OverrideUrlKey, "");

    // קוד האפליקציה שנשלח בכל בקשה לשרת.
    // אם לא נשמר ערך מקומי, משתמשים בערך מהקונפיגורציה.
    public string GetAppCode() =>
        Preferences.Default.Get(AppCodeKey, configuration["Api:AppCode"] ?? "TRIVIA-DEV-123");

    public void SetAppCode(string value)
    {
        // app code ריק לא נשמר כדי לא ליצור בקשות שייכשלו ב-401.
        if (string.IsNullOrWhiteSpace(value))
            return;

        Preferences.Default.Set(AppCodeKey, value.Trim());
    }

    // מנרמלת URL כך שלא יישארו רווחים או slash מיותר בסוף.
    public static bool TryNormalizeHttpUrl(string? input, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;

        // URL ריק אינו תקין.
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // חייב להיות URL מלא כמו http://localhost:5040 ולא רק localhost.
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri))
            return false;

        // האפליקציה תומכת רק ב-HTTP/HTTPS.
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        // מסירים slash סופי כדי שחיבור path לא ייצור כפילויות.
        normalizedUrl = uri.ToString().TrimEnd('/');
        return true;
    }
}
