using Models;

namespace TriviaGame.Services
{
    // מצב session קל ופשוט ששמור לזמן החיים של הבקשה/ה-scope.
    public sealed class UserSessionState
    {
        // המזהה של המשתמש המחובר, אם כבר זוהה.
        public int? CurrentUserId { get; set; }
        // התפקיד הנוכחי של המשתמש לצורך בדיקות הרשאה.
        public UserRole CurrentRole { get; set; } = UserRole.User;
    }
}
