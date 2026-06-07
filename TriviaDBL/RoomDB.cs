using Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DBL
{
    public class RoomDB
    {
        // מחרוזת חיבור משותפת לכל שאילתות החדרים.
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        public async Task<Room?> GetRoomByCodeAsync(string roomCode)
        {
            // קוד חדר ריק או null לא יכול לזהות חדר.
            // #roomdb #room-code #search
            if (string.IsNullOrWhiteSpace(roomCode))
                return null;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // מנרמלים את קוד החדר לאותיות גדולות לפני החיפוש.
                const string sql = @"
SELECT room_id, room_code, room_name, host_id, is_active, is_public, question_type_id, created_at
FROM rooms
WHERE room_code = @code
LIMIT 1;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@code", roomCode.Trim().ToUpperInvariant());

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return null;

                // כל עמודה שנבחרה מועתקת למודל Room.
                return new Room
                {
                    RoomID = Convert.ToInt32(reader["room_id"]),
                    RoomCode = Convert.ToString(reader["room_code"]) ?? "",
                    RoomName = Convert.ToString(reader["room_name"]) ?? "",
                    HostID = Convert.ToInt32(reader["host_id"]),
                    IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                    IsPublic = Convert.ToInt32(reader["is_public"]) == 1,
                    QuestionTypeID = reader.IsDBNull(reader.GetOrdinal("question_type_id"))
                        ? (int?)null
                        : Convert.ToInt32(reader["question_type_id"]),
                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                };
            }
            catch (MySqlException ex)
            {
                // שגיאות מסד עוברות עטיפה עם הודעה ברורה יותר לשכבת השירות.
                throw new Exception($"Database error while fetching room: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching room: {ex.Message}", ex);
            }
        }

        public async Task<RoomPlayer?> JoinRoomAsync(int roomId, int userId, string nickname)
        {
            // משתמש צריך גם חדר תקין וגם חשבון תקין לפני הצטרפות.
            // #roomdb #join-room #groups
            if (roomId <= 0 || userId <= 0)
                return null;

            nickname = (nickname ?? "").Trim();
            if (nickname.Length == 0)
                nickname = $"Player{userId}";

            // שומרים על nickname קצר כדי שייכנס ל־UI ולעמודת המסד.
            if (nickname.Length > 50)
                nickname = nickname.Substring(0, 50);

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // קודם מנסים להכניס שורת שחקן. אילוץ ייחודיות מטפל בכפילויות.
                const string insertSql = @"
INSERT INTO room_players (room_id, user_id, nickname, last_seen)
VALUES (@room_id, @user_id, @nickname, NOW());";

                await using (var cmd = new MySqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@room_id", roomId);
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@nickname", nickname);

                    try
                    {
                        var rows = await cmd.ExecuteNonQueryAsync();
                        if (rows != 1)
                            return null;
                    }
                    catch (MySqlException ex)
                    {
                        // שגיאת MySQL 1062 אומרת שהשחקן כבר נמצא בחדר.
                        if (ex.Number != 1062)
                            throw new Exception($"Database error while joining room: {ex.Message}", ex);
                    }
                }

                // בין אם המשתמש חדש או קיים, מרעננים את ה־heartbeat.
                const string touchSql = @"
UPDATE room_players
SET last_seen = NOW()
WHERE room_id = @room_id AND user_id = @user_id;";

                await using (var touchCmd = new MySqlCommand(touchSql, conn))
                {
                    touchCmd.Parameters.AddWithValue("@room_id", roomId);
                    touchCmd.Parameters.AddWithValue("@user_id", userId);
                    await touchCmd.ExecuteNonQueryAsync();
                }

                // מחזירים את השורה הקנונית מהמסד אחרי ההצטרפות.
                const string selectSql = @"
SELECT room_player_id, room_id, user_id, nickname, joined_at
FROM room_players
WHERE room_id = @room_id AND user_id = @user_id
LIMIT 1;";

                await using (var cmd2 = new MySqlCommand(selectSql, conn))
                {
                    cmd2.Parameters.AddWithValue("@room_id", roomId);
                    cmd2.Parameters.AddWithValue("@user_id", userId);

                    await using var reader = await cmd2.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        return null;

                    return new RoomPlayer
                    {
                        RoomPlayerID = Convert.ToInt32(reader["room_player_id"]),
                        RoomID = Convert.ToInt32(reader["room_id"]),
                        UserID = Convert.ToInt32(reader["user_id"]),
                        Nickname = Convert.ToString(reader["nickname"]) ?? "",
                        JoinedAt = Convert.ToDateTime(reader["joined_at"])
                    };
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while joining room: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error joining room: {ex.Message}", ex);
            }
        }

        public async Task<List<RoomPlayer>> GetPlayersAsync(int roomId)
        {
            // אם roomId ריק/לא תקין, מחזירים רשימה ריקה.
            var result = new List<RoomPlayer>();
            if (roomId <= 0)
                return result;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // מחזירים את השחקנים לפי סדר ההצטרפות כדי שה־UI יראה רשימה יציבה.
                const string sql = @"
SELECT room_player_id, room_id, user_id, nickname, joined_at
FROM room_players
WHERE room_id = @room_id
ORDER BY joined_at ASC;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@room_id", roomId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new RoomPlayer
                    {
                        RoomPlayerID = Convert.ToInt32(reader["room_player_id"]),
                        RoomID = Convert.ToInt32(reader["room_id"]),
                        UserID = Convert.ToInt32(reader["user_id"]),
                        Nickname = Convert.ToString(reader["nickname"]) ?? "",
                        JoinedAt = Convert.ToDateTime(reader["joined_at"])
                    });
                }

                return result;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching players: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching players: {ex.Message}", ex);
            }
        }

        public async Task<Room?> CreateRoomAsync(string roomName, int hostId, bool isPublic, int? questionTypeId)
        {
            // מנקים את שם החדר לפני כתיבה למסד.
            // #roomdb #create-room #public-room #private-room
            roomName = (roomName ?? "").Trim();
            if (roomName.Length == 0 || hostId <= 0)
                return null;

            if (roomName.Length > 100)
                throw new ArgumentException("Room name cannot exceed 100 characters");

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // מייצרים קוד חדר קצר וקריא ומנסים שוב אם יש התנגשות.
                string code = "";
                var rng = new Random();

                for (int tries = 0; tries < 20; tries++)
                {
                    code = "";
                    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                    for (int i = 0; i < 6; i++)
                        code += chars[rng.Next(chars.Length)];

                    const string checkSql = "SELECT COUNT(*) FROM rooms WHERE room_code=@c";
                    await using var check = new MySqlCommand(checkSql, conn);
                    check.Parameters.AddWithValue("@c", code);
                    var count = Convert.ToInt32(await check.ExecuteScalarAsync());
                    if (count == 0) break;
                }

                if (string.IsNullOrEmpty(code))
                    throw new Exception("Failed to generate unique room code");

                    // חדרים חדשים מתחילים פעילים ויכולים להיות ציבוריים או פרטיים.
                const string insertSql = @"
INSERT INTO rooms (room_code, room_name, host_id, is_active, is_public, question_type_id, last_seen)
VALUES (@code, @name, @host, 1, @is_public, @question_type_id, NOW());";

                await using (var cmd = new MySqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@name", roomName);
                    cmd.Parameters.AddWithValue("@host", hostId);
                    cmd.Parameters.AddWithValue("@is_public", isPublic ? 1 : 0);
                    cmd.Parameters.AddWithValue("@question_type_id", (object?)questionTypeId ?? DBNull.Value);

                    var rows = await cmd.ExecuteNonQueryAsync();
                    if (rows != 1)
                        throw new Exception("Failed to create room");
                }

                return await GetRoomByCodeAsync(code);
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while creating room: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating room: {ex.Message}", ex);
            }
        }

        public async Task<int> DeleteExpiredRoomsAsync()
        {
            // האפליקציה מתייחסת לחדרים ישנים מאוד כמידע מת והורסת אותם.
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
DELETE FROM rooms
WHERE created_at < (NOW() - INTERVAL 3 HOUR);";

            await using var cmd = new MySqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> CleanupDisconnectedRoomsAsync()
        {
            // #room-cleanup #disconnect #heartbeat #last-seen
            // זו פעולת ניקוי כללית למצבים שבהם המשתמש נעלם בלי Leave מסודר.
            // לדוגמה: סגירת דפדפן, נפילת אינטרנט, כיבוי מחשב, או קריסת SignalR.
            // במקרה כזה OnDisconnectedAsync לא תמיד מספיק, ולכן מסתמכים גם על last_seen.
            var deleted = 0;

            // קודם מוחקים חדרים ישנים מאוד לפי created_at.
            // זה תופס חדרים שנשארו במסד יותר מדי זמן גם אם היו בהם פעימות לב בעבר.
            deleted += await DeleteExpiredRoomsAsync();

            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            // אחר כך מוחקים שחקנים שלא שלחו heartbeat בזמן האחרון.
            // אם משתמש לא שלח RoomHeartbeat יותר משתי דקות, הוא נחשב מנותק.
            deleted += await DeleteStaleRoomPlayersAsync(conn);

            // אחרי שמחקנו שחקנים מנותקים, ייתכן שנשארו חדרים בלי אף שחקן.
            // חדר בלי שחקנים הוא חדר מת ולכן מוחקים אותו.
            deleted += await DeleteEmptyRoomsAsync(conn);

            return deleted;
        }

        public async Task<List<Room>> GetPublicRoomsAsync()
        {
            // רק חדרים ציבוריים פעילים עם לפחות שחקן אחד עדכני מוצגים בלובי.
            // #roomdb #public-rooms #lobby
            var result = new List<Room>();

            try
            {
                // לפני שמציגים חדרים ציבוריים מנקים חדרים/שחקנים שמתו.
                // אותו ניקוי רץ גם ברקע דרך RoomCleanupService, אבל כאן יש שכבת הגנה נוספת.
                await CleanupDisconnectedRoomsAsync();

                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
SELECT r.room_id, r.room_code, r.room_name, r.host_id, r.is_active, r.is_public, r.question_type_id, r.created_at
FROM rooms r
JOIN (
    SELECT room_id, COUNT(*) AS player_count
    FROM room_players
    WHERE last_seen IS NOT NULL
      AND last_seen >= (NOW() - INTERVAL 2 MINUTE)
    GROUP BY room_id
) rp ON rp.room_id = r.room_id
WHERE r.is_active = 1
  AND r.is_public = 1
  AND rp.player_count > 0
ORDER BY r.created_at DESC;";

                await using var cmd = new MySqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new Room
                    {
                        RoomID = Convert.ToInt32(reader["room_id"]),
                        RoomCode = Convert.ToString(reader["room_code"]) ?? "",
                        RoomName = Convert.ToString(reader["room_name"]) ?? "",
                        HostID = Convert.ToInt32(reader["host_id"]),
                        IsActive = Convert.ToInt32(reader["is_active"]) == 1,
                        IsPublic = Convert.ToInt32(reader["is_public"]) == 1,
                        QuestionTypeID = reader.IsDBNull(reader.GetOrdinal("question_type_id"))
                            ? (int?)null
                            : Convert.ToInt32(reader["question_type_id"]),
                        CreatedAt = Convert.ToDateTime(reader["created_at"])
                    });
                }

                return result;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching public rooms: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching public rooms: {ex.Message}", ex);
            }
        }

        public async Task<bool> RemovePlayerAsync(int roomId, int userId)
        {
            // זו פעולת יציאה מחדר.
            if (roomId <= 0 || userId <= 0)
                return false;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
DELETE FROM room_players
WHERE room_id = @room_id AND user_id = @user_id;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@room_id", roomId);
                cmd.Parameters.AddWithValue("@user_id", userId);

                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while removing player: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error removing player: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteRoomAsync(int roomId)
        {
            // מוחקים את החדר עצמו, בדרך כלל כשהמארח סוגר אותו.
            if (roomId <= 0)
                return false;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
DELETE FROM rooms
WHERE room_id = @room_id;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@room_id", roomId);

                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while deleting room: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting room: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteRoomIfNoPlayersAsync(int roomId)
        {
            // עזר בטיחות: מוחק את החדר רק אם אין בו שחקנים.
            if (roomId <= 0)
                return false;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
DELETE r
FROM rooms r
LEFT JOIN room_players rp ON rp.room_id = r.room_id
WHERE r.room_id = @room_id
GROUP BY r.room_id
HAVING COUNT(rp.room_player_id) = 0;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@room_id", roomId);
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while deleting empty room: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting empty room: {ex.Message}", ex);
            }
        }

        private async Task<int> DeleteEmptyRoomsAsync(MySqlConnection conn)
        {
            // מוחק חדרים שאין בהם אף שחקן.
            const string sql = @"
DELETE r
FROM rooms r
LEFT JOIN room_players rp ON rp.room_id = r.room_id
WHERE rp.room_id IS NULL;";

            await using var cmd = new MySqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> DeleteStaleRoomPlayersAsync(MySqlConnection conn)
        {
            // שחקנים שהפסיקו לשלוח heartbeat נחשבים מנותקים.
            const string sql = @"
DELETE FROM room_players
WHERE last_seen IS NULL
   OR last_seen < (NOW() - INTERVAL 2 MINUTE);";

            await using var cmd = new MySqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> UpdateRoomLastSeenAsync(string roomCode)
        {
            // heartbeat ברמת חדר שומר עליו חי בלובי.
            if (string.IsNullOrWhiteSpace(roomCode))
                return false;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
UPDATE rooms
SET last_seen = NOW()
WHERE room_code = @code;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@code", roomCode.Trim().ToUpperInvariant());
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while updating room heartbeat: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating room heartbeat: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateRoomPlayerLastSeenAsync(string roomCode, int userId)
        {
            // heartbeat של שחקן נשמר בנפרד כדי לדעת מי עדיין פעיל בתוך החדר.
            if (string.IsNullOrWhiteSpace(roomCode) || userId <= 0)
                return false;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
UPDATE room_players rp
JOIN rooms r ON r.room_id = rp.room_id
SET rp.last_seen = NOW()
WHERE r.room_code = @code AND rp.user_id = @user_id;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@code", roomCode.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@user_id", userId);

                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while updating player heartbeat: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating player heartbeat: {ex.Message}", ex);
            }
        }
    }
}
