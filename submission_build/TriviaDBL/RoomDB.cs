// TriviaDBL/RoomDB.cs
// Adds: GetRoomByCodeAsync, JoinRoomAsync, GetPlayersAsync
// Assumes you already have Models/Room.cs and a RoomPlayer model (included below if you don't)

using Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DBL
{
    public class RoomDB
    {
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        // ---------------------------
        // GetRoomByCodeAsync
        // ---------------------------
        public async Task<Room?> GetRoomByCodeAsync(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                return null;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

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

                return new Room
                {
                    RoomID = reader.GetInt32("room_id"),
                    RoomCode = reader.GetString("room_code"),
                    RoomName = reader.GetString("room_name"),
                    HostID = reader.GetInt32("host_id"),
                    IsActive = reader.GetBoolean("is_active"),
                    IsPublic = reader.GetBoolean("is_public"),
                    QuestionTypeID = reader.IsDBNull(reader.GetOrdinal("question_type_id"))
                        ? (int?)null
                        : reader.GetInt32("question_type_id"),
                    CreatedAt = reader.GetDateTime("created_at")
                };
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching room: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching room: {ex.Message}", ex);
            }
        }

        // ---------------------------
        // JoinRoomAsync
        // Inserts into room_players
        // Enforces unique(room_id,user_id) via DB constraint:
        // - If already exists: returns existing row
        // - If new: returns inserted row
        // ---------------------------
        public async Task<RoomPlayer?> JoinRoomAsync(int roomId, int userId, string nickname)
        {
            if (roomId <= 0 || userId <= 0)
                return null;

            nickname = (nickname ?? "").Trim();
            if (nickname.Length == 0)
                nickname = $"Player{userId}";
            
            // Validate nickname length (max 50 chars)
            if (nickname.Length > 50)
                nickname = nickname.Substring(0, 50);

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // First try insert, if duplicate -> fetch existing
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
                        // 1062 = duplicate key (uq_room_players room_id,user_id)
                        if (ex.Number != 1062)
                            throw new Exception($"Database error while joining room: {ex.Message}", ex);
                    }
                }

                // Touch last_seen on join (or re-join)
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

                // Return the player's row (either newly inserted or existing)
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
                        RoomPlayerID = reader.GetInt32("room_player_id"),
                        RoomID = reader.GetInt32("room_id"),
                        UserID = reader.GetInt32("user_id"),
                        Nickname = reader.GetString("nickname"),
                        JoinedAt = reader.GetDateTime("joined_at")
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

        // ---------------------------
        // GetPlayersAsync
        // Returns all room_players for a room
        // ---------------------------
        public async Task<List<RoomPlayer>> GetPlayersAsync(int roomId)
        {
            var result = new List<RoomPlayer>();
            if (roomId <= 0)
                return result;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

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
                        RoomPlayerID = reader.GetInt32("room_player_id"),
                        RoomID = reader.GetInt32("room_id"),
                        UserID = reader.GetInt32("user_id"),
                        Nickname = reader.GetString("nickname"),
                        JoinedAt = reader.GetDateTime("joined_at")
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
            roomName = (roomName ?? "").Trim();
            if (roomName.Length == 0 || hostId <= 0) 
                return null;

            // Validate room name length (max 100 chars)
            if (roomName.Length > 100)
                throw new ArgumentException("Room name cannot exceed 100 characters");

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // generate short code
                string code = "";
                var rng = new Random();

                for (int tries = 0; tries < 20; tries++)
                {
                    code = "";
                    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                    for (int i = 0; i < 6; i++)
                        code += chars[rng.Next(chars.Length)];

                    // ensure unique
                    const string checkSql = "SELECT COUNT(*) FROM rooms WHERE room_code=@c";
                    await using var check = new MySqlCommand(checkSql, conn);
                    check.Parameters.AddWithValue("@c", code);
                    var count = Convert.ToInt32(await check.ExecuteScalarAsync());
                    if (count == 0) break;
                }

                if (string.IsNullOrEmpty(code))
                    throw new Exception("Failed to generate unique room code");

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

                // return created room
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
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
DELETE FROM rooms
WHERE created_at < (NOW() - INTERVAL 3 HOUR);";

            await using var cmd = new MySqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Room>> GetPublicRoomsAsync()
        {
            var result = new List<Room>();

            try
            {
                await DeleteExpiredRoomsAsync();

                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                await DeleteStaleRoomPlayersAsync(conn);
                await DeleteEmptyRoomsAsync(conn);

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
                        RoomID = reader.GetInt32("room_id"),
                        RoomCode = reader.GetString("room_code"),
                        RoomName = reader.GetString("room_name"),
                        HostID = reader.GetInt32("host_id"),
                        IsActive = reader.GetBoolean("is_active"),
                        IsPublic = reader.GetBoolean("is_public"),
                        QuestionTypeID = reader.IsDBNull(reader.GetOrdinal("question_type_id"))
                            ? (int?)null
                            : reader.GetInt32("question_type_id"),
                        CreatedAt = reader.GetDateTime("created_at")
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
            const string sql = @"
DELETE FROM room_players
WHERE last_seen IS NULL
   OR last_seen < (NOW() - INTERVAL 2 MINUTE);";

            await using var cmd = new MySqlCommand(sql, conn);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> UpdateRoomLastSeenAsync(string roomCode)
        {
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
