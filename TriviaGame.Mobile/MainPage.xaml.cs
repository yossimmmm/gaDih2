using Microsoft.Maui.Storage;
using System.Net.Http;

namespace TriviaGame.Mobile;

public partial class MainPage : ContentPage
{
    // מפתח שמירה לכתובת השרת בהעדפות המכשיר
    private const string BackendUrlPreferenceKey = "backend_url";

    // כתובת ברירת מחדל לשרת לפי פלטפורמה (אמולטור אנדרואיד מול מחשב)
    private static readonly string DefaultBackendUrl = DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5038"
        : "http://localhost:5038";

    public MainPage()
    {
        // אתחול רכיבי XAML
        InitializeComponent();
        // טעינת URL אחרון שנשמר
        LoadSavedUrl();
    }

    private void LoadSavedUrl()
    {
        // קריאת כתובת אחרונה מה-Preferences
        var url = Preferences.Default.Get(BackendUrlPreferenceKey, DefaultBackendUrl);
        // עדכון שדה קלט במסך
        BackendUrlEntry.Text = url;
        // טעינת האתר ב-WebView
        LoadWebView(url);
    }

    private async void OnLoadClicked(object? sender, EventArgs e)
    {
        // קריאת ערך מהקלט וניקוי רווחים
        var input = BackendUrlEntry.Text?.Trim() ?? string.Empty;
        // בדיקת תקינות כתובת
        if (!TryNormalizeHttpUrl(input, out var normalizedUrl))
        {
            await DisplayAlertAsync("Invalid URL", "Use a full URL like http://192.168.1.23:5038", "OK");
            return;
        }

        // שמירת הכתובת התקינה לשימוש עתידי
        Preferences.Default.Set(BackendUrlPreferenceKey, normalizedUrl);
        // טעינה ב-WebView
        LoadWebView(normalizedUrl);
    }

    private void LoadWebView(string url)
    {
        // הצבת URL כמקור ה-WebView
        GameWebView.Source = url;
    }

    private async void OnCheckHealthClicked(object? sender, EventArgs e)
    {
        // קריאת כתובת וניקוי קלט
        var input = BackendUrlEntry.Text?.Trim() ?? string.Empty;
        // בדיקת תקינות כתובת לפני בדיקת health
        if (!TryNormalizeHttpUrl(input, out var normalizedUrl))
        {
            HealthLabel.Text = "Health: invalid backend URL";
            return;
        }

        try
        {
            // יצירת HttpClient זמני עם timeout קצר
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            // קריאה ל-endpoint בריאות של השרת
            var res = await http.GetAsync($"{normalizedUrl}/api/health");
            // הצגת סטטוס הצלחה/שגיאה למשתמש
            HealthLabel.Text = res.IsSuccessStatusCode
                ? "Health: backend is reachable"
                : $"Health: backend error {(int)res.StatusCode}";
        }
        catch (Exception ex)
        {
            // הצגת סוג השגיאה במקרה כשל תקשורת
            HealthLabel.Text = $"Health: failed ({ex.GetType().Name})";
        }
    }

    private async void OnGameWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        // מריצים סקריפט רק לאחר ניווט מוצלח
        if (e.Result != WebNavigationResult.Success)
            return;

        // סקריפט שמוודא meta viewport מתאים לתצוגת מובייל
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
            // הזרקת סקריפט לדף הנטען ב-WebView
            await GameWebView.EvaluateJavaScriptAsync(mobileViewportScript);
        }
        catch
        {
            // במקרה כשל הזרקה מתעלמים כדי לא לשבור ניווט
        }
    }

    private static bool TryNormalizeHttpUrl(string input, out string normalizedUrl)
    {
        // ברירת מחדל ליציאה
        normalizedUrl = string.Empty;
        // קלט ריק אינו תקין
        if (string.IsNullOrWhiteSpace(input))
            return false;
        // ניסיון המרה ל-URI מלא
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return false;
        // מתירים רק HTTP/HTTPS
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        // נרמול סופי: מחרוזת URL ללא slash בסוף
        normalizedUrl = uri.ToString().TrimEnd('/');
        return true;
    }
}
