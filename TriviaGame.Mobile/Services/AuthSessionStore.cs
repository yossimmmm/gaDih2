using Microsoft.Maui.Storage;

namespace TriviaGame.Mobile.Services;

public sealed class AuthSessionStore
{
    private const string TokenKey = "session_token";

    // שמירת token לאחר login מוצלח.
    public void SaveToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;
        Preferences.Default.Set(TokenKey, token.Trim());
    }

    // שליפת token קיים (אם יש).
    public string? GetToken()
    {
        var token = Preferences.Default.Get(TokenKey, "");
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    // ניקוי session בעת logout/401.
    public void Clear() => Preferences.Default.Remove(TokenKey);
}
