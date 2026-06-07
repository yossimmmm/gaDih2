using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// נקודות קצה של חדרים:
// סוגי שאלות, יצירת חדר, חדרים ציבוריים, הצטרפות, יציאה ושחקנים.
[ApiController]
[Route("api/rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly RoomsDomainService roomsDomainService;

    public RoomsController(RoomsDomainService roomsDomainService)
    {
        // ה-controller לא נוגע ישירות במסד; השירות מטפל בלוגיקה.
        this.roomsDomainService = roomsDomainService;
    }

    // מחזיר את הקטגוריות הקיימות כדי שהלקוח יוכל להציג מסנן יצירה.
    // #question-types #question #rooms - endpoint לטעינת סוגי השאלות.
    [HttpGet("question-types")]
    public async Task<IActionResult> GetQuestionTypes()
    {
        // שולפים את סוגי השאלות מה-DB כדי למלא את הבחירה במסך יצירת חדר.
        var rows = await roomsDomainService.GetQuestionTypesAsync();
        return Ok(rows);
    }

    // יוצר חדר חדש עבור המשתמש המחובר.
    // #create-room #rooms - endpoint שיוצר חדר חדש במסד הנתונים.
    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        // השירות אחראי לוולידציה, יצירת קוד חדר, ושמירת החדר במסד.
        var (ok, message, room) = await roomsDomainService.CreateRoomAsync(request.UserId, request);

        // במקרה הצלחה מחזירים גם את פרטי החדר שנוצרו.
        return ok
            ? Ok(new { ok = true, message, room })
            : BadRequest(new { ok = false, message });
    }

    // מחזיר את כל החדרים הציבוריים שהמשתמש יכול להצטרף אליהם.
    // #public-rooms #rooms - endpoint לרשימת החדרים הציבוריים.
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicRooms()
    {
        // הרשימה כוללת רק חדרים ציבוריים ופעילים שהמשתמש יכול לראות בלובי.
        var rooms = await roomsDomainService.GetPublicRoomsAsync();
        return Ok(rooms);
    }

    // מאתר חדר לפי קוד החדר.
    // #get-room #room-code #rooms - endpoint לטעינת חדר מסוים לפי הקוד שלו.
    [HttpGet("{roomCode}")]
    public async Task<IActionResult> GetRoom(string roomCode)
    {
        // קוד החדר מגיע מהנתיב, והשירות מנרמל אותו ומחפש במסד.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        return room is null ? NotFound(new { ok = false, message = "Room not found." }) : Ok(room);
    }

    // מצרף משתמש לחדר לפי קוד חדר.
    // #join-room #rooms #players - endpoint שמצרף משתמש לחדר.
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRoomRequest request)
    {
        // השירות בודק שהחדר קיים ופעיל, ואז יוצר או מחזיר רשומת שחקן בחדר.
        var (ok, message, player, room) = await roomsDomainService.JoinRoomAsync(request.UserId, request);

        // הלקוח צריך גם את החדר וגם את player כדי לדעת את roomPlayerId להמשך המשחק.
        return ok
            ? Ok(new { ok = true, message, room, player })
            : BadRequest(new { ok = false, message });
    }

    // מסיר שחקן מהחדר.
    // #leave-room #rooms #players - endpoint שמוציא משתמש מחדר.
    [HttpPost("{roomCode}/leave")]
    public async Task<IActionResult> Leave(string roomCode, [FromQuery] int userId)
    {
        // קודם מאתרים את החדר לפי הקוד כדי לקבל roomId פנימי.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        // אחרי שיש roomId, השירות מוחק את הקשר בין המשתמש לחדר.
        var ok = await roomsDomainService.LeaveRoomAsync(room.RoomID, userId);
        return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, message = "Failed to leave room." });
    }

    // מחזיר את רשימת השחקנים הרשומים בחדר.
    // #room-players #rooms #players - endpoint לטעינת שחקני החדר.
    [HttpGet("{roomCode}/players")]
    public async Task<IActionResult> Players(string roomCode)
    {
        // שוב מתחילים מקוד חדר חיצוני וממירים אותו לחדר אמיתי במסד.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        // מחזירים את כל השחקנים כדי שהלקוח יציג מי נמצא בחדר.
        var players = await roomsDomainService.GetPlayersAsync(room.RoomID);
        return Ok(players);
    }
}
