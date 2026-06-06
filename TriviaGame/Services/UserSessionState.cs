using Models;

namespace TriviaGame.Services
{
    public sealed class UserSessionState
    {
        public int? CurrentUserId { get; set; }
        public UserRole CurrentRole { get; set; } = UserRole.User;
    }
}
