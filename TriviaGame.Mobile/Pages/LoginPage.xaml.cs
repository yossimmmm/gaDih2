using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף ההתחברות של MAUI.
// מכאן מתחילה הזרימה: XAML מציג שדות וכפתורים, וה-code-behind קורא ל-TriviaApiClient.
public partial class LoginPage : ContentPage
{
    private readonly TriviaApiClient api;
    private readonly MobileSessionState session;

    public LoginPage()
    {
        // InitializeComponent טוען את LoginPage.xaml ויוצר את EmailEntry, PasswordEntry וכו'.
        InitializeComponent();

        // שולפים את שכבת ה-API ואת מצב הסשן מה-DI של MAUI.
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // בכל כניסה למסך מרעננים את הטקסט לפי המשתמש שנשמר בזיכרון.
        UpdateAuthLabels();
    }

    private async Task RunUiActionAsync(string actionName, Func<Task> action)
    {
        // לפני קריאת API מציגים loading כדי שיהיה ברור שהלחיצה נקלטה.
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        StatusLabel.Text = $"Status: {actionName}...";

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            // שגיאת רשת, API סגור או JSON לא תקין לא מפילים את האפליקציה.
            StatusLabel.Text = $"Status: {actionName} failed - {ex.Message}";
        }
        finally
        {
            // מכבים loading גם בהצלחה וגם בכישלון.
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("login", async () =>
        {
            // #login #auth #api-fetch
            // שולחים email + password ל-TriviaApiClient.
            // TriviaApiClient יעביר את זה ל-ApiClient, ומשם ל-controller של ה-API.
            var result = await api.LoginAsync(EmailEntry.Text ?? "", PasswordEntry.Text ?? "");
            if (!result.Success || result.Data is null || !result.Data.Ok)
            {
                StatusLabel.Text = $"Status: login failed - {result.Data?.Message ?? result.Message}";
                return;
            }

            // ה-API מחזיר מזהה משתמש, username ותפקיד.
            // MAUI שומר את זה ב-MobileSessionState כי אין לו cookie דפדפן כמו באתר.
            session.CurrentUser = new CurrentUserResponse
            {
                Authenticated = true,
                UserId = result.Data.UserId,
                Username = result.Data.Username,
                Email = EmailEntry.Text ?? "",
                Role = result.Data.Role
            };

            // אחרי login טוענים פרופיל מלא כדי לקבל גם full name ו-email מעודכנים.
            await RefreshUserFromApiAsync();
            StatusLabel.Text = "Status: login succeeded.";

            // מעבר למסך התפריט הראשי.
            await Shell.Current.GoToAsync("//menu");
        });
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("register", async () =>
        {
            // #register #auth #api-fetch
            // Register יוצר משתמש חדש, אבל לא חייב להתחבר אוטומטית.
            var result = await api.RegisterAsync(
                UsernameEntry.Text ?? "",
                FullNameEntry.Text ?? "",
                EmailEntry.Text ?? "",
                PasswordEntry.Text ?? "");

            StatusLabel.Text = result.Success
                ? $"Status: register succeeded - {result.Data?.Message}"
                : $"Status: register failed - {result.Message}";
        });
    }

    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("forgot password", async () =>
        {
            // #forgot-password #email #api-fetch
            // מבקשים מהשרת ליצור token ולא לשלוח את הסיסמה החדשה עדיין.
            var email = (ForgotEmailEntry.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email))
                email = (EmailEntry.Text ?? "").Trim();

            // #email-validation #forgot-password-validation #validation
            if (string.IsNullOrWhiteSpace(email))
            {
                PasswordRecoveryLabel.Text = "Password recovery: enter email first.";
                StatusLabel.Text = "Status: enter email first.";
                return;
            }

            var result = await api.ForgotPasswordAsync(email);
            var message = result.Data?.Message ?? result.Message;
            PasswordRecoveryLabel.Text = result.Success && result.Data?.Ok == true
                ? $"Password recovery: {message}"
                : $"Password recovery: failed - {message}";
        });
    }

    private async void OnResetPasswordClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("reset password", async () =>
        {
            // #reset-password #token #new-password #api-fetch
            // אפשר להדביק קישור מלא מהמייל או רק token.
            var token = ExtractResetToken(ResetTokenEntry.Text ?? "");
            var newPassword = NewPasswordEntry.Text ?? "";

            // #token-validation #password-validation #reset-password-validation #validation
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                PasswordRecoveryLabel.Text = "Password recovery: token and new password are required.";
                StatusLabel.Text = "Status: token and new password are required.";
                return;
            }

            var result = await api.ResetPasswordAsync(token, newPassword);
            var message = result.Data?.Message ?? result.Message;
            PasswordRecoveryLabel.Text = result.Success && result.Data?.Ok == true
                ? $"Password recovery: {message}"
                : $"Password recovery: reset failed - {message}";

            if (result.Success && result.Data?.Ok == true)
            {
                ResetTokenEntry.Text = "";
                NewPasswordEntry.Text = "";
                PasswordEntry.Text = "";
            }
        });
    }

    private async void OnLoadMeClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load me", RefreshUserFromApiAsync);
    }

    private async void OnUpdateProfileClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("update profile", async () =>
        {
            if (!session.IsLoggedIn || session.CurrentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            // #profile #update-profile #api-fetch
            // שולחים את הערכים מהשדות ל-API כדי לעדכן את המשתמש במסד.
            var result = await api.UpdateProfileAsync(
                session.CurrentUser.UserId,
                UsernameEntry.Text ?? "",
                FullNameEntry.Text ?? "",
                EmailEntry.Text ?? "");

            StatusLabel.Text = result.Success
                ? $"Status: profile updated - {result.Data?.Message}"
                : $"Status: profile update failed - {result.Message}";

            if (result.Success)
                await RefreshUserFromApiAsync();
        });
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("logout", async () =>
        {
            // #logout #auth
            // Logout ב-MAUI מנקה את הזיכרון המקומי.
            // אין כאן cookie למחוק ואין session token של דפדפן.
            session.Logout();
            UpdateAuthLabels();
            await Task.CompletedTask;
        });
    }

    private async Task RefreshUserFromApiAsync()
    {
        if (!session.IsLoggedIn || session.CurrentUser is null)
        {
            StatusLabel.Text = "Status: login first.";
            return;
        }

        // #me #profile #auth #api-fetch
        // מביאים את הפרופיל לפי UserId ששמור ב-session המקומי.
        var result = await api.GetMeAsync(session.CurrentUser.UserId);
        if (!result.Success || result.Data is null || !result.Data.Authenticated)
        {
            StatusLabel.Text = $"Status: load me failed - {result.Message}";
            return;
        }

        session.CurrentUser = result.Data;
        EmailEntry.Text = result.Data.Email;
        UsernameEntry.Text = result.Data.Username;
        FullNameEntry.Text = result.Data.FullName;
        UpdateAuthLabels();
    }

    private void UpdateAuthLabels()
    {
        var user = session.CurrentUser;
        AuthStateLabel.Text = session.IsLoggedIn && user is not null
            ? $"Auth: userId={user.UserId}, username={user.Username}, role={user.Role}"
            : "Auth: not loaded";
    }

    private static string ExtractResetToken(string input)
    {
        var trimmed = input.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed;

        // אם הודבק קישור מלא, מחלצים ממנו את query parameter בשם token.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(parts[0], "token", StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(parts[1]);
        }

        return "";
    }
}
