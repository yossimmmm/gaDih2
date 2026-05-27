using DBL;
using Microsoft.AspNetCore.Http;
using Models;
using System.Threading.Tasks;

namespace TriviaGame.Services
{
    public sealed class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SessionDB _sessionDb = new();

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(int UserId, UserRole Role)?> TryGetAuthStateFromHttpContextAsync()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null)
                return null;

            if (!ctx.Request.Cookies.TryGetValue("session_token", out var token) || string.IsNullOrWhiteSpace(token))
                return (0, UserRole.User);

            var userId = await _sessionDb.GetUserIdByTokenAsync(token);
            if (!userId.HasValue)
                return (0, UserRole.User);

            var userDb = new UserDB();
            var user = await userDb.GetByIdAsync(userId.Value);
            return (userId.Value, user?.Role ?? UserRole.User);
        }

        public async Task<int?> TryGetUserIdFromHttpContextAsync()
        {
            var state = await TryGetAuthStateFromHttpContextAsync();
            return state?.UserId;
        }
    }
}
