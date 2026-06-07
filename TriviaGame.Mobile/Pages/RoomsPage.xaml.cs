using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// דף חדרים.
// כאן המשתמש יוצר חדר, מצטרף לחדר, רואה חדרים ציבוריים ורואה שחקנים בחדר.
public partial class RoomsPage : ContentPage
{
    // שכבת הקריאות ל-API. הדף אינו יוצר HttpClient ואינו כותב URLs בעצמו.
    private readonly TriviaApiClient api;

    // state משותף שמחבר בין RoomsPage לבין PlayPage.
    private readonly MobileSessionState session;

    // עותק מקומי של סוגי השאלות שהגיעו מהשרת.
    // ה-Picker מציג את אותה רשימה, וה-index שלו משמש למציאת QuestionTypeID.
    private readonly List<QuestionTypeRow> questionTypes = new();

    // עותק מקומי של החדרים הציבוריים שהגיעו מהשרת.
    // ה-CollectionView מציג את האובייקטים מתוך הרשימה הזאת.
    private readonly List<RoomRow> publicRooms = new();

    public RoomsPage()
    {
        // טוען את XAML ויוצר את QuestionTypePicker, PublicRoomsView ושאר הפקדים.
        InitializeComponent();

        // מקבלים את אותם Singleton services שנרשמו ב-MauiProgram.
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();

        // ItemsSource מחבר בין רשימת C# לבין הרשימה הוויזואלית ב-XAML.
        PublicRoomsView.ItemsSource = publicRooms;
    }

    protected override void OnAppearing()
    {
        // כשחוזרים מהמשחק, מציגים שוב את החדר ששמור ב-session.
        base.OnAppearing();
        UpdateActiveRoomLabel();
    }

    private async Task RunUiActionAsync(string actionName, Func<Task> action)
    {
        // פעולה משותפת לכל הכפתורים בדף כדי לא לשכפל loading ו-error handling.
        BusyIndicator.IsVisible = true;
        BusyIndicator.IsRunning = true;
        StatusLabel.Text = $"Status: {actionName}...";

        try
        {
            // מפעיל את ה-lambda שה-event handler העביר לפונקציה.
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

            // קריאת API נחשבת תקינה רק אם HTTP הצליח וגם התקבל גוף JSON.
            if (!result.Success || result.Data is null)
            {
                StatusLabel.Text = $"Status: failed loading types - {result.Message}";
                return;
            }

            // מנקים נתונים ישנים כדי שלחיצות חוזרות לא ייצרו כפילויות.
            questionTypes.Clear();
            questionTypes.AddRange(result.Data);

            // Picker קורא ל-ToString של QuestionTypeRow כדי להציג טקסט.
            QuestionTypePicker.ItemsSource = questionTypes;

            // אם הרשימה אינה ריקה, בוחרים אוטומטית את האפשרות הראשונה.
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

            // בודקים שה-index נמצא בתוך גבולות הרשימה לפני גישה אליו.
            // אם לא נבחר סוג, נשאר null והשרת יכול לבחור שאלות מכל הסוגים.
            if (QuestionTypePicker.SelectedIndex >= 0 && QuestionTypePicker.SelectedIndex < questionTypes.Count)
                selectedTypeId = questionTypes[QuestionTypePicker.SelectedIndex].QuestionTypeID;

            // #create-room #rooms #api-fetch
            // השרת מחליט את RoomID ואת RoomCode.
            // ה-UI שולח רק שם חדר, האם ציבורי, וסוג שאלות אופציונלי.
            var createResult = await api.CreateRoomAsync(
                // UserId נשלח כדי שהשרת ישמור את המשתמש כ-HostID.
                session.CurrentUser.UserId,

                // שם החדר נלקח מה-Entry. null מוחלף במחרוזת ריקה.
                RoomNameEntry.Text ?? "",

                // ערך ה-Switch קובע אם החדר יופיע ברשימת החדרים הציבוריים.
                IsPublicSwitch.IsToggled,
                selectedTypeId);

            if (!createResult.Success || createResult.Data?.Ok != true || createResult.Data.Room is null)
            {
                StatusLabel.Text = $"Status: create failed - {createResult.Data?.Message ?? createResult.Message}";
                return;
            }

            // שומרים את ה-Room שהשרת יצר, כולל RoomID ו-RoomCode שהשרת החליט.
            session.CurrentRoom = createResult.Data.Room;

            // ממלאים את שדה הקוד כדי שהמשתמש יראה ויוכל לשתף אותו.
            RoomCodeEntry.Text = createResult.Data.Room.RoomCode;

            // #join-room #create-room
            // אחרי יצירת חדר מצרפים את היוצר לחדר, כדי שיהיה לו RoomPlayerID לתשובות.
            var joinResult = await api.JoinRoomAsync(
                session.CurrentUser.UserId,
                createResult.Data.Room.RoomCode,
                session.CurrentUser.Username);

            if (joinResult.Success && joinResult.Data?.Ok == true)
            {
                // Room מתשובת join עשוי להכיל מצב מעודכן יותר.
                session.CurrentRoom = joinResult.Data.Room ?? session.CurrentRoom;

                // RoomPlayer נשמר כי SubmitAnswer דורש RoomPlayerID ולא רק UserId.
                session.CurrentPlayer = joinResult.Data.Player;
            }

            UpdateActiveRoomLabel();
            StatusLabel.Text = session.CurrentPlayer is not null
                ? $"Status: created and joined {session.CurrentRoom?.RoomCode}."
                : $"Status: room created, join failed - {joinResult.Message}";

            // חדר ציבורי חדש צריך להופיע מיד ברשימה בלי לחכות ללחיצה נוספת.
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
        // הפונקציה משמשת גם כ-helper פנימי, לכן בכישלון פשוט לא מחליפים את הרשימה הקיימת.
        if (!result.Success || result.Data is null)
            return;

        // מחליפים את תוכן הרשימה המקומית בתוצאה העדכנית מהשרת.
        publicRooms.Clear();
        publicRooms.AddRange(result.Data);

        // מאפסים את ItemsSource כדי לאלץ את ה-CollectionView להתרענן,
        // מפני ש-List רגיל אינו שולח notification כמו ObservableCollection.
        PublicRoomsView.ItemsSource = null;
        PublicRoomsView.ItemsSource = publicRooms;
    }

    private void OnPublicRoomSelected(object? sender, SelectionChangedEventArgs e)
    {
        // #public-rooms #join-room
        // בחירת חדר מהרשימה רק ממלאת את הקוד בשדה. ההצטרפות קורית בלחיצה על Join.
        // CurrentSelection היא רשימה כי CollectionView יכול לתמוך גם בבחירה מרובה.
        // כאן SelectionMode=Single, לכן לוקחים את הפריט הראשון בלבד.
        if (e.CurrentSelection.FirstOrDefault() is RoomRow room)
        {
            // בחירה אינה מצטרפת אוטומטית. היא רק מעתיקה את הקוד לשדה.
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

            // קודי חדר נשמרים בצורה אחידה באותיות גדולות.
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();

            // אם המשתמש לא כתב nickname, משתמשים ב-username של החשבון.
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

            // שני האובייקטים משותפים עם PlayPage דרך MobileSessionState.
            session.CurrentRoom = result.Data.Room;
            session.CurrentPlayer = result.Data.Player;
            UpdateActiveRoomLabel();
            StatusLabel.Text = $"Status: joined room {roomCode}.";
            // אחרי join טוענים את רשימת השחקנים כדי להציג שהמשתמש אכן נמצא בחדר.
            await LoadPlayersAsync(roomCode);
        });
    }

    private async void OnLoadPlayersClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load players", async () =>
        {
            // מעדיפים את החדר הפעיל ב-session.
            // אם אין חדר פעיל, מאפשרים לבדוק חדר לפי הקוד שהוקלד בשדה.
            var roomCode = session.CurrentRoom?.RoomCode
                ?? (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
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

        // GET אינו משנה נתונים, הוא רק מחזיר את השחקנים הפעילים בחדר.
        var result = await api.GetRoomPlayersAsync(roomCode);
        if (!result.Success || result.Data is null)
        {
            PlayersLabel.Text = $"Players: failed - {result.Message}";
            return;
        }

        // Select ממיר כל RoomPlayerRow לטקסט, ו-string.Join מחבר את כולם לשורה אחת.
        PlayersLabel.Text = "Players: " + string.Join(
            ", ",
            result.Data.Select(p => $"{p.Nickname}#{p.RoomPlayerID}"));
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
            // שומרים את הקוד במשתנה לפני ניקוי ה-session.
            // אחרת לא יהיה אפשר להציג איזה חדר נעזב.
            var roomCode = session.CurrentRoom.RoomCode;

            // קודם מודיעים לשרת כדי שיעדכן את room_players/מצב החדר.
            var result = await api.LeaveRoomAsync(roomCode, session.CurrentUser.UserId);

            // לאחר מכן מנקים את החדר, השחקן, השאלה והתשובה מהזיכרון המקומי.
            session.ClearGame();
            UpdateActiveRoomLabel();
            StatusLabel.Text = result.Success
                ? $"Status: left room {roomCode}."
                : $"Status: leave failed - {result.Message}";
            // ה-lambda אסינכרוני בגלל המבנה המשותף, ואין כאן await נוסף אחרי העדכון.
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
        // הטקסט נגזר מה-session, לכן כל דף רואה את אותו חדר פעיל.
        ActiveRoomLabel.Text = session.CurrentRoom is null
            ? "Active room: none"
            : $"Active room: {session.CurrentRoom.RoomName} ({session.CurrentRoom.RoomCode}), playerId={session.CurrentPlayer?.RoomPlayerID}";
    }
}
