using Models;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace DBL
{
    public class UserDB
    {
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        public async Task<User?> GetByIdAsync(int userId)
        {
            if (userId <= 0) return null;
            return await GetSingleUserAsync("WHERE user_id = @value", userId);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await GetSingleUserAsync("WHERE email = @value", email.Trim().ToLowerInvariant());
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            return await GetSingleUserAsync("WHERE username = @value", username.Trim());
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            const string sql = @"
                SELECT user_id, username, full_name, email, password_hash, role
                FROM users
                ORDER BY user_id;";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                users.Add(MapUser(reader));
            return users;
        }

        public async Task<bool> UpdateProfileAsync(int userId, string username, string fullName, string email)
        {
            if (userId <= 0) return false;
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            const string sql = @"
                UPDATE users
                SET username = @username, full_name = @full_name, email = @email
                WHERE user_id = @user_id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", (username ?? "").Trim());
            cmd.Parameters.AddWithValue("@full_name", (fullName ?? "").Trim());
            cmd.Parameters.AddWithValue("@email", (email ?? "").Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@user_id", userId);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, UserRole role)
        {
            if (userId <= 0) return false;
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            const string sql = @"UPDATE users SET role = @role WHERE user_id = @user_id;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@role", ToDbRole(role));
            cmd.Parameters.AddWithValue("@user_id", userId);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> UpdateUserByAdminAsync(int userId, string username, string fullName, string email, UserRole role)
        {
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
            cmd.Parameters.AddWithValue("@username", (username ?? "").Trim());
            cmd.Parameters.AddWithValue("@full_name", (fullName ?? "").Trim());
            cmd.Parameters.AddWithValue("@email", (email ?? "").Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("@role", ToDbRole(role));
            cmd.Parameters.AddWithValue("@user_id", userId);
            return await cmd.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            if (userId <= 0) return false;
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            const string deleteResetsSql = @"DELETE FROM password_reset_tokens WHERE user_id = @user_id;";
            await using (var deleteResets = new MySqlCommand(deleteResetsSql, conn, (MySqlTransaction)tx))
            {
                deleteResets.Parameters.AddWithValue("@user_id", userId);
                await deleteResets.ExecuteNonQueryAsync();
            }

            const string deleteSessionsSql = @"DELETE FROM user_sessions WHERE user_id = @user_id;";
            await using (var deleteSessions = new MySqlCommand(deleteSessionsSql, conn, (MySqlTransaction)tx))
            {
                deleteSessions.Parameters.AddWithValue("@user_id", userId);
                await deleteSessions.ExecuteNonQueryAsync();
            }

            const string deleteUserSql = @"DELETE FROM users WHERE user_id = @user_id;";
            await using var deleteUser = new MySqlCommand(deleteUserSql, conn, (MySqlTransaction)tx);
            deleteUser.Parameters.AddWithValue("@user_id", userId);
            var rows = await deleteUser.ExecuteNonQueryAsync();

            if (rows != 1)
            {
                await tx.RollbackAsync();
                return false;
            }

            await tx.CommitAsync();
            return true;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
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
            await cmd.ExecuteNonQueryAsync();
            return await GetByEmailAsync(u.Email);
        }

        public async Task<string> CreatePasswordResetTokenAsync(int userId, TimeSpan ttl)
        {
            if (userId <= 0) throw new ArgumentException("Invalid user id.", nameof(userId));
            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var tokenHash = Sha256Hex(rawToken);
            var expiresAt = DateTime.UtcNow.Add(ttl);

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

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
            return rawToken;
        }

        public async Task<bool> ResetPasswordByTokenAsync(string token, string newPasswordHash)
        {
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

            const string markUsedSql = @"UPDATE password_reset_tokens SET used_at = UTC_TIMESTAMP() WHERE token_hash = @token_hash;";
            await using (var usedCmd = new MySqlCommand(markUsedSql, conn, (MySqlTransaction)tx))
            {
                usedCmd.Parameters.AddWithValue("@token_hash", tokenHash);
                await usedCmd.ExecuteNonQueryAsync();
            }

            const string clearSessionsSql = @"DELETE FROM user_sessions WHERE user_id = @user_id;";
            await using (var clearSessionsCmd = new MySqlCommand(clearSessionsSql, conn, (MySqlTransaction)tx))
            {
                clearSessionsCmd.Parameters.AddWithValue("@user_id", userId);
                await clearSessionsCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            return true;
        }

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

        private static UserRole ParseRole(string? raw) =>
            raw?.ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin,
                "manager" => UserRole.Manager,
                _ => UserRole.User
            };

        private static string ToDbRole(UserRole role) =>
            role switch
            {
                UserRole.Admin => "Admin",
                UserRole.Manager => "Manager",
                _ => "User"
            };

        private static string Sha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
