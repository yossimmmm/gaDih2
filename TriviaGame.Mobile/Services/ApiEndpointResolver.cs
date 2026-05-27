using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace TriviaGame.Mobile.Services;

public sealed class ApiEndpointResolver
{
    private const string EnvKey = "api_env";
    private const string OverrideUrlKey = "api_override_url";
    private const string DeviceUrlKey = "api_device_url";

    private readonly IConfiguration configuration;

    public ApiEndpointResolver(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    // החזרת בסיס URL לפי environment + פלטפורמה/אמולטור.
    public string GetBaseUrl()
    {
        var overrideUrl = Preferences.Default.Get(OverrideUrlKey, "");
        if (TryNormalizeHttpUrl(overrideUrl, out var normalizedOverride))
            return normalizedOverride;

        var env = Preferences.Default.Get(EnvKey, configuration["Api:Environment"] ?? "Development");
        var envPath = $"Api:{env}";

        // מחשב/desktop.
        var desktopUrl = configuration[$"{envPath}:DesktopBaseUrl"] ?? "http://localhost:5040";
        // אנדרואיד אמולטור (10.0.2.2).
        var emulatorUrl = configuration[$"{envPath}:AndroidEmulatorBaseUrl"] ?? "http://10.0.2.2:5040";
        // מכשיר פיזי - ניתן לשנות ב-Preferences.
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

    public string GetCurrentEnvironment() =>
        Preferences.Default.Get(EnvKey, configuration["Api:Environment"] ?? "Development");

    public void SetEnvironment(string environmentName)
    {
        if (!string.IsNullOrWhiteSpace(environmentName))
            Preferences.Default.Set(EnvKey, environmentName.Trim());
    }

    public string GetDeviceBaseUrl() =>
        Preferences.Default.Get(DeviceUrlKey, configuration["Api:Development:DeviceBaseUrl"] ?? "http://192.168.1.23:5040");

    public void SetDeviceBaseUrl(string value)
    {
        if (TryNormalizeHttpUrl(value, out var normalized))
            Preferences.Default.Set(DeviceUrlKey, normalized);
    }

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
