using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace DBL
{
    public class SessionDB
    {
        // מחרוזת חיבור למסד
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        public async Task<string> CreateSessionAsync(int userId, TimeSpan lifetime)
        {
            // ולידציה בסיסית למזהה משתמש
            if (userId <= 0)
                throw new ArgumentException("Invalid user id", nameof(userId));

            // יצירת טוקן ייחודי לסשן וחישוב זמן תפוגה
            var token = Guid.NewGuid().ToString("N");
            var expiresAt = DateTime.UtcNow.Add(lifetime);

            // פתיחת חיבור למסד
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            // שמירת סשן חדש במסד
            const string sql = @"
INSERT INTO user_sessions (session_token, user_id, expires_at, last_seen)
VALUES (@token, @user_id, @expires_at, NOW());";

            await using var cmd = new MySqlCommand(sql, conn);
            // הזרקת פרמטרים בצורה מאובטחת
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.Parameters.AddWithValue("@expires_at", expiresAt);

            // ביצוע שאילתה והחזרת הטוקן שנוצר
            await cmd.ExecuteNonQueryAsync();
            return token;
        }

        public async Task<int?> GetUserIdByTokenAsync(string token)
        {
            // אם הטוקן ריק אין משתמש מחובר
            if (string.IsNullOrWhiteSpace(token))
                return null;

            // פתיחת חיבור למסד
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            // שליפת מזהה משתמש עבור טוקן תקף בלבד
            const string sql = @"
SELECT user_id
FROM user_sessions
WHERE session_token = @token
  AND expires_at > NOW()
LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);

            // ניסיון קריאת מזהה המשתמש מהמסד
            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value)
                return null;

            // עדכון חותמת זמן פעילות אחרונה של הסשן
            await using var touch = new MySqlCommand(
                "UPDATE user_sessions SET last_seen = NOW() WHERE session_token = @token;", conn);
            touch.Parameters.AddWithValue("@token", token);
            await touch.ExecuteNonQueryAsync();

            // החזרת מזהה המשתמש שהתקבל
            return Convert.ToInt32(obj);
        }

        public async Task DeleteSessionAsync(string token)
        {
            // אם הטוקן ריק אין מה למחוק
            if (string.IsNullOrWhiteSpace(token))
                return;

            // פתיחת חיבור למסד
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            // מחיקת הסשן מהמסד
            const string sql = "DELETE FROM user_sessions WHERE session_token = @token;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
