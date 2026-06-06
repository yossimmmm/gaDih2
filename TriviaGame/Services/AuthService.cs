using DBL;
using Microsoft.AspNetCore.Http;
using Models;
using System.Threading.Tasks;

namespace TriviaGame.Services
{
    // שירות קטן שמרכז את קריאת מצב האימות מתוך ה-HttpContext.
    public sealed class AuthService
    {
        // מאפשר גישה לבקשה הנוכחית בלי להעביר HttpContext ידנית בכל מקום.
        private readonly IHttpContextAccessor _httpContextAccessor;
        // שכבת ה-DB שמחזיקה session tokens.
        private readonly SessionDB _sessionDb = new();

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // מחזיר את המשתמש והתפקיד שלו לפי ה-session cookie של הבקשה.
        public async Task<(int UserId, UserRole Role)?> TryGetAuthStateFromHttpContextAsync()
        {
            // אם אין בכלל HttpContext, אין בקשה פעילה לקרוא ממנה.
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
                return null;

            // ה-cookie מחזיק טוקן session שממנו מזהים את המשתמש.
            if (!ctx.Request.Cookies.TryGetValue("session_token", out var token) || string.IsNullOrWhiteSpace(token))
                return (0, UserRole.User);

            // מאמתים שהטוקן קיים ותקף במסד.
            var userId = await _sessionDb.GetUserIdByTokenAsync(token);
            if (!userId.HasValue)
                return (0, UserRole.User);

            // אחרי שיש userId, טוענים את המשתמש כדי לדעת גם את התפקיד שלו.
            var userDb = new UserDB();
            var user = await userDb.GetByIdAsync(userId.Value);
            return (userId.Value, user?.Role ?? UserRole.User);
        }

        // עזר נוח למקרים שבהם צריך רק את המזהה ולא את כל המצב.
        public async Task<int?> TryGetUserIdFromHttpContextAsync()
        {
            var state = await TryGetAuthStateFromHttpContextAsync();
            return state?.UserId;
        }
    }
}
