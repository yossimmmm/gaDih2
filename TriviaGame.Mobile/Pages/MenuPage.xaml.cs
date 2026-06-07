using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// תפריט ראשי אחרי התחברות.
// הדף הזה לא עושה עבודה כבדה, אלא מנתב את המשתמש למסכים: חדרים, משחק, סטטיסטיקות והגדרות.
public partial class MenuPage : ContentPage
{
    private readonly TriviaApiClient api;
    private readonly MobileSessionState session;

    public MenuPage()
    {
        InitializeComponent();
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateUserLabel();
    }

    private void UpdateUserLabel()
    {
        var user = session.CurrentUser;
        UserLabel.Text = session.IsLoggedIn && user is not null
            ? $"User: {user.Username} ({user.Role})"
            : "User: not logged in";
    }

    private async void OnRoomsClicked(object? sender, EventArgs e)
    {
        // #rooms #navigation
        // מעבר למסך חדרים, שם יוצרים חדר או מצטרפים לחדר קיים.
        await Shell.Current.GoToAsync("//rooms");
    }

    private async void OnPlayClicked(object? sender, EventArgs e)
    {
        // #play #navigation
        // מעבר למשחק קיים. אם אין חדר, PlayPage יציג הודעה ברורה.
        await Shell.Current.GoToAsync("//play");
    }

    private async void OnStatsClicked(object? sender, EventArgs e)
    {
        // #stats #assistant #navigation
        await Shell.Current.GoToAsync("//stats");
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        // #api-settings #navigation
        await Shell.Current.GoToAsync("//settings");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        // #logout
        // מנקים את הסשן המקומי ומחזירים למסך Login.
        session.Logout();
        await Shell.Current.GoToAsync("//login");
    }

    private async void OnStartSoloClicked(object? sender, EventArgs e)
    {
        // #solo-game #create-room #join-room #start-game
        // משחק יחיד הוא בעצם חדר פרטי שבו המשתמש יוצר חדר, מצטרף אליו, ואז מתחיל משחק.
        if (!session.IsLoggedIn || session.CurrentUser is null)
        {
            StatusLabel.Text = "Status: login first.";
            return;
        }

        try
        {
            StatusLabel.Text = "Status: creating solo game...";
            var user = session.CurrentUser;

            // יוצרים חדר פרטי. questionTypeId=null אומר שהשרת יכול לבחור מכל סוגי השאלות.
            var createResult = await api.CreateRoomAsync(
                user.UserId,
                $"Solo {DateTime.Now:HH:mm}",
                isPublic: false,
                questionTypeId: null);

            if (!createResult.Success || createResult.Data?.Ok != true || createResult.Data.Room is null)
            {
                StatusLabel.Text = $"Status: create solo failed - {createResult.Data?.Message ?? createResult.Message}";
                return;
            }

            session.CurrentRoom = createResult.Data.Room;

            // גם במשחק יחיד צריך RoomPlayer, כי תשובות נשמרות לפי שחקן בחדר.
            var joinResult = await api.JoinRoomAsync(
                user.UserId,
                createResult.Data.Room.RoomCode,
                user.Username);

            if (!joinResult.Success || joinResult.Data?.Ok != true || joinResult.Data.Player is null)
            {
                StatusLabel.Text = $"Status: join solo failed - {joinResult.Data?.Message ?? joinResult.Message}";
                return;
            }

            session.CurrentPlayer = joinResult.Data.Player;
            session.CurrentRoom = joinResult.Data.Room ?? session.CurrentRoom;

            // מתחילים את המשחק עם 10 שאלות כברירת מחדל.
            var startResult = await api.StartGameAsync(user.UserId, session.CurrentRoom.RoomCode, questionCount: 10);
            if (!startResult.Success || startResult.Data?.Ok != true)
            {
                StatusLabel.Text = $"Status: start solo failed - {startResult.Data?.Message ?? startResult.Message}";
                return;
            }

            StatusLabel.Text = "Status: solo game started.";
            await Shell.Current.GoToAsync("//play");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Status: solo game failed - {ex.Message}";
        }
    }
}
