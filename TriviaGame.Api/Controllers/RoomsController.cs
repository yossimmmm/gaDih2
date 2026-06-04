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
    [HttpGet("question-types")]
    public async Task<IActionResult> GetQuestionTypes()
    {
        var rows = await roomsDomainService.GetQuestionTypesAsync();
        return Ok(rows);
    }

    // יוצר חדר חדש עבור המשתמש המחובר.
    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var (ok, message, room) = await roomsDomainService.CreateRoomAsync(request.UserId, request);
        return ok
            ? Ok(new { ok = true, message, room })
            : BadRequest(new { ok = false, message });
    }

    // מחזיר את כל החדרים הציבוריים שהמשתמש יכול להצטרף אליהם.
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicRooms()
    {
        var rooms = await roomsDomainService.GetPublicRoomsAsync();
        return Ok(rooms);
    }

    // מאתר חדר לפי קוד החדר.
    [HttpGet("{roomCode}")]
    public async Task<IActionResult> GetRoom(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        return room is null ? NotFound(new { ok = false, message = "Room not found." }) : Ok(room);
    }

    // מצרף משתמש לחדר לפי קוד חדר.
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRoomRequest request)
    {
        var (ok, message, player, room) = await roomsDomainService.JoinRoomAsync(request.UserId, request);
        return ok
            ? Ok(new { ok = true, message, room, player })
            : BadRequest(new { ok = false, message });
    }

    // מסיר שחקן מהחדר.
    [HttpPost("{roomCode}/leave")]
    public async Task<IActionResult> Leave(string roomCode, [FromQuery] int userId)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        var ok = await roomsDomainService.LeaveRoomAsync(room.RoomID, userId);
        return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, message = "Failed to leave room." });
    }

    // מחזיר את רשימת השחקנים הרשומים בחדר.
    [HttpGet("{roomCode}/players")]
    public async Task<IActionResult> Players(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        var players = await roomsDomainService.GetPlayersAsync(room.RoomID);
        return Ok(players);
    }
}
