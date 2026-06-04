using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace DBL
{
    // שכבת אחסון ואימות של טוקני session.
    // כאן שומרים session ב־DB וגם בודקים אם הוא עדיין בתוקף.
    public class SessionDB
    {
        // מחרוזת חיבור משותפת למסד המקומי.
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        // יוצר טוקן סשן אקראי למשתמש ושומר גם את זמן התפוגה שלו.
        public async Task<string> CreateSessionAsync(int userId, TimeSpan lifetime)
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user id", nameof(userId));

            var token = Guid.NewGuid().ToString("N");
            var expiresAt = DateTime.UtcNow.Add(lifetime);

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
INSERT INTO user_sessions (session_token, user_id, expires_at, last_seen)
VALUES (@token, @user_id, @expires_at, NOW());";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.Parameters.AddWithValue("@expires_at", expiresAt);

            await cmd.ExecuteNonQueryAsync();
            return token;
        }

        // מחזירה את ה-userId עבור טוקן תקין שעדיין לא פג תוקף.
        public async Task<int?> GetUserIdByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
SELECT user_id
FROM user_sessions
WHERE session_token = @token
  AND expires_at > NOW()
LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);

            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value)
                return null;

            // מרעננים last_seen כדי לסמן שהסשן עדיין פעיל.
            await using var touch = new MySqlCommand(
                "UPDATE user_sessions SET last_seen = NOW() WHERE session_token = @token;", conn);
            touch.Parameters.AddWithValue("@token", token);
            await touch.ExecuteNonQueryAsync();

            return Convert.ToInt32(obj);
        }

        // מוחקת טוקן סשן כשהמשתמש עושה logout.
        public async Task DeleteSessionAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = "DELETE FROM user_sessions WHERE session_token = @token;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
