using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

public sealed class RoomsDomainService
{
    // שליפת קטגוריות שאלות ל-create-room/single-player.
    public async Task<List<QuestionType>> GetQuestionTypesAsync()
    {
        var questionTypeDb = new QuestionTypeDB();
        return await questionTypeDb.GetAllAsync();
    }

    // יצירת חדר חדש לפי משתמש מחובר.
    public async Task<(bool Ok, string Message, Room? Room)> CreateRoomAsync(int userId, CreateRoomRequest req)
    {
        var (validName, nameError) = ValidationHelper.ValidateRoomName(req.RoomName);
        if (!validName)
            return (false, nameError, null);

        var roomDb = new RoomDB();
        var room = await roomDb.CreateRoomAsync(req.RoomName.Trim(), userId, req.IsPublic, req.QuestionTypeId);
        return room is null
            ? (false, "Failed to create room.", null)
            : (true, "Room created.", room);
    }

    // הצטרפות לחדר לפי קוד + nickname.
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

    // שליפת חדר לפי קוד.
    public async Task<Room?> GetRoomByCodeAsync(string roomCode)
    {
        var roomDb = new RoomDB();
        return await roomDb.GetRoomByCodeAsync((roomCode ?? "").Trim().ToUpperInvariant());
    }

    // רשימת חדרים ציבוריים פעילים.
    public async Task<List<Room>> GetPublicRoomsAsync()
    {
        var roomDb = new RoomDB();
        return await roomDb.GetPublicRoomsAsync();
    }

    // רשימת שחקנים בחדר.
    public async Task<List<RoomPlayer>> GetPlayersAsync(int roomId)
    {
        var roomDb = new RoomDB();
        return await roomDb.GetPlayersAsync(roomId);
    }

    // יציאה מחדר.
    public async Task<bool> LeaveRoomAsync(int roomId, int userId)
    {
        var roomDb = new RoomDB();
        return await roomDb.RemovePlayerAsync(roomId, userId);
    }
}
