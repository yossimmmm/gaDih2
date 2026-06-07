using Microsoft.Extensions.DependencyInjection;
using TriviaGame.Mobile.Models;
using TriviaGame.Mobile.Services;

namespace TriviaGame.Mobile;

// זה המסך הראשי של האפליקציה.
// כל כפתור כאן מפעיל קריאת HTTP ל־API, והטקסטים על המסך רק מציגים את המצב והתוצאה.
public partial class MainPage : ContentPage
{
    // עטיפת ה־API שבה משתמש כל המסך.
    // כל הקריאות עוברות דרכה ולא ישירות דרך HttpClient.
    private readonly TriviaApiClient api;

    // אובייקט שאחראי לבחור base URL, סביבה וקוד אפליקציה.
    private readonly ApiEndpointResolver endpointResolver;

    // מצב המשתמש המחובר כרגע.
    private CurrentUserResponse? currentUser;

    // מזהה השחקן של המשתמש בתוך החדר הפעיל.
    private int currentRoomPlayerId;

    // השאלה הפעילה כרגע.
    private QuestionRow? currentQuestion;

    // האפשרות שנבחרה כרגע עבור התשובה.
    private QuestionOptionRow? selectedOption;

    // רשימות בזיכרון שמחוברות ל־UI.
    private readonly List<QuestionTypeRow> questionTypes = new();
    private readonly List<RoomRow> publicRooms = new();
    private readonly List<RoomPlayerRow> currentPlayers = new();

    public MainPage()
    {
        InitializeComponent();

        // שולפים services מה־container של MAUI.
        // כאן TriviaApiClient ו־ApiEndpointResolver מוזרקים לעמוד.
        var services = Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("Service provider is unavailable.");
        api = services.GetRequiredService<TriviaApiClient>();
        endpointResolver = services.GetRequiredService<ApiEndpointResolver>();

        // מחברים את האוספים לפקדי התצוגה.
        PublicRoomsView.ItemsSource = publicRooms;
        OptionsView.ItemsSource = Array.Empty<QuestionOptionRow>();

        // טוענים את ההגדרות שנשמרו מקומית כדי שהמסך יראה את המצב הנוכחי.
        LoadApiSettingsToUi();
        UpdateResolvedBaseUrlLabel();
    }

    // קורא את ההגדרות המקומיות ומחזיר אותן אל הפקדים.
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

    // מציג למשתמש את ה־base URL שנבחר בפועל.
    private void UpdateResolvedBaseUrlLabel() =>
        ResolvedBaseUrlLabel.Text = $"Base URL: {endpointResolver.GetBaseUrl()}";

    // עזר מרכזי שמנהל מצב טעינה, סטטוס וטיפול בחריגות במקום אחד.
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

    // שינוי סביבה ב־UI מעדכן מיד את ההגדרה המקומית.
    private void OnEnvironmentChanged(object? sender, EventArgs e)
    {
        endpointResolver.SetEnvironment(EnvironmentPicker.SelectedItem?.ToString() ?? "Development");
        UpdateResolvedBaseUrlLabel();
    }

    // שומר את ההגדרות שנבחרו ומחשב מחדש את כתובת ה־API.
    private void OnApplyApiSettingsClicked(object? sender, EventArgs e)
    {
        endpointResolver.SetEnvironment(EnvironmentPicker.SelectedItem?.ToString() ?? "Development");
        endpointResolver.SetDeviceBaseUrl(DeviceBaseUrlEntry.Text ?? "");
        endpointResolver.SetOverrideBaseUrl(OverrideBaseUrlEntry.Text ?? "");
        UpdateResolvedBaseUrlLabel();
        StatusLabel.Text = "Status: API settings applied.";
    }

    // התחברות: שולח אימייל וסיסמה לשרת ומעדכן את מצב המשתמש המקומי.
    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("login", async () =>
        {
            // #login #auth #api-fetch - מכאן לחיצה על Login עוברת ל-TriviaApiClient.
            var result = await api.LoginAsync(EmailEntry.Text ?? "", PasswordEntry.Text ?? "");
            if (!result.Success || result.Data is null || !result.Data.Ok)
            {
                StatusLabel.Text = $"Status: login failed - {result.Message}";
                return;
            }

            // ה־API מחזיר userId, username ו־role.
            // שומרים אותם בזיכרון כדי ששאר המסך יוכל להשתמש בהם אחרי ההתחברות.
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

    // הרשמה: שולח פרטי משתמש חדשים לשרת.
    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("register", async () =>
        {
            // #register #auth #api-fetch - מכאן נשלחים שדות ההרשמה ל-API.
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

    // #forgot-password #email
    // לחיצה על Forgot Password מתחילה את התהליך, אבל עדיין לא משנה את הסיסמה.
    // היא רק מבקשת מהשרת ליצור token זמני ולשלוח קישור אל האימייל שהוזן.
    private async void OnForgotPasswordClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("forgot password", async () =>
        {
            // קוראים קודם את האימייל מאזור שחזור הסיסמה.
            // אם המשתמש כבר מילא את האימייל באזור ה-login, משתמשים בו כ-fallback.
            var email = (ForgotEmailEntry.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email))
                email = (EmailEntry.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                StatusLabel.Text = "Status: enter an email first.";
                PasswordRecoveryLabel.Text = "Password recovery: enter the account email.";
                return;
            }

            // #forgot-password #api-fetch
            // מכאן הבקשה עוברת ל-TriviaApiClient ומשם אל POST /api/auth/forgot-password.
            var result = await api.ForgotPasswordAsync(email);

            // Success מתאר אם התקבלה תשובת HTTP תקינה;
            // Data.Ok הוא הערך שה-controller החזיר בתוך גוף ה-JSON.
            if (result.Success && result.Data?.Ok == true)
            {
                // מעדכנים גם את שדה ה-login כדי שיהיה ברור לאיזה חשבון בוצע reset.
                EmailEntry.Text = email;
                PasswordRecoveryLabel.Text = $"Password recovery: {result.Data.Message}";
                StatusLabel.Text = $"Status: {result.Data.Message}";
                return;
            }

            var message = result.Data?.Message ?? result.Message;
            PasswordRecoveryLabel.Text = $"Password recovery: failed - {message}";
            StatusLabel.Text = $"Status: forgot password failed - {message}";
        });
    }

    // #reset-password #token #new-password
    // לחיצה על Reset Password משלימה את התהליך בעזרת הטוקן שהגיע במייל.
    private async void OnResetPasswordClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("reset password", async () =>
        {
            // המשתמש יכול להדביק token בלבד או את כל הקישור שקיבל במייל.
            // הפונקציה מחלצת את ערך token מה-query string כאשר הודבק קישור מלא.
            var token = ExtractResetToken(ResetTokenEntry.Text ?? "");
            // זו הסיסמה החדשה שתעבור validation ו-hash בצד השרת.
            var newPassword = NewPasswordEntry.Text ?? "";

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                StatusLabel.Text = "Status: enter the reset token and a new password.";
                PasswordRecoveryLabel.Text = "Password recovery: paste the email link/token and enter a new password.";
                return;
            }

            // #reset-password #api-fetch
            // מכאן נשלחים הטוקן והסיסמה אל POST /api/auth/reset-password.
            var result = await api.ResetPasswordAsync(token, newPassword);
            if (!result.Success || result.Data?.Ok != true)
            {
                var message = result.Data?.Message ?? result.Message;
                PasswordRecoveryLabel.Text = $"Password recovery: reset failed - {message}";
                StatusLabel.Text = $"Status: reset password failed - {message}";
                return;
            }

            // אחרי הצלחה מוחקים מהמסך את המידע הרגיש ולא משאירים token או סיסמה גלויים בזיכרון ה-UI.
            ResetTokenEntry.Text = "";
            NewPasswordEntry.Text = "";
            PasswordEntry.Text = "";
            PasswordRecoveryLabel.Text = $"Password recovery: {result.Data.Message}";
            StatusLabel.Text = $"Status: {result.Data.Message}";
        });
    }

    // #reset-password #token
    // ממיר את הקלט שבשדה האיפוס לטוקן שה-API מצפה לקבל.
    private static string ExtractResetToken(string input)
    {
        var trimmed = input.Trim();

        // אם זה לא URI תקין, מניחים שהמשתמש כבר הדביק את הטוקן עצמו.
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var resetUri))
            return trimmed;

        // Query מחזיר טקסט שמתחיל ב-?, לכן מסירים אותו ומחלקים לזוגות key=value.
        foreach (var pair in resetUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(parts[0], "token", StringComparison.OrdinalIgnoreCase))
            {
                // הטוקן בקישור עבר URL encoding; מחזירים אותו לצורה המקורית לפני השליחה לשרת.
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        // קישור בלי token אינו בקשת איפוס תקינה, ולכן מחזירים מחרוזת ריקה כדי שה-validation יעצור.
        return "";
    }

    // יציאה רק מנקה את המצב המקומי.
    // אין כאן session בצד שרת, לכן לא צריך לבטל משהו בשרת עצמו.
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

    // טוען מחדש את המידע של המשתמש המחובר.
    private async void OnLoadMeClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load me", async () =>
        {
            await RefreshUserFromApiAsync();
        });
    }

    // קורא את פרופיל המשתמש מה־API ומעדכן את שדות המסך.
    private async Task RefreshUserFromApiAsync()
    {
        if (currentUser is null)
        {
            StatusLabel.Text = "Status: login first.";
            return;
        }

        // #me #profile #auth #api-fetch - רענון פרטי המשתמש מהשרת לאחר התחברות או עדכון.
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

    // עדכון פרופיל: שולח username, full name ו־email יחד עם userId.
    private async void OnUpdateProfileClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("update profile", async () =>
        {
            if (currentUser is null)
            {
                StatusLabel.Text = "Status: login first.";
                return;
            }

            // #profile #update-profile #api-fetch - לחיצת עדכון הפרופיל שולחת את הערכים הנוכחיים.
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

    // טוען את סוגי השאלות כדי למלא את ה־picker של יצירת החדר.
    private async void OnLoadQuestionTypesClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load question types", async () =>
        {
            // #question-types #question #rooms #api-fetch - טוען קטגוריות לתיבת הבחירה.
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

    // יוצר חדר חדש ושולח את הגדרות השאלות לשרת.
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

            // #create-room #rooms #api-fetch - יוצר את החדר לפי שם, פרטיות וסוג שאלות.
            var result = await api.CreateRoomAsync(
                currentUser.UserId,
                RoomNameEntry.Text ?? "",
                IsPublicSwitch.IsToggled,
                selectedQuestionTypeId);

            if (!result.Success || result.Data is null || !result.Data.Ok || result.Data.Room is null)
            {
                var error = result.Data?.Message ?? result.Message;
                RoomStatusLabel.Text = $"Rooms: create failed - {error}";
                StatusLabel.Text = $"Status: create room failed - {error}";
                return;
            }

            // מציגים מיד את קוד החדר שהשרת יצר כדי שאפשר יהיה להצטרף ולהתחיל משחק.
            RoomCodeEntry.Text = result.Data.Room.RoomCode;
            ActiveRoomLabel.Text = $"Active room: {result.Data.Room.RoomCode}";
            // #join-room #create-room #rooms #api-fetch - לאחר יצירה, המארח מצטרף אוטומטית לחדר.
            var joinResult = await api.JoinRoomAsync(
                currentUser.UserId,
                result.Data.Room.RoomCode,
                currentUser.Username);

            if (joinResult.Success && joinResult.Data?.Ok == true)
                currentRoomPlayerId = joinResult.Data.Player?.RoomPlayerID ?? 0;

            RoomStatusLabel.Text = currentRoomPlayerId > 0
                ? $"Rooms: created and joined {result.Data.Room.RoomName} ({result.Data.Room.RoomCode})."
                : $"Rooms: created {result.Data.Room.RoomName} ({result.Data.Room.RoomCode}); join failed.";
            StatusLabel.Text = currentRoomPlayerId > 0
                ? $"Status: {result.Data.Message} Host joined as playerId={currentRoomPlayerId}."
                : $"Status: room created, but host join failed - {joinResult.Message}";

            // חדר ציבורי חדש אמור להופיע מיד ברשימה בלי לחיצה נוספת.
            if (result.Data.Room.IsPublic)
                await RefreshPublicRoomsAsync();
        });
    }

    private async Task RefreshPublicRoomsAsync()
    {
        // #public-rooms #rooms #api-fetch - רענון פנימי של רשימת החדרים הציבוריים.
        var result = await api.GetPublicRoomsAsync();
        if (!result.Success || result.Data is null)
            return;

        publicRooms.Clear();
        publicRooms.AddRange(result.Data);
        PublicRoomsView.ItemsSource = null;
        PublicRoomsView.ItemsSource = publicRooms;
    }

    // טוען חדרים ציבוריים למסך.
    private async void OnLoadPublicRoomsClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load public rooms", async () =>
        {
            // #public-rooms #rooms #api-fetch - לחיצת הכפתור טוענת חדרים ציבוריים מהשרת.
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
            RoomStatusLabel.Text = $"Rooms: loaded {publicRooms.Count} public rooms.";
            StatusLabel.Text = $"Status: loaded {publicRooms.Count} public rooms.";
        });
    }

    // בחירת חדר ציבורי ממלאת את שדה קוד החדר.
    private void OnPublicRoomSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is RoomRow room)
        {
            RoomCodeEntry.Text = room.RoomCode;
            RoomStatusLabel.Text = $"Rooms: selected {room.RoomName} ({room.RoomCode}).";
        }
    }

    // הצטרפות לחדר: שולח userId, roomCode ו־nickname לשרת.
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
            // #join-room #rooms #players #api-fetch - מצרף את המשתמש לחדר לפי הקוד שהוזן.
            var result = await api.JoinRoomAsync(currentUser.UserId, roomCode, NicknameEntry.Text ?? "");
            if (!result.Success || result.Data is null || !result.Data.Ok)
            {
                var error = result.Data?.Message ?? result.Message;
                RoomStatusLabel.Text = $"Rooms: join failed - {error}";
                StatusLabel.Text = $"Status: join failed - {error}";
                return;
            }

            RoomCodeEntry.Text = roomCode;
            ActiveRoomLabel.Text = $"Active room: {roomCode}";
            currentRoomPlayerId = result.Data.Player?.RoomPlayerID ?? 0;
            RoomStatusLabel.Text = $"Rooms: joined {roomCode} as playerId={currentRoomPlayerId}.";
            StatusLabel.Text = $"Status: joined room {roomCode} as playerId={currentRoomPlayerId}.";
        });
    }

    // טוען את רשימת השחקנים בחדר הנוכחי.
    private async void OnLoadPlayersClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load room players", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            // #room-players #rooms #players #api-fetch - טוען את רשימת השחקנים בחדר הפעיל.
            var result = await api.GetRoomPlayersAsync(roomCode);
            if (!result.Success || result.Data is null)
            {
                RoomStatusLabel.Text = $"Rooms: failed loading players - {result.Message}";
                StatusLabel.Text = $"Status: failed loading players - {result.Message}";
                return;
            }

            currentPlayers.Clear();
            currentPlayers.AddRange(result.Data);

            // אם השחקן של המשתמש עוד לא נשמר, מנסים למצוא אותו ברשימה לפי userId.
            if (currentUser is not null && currentRoomPlayerId == 0)
            {
                var meInRoom = currentPlayers.FirstOrDefault(p => p.UserID == currentUser.UserId);
                if (meInRoom is not null)
                    currentRoomPlayerId = meInRoom.RoomPlayerID;
            }

            PlayersLabel.Text = currentPlayers.Count == 0
                ? "Players: none"
                : $"Players: {string.Join(", ", currentPlayers.Select(p => p.Nickname))}";
            RoomStatusLabel.Text = $"Rooms: players loaded ({currentPlayers.Count}).";
            StatusLabel.Text = $"Status: players loaded ({currentPlayers.Count}). My roomPlayerId={currentRoomPlayerId}.";
        });
    }

    // מתחיל את המשחק על ידי שליחת userId של המארח ומספר השאלות המבוקש.
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
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                GameStatusLabel.Text = "Game: create or join a room first.";
                StatusLabel.Text = "Status: room code is required.";
                return;
            }

            var questionCount = int.TryParse(QuestionCountEntry.Text, out var parsed) ? parsed : 10;
            // #start-game #game #play #api-fetch - המארח מבקש מהשרת לפתוח משחק בחדר.
            var result = await api.StartGameAsync(currentUser.UserId, roomCode, questionCount);
            if (!result.Success || result.Data?.Ok != true)
            {
                var error = result.Data?.Message ?? result.Message;
                GameStatusLabel.Text = $"Game: start failed - {error}";
                StatusLabel.Text = $"Status: start game failed - {error}";
                return;
            }

            ActiveRoomLabel.Text = $"Active room: {roomCode}";
            GameStatusLabel.Text = "Game: started. Loading first question...";
            StatusLabel.Text = $"Status: {result.Data.Message}";
            await LoadCurrentQuestionAsync(roomCode);
        });
    }

    // טוען את השאלה הנוכחית מהשרת ומציב את האפשרויות ב־CollectionView.
    private async void OnLoadCurrentQuestionClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load current question", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            await LoadCurrentQuestionAsync(roomCode);
        });
    }

    private async Task LoadCurrentQuestionAsync(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            GameStatusLabel.Text = "Game: create or join a room first.";
            StatusLabel.Text = "Status: room code is required.";
            return;
        }

        // #question #current-question #game #play #api-fetch - טוען את השאלה הפעילה ואת אפשרויות התשובה.
        var result = await api.GetCurrentQuestionAsync(roomCode);
        if (!result.Success || result.Data is null)
        {
            GameStatusLabel.Text = $"Game: question load failed - {result.Message}";
            StatusLabel.Text = $"Status: failed loading question - {result.Message}";
            return;
        }

        if (result.Data.Finished || result.Data.Question is null)
        {
            currentQuestion = null;
            selectedOption = null;
            OptionsView.SelectedItem = null;
            OptionsView.ItemsSource = null;
            QuestionTextLabel.Text = "Question: game finished.";
            GameStatusLabel.Text = "Game: finished. Load the scoreboard.";
            StatusLabel.Text = "Status: no active question.";
            return;
        }

        currentQuestion = result.Data.Question;
        selectedOption = null;
        OptionsView.SelectedItem = null;
        QuestionTextLabel.Text = $"Question: {currentQuestion.QuestionText}";
        OptionsView.ItemsSource = null;
        OptionsView.ItemsSource = currentQuestion.Options;
        ActiveRoomLabel.Text = $"Active room: {roomCode}";
        GameStatusLabel.Text =
            $"Game: question #{currentQuestion.QuestionID}, {currentQuestion.TimeLimitSec} seconds.";
        StatusLabel.Text = $"Status: question loaded (id={currentQuestion.QuestionID}).";
    }

    // שומר איזה option נבחרה.
    private void OnOptionSelected(object? sender, SelectionChangedEventArgs e)
    {
        selectedOption = e.CurrentSelection.FirstOrDefault() as QuestionOptionRow;
        GameStatusLabel.Text = selectedOption is null
            ? "Game: select an answer."
            : $"Game: selected \"{selectedOption.OptionText}\".";
    }

    // שולח את התשובה שנבחרה לשרת.
    private async void OnSubmitAnswerClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("submit answer", async () =>
        {
            if (currentQuestion is null || selectedOption is null)
            {
                GameStatusLabel.Text = "Game: load a question and select an answer.";
                StatusLabel.Text = "Status: load question and select an option first.";
                return;
            }

            if (currentRoomPlayerId <= 0)
            {
                GameStatusLabel.Text = "Game: join the room before answering.";
                StatusLabel.Text = "Status: roomPlayerId missing. Join room and load players first.";
                return;
            }

            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            // #answer #submit-answer #question #game #play #api-fetch - שולח לשרת את התשובה שנבחרה.
            var result = await api.SubmitAnswerAsync(
                roomCode,
                currentRoomPlayerId,
                currentQuestion.QuestionID,
                selectedOption.OptionID);

            if (!result.Success || result.Data?.Ok != true)
            {
                var error = result.Data?.Message ?? result.Message;
                GameStatusLabel.Text = $"Game: answer failed - {error}";
                StatusLabel.Text = $"Status: submit answer failed - {error}";
                return;
            }

            GameStatusLabel.Text = "Game: answer submitted. Loading next question...";
            StatusLabel.Text = $"Status: answer submitted - {result.Data.Message}";
            await LoadCurrentQuestionAsync(roomCode);
        });
    }

    // טוען את טבלת הניקוד של החדר.
    private async void OnLoadScoreboardClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load scoreboard", async () =>
        {
            var roomCode = (RoomCodeEntry.Text ?? "").Trim().ToUpperInvariant();
            // #scoreboard #results #game #api-fetch - טוען את תוצאות השחקנים בחדר.
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
            GameStatusLabel.Text = $"Game: scoreboard loaded ({result.Data.Rows.Count} players).";
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

            // #stats #statistics #results #api-fetch - טוען סטטיסטיקות היסטוריות של המשתמש.
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

    // טוען את רשימת השחקנים המובילים מה־API ומציג סיכום קצר.
    private async void OnLoadTopPlayersClicked(object? sender, EventArgs e)
    {
        await RunUiActionAsync("load top players", async () =>
        {
            // #top-players #leaderboard #statistics #api-fetch - טוען את עשרת השחקנים המובילים.
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

    // שולח שאלה לעוזר.
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

            // #assistant #gemini #advice #api-fetch - שולח את הטקסט לעוזר דרך ה-API.
            var result = await api.AskAssistantAsync(currentUser.UserId, prompt);
            if (!result.Success || result.Data is null)
            {
                AssistantResponseLabel.Text = $"Assistant: {result.Message}";
                StatusLabel.Text = $"Status: assistant failed - {result.Message}";
                return;
            }

            AssistantResponseLabel.Text =
                $"Assistant: {(!string.IsNullOrWhiteSpace(result.Data.Text) ? result.Data.Text : result.Data.Message)}";
            StatusLabel.Text = "Status: assistant reply received.";
        });
    }
}
