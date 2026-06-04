using Models;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace DBL
{
    public class UserDB
    {
        // מחרוזת חיבור מרכזית לכל שאילתות המשתמשים במחלקה הזו.
        // הקוד משתמש במסד MySQL מקומי בשם trivia_game.
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        public async Task<User?> GetByIdAsync(int userId)
        {
            // מזהים לא תקינים נדחים מיד כדי לא לפנות למסד על קלט חסר משמעות.
            if (userId <= 0) return null;

            // המתודה העוזרת למטה עושה את השאילתה בפועל; כאן רק בוחרים את תנאי ה־WHERE.
            return await GetSingleUserAsync("WHERE user_id = @value", userId);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            // חיפוש לפי אימייל מנורמל ל־lowercase ול־trim כדי שאותו חשבון יזוהה תמיד אותו דבר.
            if (string.IsNullOrWhiteSpace(email)) return null;

            return await GetSingleUserAsync("WHERE email = @value", email.Trim().ToLowerInvariant());
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            // חיפוש לפי username פשוט יותר, אבל עדיין עושים trim לפני השאילתה.
            if (string.IsNullOrWhiteSpace(username)) return null;

            return await GetSingleUserAsync("WHERE username = @value", username.Trim());
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            // זו שאילתה בסגנון אדמין: היא מחזירה את כל המשתמשים בטבלה.
            var users = new List<User>();
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
                SELECT user_id, username, full_name, email, password_hash, role
                FROM users
                ORDER BY user_id;";

            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            // כל שורה מה־SQL הופכת לאובייקט User אחד.
            while (await reader.ReadAsync())
                users.Add(MapUser(reader));

            return users;
        }

        public async Task<bool> UpdateProfileAsync(int userId, string username, string fullName, string email)
        {
            // אפשר לעדכן רק אם userId תקין.
            if (userId <= 0) return false;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
                UPDATE users
                SET username = @username, full_name = @full_name, email = @email
                WHERE user_id = @user_id;";

            await using var cmd = new MySqlCommand(sql, conn);

            // פרמטרים מגינים על השאילתה וגם מנרמלים את הערכים לפני השמירה.
            cmd.Parameters.AddWithValue("@username", (username ?? "").Trim());
            cmd.Parameters.AddWithValue("@full_name", (fullName ?? "").Trim());
            cmd.Parameters.AddWithValue("@email", (email ?? "").Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@user_id", userId);

            // מצפים לשינוי של שורה אחת בדיוק כשה־ID קיים.
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, UserRole role)
        {
            // המתודה הזו משנה רק את עמודת ה־role ולא נוגעת בשאר הפרופיל.
            if (userId <= 0) return false;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"UPDATE users SET role = @role WHERE user_id = @user_id;";
            await using var cmd = new MySqlCommand(sql, conn);

            // במסד role נשמר כמחרוזת, לכן ממירים את ה־enum קודם.
            cmd.Parameters.AddWithValue("@role", ToDbRole(role));
            cmd.Parameters.AddWithValue("@user_id", userId);

            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> UpdateUserByAdminAsync(int userId, string username, string fullName, string email, UserRole role)
        {
            // עריכת אדמין משתמשת באותו נתיב עדכון, אבל מותר לה גם לשנות role.
            if (userId <= 0) return false;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
                UPDATE users
                SET username = @username,
                    full_name = @full_name,
                    email = @email,
                    role = @role
                WHERE user_id = @user_id;";

            await using var cmd = new MySqlCommand(sql, conn);

            // מנרמלים את כל הקלט לפני הכתיבה חזרה לטבלה.
            cmd.Parameters.AddWithValue("@username", (username ?? "").Trim());
            cmd.Parameters.AddWithValue("@full_name", (fullName ?? "").Trim());
            cmd.Parameters.AddWithValue("@email", (email ?? "").Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@role", ToDbRole(role));
            cmd.Parameters.AddWithValue("@user_id", userId);

            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            // מחיקת משתמש חייבת לנקות גם טוקני איפוס וסשנים, לכן עוטפים הכול בטרנזקציה.
            if (userId <= 0) return false;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            // מוחקים קודם טוקני איפוס סיסמה כדי שלא יישאר טוקן ישן אחרי המחיקה.
            const string deleteResetsSql = @"DELETE FROM password_reset_tokens WHERE user_id = @user_id;";
            await using (var deleteResets = new MySqlCommand(deleteResetsSql, conn, (MySqlTransaction)tx))
            {
                deleteResets.Parameters.AddWithValue("@user_id", userId);
                await deleteResets.ExecuteNonQueryAsync();
            }

            // אחר כך מוחקים סשנים פעילים כדי שהחשבון המחוק לא ימשיך להשתמש במצב התחברות ישן.
            const string deleteSessionsSql = @"DELETE FROM user_sessions WHERE user_id = @user_id;";
            await using (var deleteSessions = new MySqlCommand(deleteSessionsSql, conn, (MySqlTransaction)tx))
            {
                deleteSessions.Parameters.AddWithValue("@user_id", userId);
                await deleteSessions.ExecuteNonQueryAsync();
            }

            // בסוף מוחקים את שורת המשתמש עצמה.
            const string deleteUserSql = @"DELETE FROM users WHERE user_id = @user_id;";
            await using var deleteUser = new MySqlCommand(deleteUserSql, conn, (MySqlTransaction)tx);
            deleteUser.Parameters.AddWithValue("@user_id", userId);

            var rows = await deleteUser.ExecuteNonQueryAsync();
            if (rows != 1)
            {
                // אם המחיקה המרכזית נכשלת, מחזירים rollback לכל פעולות הניקוי.
                await tx.RollbackAsync();
                return false;
            }

            await tx.CommitAsync();
            return true;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
            // שינויי סיסמה נשמרים כ־hash בלבד, לא כטקסט רגיל.
            if (userId <= 0 || string.IsNullOrWhiteSpace(passwordHash)) return false;

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"UPDATE users SET password_hash = @password_hash WHERE user_id = @user_id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
            cmd.Parameters.AddWithValue("@user_id", userId);

            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        public async Task<User?> InsertUserAsync(User u)
        {
            // ההכנסה למסד דורשת את השדות החשובים.
            if (u == null) throw new ArgumentNullException(nameof(u));
            if (string.IsNullOrWhiteSpace(u.Email)) throw new ArgumentException("Email is required", nameof(u));
            if (string.IsNullOrWhiteSpace(u.PasswordHash)) throw new ArgumentException("Password hash is required", nameof(u));

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO users (username, full_name, email, password_hash, role)
                VALUES (@username, @full_name, @email, @password_hash, @role);";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", (u.Username ?? "").Trim());
            cmd.Parameters.AddWithValue("@full_name", (u.FullName ?? "").Trim());
            cmd.Parameters.AddWithValue("@email", u.Email.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@password_hash", u.PasswordHash);
            cmd.Parameters.AddWithValue("@role", ToDbRole(u.Role));

            // ה־insert לא מחזיר את השורה, לכן שולפים אותה שוב לפי אימייל אחרי השמירה.
            await cmd.ExecuteNonQueryAsync();
            return await GetByEmailAsync(u.Email);
        }

        public async Task<string> CreatePasswordResetTokenAsync(int userId, TimeSpan ttl)
        {
            // קישור האיפוס מכיל טוקן גולמי, אבל במסד נשמר רק ה־hash שלו.
            if (userId <= 0) throw new ArgumentException("Invalid user id.", nameof(userId));

            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var tokenHash = Sha256Hex(rawToken);
            var expiresAt = DateTime.UtcNow.Add(ttl);

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            // מוחקים טוקנים ישנים של אותו משתמש וגם טוקנים שפגו/שימשו כבר.
            const string clearSql = @"DELETE FROM password_reset_tokens WHERE user_id = @user_id OR expires_at < UTC_TIMESTAMP() OR used_at IS NOT NULL;";
            await using (var clearCmd = new MySqlCommand(clearSql, conn, (MySqlTransaction)tx))
            {
                clearCmd.Parameters.AddWithValue("@user_id", userId);
                await clearCmd.ExecuteNonQueryAsync();
            }

            const string insertSql = @"
                INSERT INTO password_reset_tokens (user_id, token_hash, expires_at)
                VALUES (@user_id, @token_hash, @expires_at);";

            await using (var insertCmd = new MySqlCommand(insertSql, conn, (MySqlTransaction)tx))
            {
                insertCmd.Parameters.AddWithValue("@user_id", userId);
                insertCmd.Parameters.AddWithValue("@token_hash", tokenHash);
                insertCmd.Parameters.AddWithValue("@expires_at", expiresAt);
                await insertCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            // The raw token is returned to the caller so it can be sent in email.
            return rawToken;
        }

        public async Task<bool> ResetPasswordByTokenAsync(string token, string newPasswordHash)
        {
            // The token must exist, still be unused, and still be within its expiry window.
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPasswordHash)) return false;

            var tokenHash = Sha256Hex(token.Trim());

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            int userId;
            const string getSql = @"
                SELECT user_id
                FROM password_reset_tokens
                WHERE token_hash = @token_hash AND used_at IS NULL AND expires_at > UTC_TIMESTAMP()
                LIMIT 1 FOR UPDATE;";

            await using (var getCmd = new MySqlCommand(getSql, conn, (MySqlTransaction)tx))
            {
                getCmd.Parameters.AddWithValue("@token_hash", tokenHash);

                // ExecuteScalarAsync מחזירה את user_id אם הטוקן תקין.
                var userObj = await getCmd.ExecuteScalarAsync();
                if (userObj is null || userObj == DBNull.Value)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                userId = Convert.ToInt32(userObj);
            }

            const string updateUserSql = @"UPDATE users SET password_hash = @password_hash WHERE user_id = @user_id;";
            await using (var updateUserCmd = new MySqlCommand(updateUserSql, conn, (MySqlTransaction)tx))
            {
                updateUserCmd.Parameters.AddWithValue("@password_hash", newPasswordHash);
                updateUserCmd.Parameters.AddWithValue("@user_id", userId);

                if (await updateUserCmd.ExecuteNonQueryAsync() != 1)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            // מסמנים את הטוקן כמשומש כדי שלא יהיה אפשר להשתמש בו שוב.
            const string markUsedSql = @"UPDATE password_reset_tokens SET used_at = UTC_TIMESTAMP() WHERE token_hash = @token_hash;";
            await using (var usedCmd = new MySqlCommand(markUsedSql, conn, (MySqlTransaction)tx))
            {
                usedCmd.Parameters.AddWithValue("@token_hash", tokenHash);
                await usedCmd.ExecuteNonQueryAsync();
            }

            // איפוס סיסמה צריך לבטל את כל הסשנים הישנים של אותו משתמש.
            const string clearSessionsSql = @"DELETE FROM user_sessions WHERE user_id = @user_id;";
            await using (var clearSessionsCmd = new MySqlCommand(clearSessionsSql, conn, (MySqlTransaction)tx))
            {
                clearSessionsCmd.Parameters.AddWithValue("@user_id", userId);
                await clearSessionsCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return true;
        }

        // עזר משותף לכל שאילתות "משתמש אחד בלבד".
        private async Task<User?> GetSingleUserAsync(string whereClause, object value)
        {
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            var sql = $@"
                SELECT user_id, username, full_name, email, password_hash, role
                FROM users
                {whereClause}
                LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@value", value);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return MapUser(reader);
        }

        // ממיר שורת SQL אחת למודל User שבשאר האפליקציה.
        private static User MapUser(DbDataReader reader)
        {
            return new User
            {
                UserID = Convert.ToInt32(reader["user_id"]),
                Username = Convert.ToString(reader["username"]) ?? "",
                FullName = Convert.ToString(reader["full_name"]) ?? "",
                Email = Convert.ToString(reader["email"]) ?? "",
                PasswordHash = Convert.ToString(reader["password_hash"]) ?? "",
                Role = ParseRole(reader["role"]?.ToString())
            };
        }

        // במסד התפקיד נשמר כטקסט, אז מתרגמים אותו חזרה ל־enum.
        private static UserRole ParseRole(string? raw) =>
            raw?.ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin,
                "manager" => UserRole.Manager,
                _ => UserRole.User
            };

        // זו ההמרה ההפוכה כששומרים enum חזרה ל־SQL.
        private static string ToDbRole(UserRole role) =>
            role switch
            {
                UserRole.Admin => "Admin",
                UserRole.Manager => "Manager",
                _ => "User"
            };

        // עזר פשוט ל־SHA-256 שמשמש בזרימת טוקן האיפוס.
        private static string Sha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
