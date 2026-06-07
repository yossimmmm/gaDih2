using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף הגדרות API.
// הוא קובע לאיזה שרת MAUI ישלח בקשות: localhost במחשב, כתובת LAN של מכשיר, או override ידני.
public partial class SettingsPage : ContentPage
{
    // ה-resolver מרכז את כל ההחלטות לגבי כתובת השרת.
    // הדף אינו משנה את ApiClient ישירות.
    private readonly ApiEndpointResolver endpointResolver;

    public SettingsPage()
    {
        // טוען את SettingsPage.xaml ויוצר Picker, Entries ו-Labels.
        InitializeComponent();

        // מקבל את אותו resolver שבו ApiClient משתמש לפני כל בקשה.
        endpointResolver = PageServiceLocator.Get<ApiEndpointResolver>();
    }

    protected override void OnAppearing()
    {
        // OnAppearing נקרא בכל כניסה למסך, כדי להציג את הערכים העדכניים מ-Preferences.
        base.OnAppearing();
        LoadSettingsToUi();
    }

    private void LoadSettingsToUi()
    {
        // #api-settings
        // קוראים את הערכים שנשמרו ב-Preferences ומציגים אותם למשתמש.
        // הסביבה נשמרת כמחרוזת, אבל Picker עובד עם SelectedIndex.
        var env = endpointResolver.GetCurrentEnvironment();

        // switch expression ממיר Development/Staging/Production למיקום המתאים ב-Picker.
        EnvironmentPicker.SelectedIndex = env switch
        {
            "Staging" => 1,
            "Production" => 2,
            _ => 0
        };

        // Device URL משמש מכשיר פיזי כדי להגיע למחשב דרך כתובת LAN.
        DeviceBaseUrlEntry.Text = endpointResolver.GetDeviceBaseUrl();

        // Override, אם קיים, מקבל עדיפות על כל environment אחר.
        OverrideBaseUrlEntry.Text = endpointResolver.GetOverrideBaseUrl();

        // אחרי טעינת הערכים מחשבים ומציגים את הכתובת שבה באמת ישתמש ApiClient.
        UpdateBaseUrlLabel();
    }

    private void OnApplyClicked(object? sender, EventArgs e)
    {
        // #api-settings #api-url
        // שומרים את ההגדרות המקומיות. הקריאות הבאות ל-API ישתמשו בכתובת החדשה.
        // SelectedItem יכול להיות null, לכן Development משמש fallback.
        endpointResolver.SetEnvironment(
            EnvironmentPicker.SelectedItem?.ToString() ?? "Development");

        // SetDeviceBaseUrl שומר רק URL חוקי מסוג HTTP/HTTPS.
        endpointResolver.SetDeviceBaseUrl(DeviceBaseUrlEntry.Text ?? "");

        // ערך override ריק מסיר את ה-override וחוזר לבחירה הרגילה של ה-resolver.
        endpointResolver.SetOverrideBaseUrl(OverrideBaseUrlEntry.Text ?? "");

        // מיד לאחר השמירה מציגים את ה-base URL החדש.
        UpdateBaseUrlLabel();
        StatusLabel.Text = "Status: API settings applied.";
    }

    private void UpdateBaseUrlLabel()
    {
        // מציג את הכתובת הסופית אחרי כל הלוגיקה של environment/device/override.
        // אותה GetBaseUrl נקראת גם מתוך ApiClient כשהוא בונה בקשת HTTP.
        BaseUrlLabel.Text = $"Base URL: {endpointResolver.GetBaseUrl()}";
    }
}
