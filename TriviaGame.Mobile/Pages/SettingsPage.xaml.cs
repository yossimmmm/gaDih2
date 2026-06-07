using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף הגדרות API.
// הוא קובע לאיזה שרת MAUI ישלח בקשות: localhost במחשב, כתובת LAN של מכשיר, או override ידני.
public partial class SettingsPage : ContentPage
{
    private readonly ApiEndpointResolver endpointResolver;

    public SettingsPage()
    {
        InitializeComponent();
        endpointResolver = PageServiceLocator.Get<ApiEndpointResolver>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSettingsToUi();
    }

    private void LoadSettingsToUi()
    {
        // #api-settings
        // קוראים את הערכים שנשמרו ב-Preferences ומציגים אותם למשתמש.
        var env = endpointResolver.GetCurrentEnvironment();
        EnvironmentPicker.SelectedIndex = env switch
        {
            "Staging" => 1,
            "Production" => 2,
            _ => 0
        };

        DeviceBaseUrlEntry.Text = endpointResolver.GetDeviceBaseUrl();
        OverrideBaseUrlEntry.Text = endpointResolver.GetOverrideBaseUrl();
        UpdateBaseUrlLabel();
    }

    private void OnApplyClicked(object? sender, EventArgs e)
    {
        // #api-settings #api-url
        // שומרים את ההגדרות המקומיות. הקריאות הבאות ל-API ישתמשו בכתובת החדשה.
        endpointResolver.SetEnvironment(EnvironmentPicker.SelectedItem?.ToString() ?? "Development");
        endpointResolver.SetDeviceBaseUrl(DeviceBaseUrlEntry.Text ?? "");
        endpointResolver.SetOverrideBaseUrl(OverrideBaseUrlEntry.Text ?? "");
        UpdateBaseUrlLabel();
        StatusLabel.Text = "Status: API settings applied.";
    }

    private void UpdateBaseUrlLabel()
    {
        // מציג את הכתובת הסופית אחרי כל הלוגיקה של environment/device/override.
        BaseUrlLabel.Text = $"Base URL: {endpointResolver.GetBaseUrl()}";
    }
}
