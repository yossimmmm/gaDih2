using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף סטטיסטיקות ועוזר.
// כאן MAUI קורא ל-endpoints של stats, top players ו-Gemini assistant דרך ה-API.
public partial class StatsPage : ContentPage
{
    private readonly TriviaApiClient api;
    private readonly MobileSessionState session;
    private readonly List<TopPlayerRow> topPlayers = new();

    public StatsPage()
    {
        InitializeComponent();
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();
        TopPlayersView.ItemsSource = topPlayers;
    }

    private async Task RunUiActionAsync(string actionName, Func<Task> action)
    {
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        StatusLabel.Text = $"Status: {actionName}...";

        try
        {
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
        await RunUiActionAsync("load stats", async () =>
        {
            if (!session.IsLoggedIn || session.CurrentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            // #stats #statistics #results #api-fetch
            // טוען סטטיסטיקה מצטברת של המשתמש המחובר.
            var result = await api.GetMyStatsAsync(session.CurrentUser.UserId);
            if (!result.Success || result.Data is null)
            {
                StatsLabel.Text = $"Stats: failed - {result.Message}";
                return;
            }

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
            var result = await api.GetTopPlayersAsync();
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: top players failed - {result.Message}";
                return;
            }

            topPlayers.Clear();
            topPlayers.AddRange(result.Data);
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
            var result = await api.AskAssistantAsync(session.CurrentUser.UserId, prompt);
            AssistantLabel.Text = result.Success && result.Data?.Ok == true
                ? $"Assistant: {result.Data.Text}"
                : $"Assistant: failed - {result.Data?.Message ?? result.Message}";
        });
    }
}
