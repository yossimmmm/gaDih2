using TriviaGame.Mobile.Models;

namespace TriviaGame.Mobile.Services;

// TriviaApiClient הוא ה-wrapper העסקי של ה-UI.
// כאן לא רואים HttpClient ישירות, אלא פעולות כמו Login, JoinRoom, StartGame וכו'.
// כל מתודה כאן ממפה action של המשתמש ל-endpoint ספציפי בשרת.
public sealed class TriviaApiClient
{
    private readonly ApiClient apiClient;

    public TriviaApiClient(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // Login שולח email + password לשרת.
    // השרת מחזיר פרטי משתמש, וה-UI שומר אותם לזיכרון המקומי.
    public Task<ApiResult<AuthResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, AuthResponse>(
            "/api/auth/login",
            new { email, password },
            cancellationToken);

    // הרשמה שולחת את פרטי המשתמש החדשים.
    public Task<ApiResult<ApiSimpleResponse>> RegisterAsync(string username, string fullName, string email, string password, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            "/api/auth/register",
            new { username, fullName, email, password },
            cancellationToken);

    // פרטי המשתמש הנוכחי נשלפים לפי userId.
    // אין session, לכן כל בקשה מקבלת את ההקשר ישירות מה-UI.
    public Task<ApiResult<CurrentUserResponse>> GetMeAsync(int userId, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<CurrentUserResponse>($"/api/auth/me?userId={userId}", cancellationToken);

    // סוגי שאלות זמינים בחדר.
    public Task<ApiResult<List<QuestionTypeRow>>> GetQuestionTypesAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<QuestionTypeRow>>("/api/rooms/question-types", cancellationToken);

    // יצירת חדר חדש.
    public Task<ApiResult<ApiSimpleResponse>> CreateRoomAsync(int userId, string roomName, bool isPublic, int? questionTypeId, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            "/api/rooms",
            new { userId, roomName, isPublic, questionTypeId },
            cancellationToken);

    // רשימת חדרים ציבוריים.
    public Task<ApiResult<List<RoomRow>>> GetPublicRoomsAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<RoomRow>>("/api/rooms/public", cancellationToken);

    // הצטרפות לחדר לפי קוד.
    public Task<ApiResult<JoinRoomResponse>> JoinRoomAsync(int userId, string roomCode, string nickname, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, JoinRoomResponse>(
            "/api/rooms/join",
            new { userId, roomCode, nickname },
            cancellationToken);

    // רשימת שחקנים בחדר מסוים.
    public Task<ApiResult<List<RoomPlayerRow>>> GetRoomPlayersAsync(string roomCode, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<RoomPlayerRow>>($"/api/rooms/{roomCode}/players", cancellationToken);

    // יציאה מחדר.
    public Task<ApiResult<ApiSimpleResponse>> LeaveRoomAsync(string roomCode, int userId, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>($"/api/rooms/{roomCode}/leave?userId={userId}", new { }, cancellationToken);

    // התחלת משחק.
    public Task<ApiResult<ApiSimpleResponse>> StartGameAsync(int userId, string roomCode, int questionCount = 10, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>($"/api/game/{roomCode}/start", new { userId, questionCount }, cancellationToken);

    // שליפת השאלה הנוכחית מהשרת.
    public Task<ApiResult<CurrentQuestionResponse>> GetCurrentQuestionAsync(string roomCode, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<CurrentQuestionResponse>($"/api/game/{roomCode}/current-question", cancellationToken);

    // שליחת תשובה של שחקן.
    public Task<ApiResult<ApiSimpleResponse>> SubmitAnswerAsync(string roomCode, int roomPlayerId, int questionId, int optionId, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            $"/api/game/{roomCode}/answer",
            new { roomPlayerId, questionId, optionId },
            cancellationToken);

    // טבלת ניקוד של החדר.
    public Task<ApiResult<ScoreboardResponse>> GetScoreboardAsync(string roomCode, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<ScoreboardResponse>($"/api/game/{roomCode}/scoreboard", cancellationToken);

    // שחקנים מובילים.
    public Task<ApiResult<List<TopPlayerRow>>> GetTopPlayersAsync(int limit = 10, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<TopPlayerRow>>($"/api/game/top-players?limit={limit}", cancellationToken);

    // סטטיסטיקות אישיות של המשתמש.
    public Task<ApiResult<UserStatsResponse>> GetMyStatsAsync(int userId, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<UserStatsResponse>($"/api/users/me/stats?userId={userId}", cancellationToken);

    // עדכון פרופיל.
    public Task<ApiResult<ApiSimpleResponse>> UpdateProfileAsync(int userId, string username, string fullName, string email, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<object, ApiSimpleResponse>("/api/users/me/profile", new { userId, username, fullName, email }, cancellationToken);

    // שינוי סיסמה.
    public Task<ApiResult<ApiSimpleResponse>> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<object, ApiSimpleResponse>("/api/users/me/password", new { userId, currentPassword, newPassword }, cancellationToken);

    // בקשה לעוזר האישי.
    public Task<ApiResult<ApiSimpleResponse>> AskAssistantAsync(int userId, string message, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>("/api/assistant/chat", new { userId, message, history = new object[0] }, cancellationToken);
}
