using Microsoft.Extensions.DependencyInjection;
using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile;

// זהו המסך הראשי של האפליקציה.
// כל כפתור כאן מפעיל HTTP call אחר ל-API, והטקסטים על המסך רק מציגים את התוצאה.
public partial class MainPage : ContentPage
{
    // ה-wrapper שמדבר עם ה-API.
    // כל הפעולות עובדות דרכו ולא ישירות מול HttpClient.
    private readonly TriviaApiClient api;

    // אחראי על בחירת base URL, סביבת עבודה, ו-app code.
    private readonly ApiEndpointResolver endpointResolver;

    // מצב מקומי של המשתמש המחובר.
    private CurrentUserResponse? currentUser;

    // מזהה השחקן בתוך החדר הנוכחי.
    private int currentRoomPlayerId;

    // השאלה הפעילה ביותר כרגע.
    private QuestionRow? currentQuestion;

    // תשובה שנבחרה ברגע נתון.
    private QuestionOptionRow? selectedOption;

    // רשימות בזיכרון שמאכלסות CollectionView/Picker.
    private readonly List<QuestionTypeRow> questionTypes = new();
    private readonly List<RoomRow> publicRooms = new();
    private readonly List<RoomPlayerRow> currentPlayers = new();

    public MainPage()
    {
        InitializeComponent();

        // כאן שולפים services מה-container של MAUI.
        // זה המקום שבו TriviaApiClient ו-ApiEndpointResolver מוזרקים למסך.
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("Service provider is unavailable.");
        api = services.GetRequiredService<TriviaApiClient>();
        endpointResolver = services.GetRequiredService<ApiEndpointResolver>();

        // מחברים את הרשימות למסכים הוויזואליים.
        PublicRoomsView.ItemsSource = publicRooms;
        OptionsView.ItemsSource = Array.Empty<QuestionOptionRow>();

        // טוענים את ההגדרות שנשמרו מקומית לממשק.
        LoadApiSettingsToUi();
        UpdateResolvedBaseUrlLabel();
    }

    // לוקח את ההגדרות שנשמרו ב-Preferences ומציג אותן בשדות.
    private void LoadApiSettingsToUi()
    {
        var env = endpointResolver.GetCurrentEnvironment();
        EnvironmentPicker.SelectedIndex = env switch
        {
            "Staging" => 1,
            "Production" => 2,
            _ => 0
        };

        DeviceBaseUrlEntry.Text = endpointResolver.GetDeviceBaseUrl();
        OverrideBaseUrlEntry.Text = endpointResolver.GetOverrideBaseUrl();
    }

    // מראה למשתמש לאיזה API base URL האפליקציה תפנה בפועל.
    private void UpdateResolvedBaseUrlLabel() =>
        ResolvedBaseUrlLabel.Text = $"Base URL: {endpointResolver.GetBaseUrl()}";

    // helper שמרכז מצב טעינה, status, וטיפול בשגיאות ברמת UI.
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

    // שינוי environment ב-UI מעדכן את ההגדרה המקומית בלבד.
    private void OnEnvironmentChanged(object? sender, EventArgs e)
    {
        endpointResolver.SetEnvironment(EnvironmentPicker.SelectedItem?.ToString() ?? "Development");
        UpdateResolvedBaseUrlLabel();
    }

    // שומר את ההגדרות המקומיות ומחשב מחדש את כתובת ה-API.
    private void OnApplyApiSettingsClicked(object? sender, EventArgs e)
    {
        endpointResolver.SetEnvironment(EnvironmentPicker.SelectedItem?.ToString() ?? "Development");
        endpointResolver.SetDeviceBaseUrl(DeviceBaseUrlEntry.Text ?? "");
        endpointResolver.SetOverrideBaseUrl(OverrideBaseUrlEntry.Text ?? "");
        UpdateResolvedBaseUrlLabel();
        StatusLabel.Text = "Status: API settings applied.";
    }

    // Login: שולח email/password ל-API ומעדכן את מצב המשתמש המקומי.
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

            // ה-API מחזיר userId, username, role.
            // כאן אנחנו שומרים אותו בזיכרון כדי להשתמש בו בבקשות הבאות.
            currentUser = new CurrentUserResponse
            {
                Authenticated = true,
                UserId = result.Data.UserId,
                Username = result.Data.Username,
                FullName = "",
                Email = EmailEntry.Text ?? "",
                Role = result.Data.Role
            };

            AuthStateLabel.Text = $"Auth: userId={currentUser.UserId}, username={currentUser.Username}, role={currentUser.Role}";
            UsernameEntry.Text = currentUser.Username;
            EmailEntry.Text = currentUser.Email;
            StatusLabel.Text = "Status: login succeeded.";
            await RefreshUserFromApiAsync();
        });
    }

    // Register: שולח פרטי משתמש חדשים לשרת.
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

    // Logout מנקה רק את המצב המקומי.
    // אין session בצד השרת, אז אין מה לבטל שם.
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("logout", async () =>
        {
            currentUser = null;
            currentRoomPlayerId = 0;
            currentQuestion = null;
            selectedOption = null;
            AuthStateLabel.Text = "Auth: logged out";
            StatusLabel.Text = "Status: logged out.";
            await Task.CompletedTask;
        });
    }

    // טעינת פרטי המשתמש מה-API לפי userId השמור.
    private async void OnLoadMeClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load me", async () =>
        {
            await RefreshUserFromApiAsync();
        });
    }

    // קורא ל-API ומעדכן את שדות הפרופיל בממשק.
    private async Task RefreshUserFromApiAsync()
    {
        if (currentUser is null)
        {
            StatusLabel.Text = "Status: login first.";
            return;
        }

        var me = await api.GetMeAsync(currentUser.UserId);
        if (!me.Success || me.Data is null || !me.Data.Authenticated)
        {
            StatusLabel.Text = $"Status: load me failed - {me.Message}";
            return;
        }

        currentUser = me.Data;
        UsernameEntry.Text = me.Data.Username;
        FullNameEntry.Text = me.Data.FullName;
        EmailEntry.Text = me.Data.Email;
        AuthStateLabel.Text = $"Auth: userId={me.Data.UserId}, username={me.Data.Username}, role={me.Data.Role}";
        StatusLabel.Text = "Status: auth data loaded.";
    }

    // עדכון פרופיל: שולח username/fullName/email יחד עם userId.
    private async void OnUpdateProfileClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("update profile", async () =>
        {
            if (currentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            var result = await api.UpdateProfileAsync(
                currentUser.UserId,
                UsernameEntry.Text ?? "",
                FullNameEntry.Text ?? "",
                EmailEntry.Text ?? "");

            StatusLabel.Text = result.Success
                ? $"Status: profile updated - {result.Data?.Message}"
                : $"Status: profile update failed - {result.Message}";
        });
    }

    // טוען את סוגי השאלות הזמינים ליצירת חדר.
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
            QuestionTypePicker.ItemsSource = questionTypes.Select(x => $"{x.TypeName} ({x.QuestionTypeID})").ToList();
            QuestionTypePicker.SelectedIndex = questionTypes.Count > 0 ? 0 : -1;
            StatusLabel.Text = $"Status: loaded {questionTypes.Count} question types.";
        });
    }

    // יוצר חדר חדש ושולח ל-API את השדות שהמשתמש מילא.
    private async void OnCreateRoomClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("create room", async () =>
        {
            if (currentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            int? selectedQuestionTypeId = null;
            if (QuestionTypePicker.SelectedIndex >= 0 && QuestionTypePicker.SelectedIndex < questionTypes.Count)
                selectedQuestionTypeId = questionTypes[QuestionTypePicker.SelectedIndex].QuestionTypeID;

            var result = await api.CreateRoomAsync(
                currentUser.UserId,
                RoomNameEntry.Text ?? "",
                IsPublicSwitch.IsToggled,
                selectedQuestionTypeId);

            StatusLabel.Text = result.Success
                ? $"Status: room created - {result.Data?.Message}"
                : $"Status: create room failed - {result.Message}";
        });
    }

    // טוען חדרים ציבוריים בלבד.
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

    // בחירה של חדר ציבורי ממלאת אוטומטית את קוד החדר.
    private void OnPublicRoomSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is RoomRow room)
            RoomCodeEntry.Text = room.RoomCode;
    }

    // הצטרפות לחדר שולחת userId, roomCode, nickname לשרת.
    private async void OnJoinRoomClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("join room", async () =>
        {
            if (currentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var result = await api.JoinRoomAsync(currentUser.UserId, roomCode, NicknameEntry.Text ?? "");
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

    // טוען את רשימת השחקנים בחדר הנוכחי.
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

            if (currentUser is not null && currentRoomPlayerId == 0)
            {
                var meInRoom = currentPlayers.FirstOrDefault(p => p.UserID == currentUser.UserId);
                if (meInRoom is not null)
                    currentRoomPlayerId = meInRoom.RoomPlayerID;
            }

            StatusLabel.Text = $"Status: players loaded ({currentPlayers.Count}). My roomPlayerId={currentRoomPlayerId}.";
        });
    }

    // התחלת משחק שולחת לשרת את ה-userId של המארח ואת מספר השאלות המבוקש.
    private async void OnStartGameClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("start game", async () =>
        {
            if (currentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var questionCount = int.TryParse(QuestionCountEntry.Text, out var parsed) ? parsed : 10;
            var result = await api.StartGameAsync(currentUser.UserId, roomCode, questionCount);
            StatusLabel.Text = result.Success
                ? $"Status: start game request succeeded - {result.Data?.Message}"
                : $"Status: start game failed - {result.Message}";
        });
    }

    // טוען את השאלה הנוכחית מהשרת ומעדכן את ה-CollectionView של התשובות.
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

    // מסמן איזו תשובה נבחרה כרגע.
    private void OnOptionSelected(object? sender, SelectionChangedEventArgs e)
    {
        selectedOption = e.CurrentSelection.FirstOrDefault() as QuestionOptionRow;
    }

    // שולח את התשובה שנבחרה לשרת.
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

    // טוען scoreboard של החדר.
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

    // טוען את הסטטיסטיקות האישיות של המשתמש המחובר.
    private async void OnLoadStatsClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load stats", async () =>
        {
            if (currentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            var result = await api.GetMyStatsAsync(currentUser.UserId);
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

    // טוען את top players מה-API ומציג סיכום קצר.
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

    // שולח שאלה לעוזר האישי.
    private async void OnAskAssistantClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("ask assistant", async () =>
        {
            if (currentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            var prompt = AssistantPromptEntry.Text ?? "";
            if (string.IsNullOrWhiteSpace(prompt))
            {
                StatusLabel.Text = "Status: enter a prompt first.";
                return;
            }

            var result = await api.AskAssistantAsync(currentUser.UserId, prompt);
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
