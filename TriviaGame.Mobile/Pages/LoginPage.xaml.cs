using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף ההתחברות של MAUI.
// מכאן מתחילה הזרימה: XAML מציג שדות וכפתורים, וה-code-behind קורא ל-TriviaApiClient.
public partial class LoginPage : ContentPage
{
    // reference לשירות שמכיל את כל פעולות ה-API שהדף רשאי לבצע.
    // readonly אומר שאפשר להציב אותו ב-constructor, אבל אי אפשר להחליף אותו אחר כך.
    private readonly TriviaApiClient api;

    // reference לאובייקט המשותף לכל מסכי MAUI.
    // בגלל שהוא Singleton, LoginPage ו-RoomsPage מקבלים את אותו אובייקט בדיוק.
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
        // OnAppearing מופעל בכל פעם שהדף נהיה גלוי, לא רק בפעם הראשונה שהוא נוצר.
        // לכן הוא מתאים לרענון UI אחרי שחוזרים למסך מדף אחר.
        base.OnAppearing();

        // בכל כניסה למסך מרעננים את הטקסט לפי המשתמש שנשמר בזיכרון.
        UpdateAuthLabels();
    }

    // Func<Task> action הוא פרמטר שמכיל פונקציה אסינכרונית אחרת.
    // כך כל כפתור מעביר לכאן את הפעולה שלו, והקוד של loading/try/catch נכתב פעם אחת בלבד.
    private async Task RunUiActionAsync(string actionName, Func<Task> action)
    {
        // לפני קריאת API מציגים loading כדי שיהיה ברור שהלחיצה נקלטה.
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        StatusLabel.Text = $"Status: {actionName}...";

        try
        {
            // await מחכה לפעולה בלי לחסום את thread של ה-UI.
            // בזמן ההמתנה האפליקציה עדיין יכולה לצייר את המסך ולהגיב למערכת.
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
        // event handler של MAUI חייב להחזיר void לפי החתימה של Clicked.
        // מוסיפים async כדי שיהיה אפשר להשתמש ב-await בתוך האירוע.
        await RunUiActionAsync("login", async () =>
        {
            // #login #auth #api-fetch
            // שולחים email + password ל-TriviaApiClient.
            // TriviaApiClient יעביר את זה ל-ApiClient, ומשם ל-controller של ה-API.
            // Text יכול להיות null אם המשתמש לא כתב דבר.
            // האופרטור ?? מחליף null במחרוזת ריקה כדי לא לשלוח null לשירות.
            var result = await api.LoginAsync(
                EmailEntry.Text ?? "",
                PasswordEntry.Text ?? "");

            // Success בודק אם קריאת HTTP הצליחה.
            // Data בודק אם הצלחנו לפרק את גוף ה-JSON.
            // Data.Ok הוא הערך העסקי שה-controller החזיר.
            if (!result.Success || result.Data is null || !result.Data.Ok)
            {
                StatusLabel.Text = $"Status: login failed - {result.Data?.Message ?? result.Message}";
                return;
            }

            // ה-API מחזיר מזהה משתמש, username ותפקיד.
            // MAUI שומר את זה ב-MobileSessionState כי אין לו cookie דפדפן כמו באתר.
            session.CurrentUser = new CurrentUserResponse
            {
                // Authenticated הוא סימון מקומי שה-login הצליח.
                Authenticated = true,

                // שלושת הערכים הבאים הגיעו מגוף תשובת ה-login של ה-API.
                UserId = result.Data.UserId,
                Username = result.Data.Username,

                // תשובת login לא מכילה בהכרח את כל הפרופיל, לכן email נלקח כרגע מהשדה.
                Email = EmailEntry.Text ?? "",
                Role = result.Data.Role
            };

            // אחרי login טוענים פרופיל מלא כדי לקבל גם full name ו-email מעודכנים.
            await RefreshUserFromApiAsync();
            StatusLabel.Text = "Status: login succeeded.";

            // מעבר למסך התפריט הראשי.
            // // בתחילת route אומר ל-Shell לבצע ניווט מוחלט למסך menu,
            // ולא להוסיף עוד דף מעל מסלול ניווט ישן.
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
                // סדר הפרמטרים חייב להתאים לחתימה של RegisterAsync.
                UsernameEntry.Text ?? "",
                FullNameEntry.Text ?? "",
                EmailEntry.Text ?? "",
                PasswordEntry.Text ?? "");

            // האופרטור התנאי ?: בוחר הודעה אחת להצלחה והודעה אחרת לכישלון.
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
            // Trim מסיר רווחים בתחילת ובסוף האימייל.
            var email = (ForgotEmailEntry.Text ?? "").Trim();

            // אם שדה שחזור הסיסמה ריק, משתמשים באימייל שכבר הוזן באזור login.
            if (string.IsNullOrWhiteSpace(email))
                email = (EmailEntry.Text ?? "").Trim();

            // #email-validation #forgot-password-validation #validation
            if (string.IsNullOrWhiteSpace(email))
            {
                PasswordRecoveryLabel.Text = "Password recovery: enter email first.";
                StatusLabel.Text = "Status: enter email first.";
                return;
            }

            // השירות שולח POST ל-/api/auth/forgot-password.
            var result = await api.ForgotPasswordAsync(email);

            // לפעמים הודעת השרת נמצאת בתוך Data ולפעמים במעטפת ApiResult.
            // ?? בוחר את הראשונה שאינה null.
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
            // ExtractResetToken תומכת גם ב-token בלבד וגם בקישור מלא מהמייל.
            var token = ExtractResetToken(ResetTokenEntry.Text ?? "");
            var newPassword = NewPasswordEntry.Text ?? "";

            // #token-validation #password-validation #reset-password-validation #validation
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                PasswordRecoveryLabel.Text = "Password recovery: token and new password are required.";
                StatusLabel.Text = "Status: token and new password are required.";
                return;
            }

            // כאן הסיסמה החדשה נשלחת לשרת. השרת אחראי לבצע validation ו-hash.
            var result = await api.ResetPasswordAsync(token, newPassword);
            var message = result.Data?.Message ?? result.Message;
            PasswordRecoveryLabel.Text = result.Success && result.Data?.Ok == true
                ? $"Password recovery: {message}"
                : $"Password recovery: reset failed - {message}";

            if (result.Success && result.Data?.Ok == true)
            {
                // אחרי הצלחה מנקים מידע רגיש מהפקדים שעל המסך.
                ResetTokenEntry.Text = "";
                NewPasswordEntry.Text = "";
                PasswordEntry.Text = "";
            }
        });
    }

    private async void OnLoadMeClicked(object? sender, EventArgs e)
    {
        // כאן מעבירים method group במקום lambda.
        // RefreshUserFromApiAsync כבר מתאים בדיוק ל-Func<Task>.
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
                // UserId אומר לשרת איזו רשומת משתמש לעדכן.
                session.CurrentUser.UserId,

                // שאר הערכים מגיעים מהפקדים שהמשתמש ערך.
                UsernameEntry.Text ?? "",
                FullNameEntry.Text ?? "",
                EmailEntry.Text ?? "");

            StatusLabel.Text = result.Success
                ? $"Status: profile updated - {result.Data?.Message}"
                : $"Status: profile update failed - {result.Message}";

            if (result.Success)
                // אחרי update קוראים שוב מהשרת, כדי שה-session והמסך יכילו את הערכים שנשמרו בפועל.
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

            // מיד אחרי ניקוי ה-session מעדכנים את הטקסט כדי שלא יוצג משתמש שכבר יצא.
            UpdateAuthLabels();

            // אין כאן קריאת API אמיתית, אבל ה-lambda חייב להחזיר Task.
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
        // GetMe מבקש את רשומת הפרופיל המלאה לפי UserId.
        var result = await api.GetMeAsync(session.CurrentUser.UserId);
        if (!result.Success || result.Data is null || !result.Data.Authenticated)
        {
            StatusLabel.Text = $"Status: load me failed - {result.Message}";
            return;
        }

        // מחליפים את אובייקט המשתמש החלקי מתשובת login באובייקט המלא מהשרת.
        session.CurrentUser = result.Data;

        // מעתיקים את הנתונים מה-model אל פקדי ה-XAML.
        EmailEntry.Text = result.Data.Email;
        UsernameEntry.Text = result.Data.Username;
        FullNameEntry.Text = result.Data.FullName;
        UpdateAuthLabels();
    }

    private void UpdateAuthLabels()
    {
        // משתנה מקומי מקצר את הגישה ל-session.CurrentUser.
        var user = session.CurrentUser;

        // אם יש משתמש תקין מציגים את פרטיו; אחרת מציגים שאין auth טעון.
        AuthStateLabel.Text = session.IsLoggedIn && user is not null
            ? $"Auth: userId={user.UserId}, username={user.Username}, role={user.Role}"
            : "Auth: not loaded";
    }

    private static string ExtractResetToken(string input)
    {
        // static כי הפונקציה לא משתמשת בשדות של LoginPage.
        var trimmed = input.Trim();

        // אם הקלט אינו URL מוחלט, מניחים שהמשתמש הדביק token בלבד.
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return trimmed;

        // אם הודבק קישור מלא, מחלצים ממנו את query parameter בשם token.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            // כל חלק נראה כמו key=value. מגבילים את הפיצול לשני חלקים,
            // כדי שסימן = בתוך הערך לא ישבור את ה-token.
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(parts[0], "token", StringComparison.OrdinalIgnoreCase))
                // הקישור עבר URL encoding במייל, לכן מחזירים את הערך המקורי.
                return Uri.UnescapeDataString(parts[1]);
        }

        // URL בלי פרמטר token אינו קלט תקין לאיפוס.
        return "";
    }
}
