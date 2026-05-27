using TriviaGame.Mobile.Models;

namespace TriviaGame.Mobile.Services;

public sealed class TriviaApiClient
{
    private readonly ApiClient apiClient;
    private readonly AuthSessionStore sessionStore;

    public TriviaApiClient(ApiClient apiClient, AuthSessionStore sessionStore)
    {
        this.apiClient = apiClient;
        this.sessionStore = sessionStore;
    }

    // -------------------------
    // Auth
    // -------------------------
    public async Task<ApiResult<AuthResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.PostAsync<object, AuthResponse>(
            "/api/auth/login",
            new { email, password },
            requiresAuth: false,
            cancellationToken);

        if (result.Success && result.Data is not null && result.Data.Ok && !string.IsNullOrWhiteSpace(result.Data.Token))
            sessionStore.SaveToken(result.Data.Token);

        return result;
    }

    public Task<ApiResult<ApiSimpleResponse>> RegisterAsync(string username, string fullName, string email, string password, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            "/api/auth/register",
            new { username, fullName, email, password },
            requiresAuth: false,
            cancellationToken);

    public Task<ApiResult<CurrentUserResponse>> GetMeAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<CurrentUserResponse>("/api/auth/me", true, cancellationToken);

    public async Task<ApiResult<ApiSimpleResponse>> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var result = await apiClient.PostAsync<object, ApiSimpleResponse>("/api/auth/logout", new { }, true, cancellationToken);
        sessionStore.Clear();
        return result;
    }

    // -------------------------
    // Rooms
    // -------------------------
    public Task<ApiResult<List<QuestionTypeRow>>> GetQuestionTypesAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<QuestionTypeRow>>("/api/rooms/question-types", false, cancellationToken);

    public Task<ApiResult<ApiSimpleResponse>> CreateRoomAsync(string roomName, bool isPublic, int? questionTypeId, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            "/api/rooms",
            new { roomName, isPublic, questionTypeId },
            true,
            cancellationToken);

    public Task<ApiResult<List<RoomRow>>> GetPublicRoomsAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<RoomRow>>("/api/rooms/public", false, cancellationToken);

    public Task<ApiResult<JoinRoomResponse>> JoinRoomAsync(string roomCode, string nickname, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, JoinRoomResponse>(
            "/api/rooms/join",
            new { roomCode, nickname },
            true,
            cancellationToken);

    public Task<ApiResult<List<RoomPlayerRow>>> GetRoomPlayersAsync(string roomCode, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<RoomPlayerRow>>($"/api/rooms/{roomCode}/players", false, cancellationToken);

    public Task<ApiResult<ApiSimpleResponse>> LeaveRoomAsync(string roomCode, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>($"/api/rooms/{roomCode}/leave", new { }, true, cancellationToken);

    // -------------------------
    // Game
    // -------------------------
    public Task<ApiResult<ApiSimpleResponse>> StartGameAsync(string roomCode, int questionCount = 10, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>($"/api/game/{roomCode}/start", new { questionCount }, true, cancellationToken);

    public Task<ApiResult<CurrentQuestionResponse>> GetCurrentQuestionAsync(string roomCode, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<CurrentQuestionResponse>($"/api/game/{roomCode}/current-question", false, cancellationToken);

    public Task<ApiResult<ApiSimpleResponse>> SubmitAnswerAsync(string roomCode, int roomPlayerId, int questionId, int optionId, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>(
            $"/api/game/{roomCode}/answer",
            new { roomPlayerId, questionId, optionId },
            false,
            cancellationToken);

    public Task<ApiResult<ScoreboardResponse>> GetScoreboardAsync(string roomCode, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<ScoreboardResponse>($"/api/game/{roomCode}/scoreboard", false, cancellationToken);

    public Task<ApiResult<List<TopPlayerRow>>> GetTopPlayersAsync(int limit = 10, CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<List<TopPlayerRow>>($"/api/game/top-players?limit={limit}", false, cancellationToken);

    // -------------------------
    // Profile / stats
    // -------------------------
    public Task<ApiResult<UserStatsResponse>> GetMyStatsAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetAsync<UserStatsResponse>("/api/users/me/stats", true, cancellationToken);

    public Task<ApiResult<ApiSimpleResponse>> UpdateProfileAsync(string username, string fullName, string email, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<object, ApiSimpleResponse>("/api/users/me/profile", new { username, fullName, email }, true, cancellationToken);

    public Task<ApiResult<ApiSimpleResponse>> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
        apiClient.PutAsync<object, ApiSimpleResponse>("/api/users/me/password", new { currentPassword, newPassword }, true, cancellationToken);

    // -------------------------
    // Assistant
    // -------------------------
    public Task<ApiResult<ApiSimpleResponse>> AskAssistantAsync(string message, CancellationToken cancellationToken = default) =>
        apiClient.PostAsync<object, ApiSimpleResponse>("/api/assistant/chat", new { message, history = new object[0] }, true, cancellationToken);
}
