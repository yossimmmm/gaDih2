using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// ה-controller הזה מטפל בכל הפעולות שקשורות לחדרים:
// טעינת סוגי שאלות, יצירת חדר, הצטרפות לחדר, עזיבה, ורשימת שחקנים.
[ApiController]
[Route("api/rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly RoomsDomainService roomsDomainService;

    public RoomsController(RoomsDomainService roomsDomainService)
    {
        // ה-controller עצמו לא נוגע במסד; הוא רק מעביר את העבודה לשירות הדומיין.
        this.roomsDomainService = roomsDomainService;
    }

    // ה-UI צריך את סוגי השאלות כדי למלא picker בבחירת חדר.
    [HttpGet("question-types")]
    public async Task<IActionResult> GetQuestionTypes()
    {
        var rows = await roomsDomainService.GetQuestionTypesAsync();
        return Ok(rows);
    }

    // יצירת חדר מקבלת userId, שם חדר, האם החדר ציבורי, ואפשרות לסוג שאלות.
    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var (ok, message, room) = await roomsDomainService.CreateRoomAsync(request.UserId, request);
        return ok
            ? Ok(new { ok = true, message, room })
            : BadRequest(new { ok = false, message });
    }

    // רשימת חדרים ציבוריים נשלפת בלי userId, כי זו קריאת read-only כללית.
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicRooms()
    {
        var rooms = await roomsDomainService.GetPublicRoomsAsync();
        return Ok(rooms);
    }

    // שליפת חדר בודד לפי קוד, למשל כשעוברים מאוסף חדרים למסך חדר פעיל.
    [HttpGet("{roomCode}")]
    public async Task<IActionResult> GetRoom(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        return room is null ? NotFound(new { ok = false, message = "Room not found." }) : Ok(room);
    }

    // הצטרפות לחדר משתמשת ב-userId מפורש, כדי שהשרת ייצור row של room player לאותו משתמש.
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRoomRequest request)
    {
        var (ok, message, player, room) = await roomsDomainService.JoinRoomAsync(request.UserId, request);
        return ok
            ? Ok(new { ok = true, message, room, player })
            : BadRequest(new { ok = false, message });
    }

    // עזיבה מוחקת את שורת השחקן של המשתמש מתוך החדר.
    [HttpPost("{roomCode}/leave")]
    public async Task<IActionResult> Leave(string roomCode, [FromQuery] int userId)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        var ok = await roomsDomainService.LeaveRoomAsync(room.RoomID, userId);
        return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, message = "Failed to leave room." });
    }

    // רשימת שחקנים משמשת את ה-UI כדי לדעת מי כבר נמצא בחדר.
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
