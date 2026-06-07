using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף סטטיסטיקות ועוזר.
// כאן MAUI קורא ל-endpoints של stats, top players ו-Gemini assistant דרך ה-API.
public partial class StatsPage : ContentPage
{
    // מכיל את המתודות GetMyStats, GetTopPlayers ו-AskAssistant.
    private readonly TriviaApiClient api;

    // משמש למציאת UserId של המשתמש המחובר.
    private readonly MobileSessionState session;

    // רשימה מקומית שמחוברת ל-TopPlayersView ב-XAML.
    private readonly List<TopPlayerRow> topPlayers = new();

    public StatsPage()
    {
        // יוצר את הפקדים שהוגדרו בקובץ StatsPage.xaml.
        InitializeComponent();

        // השירותים מגיעים מה-container שנבנה ב-MauiProgram.
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();

        // מחבר את רשימת ה-C# ל-CollectionView.
        TopPlayersView.ItemsSource = topPlayers;
    }

    // action מכיל את הפעולה הספציפית של הכפתור.
    // הפונקציה מוסיפה לכל הפעולות loading, הודעת סטטוס וטיפול בחריגות.
    private async Task RunUiActionAsync(string actionName, Func<Task> action)
    {
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        StatusLabel.Text = $"Status: {actionName}...";

        try
        {
            // await אינו חוסם את מסך MAUI בזמן שהבקשה ממתינה לשרת.
            await action();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Status: {actionName} failed - {ex.Message}";
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private async void OnLoadStatsClicked(object? sender, EventArgs e)
    {
        // Clicked דורש event handler שמחזיר void, ולכן החתימה היא async void.
        await RunUiActionAsync("load stats", async () =>
        {
            if (!session.IsLoggedIn || session.CurrentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            // #stats #statistics #results #api-fetch
            // טוען סטטיסטיקה מצטברת של המשתמש המחובר.
            // UserId אומר לשרת עבור איזה חשבון לחשב סטטיסטיקה מצטברת.
            var result = await api.GetMyStatsAsync(session.CurrentUser.UserId);
            if (!result.Success || result.Data is null)
            {
                StatsLabel.Text = $"Stats: failed - {result.Message}";
                return;
            }

            // הערכים הגיעו מה-DB דרך UsersController והשירות של ה-API.
            StatsLabel.Text =
                $"Stats: games={result.Data.GamesPlayed}, wins={result.Data.Wins}, correct={result.Data.Correct}/{result.Data.Answered}";
        });
    }

    private async void OnLoadTopPlayersClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load top players", async () =>
        {
            // #top-players #leaderboard #statistics #api-fetch
            // מבקש מהשרת את המובילים לפי הנתונים שנשמרו ב-DB.
            // ברירת המחדל בשירות היא עד 10 שחקנים מובילים.
            var result = await api.GetTopPlayersAsync();
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: top players failed - {result.Message}";
                return;
            }

            // מנקים לפני AddRange כדי למנוע כפילויות ברענון חוזר.
            topPlayers.Clear();
            topPlayers.AddRange(result.Data);

            // List רגיל אינו מודיע ל-UI על שינויים, לכן מאפסים ומחברים שוב את ItemsSource.
            TopPlayersView.ItemsSource = null;
            TopPlayersView.ItemsSource = topPlayers;
            StatusLabel.Text = $"Status: loaded {topPlayers.Count} top players.";
        });
    }

    private async void OnAskAssistantClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("ask assistant", async () =>
        {
            if (!session.IsLoggedIn || session.CurrentUser is null)
            {
                AssistantLabel.Text = "Assistant: login first.";
                return;
            }

            // Text עשוי להיות null אם השדה ריק.
            var prompt = AssistantPromptEntry.Text ?? "";
            // #assistant-validation #gemini-validation #validation
            if (string.IsNullOrWhiteSpace(prompt))
            {
                AssistantLabel.Text = "Assistant: enter a question first.";
                return;
            }

            // #assistant #gemini #advice #api-fetch
            // MAUI לא פונה ישירות ל-Gemini.
            // הוא שולח בקשה ל-API, וה-API service הוא זה שמדבר עם Gemini.
            // UserId מאפשר ל-API לבנות הקשר אישי; prompt הוא השאלה שנכתבה במסך.
            var result = await api.AskAssistantAsync(
                session.CurrentUser.UserId,
                prompt);

            // עוזר מוצלח מחזיר את הטקסט ב-Data.Text.
            // בכישלון מעדיפים Message מפורט מה-controller, ואם אין משתמשים ב-ApiResult.Message.
            AssistantLabel.Text = result.Success && result.Data?.Ok == true
                ? $"Assistant: {result.Data.Text}"
                : $"Assistant: failed - {result.Data?.Message ?? result.Message}";
        });
    }
}
