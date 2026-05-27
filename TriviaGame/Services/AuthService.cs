using DBL;
using Microsoft.AspNetCore.Http;
using Models;
using System.Threading.Tasks;

namespace TriviaGame.Services
{
    public sealed class AuthService
    {
        // גישה ל-HttpContext כדי לקרוא עוגיות בקשת משתמש נוכחי
        private readonly IHttpContextAccessor _httpContextAccessor;
        // שכבת סשנים למסד הנתונים
        private readonly SessionDB _sessionDb = new();

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(int UserId, UserRole Role)?> TryGetAuthStateFromHttpContextAsync()
        {
            // שליפת הקונטקסט של הבקשה הנוכחית
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
                return null;

            // אם אין טוקן סשן - המשתמש נחשב לא מחובר
            if (!ctx.Request.Cookies.TryGetValue("session_token", out var token) || string.IsNullOrWhiteSpace(token))
                return (0, UserRole.User);

            // בדיקת תקינות הטוקן מול טבלת סשנים
            var userId = await _sessionDb.GetUserIdByTokenAsync(token);
            if (!userId.HasValue)
                return (0, UserRole.User);

            // שליפת תפקיד המשתמש מהמסד
            var userDb = new UserDB();
            var user = await userDb.GetByIdAsync(userId.Value);
            return (userId.Value, user?.Role ?? UserRole.User);
        }

        public async Task<int?> TryGetUserIdFromHttpContextAsync()
        {
            // קיצור דרך: החזרת מזהה משתמש בלבד מתוך מצב האימות
            var state = await TryGetAuthStateFromHttpContextAsync();
            return state?.UserId;
        }
    }
}
