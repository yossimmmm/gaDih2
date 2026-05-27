using Models;

namespace TriviaGame.Services
{
    // מצב סשן בצד Blazor:
    // נשמר בזיכרון השרת עבור ה-circuit הנוכחי ומשמש ניווט/הרשאות ב-UI.
    public sealed class UserSessionState
    {
        // מזהה המשתמש המחובר כרגע (null = לא מחובר).
        public int? CurrentUserId { get; set; }
        // תפקיד המשתמש המחובר (User/Manager/Admin) לשליטה בהרשאות צד לקוח.
        public UserRole CurrentRole { get; set; } = UserRole.User;
    }
}
