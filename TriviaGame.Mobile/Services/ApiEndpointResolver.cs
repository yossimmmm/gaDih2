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
        this.configuration = configuration;
    }

    // מחזירה את כתובת ה־base הנוכחית.
    // קודם בודקים override ידני, ואז בוחרים לפי platform/device.
    public string GetBaseUrl()
    {
        var overrideUrl = Preferences.Default.Get(OverrideUrlKey, "");
        if (TryNormalizeHttpUrl(overrideUrl, out var normalizedOverride))
            return normalizedOverride;

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

        var selected = isAndroid
            ? (isVirtualDevice ? emulatorUrl : configuredDeviceUrl)
            : desktopUrl;

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
        if (!string.IsNullOrWhiteSpace(environmentName))
            Preferences.Default.Set(EnvKey, environmentName.Trim());
    }

    // מחזירה את כתובת ה־LAN השמורה עבור device אמיתי.
    public string GetDeviceBaseUrl() =>
        Preferences.Default.Get(DeviceUrlKey, configuration["Api:Development:DeviceBaseUrl"] ?? "http://192.168.1.23:5040");

    // שומרת כתובת device רק אם היא URL תקין.
    public void SetDeviceBaseUrl(string value)
    {
        if (TryNormalizeHttpUrl(value, out var normalized))
            Preferences.Default.Set(DeviceUrlKey, normalized);
    }

    // שומרת override זמני לכל ה־API.
    public void SetOverrideBaseUrl(string value)
    {
        if (TryNormalizeHttpUrl(value, out var normalized))
        {
            Preferences.Default.Set(OverrideUrlKey, normalized);
            return;
        }

        Preferences.Default.Remove(OverrideUrlKey);
    }

    public string GetOverrideBaseUrl() => Preferences.Default.Get(OverrideUrlKey, "");

    // קוד האפליקציה שנשלח בכל בקשה לשרת.
    // אם לא נשמר ערך מקומי, משתמשים בערך מהקונפיגורציה.
    public string GetAppCode() =>
        Preferences.Default.Get(AppCodeKey, configuration["Api:AppCode"] ?? "TRIVIA-DEV-123");

    public void SetAppCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        Preferences.Default.Set(AppCodeKey, value.Trim());
    }

    // מנרמלת URL כך שלא יישארו רווחים או slash מיותר בסוף.
    public static bool TryNormalizeHttpUrl(string? input, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri))
            return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        normalizedUrl = uri.ToString().TrimEnd('/');
        return true;
    }
}
