using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace DBL
{
    // שכבת אחסון ואימות של טוקני session.
    // כאן יוצרים, מאמתים ומוחקים טוקנים שנשמרים במסד כדי לזהות משתמש מחובר.
    public class SessionDB
    {
        // מחרוזת חיבור משותפת למסד המקומי.
        // כל פעולה במחלקה הזאת פותחת חיבור חדש, משתמשת בו ואז סוגרת אותו.
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        // יוצר טוקן session אקראי למשתמש ושומר גם את זמן התפוגה שלו.
        // זהו המנגנון שמחליף session-cookies: השרת שומר רשומה, והלקוח שומר את הטוקן.
        public async Task<string> CreateSessionAsync(int userId, TimeSpan lifetime)
        {
            // #sessiondb #cookie #login
            // ה-DB שומר את ה-session כדי שאפשר יהיה לבטל אותו גם אם השרת רץ מחדש.
            if (userId <= 0)
                throw new ArgumentException("Invalid user id", nameof(userId));

            // token הוא המזהה שהלקוח יקבל ויצטרך לשלוח בחזרה בבקשות עתידיות.
            var token = Guid.NewGuid().ToString("N");
            // expiresAt קובע עד מתי הטוקן נחשב תקין.
            var expiresAt = DateTime.UtcNow.Add(lifetime);

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            // user_sessions שומרת את הקשר בין טוקן למשתמש.
            // last_seen מתעדכן כשמזהים שהסשן עדיין בשימוש.
            // בלי הטבלה הזו היינו תלויים רק בזיכרון של השרת, וזה לא היה שורד restart.
            const string sql = @"
INSERT INTO user_sessions (session_token, user_id, expires_at, last_seen)
VALUES (@token, @user_id, @expires_at, NOW());";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);
            cmd.Parameters.AddWithValue("@user_id", userId);
            cmd.Parameters.AddWithValue("@expires_at", expiresAt);

            // INSERT יוצר את הסשן החדש במסד.
            await cmd.ExecuteNonQueryAsync();
            return token;
        }

        // מחזירה את ה-userId עבור טוקן תקין שעדיין לא פג תוקף.
        // אם הטוקן לא קיים, ריק, או פג תוקף, מחזירים null.
        public async Task<int?> GetUserIdByTokenAsync(string token)
        {
            // #sessiondb #auth-me #session_token
            if (string.IsNullOrWhiteSpace(token))
                return null;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            // בודקים רק טוקנים קיימים שעדיין לא עברו את זמן התפוגה שלהם.
            // זהו המסנן המרכזי של כל בדיקת auth בצד השרת.
            const string sql = @"
SELECT user_id
FROM user_sessions
WHERE session_token = @token
  AND expires_at > NOW()
LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);

            // ExecuteScalarAsync מחזירה ערך אחד: כאן user_id אם הטוקן תקין.
            var obj = await cmd.ExecuteScalarAsync();
            if (obj == null || obj == DBNull.Value)
                return null;

            // אם מצאנו סשן תקין, מרעננים last_seen כדי לסמן שהוא עדיין פעיל.
            // זה עוזר לדעת אם המשתמש עדיין משתמש במערכת.
            await using var touch = new MySqlCommand(
                "UPDATE user_sessions SET last_seen = NOW() WHERE session_token = @token;", conn);
            touch.Parameters.AddWithValue("@token", token);
            await touch.ExecuteNonQueryAsync();

            return Convert.ToInt32(obj);
        }

        // מוחקת טוקן session כשהמשתמש עושה logout.
        // אחרי המחיקה הטוקן כבר לא יכול לשמש לאימות.
        public async Task DeleteSessionAsync(string token)
        {
            // #sessiondb #logout #cookie
            if (string.IsNullOrWhiteSpace(token))
                return;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            // מוחקים את הרשומה של הטוקן מהטבלה.
            // אחרי המחיקה token ישן לא יוכל לעבור auth me.
            const string sql = "DELETE FROM user_sessions WHERE session_token = @token;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@token", token);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
