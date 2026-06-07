using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף חדרים.
// כאן המשתמש יוצר חדר, מצטרף לחדר, רואה חדרים ציבוריים ורואה שחקנים בחדר.
public partial class RoomsPage : ContentPage
{
    private readonly TriviaApiClient api;
    private readonly MobileSessionState session;
    private readonly List<QuestionTypeRow> questionTypes = new();
    private readonly List<RoomRow> publicRooms = new();

    public RoomsPage()
    {
        InitializeComponent();
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();
        PublicRoomsView.ItemsSource = publicRooms;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateActiveRoomLabel();
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

    private async void OnLoadQuestionTypesClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load question types", async () =>
        {
            // #question-types #question #rooms #api-fetch
            // טוענים קטגוריות שאלות כדי שהמשתמש יוכל לבחור סוג לחדר.
            var result = await api.GetQuestionTypesAsync();
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: failed loading types - {result.Message}";
                return;
            }

            questionTypes.Clear();
            questionTypes.AddRange(result.Data);
            QuestionTypePicker.ItemsSource = questionTypes;
            QuestionTypePicker.SelectedIndex = questionTypes.Count > 0 ? 0 : -1;
            StatusLabel.Text = $"Status: loaded {questionTypes.Count} question types.";
        });
    }

    private async void OnCreateRoomClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("create room", async () =>
        {
            if (!session.IsLoggedIn || session.CurrentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            int? selectedTypeId = null;
            if (QuestionTypePicker.SelectedIndex >= 0 && QuestionTypePicker.SelectedIndex < questionTypes.Count)
                selectedTypeId = questionTypes[QuestionTypePicker.SelectedIndex].QuestionTypeID;

            // #create-room #rooms #api-fetch
            // השרת מחליט את RoomID ואת RoomCode.
            // ה-UI שולח רק שם חדר, האם ציבורי, וסוג שאלות אופציונלי.
            var createResult = await api.CreateRoomAsync(
                session.CurrentUser.UserId,
                RoomNameEntry.Text ?? "",
                IsPublicSwitch.IsToggled,
                selectedTypeId);

            if (!createResult.Success || createResult.Data?.Ok != true || createResult.Data.Room is null)
            {
                StatusLabel.Text = $"Status: create failed - {createResult.Data?.Message ?? createResult.Message}";
                return;
            }

            session.CurrentRoom = createResult.Data.Room;
            RoomCodeEntry.Text = createResult.Data.Room.RoomCode;

            // #join-room #create-room
            // אחרי יצירת חדר מצרפים את היוצר לחדר, כדי שיהיה לו RoomPlayerID לתשובות.
            var joinResult = await api.JoinRoomAsync(
                session.CurrentUser.UserId,
                createResult.Data.Room.RoomCode,
                session.CurrentUser.Username);

            if (joinResult.Success && joinResult.Data?.Ok == true)
            {
                session.CurrentRoom = joinResult.Data.Room ?? session.CurrentRoom;
                session.CurrentPlayer = joinResult.Data.Player;
            }

            UpdateActiveRoomLabel();
            StatusLabel.Text = session.CurrentPlayer is not null
                ? $"Status: created and joined {session.CurrentRoom?.RoomCode}."
                : $"Status: room created, join failed - {joinResult.Message}";

            if (createResult.Data.Room.IsPublic)
                await RefreshPublicRoomsAsync();
        });
    }

    private async void OnLoadPublicRoomsClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load public rooms", async () =>
        {
            await RefreshPublicRoomsAsync();
            StatusLabel.Text = $"Status: loaded {publicRooms.Count} public rooms.";
        });
    }

    private async Task RefreshPublicRoomsAsync()
    {
        // #public-rooms #rooms #api-fetch
        // מבקש מהשרת את החדרים הציבוריים הפעילים.
        var result = await api.GetPublicRoomsAsync();
        if (!result.Success || result.Data is null)
            return;

        publicRooms.Clear();
        publicRooms.AddRange(result.Data);
        PublicRoomsView.ItemsSource = null;
        PublicRoomsView.ItemsSource = publicRooms;
    }

    private void OnPublicRoomSelected(object? sender, SelectionChangedEventArgs e)
    {
        // #public-rooms #join-room
        // בחירת חדר מהרשימה רק ממלאת את הקוד בשדה. ההצטרפות קורית בלחיצה על Join.
        if (e.CurrentSelection.FirstOrDefault() is RoomRow room)
        {
            RoomCodeEntry.Text = room.RoomCode;
            StatusLabel.Text = $"Status: selected {room.RoomName} ({room.RoomCode}).";
        }
    }

    private async void OnJoinRoomClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("join room", async () =>
        {
            if (!session.IsLoggedIn || session.CurrentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            var nickname = string.IsNullOrWhiteSpace(NicknameEntry.Text)
                ? session.CurrentUser.Username
                : NicknameEntry.Text.Trim();

            // #room-code-validation #join-room-validation #validation
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                StatusLabel.Text = "Status: enter room code.";
                return;
            }

            // #join-room #rooms #players #api-fetch
            // השרת מחזיר גם את החדר וגם את RoomPlayer שנוצר/נמצא.
            var result = await api.JoinRoomAsync(session.CurrentUser.UserId, roomCode, nickname);
            if (!result.Success || result.Data?.Ok != true || result.Data.Player is null)
            {
                StatusLabel.Text = $"Status: join failed - {result.Data?.Message ?? result.Message}";
                return;
            }

            session.CurrentRoom = result.Data.Room;
            session.CurrentPlayer = result.Data.Player;
            UpdateActiveRoomLabel();
            StatusLabel.Text = $"Status: joined room {roomCode}.";
            await LoadPlayersAsync(roomCode);
        });
    }

    private async void OnLoadPlayersClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load players", async () =>
        {
            var roomCode = session.CurrentRoom?.RoomCode ?? (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            await LoadPlayersAsync(roomCode);
        });
    }

    private async Task LoadPlayersAsync(string roomCode)
    {
        // #room-players #rooms #api-fetch
        // מציג מי נמצא בחדר לפי roomCode.
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            PlayersLabel.Text = "Players: enter or select a room first.";
            return;
        }

        var result = await api.GetRoomPlayersAsync(roomCode);
        if (!result.Success || result.Data is null)
        {
            PlayersLabel.Text = $"Players: failed - {result.Message}";
            return;
        }

        PlayersLabel.Text = "Players: " + string.Join(", ", result.Data.Select(p => $"{p.Nickname}#{p.RoomPlayerID}"));
    }

    private async void OnLeaveRoomClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("leave room", async () =>
        {
            if (!session.IsLoggedIn || session.CurrentUser is null || session.CurrentRoom is null)
            {
                StatusLabel.Text = "Status: no active room.";
                return;
            }

            // #leave-room #rooms #api-fetch
            // מודיעים לשרת שהמשתמש יצא ואז מנקים את מצב המשחק המקומי.
            var roomCode = session.CurrentRoom.RoomCode;
            var result = await api.LeaveRoomAsync(roomCode, session.CurrentUser.UserId);
            session.ClearGame();
            UpdateActiveRoomLabel();
            StatusLabel.Text = result.Success
                ? $"Status: left room {roomCode}."
                : $"Status: leave failed - {result.Message}";
            await Task.CompletedTask;
        });
    }

    private async void OnGoToPlayClicked(object? sender, EventArgs e)
    {
        // #play #navigation
        await Shell.Current.GoToAsync("//play");
    }

    private void UpdateActiveRoomLabel()
    {
        ActiveRoomLabel.Text = session.CurrentRoom is null
            ? "Active room: none"
            : $"Active room: {session.CurrentRoom.RoomName} ({session.CurrentRoom.RoomCode}), playerId={session.CurrentPlayer?.RoomPlayerID}";
    }
}
