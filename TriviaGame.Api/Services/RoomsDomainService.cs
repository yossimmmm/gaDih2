using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// שירותי הדומיין של חדרים.
// כאן נמצאת הלוגיקה העסקית, בעוד שה־controller רק קורא לשיטות האלה.
public sealed class RoomsDomainService
{
    // מחזיר את קטלוג סוגי השאלות, כדי למלא את ה־picker במסך יצירת חדר.
    public async Task<List<QuestionType>> GetQuestionTypesAsync()
    {
        var questionTypeDb = new QuestionTypeDB();
        return await questionTypeDb.GetAllAsync();
    }

    // יוצר חדר חדש עבור המשתמש שמבצע את הבקשה.
    public async Task<(bool Ok, string Message, Room? Room)> CreateRoomAsync(int userId, CreateRoomRequest req)
    {
        // מוודאים שהשם תקין לפני שמגיעים למסד.
        var (validName, nameError) = ValidationHelper.ValidateRoomName(req.RoomName);
        if (!validName)
            return (false, nameError, null);

        var roomDb = new RoomDB();
        var room = await roomDb.CreateRoomAsync(req.RoomName.Trim(), userId, req.IsPublic, req.QuestionTypeId);
        return room is null
            ? (false, "Failed to create room.", null)
            : (true, "Room created.", room);
    }

    // מצרף שחקן לחדר לפי קוד חדר, ושומר את הקשר בין userId לחדר.
    public async Task<(bool Ok, string Message, RoomPlayer? Player, Room? Room)> JoinRoomAsync(int userId, JoinRoomRequest req)
    {
        var roomCode = (req.RoomCode ?? "").Trim().ToUpperInvariant();
        var (validCode, codeError) = ValidationHelper.ValidateRoomCode(roomCode);
        if (!validCode)
            return (false, codeError, null, null);

        if (!string.IsNullOrWhiteSpace(req.Nickname))
        {
            var (validNick, nickError) = ValidationHelper.ValidateNickname(req.Nickname);
            if (!validNick)
                return (false, nickError, null, null);
        }

        var roomDb = new RoomDB();
        var room = await roomDb.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return (false, "Room not found or inactive.", null, null);

        var nick = (req.Nickname ?? "").Trim();
        var player = await roomDb.JoinRoomAsync(room.RoomID, userId, nick);
        return player is null
            ? (false, "Failed to join room.", null, room)
            : (true, "Joined room.", player, room);
    }

    // חיפוש בסיסי לפי קוד חדר, כי הרבה פעולות אחרות מתחילות מאותו קוד.
    public async Task<Room?> GetRoomByCodeAsync(string roomCode)
    {
        var roomDb = new RoomDB();
        return await roomDb.GetRoomByCodeAsync((roomCode ?? "").Trim().ToUpperInvariant());
    }

    // מחזיר את החדרים הציבוריים הפעילים, בלי סינון לפי משתמש.
    public async Task<List<Room>> GetPublicRoomsAsync()
    {
        var roomDb = new RoomDB();
        return await roomDb.GetPublicRoomsAsync();
    }

    // מחזיר את כל השחקנים שכבר קיימים בחדר.
    public async Task<List<RoomPlayer>> GetPlayersAsync(int roomId)
    {
        var roomDb = new RoomDB();
        return await roomDb.GetPlayersAsync(roomId);
    }

    // מסיר שחקן מחדר לפי roomId ו־userId.
    public async Task<bool> LeaveRoomAsync(int roomId, int userId)
    {
        var roomDb = new RoomDB();
        return await roomDb.RemovePlayerAsync(roomId, userId);
    }
}
