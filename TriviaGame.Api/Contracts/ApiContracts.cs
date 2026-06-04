using System.Collections.Generic;
using Models;

namespace TriviaGame.Api.Contracts;

// DTO = אובייקט העברת נתונים.
// אלה מחלקות קטנות שעוברות בין ה-MAUI לבין ה-API.
// הן לא מחזיקות לוגיקה, רק נתונים שמייצגים בקשה או תשובה.

// ----------------------------
// Auth
// ----------------------------

// בקשת התחברות: אימייל + סיסמה.
public sealed record LoginRequest(string Email, string Password);

// בקשת הרשמה: שם משתמש, שם מלא, אימייל וסיסמה.
public sealed record RegisterRequest(string Username, string FullName, string Email, string Password);

// בקשה לאיפוס סיסמה: רק אימייל, כדי לשלוח קישור/טוקן.
public sealed record ForgotPasswordRequest(string Email);

// בקשה להשלמת איפוס סיסמה: הטוקן שהתקבל והסיסמה החדשה.
public sealed record ResetPasswordRequest(string Token, string NewPassword);

// ----------------------------
// Profile / Admin user management
// ----------------------------

// עדכון פרופיל בסיסי של משתמש קיים.
public sealed record UpdateProfileRequest(int UserId, string Username, string FullName, string Email);

// שינוי סיסמה: סיסמה נוכחית + סיסמה חדשה.
public sealed record ChangePasswordRequest(int UserId, string CurrentPassword, string NewPassword);

// עדכון תפקיד של משתמש.
public sealed record UpdateRoleRequest(string Role);

// עדכון משתמש מתוך מסך ניהול: כולל גם role.
public sealed record AdminUserUpdateRequest(string Username, string FullName, string Email, string Role);

// ----------------------------
// Rooms / game flow
// ----------------------------

// בקשה ליצירת חדר חדש.
// QuestionTypeId הוא אופציונלי, כי חדר יכול להיות כללי או מסונן לפי נושא.
public sealed record CreateRoomRequest(int UserId, string RoomName, bool IsPublic, int? QuestionTypeId);

// בקשה להצטרפות לחדר קיים לפי קוד חדר וכינוי.
public sealed record JoinRoomRequest(int UserId, string RoomCode, string Nickname);

// בקשה להתחיל משחק בחדר.
// QuestionCount קובע כמה שאלות לטעון, וברירת המחדל היא 10.
public sealed record StartGameRequest(int UserId, int QuestionCount = 10);

// בקשה לשליחת תשובה לשאלה מסוימת.
public sealed record SubmitAnswerRequest(int RoomPlayerId, int QuestionId, int OptionId);

// ----------------------------
// Assistant
// ----------------------------

// בקשה לעוזר חכם עם שאלה אחת ספציפית.
public sealed record AssistantAdviceRequest(Question Question);

// הודעה אחת בתוך שיחה עם העוזר.
// Role בדרך כלל יהיה user / assistant / system.
public sealed record AssistantChatMessage(string Role, string Text);

// בקשת שיחה מלאה לעוזר.
// History מאפשר לשלוח גם את ההודעות הקודמות כדי לשמור הקשר.
public sealed record AssistantChatRequest(int UserId, string Message, List<AssistantChatMessage>? History);

// ----------------------------
// Auth responses
// ----------------------------

// תשובת הצלחה/כישלון מהתחברות או הרשמה.
// Token הוא הטוקן שהאפליקציה שומרת להמשך אימות.
public sealed record AuthResultResponse(
    bool Ok,
    string Message,
    string Token,
    int UserId,
    string Username,
    string Role);

// תשובת "מי המשתמש הנוכחי".
// משתמשים בזה כדי לדעת אם כבר מחובר משתמש, ומה הפרטים שלו.
public sealed record CurrentUserResponse(
    bool Authenticated,
    int UserId,
    string Username,
    string FullName,
    string Email,
    string Role);
