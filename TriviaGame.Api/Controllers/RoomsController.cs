using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly SessionTokenService sessionTokenService;
    private readonly RoomsDomainService roomsDomainService;

    public RoomsController(SessionTokenService sessionTokenService, RoomsDomainService roomsDomainService)
    {
        this.sessionTokenService = sessionTokenService;
        this.roomsDomainService = roomsDomainService;
    }

    // רשימת סוגי שאלות.
    [HttpGet("question-types")]
    public async Task<IActionResult> GetQuestionTypes()
    {
        var rows = await roomsDomainService.GetQuestionTypesAsync();
        return Ok(rows);
    }

    // יצירת חדר חדש.
    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        var (ok, message, room) = await roomsDomainService.CreateRoomAsync(user.UserID, request);
        return ok
            ? Ok(new { ok = true, message, room })
            : BadRequest(new { ok = false, message });
    }

    // רשימת חדרים ציבוריים.
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicRooms()
    {
        var rooms = await roomsDomainService.GetPublicRoomsAsync();
        return Ok(rooms);
    }

    // שליפת חדר ספציפי.
    [HttpGet("{roomCode}")]
    public async Task<IActionResult> GetRoom(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        return room is null ? NotFound(new { ok = false, message = "Room not found." }) : Ok(room);
    }

    // הצטרפות לחדר.
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRoomRequest request)
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        var (ok, message, player, room) = await roomsDomainService.JoinRoomAsync(user.UserID, request);
        return ok
            ? Ok(new { ok = true, message, room, player })
            : BadRequest(new { ok = false, message });
    }

    // יציאה מחדר.
    [HttpPost("{roomCode}/leave")]
    public async Task<IActionResult> Leave(string roomCode)
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        var ok = await roomsDomainService.LeaveRoomAsync(room.RoomID, user.UserID);
        return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, message = "Failed to leave room." });
    }

    // רשימת שחקנים בחדר.
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
