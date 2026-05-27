using Microsoft.Extensions.DependencyInjection;
using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile;

public partial class MainPage : ContentPage
{
    // שירות API ראשי שמנתב את כל פעולות המשתמש ל-HTTP.
    private readonly TriviaApiClient api;
    // רזולבר בסיס URL לפי environment ו-device.
    private readonly ApiEndpointResolver endpointResolver;

    // מצב משתמש נוכחי בזיכרון המסך.
    private CurrentUserResponse? currentUser;
    // מצב חדר/שחקן נוכחי עבור submit answer.
    private int currentRoomPlayerId;
    private QuestionRow? currentQuestion;
    private QuestionOptionRow? selectedOption;

    // אוספים לתצוגת רשימות.
    private readonly List<QuestionTypeRow> questionTypes = new();
    private readonly List<RoomRow> publicRooms = new();
    private readonly List<RoomPlayerRow> currentPlayers = new();

    public MainPage()
    {
        InitializeComponent();

        // שליפת תלויות דרך DI גם כשהעמוד נוצר דרך XAML DataTemplate.
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("Service provider is unavailable.");
        api = services.GetRequiredService<TriviaApiClient>();
        endpointResolver = services.GetRequiredService<ApiEndpointResolver>();

        PublicRoomsView.ItemsSource = publicRooms;
        OptionsView.ItemsSource = Array.Empty<QuestionOptionRow>();

        LoadApiSettingsToUi();
        UpdateResolvedBaseUrlLabel();
    }

    // טעינת הגדרות API מה-Preferences למסך.
    private void LoadApiSettingsToUi()
    {
        var env = endpointResolver.GetCurrentEnvironment();
        var index = env switch
        {
            "Staging" => 1,
            "Production" => 2,
            _ => 0
        };
        EnvironmentPicker.SelectedIndex = index;
        DeviceBaseUrlEntry.Text = endpointResolver.GetDeviceBaseUrl();
        OverrideBaseUrlEntry.Text = endpointResolver.GetOverrideBaseUrl();
    }

    private void UpdateResolvedBaseUrlLabel() =>
        ResolvedBaseUrlLabel.Text = $"Base URL: {endpointResolver.GetBaseUrl()}";

    // פעולה עטופה ל-UI: מפעילה busy indicator, מטפלת חריגות, ומציגה הודעת סטטוס ידידותית.
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

    // -------------------------
    // API environment controls
    // -------------------------
    private void OnEnvironmentChanged(object? sender, EventArgs e)
    {
        var value = EnvironmentPicker.SelectedItem?.ToString() ?? "Development";
        endpointResolver.SetEnvironment(value);
        UpdateResolvedBaseUrlLabel();
    }

    private void OnApplyApiSettingsClicked(object? sender, EventArgs e)
    {
        endpointResolver.SetEnvironment(EnvironmentPicker.SelectedItem?.ToString() ?? "Development");
        endpointResolver.SetDeviceBaseUrl(DeviceBaseUrlEntry.Text ?? "");
        endpointResolver.SetOverrideBaseUrl(OverrideBaseUrlEntry.Text ?? "");
        UpdateResolvedBaseUrlLabel();
        StatusLabel.Text = "Status: API settings applied.";
    }

    // -------------------------
    // Auth
    // -------------------------
    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("login", async () =>
        {
            var result = await api.LoginAsync(EmailEntry.Text ?? "", PasswordEntry.Text ?? "");
            if (!result.Success || result.Data is null || !result.Data.Ok)
            {
                StatusLabel.Text = $"Status: login failed - {result.Message}";
                return;
            }

            StatusLabel.Text = "Status: login succeeded.";
            await LoadMeInternalAsync();
        });
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("register", async () =>
        {
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

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("logout", async () =>
        {
            var result = await api.LogoutAsync();
            currentUser = null;
            currentRoomPlayerId = 0;
            AuthStateLabel.Text = "Auth: logged out";
            StatusLabel.Text = result.Success ? "Status: logout succeeded." : $"Status: logout failed - {result.Message}";
        });
    }

    private async void OnLoadMeClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load me", LoadMeInternalAsync);
    }

    private async Task LoadMeInternalAsync()
    {
        var me = await api.GetMeAsync();
        if (!me.Success || me.Data is null || !me.Data.Authenticated)
        {
            currentUser = null;
            AuthStateLabel.Text = "Auth: not authenticated";
            StatusLabel.Text = $"Status: load me failed - {me.Message}";
            return;
        }

        currentUser = me.Data;
        AuthStateLabel.Text = $"Auth: userId={me.Data.UserId}, username={me.Data.Username}, role={me.Data.Role}";
        UsernameEntry.Text = me.Data.Username;
        FullNameEntry.Text = me.Data.FullName;
        EmailEntry.Text = me.Data.Email;
        StatusLabel.Text = "Status: auth state loaded.";
    }

    private async void OnUpdateProfileClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("update profile", async () =>
        {
            var result = await api.UpdateProfileAsync(
                UsernameEntry.Text ?? "",
                FullNameEntry.Text ?? "",
                EmailEntry.Text ?? "");
            StatusLabel.Text = result.Success
                ? $"Status: profile updated - {result.Data?.Message}"
                : $"Status: profile update failed - {result.Message}";
        });
    }

    // -------------------------
    // Rooms
    // -------------------------
    private async void OnLoadQuestionTypesClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load question types", async () =>
        {
            var result = await api.GetQuestionTypesAsync();
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: failed loading question types - {result.Message}";
                return;
            }

            questionTypes.Clear();
            questionTypes.AddRange(result.Data);
            QuestionTypePicker.ItemsSource = null;
            QuestionTypePicker.ItemsSource = questionTypes.Select(x => $"{x.TypeName} ({x.QuestionTypeID})").ToList();
            QuestionTypePicker.SelectedIndex = questionTypes.Count > 0 ? 0 : -1;
            StatusLabel.Text = $"Status: loaded {questionTypes.Count} question types.";
        });
    }

    private async void OnCreateRoomClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("create room", async () =>
        {
            int? selectedQuestionTypeId = null;
            if (QuestionTypePicker.SelectedIndex >= 0 && QuestionTypePicker.SelectedIndex < questionTypes.Count)
                selectedQuestionTypeId = questionTypes[QuestionTypePicker.SelectedIndex].QuestionTypeID;

            var result = await api.CreateRoomAsync(
                RoomNameEntry.Text ?? "",
                IsPublicSwitch.IsToggled,
                selectedQuestionTypeId);

            StatusLabel.Text = result.Success
                ? $"Status: room created - {result.Data?.Message}"
                : $"Status: create room failed - {result.Message}";
        });
    }

    private async void OnLoadPublicRoomsClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load public rooms", async () =>
        {
            var result = await api.GetPublicRoomsAsync();
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: failed loading public rooms - {result.Message}";
                return;
            }

            publicRooms.Clear();
            publicRooms.AddRange(result.Data);
            PublicRoomsView.ItemsSource = null;
            PublicRoomsView.ItemsSource = publicRooms;
            StatusLabel.Text = $"Status: loaded {publicRooms.Count} public rooms.";
        });
    }

    private void OnPublicRoomSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is RoomRow room)
            RoomCodeEntry.Text = room.RoomCode;
    }

    private async void OnJoinRoomClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("join room", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var result = await api.JoinRoomAsync(roomCode, NicknameEntry.Text ?? "");
            if (!result.Success || result.Data is null || !result.Data.Ok)
            {
                StatusLabel.Text = $"Status: join failed - {result.Message}";
                return;
            }

            RoomCodeEntry.Text = roomCode;
            currentRoomPlayerId = result.Data.Player?.RoomPlayerID ?? 0;
            StatusLabel.Text = $"Status: joined room {roomCode} as playerId={currentRoomPlayerId}.";
        });
    }

    private async void OnLoadPlayersClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load room players", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var result = await api.GetRoomPlayersAsync(roomCode);
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: failed loading players - {result.Message}";
                return;
            }

            currentPlayers.Clear();
            currentPlayers.AddRange(result.Data);

            // אם טרם נלכד roomPlayerId, מנסים לאתר אותו לפי userId המחובר.
            if (currentUser is not null && currentRoomPlayerId == 0)
            {
                var meInRoom = currentPlayers.FirstOrDefault(p => p.UserID == currentUser.UserId);
                if (meInRoom is not null)
                    currentRoomPlayerId = meInRoom.RoomPlayerID;
            }

            StatusLabel.Text = $"Status: players loaded ({currentPlayers.Count}). My roomPlayerId={currentRoomPlayerId}.";
        });
    }

    // -------------------------
    // Game
    // -------------------------
    private async void OnStartGameClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("start game", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var questionCount = int.TryParse(QuestionCountEntry.Text, out var parsed) ? parsed : 10;
            var result = await api.StartGameAsync(roomCode, questionCount);
            StatusLabel.Text = result.Success
                ? $"Status: start game request succeeded - {result.Data?.Message}"
                : $"Status: start game failed - {result.Message}";
        });
    }

    private async void OnLoadCurrentQuestionClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load current question", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var result = await api.GetCurrentQuestionAsync(roomCode);
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: failed loading question - {result.Message}";
                return;
            }

            if (result.Data.Finished || result.Data.Question is null)
            {
                currentQuestion = null;
                selectedOption = null;
                OptionsView.ItemsSource = null;
                QuestionTextLabel.Text = "Question: game finished or no active question.";
                StatusLabel.Text = "Status: no active question.";
                return;
            }

            currentQuestion = result.Data.Question;
            selectedOption = null;
            QuestionTextLabel.Text = $"Question: {currentQuestion.QuestionText}";
            OptionsView.ItemsSource = null;
            OptionsView.ItemsSource = currentQuestion.Options;
            StatusLabel.Text = $"Status: question loaded (id={currentQuestion.QuestionID}).";
        });
    }

    private void OnOptionSelected(object? sender, SelectionChangedEventArgs e)
    {
        selectedOption = e.CurrentSelection.FirstOrDefault() as QuestionOptionRow;
    }

    private async void OnSubmitAnswerClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("submit answer", async () =>
        {
            if (currentQuestion is null || selectedOption is null)
            {
                StatusLabel.Text = "Status: load question and select an option first.";
                return;
            }

            if (currentRoomPlayerId <= 0)
            {
                StatusLabel.Text = "Status: roomPlayerId missing. Join room and load players first.";
                return;
            }

            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var result = await api.SubmitAnswerAsync(
                roomCode,
                currentRoomPlayerId,
                currentQuestion.QuestionID,
                selectedOption.OptionID);

            StatusLabel.Text = result.Success
                ? $"Status: answer submitted - {result.Data?.Message}"
                : $"Status: submit answer failed - {result.Message}";
        });
    }

    private async void OnLoadScoreboardClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load scoreboard", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var result = await api.GetScoreboardAsync(roomCode);
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: failed loading scoreboard - {result.Message}";
                return;
            }

            var text = result.Data.Rows.Count == 0
                ? "no rows"
                : string.Join(" | ", result.Data.Rows.Select(r => $"{r.Nickname}:{r.CorrectCount}/{r.AnsweredCount}"));
            ScoreboardLabel.Text = $"Scoreboard: {text}";
            StatusLabel.Text = "Status: scoreboard loaded.";
        });
    }

    // -------------------------
    // Stats + assistant
    // -------------------------
    private async void OnLoadStatsClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load stats", async () =>
        {
            var result = await api.GetMyStatsAsync();
            if (!result.Success || result.Data is null)
            {
                StatsLabel.Text = "Stats: failed";
                StatusLabel.Text = $"Status: stats failed - {result.Message}";
                return;
            }

            StatsLabel.Text =
                $"Stats: games={result.Data.GamesPlayed}, wins={result.Data.Wins}, correct={result.Data.Correct}, answered={result.Data.Answered}";
            StatusLabel.Text = "Status: stats loaded.";
        });
    }

    private async void OnLoadTopPlayersClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load top players", async () =>
        {
            var result = await api.GetTopPlayersAsync(10);
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: top players failed - {result.Message}";
                return;
            }

            var summary = result.Data.Count == 0
                ? "none"
                : string.Join(" | ", result.Data.Take(5).Select(r => $"{r.Username} ({r.Wins}W/{r.GamesPlayed}G)"));
            StatsLabel.Text = $"Top Players: {summary}";
            StatusLabel.Text = "Status: top players loaded.";
        });
    }

    private async void OnAskAssistantClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("ask assistant", async () =>
        {
            var prompt = AssistantPromptEntry.Text ?? "";
            if (string.IsNullOrWhiteSpace(prompt))
            {
                StatusLabel.Text = "Status: enter a prompt first.";
                return;
            }

            var result = await api.AskAssistantAsync(prompt);
            if (!result.Success || result.Data is null)
            {
                AssistantResponseLabel.Text = "Assistant: request failed.";
                StatusLabel.Text = $"Status: assistant failed - {result.Message}";
                return;
            }

            AssistantResponseLabel.Text = $"Assistant: {(!string.IsNullOrWhiteSpace(result.Data.Text) ? result.Data.Text : result.Data.Message)}";
            StatusLabel.Text = "Status: assistant reply received.";
        });
    }
}
