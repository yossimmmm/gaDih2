using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile.Pages;

// תפריט ראשי אחרי התחברות.
// הדף הזה לא עושה עבודה כבדה, אלא מנתב את המשתמש למסכים: חדרים, משחק, סטטיסטיקות והגדרות.
public partial class MenuPage : ContentPage
{
    // השירות נדרש רק לפעולת Start Solo, שמבצעת כמה קריאות API ברצף.
    private readonly TriviaApiClient api;

    // הסשן מאפשר לתפריט לדעת מי מחובר ומה החדר הפעיל בלי לקבל פרמטר מכל דף.
    private readonly MobileSessionState session;

    public MenuPage()
    {
        // יוצר את הפקדים שהוגדרו ב-MenuPage.xaml.
        InitializeComponent();

        // שני האובייקטים נוצרו ונרשמו ב-MauiProgram.cs.
        api = PageServiceLocator.Get<TriviaApiClient>();
        session = PageServiceLocator.Get<MobileSessionState>();
    }

    protected override void OnAppearing()
    {
        // מופעל גם כשחוזרים לתפריט אחרי משחק או שינוי פרופיל.
        base.OnAppearing();
        UpdateUserLabel();
    }

    private void UpdateUserLabel()
    {
        // שומרים reference מקומי כדי לא לקרוא את אותו property כמה פעמים.
        var user = session.CurrentUser;
        UserLabel.Text = session.IsLoggedIn && user is not null
            ? $"User: {user.Username} ({user.Role})"
            : "User: not logged in";
    }

    private async void OnRoomsClicked(object? sender, EventArgs e)
    {
        // #rooms #navigation
        // מעבר למסך חדרים, שם יוצרים חדר או מצטרפים לחדר קיים.
        // Shell.Current הוא ה-Shell הפעיל שנוצר ב-App.xaml.cs.
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
        // מנקה CurrentUser וגם את כל מצב החדר/משחק.
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
            // אין RunUiActionAsync בדף הזה, לכן מטפלים כאן ישירות ב-try/catch.
            StatusLabel.Text = "Status: creating solo game...";

            // אחרי בדיקת null למעלה, user הוא משתמש מחובר תקין.
            var user = session.CurrentUser;

            // יוצרים חדר פרטי. questionTypeId=null אומר שהשרת יכול לבחור מכל סוגי השאלות.
            var createResult = await api.CreateRoomAsync(
                // המשתמש המחובר הופך ל-host של החדר.
                user.UserId,

                // DateTime משמש רק ליצירת שם נוח לחדר הסולו.
                $"Solo {DateTime.Now:HH:mm}",
                isPublic: false,
                questionTypeId: null);

            if (!createResult.Success || createResult.Data?.Ok != true || createResult.Data.Room is null)
            {
                StatusLabel.Text = $"Status: create solo failed - {createResult.Data?.Message ?? createResult.Message}";
                return;
            }

            // שומרים את החדר מיד, כדי שהדפים האחרים יוכלו לגשת ל-roomCode.
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

            // Player מכיל RoomPlayerID הנדרש לשליחת תשובות.
            session.CurrentPlayer = joinResult.Data.Player;

            // אם join החזיר Room מעודכן משתמשים בו; אחרת משאירים את החדר שכבר נשמר.
            session.CurrentRoom = joinResult.Data.Room ?? session.CurrentRoom;

            // מתחילים את המשחק עם 10 שאלות כברירת מחדל.
            // רק לאחר שיש גם Room וגם RoomPlayer מתחילים את המשחק.
            var startResult = await api.StartGameAsync(
                user.UserId,
                session.CurrentRoom.RoomCode,
                questionCount: 10);
            if (!startResult.Success || startResult.Data?.Ok != true)
            {
                StatusLabel.Text = $"Status: start solo failed - {startResult.Data?.Message ?? startResult.Message}";
                return;
            }

            StatusLabel.Text = "Status: solo game started.";

            // PlayPage יקרא את CurrentRoom ו-CurrentPlayer מה-session המשותף.
            await Shell.Current.GoToAsync("//play");
        }
        catch (Exception ex)
        {
            // תופס שגיאת HTTP או שגיאה לא צפויה כדי שהאפליקציה לא תיסגר.
            StatusLabel.Text = $"Status: solo game failed - {ex.Message}";
        }
    }
}
