using Models;

namespace TriviaGame.Api.Contracts;

// בקשות auth בסיסיות
public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Username, string FullName, string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);

// בקשות משתמש/אדמין
public sealed record UpdateProfileRequest(string Username, string FullName, string Email);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record UpdateRoleRequest(string Role);
public sealed record AdminUserUpdateRequest(string Username, string FullName, string Email, string Role);

// בקשות חדר/משחק
public sealed record CreateRoomRequest(string RoomName, bool IsPublic, int? QuestionTypeId);
public sealed record JoinRoomRequest(string RoomCode, string Nickname);
public sealed record StartGameRequest(int QuestionCount = 10);
public sealed record SubmitAnswerRequest(int RoomPlayerId, int QuestionId, int OptionId);

// בקשות יכולות ייחודיות
public sealed record AssistantAdviceRequest(Question Question);
public sealed record AssistantChatMessage(string Role, string Text);
public sealed record AssistantChatRequest(string Message, List<AssistantChatMessage>? History);

// תגובת auth אחידה למובייל
public sealed record AuthResultResponse(
    bool Ok,
    string Message,
    string Token,
    int UserId,
    string Username,
    string Role);

// תגובת me/profile למובייל
public sealed record CurrentUserResponse(
    bool Authenticated,
    int UserId,
    string Username,
    string FullName,
    string Email,
    string Role);
