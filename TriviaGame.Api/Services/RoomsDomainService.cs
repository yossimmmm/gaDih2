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
        // אין כאן חוק עסקי מיוחד; רק שליפה של טבלת question_types.
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

        // RoomDB יוצר קוד חדר ייחודי ושומר את החדר עם hostId של המשתמש.
        var room = await roomDb.CreateRoomAsync(req.RoomName.Trim(), userId, req.IsPublic, req.QuestionTypeId);
        return room is null
            ? (false, "Failed to create room.", null)
            : (true, "Room created.", room);
    }

    // מצרף שחקן לחדר לפי קוד חדר, ושומר את הקשר בין userId לחדר.
    public async Task<(bool Ok, string Message, RoomPlayer? Player, Room? Room)> JoinRoomAsync(int userId, JoinRoomRequest req)
    {
        // מנרמלים קוד חדר לאותיות גדולות כי קודי חדר נשמרים כך במסד.
        var roomCode = (req.RoomCode ?? "").Trim().ToUpperInvariant();
        var (validCode, codeError) = ValidationHelper.ValidateRoomCode(roomCode);
        if (!validCode)
            return (false, codeError, null, null);

        // nickname הוא אופציונלי, אבל אם המשתמש כתב אותו הוא חייב להיות תקין.
        if (!string.IsNullOrWhiteSpace(req.Nickname))
        {
            var (validNick, nickError) = ValidationHelper.ValidateNickname(req.Nickname);
            if (!validNick)
                return (false, nickError, null, null);
        }

        var roomDb = new RoomDB();

        // קודם מוצאים את החדר עצמו כדי לוודא שאפשר להצטרף אליו.
        var room = await roomDb.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return (false, "Room not found or inactive.", null, null);

        // אם אין nickname, שכבת ה-DB יכולה להשתמש בברירת מחדל לפי המשתמש.
        var nick = (req.Nickname ?? "").Trim();
        var player = await roomDb.JoinRoomAsync(room.RoomID, userId, nick);

        // מחזירים גם Player וגם Room כי הלקוח צריך את שניהם אחרי הצטרפות.
        return player is null
            ? (false, "Failed to join room.", null, room)
            : (true, "Joined room.", player, room);
    }

    // חיפוש בסיסי לפי קוד חדר, כי הרבה פעולות אחרות מתחילות מאותו קוד.
    public async Task<Room?> GetRoomByCodeAsync(string roomCode)
    {
        var roomDb = new RoomDB();

        // כל קריאה לפי קוד חדר עוברת trim ו-uppercase כדי למנוע כישלון בגלל הקלדה.
        return await roomDb.GetRoomByCodeAsync((roomCode ?? "").Trim().ToUpperInvariant());
    }

    // מחזיר את החדרים הציבוריים הפעילים, בלי סינון לפי משתמש.
    public async Task<List<Room>> GetPublicRoomsAsync()
    {
        var roomDb = new RoomDB();

        // RoomDB כבר יודע לסנן רק חדרים ציבוריים/פעילים.
        return await roomDb.GetPublicRoomsAsync();
    }

    // מחזיר את כל השחקנים שכבר קיימים בחדר.
    public async Task<List<RoomPlayer>> GetPlayersAsync(int roomId)
    {
        var roomDb = new RoomDB();

        // משמש למסך חדר כדי להציג מי נמצא לפני ובזמן המשחק.
        return await roomDb.GetPlayersAsync(roomId);
    }

    // מסיר שחקן מחדר לפי roomId ו־userId.
    public async Task<bool> LeaveRoomAsync(int roomId, int userId)
    {
        var roomDb = new RoomDB();

        // מחיקה כאן היא מחיקת הקשר room_player, לא מחיקת המשתמש עצמו.
        return await roomDb.RemovePlayerAsync(roomId, userId);
    }
}
