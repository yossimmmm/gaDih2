using Microsoft.Maui.Storage;
using System.Net.Http;

namespace TriviaGame.Mobile;

public partial class MainPage : ContentPage
{
    private const string BackendUrlPreferenceKey = "backend_url";

    private static readonly string DefaultBackendUrl = DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5038"
        : "http://localhost:5038";

    public MainPage()
    {
        InitializeComponent();
        LoadSavedUrl();
    }

    private void LoadSavedUrl()
    {
        var url = Preferences.Default.Get(BackendUrlPreferenceKey, DefaultBackendUrl);
        BackendUrlEntry.Text = url;
        LoadWebView(url);
    }

    private async void OnLoadClicked(object? sender, EventArgs e)
    {
        var input = BackendUrlEntry.Text?.Trim() ?? string.Empty;
        if (!TryNormalizeHttpUrl(input, out var normalizedUrl))
        {
            await DisplayAlertAsync("Invalid URL", "Use a full URL like http://192.168.1.23:5038", "OK");
            return;
        }

        Preferences.Default.Set(BackendUrlPreferenceKey, normalizedUrl);
        LoadWebView(normalizedUrl);
    }

    private void LoadWebView(string url)
    {
        GameWebView.Source = url;
    }

    private async void OnCheckHealthClicked(object? sender, EventArgs e)
    {
        var input = BackendUrlEntry.Text?.Trim() ?? string.Empty;
        if (!TryNormalizeHttpUrl(input, out var normalizedUrl))
        {
            HealthLabel.Text = "Health: invalid backend URL";
            return;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var res = await http.GetAsync($"{normalizedUrl}/api/health");
            HealthLabel.Text = res.IsSuccessStatusCode
                ? "Health: backend is reachable"
                : $"Health: backend error {(int)res.StatusCode}";
        }
        catch (Exception ex)
        {
            HealthLabel.Text = $"Health: failed ({ex.GetType().Name})";
        }
    }

    private async void OnGameWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success)
            return;

        const string mobileViewportScript = """
(() => {
  const content = "width=390, initial-scale=1, maximum-scale=1, viewport-fit=cover";
  let meta = document.querySelector('meta[name="viewport"]');
  if (!meta) {
    meta = document.createElement('meta');
    meta.name = 'viewport';
    document.head.appendChild(meta);
  }
  meta.setAttribute('content', content);
})();
""";

        try
        {
            await GameWebView.EvaluateJavaScriptAsync(mobileViewportScript);
        }
        catch
        {
        }
    }

    private static bool TryNormalizeHttpUrl(string input, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        normalizedUrl = uri.ToString().TrimEnd('/');
        return true;
    }
}
