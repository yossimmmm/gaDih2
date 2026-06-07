using TriviaGame.Mobile.Models;

namespace TriviaGame.Mobile.Services;

// TriviaApiClient היא שכבת ה-API של המסכים.
// במקום לעבוד ישירות עם HttpClient, המסכים קוראים למתודות ברורות כמו login או join room.
public sealed class TriviaApiClient
{
    private readonly ApiClient apiClient;

    public TriviaApiClient(ApiClient apiClient)
    {
        // ApiClient הוא השכבה הגנרית שמבצעת את בקשת HTTP בפועל.
        this.apiClient = apiClient;
    }

    // שולחת login עם email + password לשרת.
    // השרת מחזיר פרטי משתמש, וה־UI שומר אותם לזיכרון המקומי.
    // #login #auth #api-fetch - שולח את פרטי ההתחברות ל-API.
    public Task<ApiResult<AuthResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, AuthResponse>(
            // endpoint של התחברות בשרת.
            "/api/auth/login",
            // גוף הבקשה חייב להתאים ל-LoginRequest ב-API.
            new { email, password },
            cancellationToken);

    // שולחת register עם פרטי המשתמש החדשים.
    // #register #auth #api-fetch - שולח ל-API את פרטי המשתמש החדש.
    public Task<ApiResult<ApiSimpleResponse>> RegisterAsync(string username, string fullName, string email, string password, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            // endpoint של הרשמת משתמש חדש.
            "/api/auth/register",
            // השמות כאן צריכים להתאים ל-RegisterRequest בצד השרת.
            new { username, fullName, email, password },
            cancellationToken);

    // #forgot-password #email #reset-token #api-fetch
    // שולח לשרת את כתובת האימייל שעבורה מבקשים לאפס סיסמה.
    // השרת מאתר את המשתמש, יוצר token זמני ושולח קישור איפוס במייל.
    public Task<ApiResult<ApiSimpleResponse>> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            // זהו אותו endpoint שמוגדר ב-AuthController תחת HttpPost("forgot-password").
            "/api/auth/forgot-password",
            // שם השדה חייב להיות email כדי להתאים ל-ForgotPasswordRequest בצד השרת.
            new { email },
            cancellationToken);

    // #reset-password #token #new-password #api-fetch
    // שולח לשרת את הטוקן מהמייל ואת הסיסמה החדשה שהמשתמש בחר.
    // השרת בודק שהטוקן קיים, לא פג תוקף ולא נוצל לפני שינוי ה-password hash.
    public Task<ApiResult<ApiSimpleResponse>> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            // זהו אותו endpoint שמוגדר ב-AuthController תחת HttpPost("reset-password").
            "/api/auth/reset-password",
            // שמות השדות חייבים להתאים ל-ResetPasswordRequest בצד השרת.
            new { token, newPassword },
            cancellationToken);

    // מחזירה את הפרופיל הנוכחי לפי userId.
    // אין כאן session, לכן כל קריאה נשענת על המשתמש ששמור באפליקציה.
    // #me #profile #auth #api-fetch - מבקש מה-API את פרטי המשתמש המחובר.
    public Task<ApiResult<CurrentUserResponse>> GetMeAsync(int userId, CancellationToken cancellationToken = default) =>
        // userId עובר ב-query string כי זה endpoint קריאה פשוט.
        apiClient.GetAsync<CurrentUserResponse>($"/api/auth/me?userId={userId}", cancellationToken);

    // סוגי שאלות זמינים לחדר.
    // #question-types #question #rooms #api-fetch - טוען את קטגוריות השאלות לבחירת חדר.
    public Task<ApiResult<List<QuestionTypeRow>>> GetQuestionTypesAsync(CancellationToken cancellationToken = default) =>
        // משמש למילוי Picker של סוגי שאלות במסך יצירת חדר.
        apiClient.GetAsync<List<QuestionTypeRow>>("/api/rooms/question-types", cancellationToken);

    // יוצר חדר חדש.
    // #create-room #rooms #api-fetch - מבקש מה-API ליצור חדר חדש במסד הנתונים.
    public Task<ApiResult<CreateRoomResponse>> CreateRoomAsync(int userId, string roomName, bool isPublic, int? questionTypeId, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, CreateRoomResponse>(
            // יצירת חדר היא POST כי היא מוסיפה רשומה חדשה במסד.
            "/api/rooms",
            new { userId, roomName, isPublic, questionTypeId },
            cancellationToken);

    // מחזירה את רשימת החדרים הציבוריים.
    // #public-rooms #rooms #api-fetch - מבקש את רשימת החדרים הציבוריים.
    public Task<ApiResult<List<RoomRow>>> GetPublicRoomsAsync(CancellationToken cancellationToken = default) =>
        // מחזיר חדרים ציבוריים בלבד.
        apiClient.GetAsync<List<RoomRow>>("/api/rooms/public", cancellationToken);

    // מצרפת משתמש לחדר.
    // #join-room #rooms #players #api-fetch - מצרף משתמש לחדר ומחזיר את שחקן החדר.
    public Task<ApiResult<JoinRoomResponse>> JoinRoomAsync(int userId, string roomCode, string nickname, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, JoinRoomResponse>(
            // הצטרפות יוצרת או מחזירה RoomPlayer, לכן זו פעולת POST.
            "/api/rooms/join",
            new { userId, roomCode, nickname },
            cancellationToken);

    // מחזירה את רשימת השחקנים בחדר.
    // #room-players #rooms #players #api-fetch - טוען את כל השחקנים שנמצאים בחדר.
    public Task<ApiResult<List<RoomPlayerRow>>> GetRoomPlayersAsync(string roomCode, CancellationToken cancellationToken = default) =>
        // roomCode נמצא בנתיב כדי לזהות את החדר.
        apiClient.GetAsync<List<RoomPlayerRow>>($"/api/rooms/{roomCode}/players", cancellationToken);

    // יציאה מחדר.
    // #leave-room #rooms #logout-room #api-fetch - מודיע ל-API שהמשתמש עוזב את החדר.
    public Task<ApiResult<ApiSimpleResponse>> LeaveRoomAsync(string roomCode, int userId, CancellationToken cancellationToken = default) =>
        // userId עובר ב-query string והגוף ריק כי אין נתונים נוספים.
        apiClient.PostAsync<object, ApiSimpleResponse>($"/api/rooms/{roomCode}/leave?userId={userId}", new { }, cancellationToken);

    // התחלת המשחק.
    // #start-game #game #play #api-fetch - מבקש להתחיל את המשחק ולבחור שאלות לחדר.
    public Task<ApiResult<ApiSimpleResponse>> StartGameAsync(int userId, string roomCode, int questionCount = 10, CancellationToken cancellationToken = default) =>
        // רק host רשאי להתחיל משחק; השרת בודק את userId מול HostID.
        apiClient.PostAsync<object, ApiSimpleResponse>($"/api/game/{roomCode}/start", new { userId, questionCount }, cancellationToken);

    // שליפת השאלה הנוכחית מהשרת.
    // #question #current-question #game #play #api-fetch - מבקש את השאלה הפעילה כרגע.
    public Task<ApiResult<CurrentQuestionResponse>> GetCurrentQuestionAsync(string roomCode, CancellationToken cancellationToken = default) =>
        // השרת מחזיר או שאלה פעילה או finished=true.
        apiClient.GetAsync<CurrentQuestionResponse>($"/api/game/{roomCode}/current-question", cancellationToken);

    // שליחת תשובה של שחקן.
    // #answer #submit-answer #question #game #play #api-fetch - שולח את אפשרות התשובה שהשחקן בחר.
    public Task<ApiResult<ApiSimpleResponse>> SubmitAnswerAsync(string roomCode, int roomPlayerId, int questionId, int optionId, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            // שליחת תשובה משנה state במסד, לכן זו POST.
            $"/api/game/{roomCode}/answer",
            // שלושת המזהים מספיקים לשרת כדי לדעת מי ענה, על מה, ומה נבחר.
            new { roomPlayerId, questionId, optionId },
            cancellationToken);

    // טבלת ניקוד של החדר.
    // #scoreboard #results #game #api-fetch - טוען את הניקוד והתוצאות של שחקני החדר.
    public Task<ApiResult<ScoreboardResponse>> GetScoreboardAsync(string roomCode, CancellationToken cancellationToken = default) =>
        // משמש לרענון טבלת הניקוד של החדר.
        apiClient.GetAsync<ScoreboardResponse>($"/api/game/{roomCode}/scoreboard", cancellationToken);

    // שחקנים מובילים.
    // #top-players #leaderboard #statistics #api-fetch - טוען את טבלת השחקנים המובילים.
    public Task<ApiResult<List<TopPlayerRow>>> GetTopPlayersAsync(int limit = 10, CancellationToken cancellationToken = default) =>
        // limit מגביל כמה שורות leaderboard יחזרו.
        apiClient.GetAsync<List<TopPlayerRow>>($"/api/game/top-players?limit={limit}", cancellationToken);

    // סטטיסטיקות אישיות של המשתמש.
    // #stats #statistics #results #api-fetch - טוען את הסטטיסטיקות המצטברות של המשתמש.
    public Task<ApiResult<UserStatsResponse>> GetMyStatsAsync(int userId, CancellationToken cancellationToken = default) =>
        // מחזיר סטטיסטיקה מצטברת למשתמש המחובר.
        apiClient.GetAsync<UserStatsResponse>($"/api/users/me/stats?userId={userId}", cancellationToken);

    // עדכון פרופיל.
    // #profile #update-profile #user #api-fetch - שולח ל-API את פרטי הפרופיל המעודכנים.
    public Task<ApiResult<ApiSimpleResponse>> UpdateProfileAsync(int userId, string username, string fullName, string email, CancellationToken cancellationToken = default) =>
        // PUT מתאים לעדכון פרופיל קיים.
        apiClient.PutAsync<object, ApiSimpleResponse>("/api/users/me/profile", new { userId, username, fullName, email }, cancellationToken);

    // שינוי סיסמה.
    // #password #change-password #profile #api-fetch - שולח סיסמה קיימת וחדשה לצורך החלפה.
    public Task<ApiResult<ApiSimpleResponse>> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
        // שינוי סיסמה שולח גם סיסמה קיימת וגם חדשה כדי שהשרת יאמת לפני עדכון.
        apiClient.PutAsync<object, ApiSimpleResponse>("/api/users/me/password", new { userId, currentPassword, newPassword }, cancellationToken);

    // שאלה לעוזר ה־AI.
    // #assistant #gemini #advice #api-fetch - שולח את שאלת המשתמש לעוזר החכם דרך ה-API.
    public Task<ApiResult<ApiSimpleResponse>> AskAssistantAsync(int userId, string message, CancellationToken cancellationToken = default) =>
        // כרגע נשלחת היסטוריה ריקה; המסך יכול בעתיד להעביר history אמיתי.
        apiClient.PostAsync<object, ApiSimpleResponse>("/api/assistant/chat", new { userId, message, history = new object[0] }, cancellationToken);
}
