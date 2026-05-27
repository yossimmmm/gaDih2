using DBL;
using Models;

namespace TriviaGame.Api.Services;

public sealed class SessionTokenService
{
    // שליפת token מתוך Authorization header או cookie.
    public string? TryReadToken(HttpContext http)
    {
        var header = http.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header) &&
            header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var bearer = header["Bearer ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(bearer))
                return bearer;
        }

        return http.Request.Cookies.TryGetValue("session_token", out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;
    }

    // אימות token והחזרת אובייקט משתמש מלא.
    public async Task<User?> TryGetUserAsync(HttpContext http)
    {
        var token = TryReadToken(http);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var sessionDb = new SessionDB();
        var userId = await sessionDb.GetUserIdByTokenAsync(token);
        if (!userId.HasValue)
            return null;

        var userDb = new UserDB();
        return await userDb.GetByIdAsync(userId.Value);
    }

    // בדיקת הרשאת אדמין על בסיס session נוכחי.
    public async Task<bool> IsAdminAsync(HttpContext http)
    {
        var user = await TryGetUserAsync(http);
        return user is not null && user.Role == UserRole.Admin;
    }
}
