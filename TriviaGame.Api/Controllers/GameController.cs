using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// נקודות קצה של המשחק:
// התחלת סיבוב, שליפת שאלה נוכחית, שליחת תשובה, שמירת תוצאות, scoreboard ו־leaderboard.
[ApiController]
[Route("api/game")]
public sealed class GameController : ControllerBase
{
    private readonly RoomsDomainService roomsDomainService;
    private readonly GameDomainService gameDomainService;

    public GameController(RoomsDomainService roomsDomainService, GameDomainService gameDomainService)
    {
        // שירות החדר בודק את מצב החדר; שירות המשחק מטפל בלוגיקת הטריוויה.
        this.roomsDomainService = roomsDomainService;
        this.gameDomainService = gameDomainService;
    }

    // מתחיל סיבוב משחק חדש על ידי בחירת שאלות לחדר.
    [HttpPost("{roomCode}/start")]
    public async Task<IActionResult> StartGame(string roomCode, [FromBody] StartGameRequest request)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        if (room.HostID != request.UserId)
            return BadRequest(new { ok = false, message = "Only host can start the game." });

        // מגבילים את מספר השאלות לטווח הנתמך.
        var count = request.QuestionCount <= 0 ? 10 : Math.Min(request.QuestionCount, 50);
        var (ok, message, inserted) = await gameDomainService.StartGameAsync(room.RoomID, count);
        return ok
            ? Ok(new { ok = true, message, inserted })
            : BadRequest(new { ok = false, message });
    }

    // מחזיר את השאלה הפעילה של החדר, או מסמן שהחדר הסתיים.
    [HttpGet("{roomCode}/current-question")]
    public async Task<IActionResult> GetCurrentQuestion(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        var q = await gameDomainService.GetCurrentQuestionAsync(room.RoomID);
        if (q is null)
            return Ok(new { ok = true, finished = true });

        return Ok(new { ok = true, finished = false, question = q });
    }

    // שומר תשובה אחת ואז מרענן את תוצאות החדר.
    [HttpPost("{roomCode}/answer")]
    public async Task<IActionResult> SubmitAnswer(string roomCode, [FromBody] SubmitAnswerRequest request)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        var (ok, message) = await gameDomainService.SubmitAnswerAsync(request);
        if (!ok)
            return BadRequest(new { ok = false, message });

        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true, message });
    }

    // כופה שמירה של סיכום החדר, שימושי אחרי סיום סיבוב.
    [HttpPost("{roomCode}/save-results")]
    public async Task<IActionResult> SaveResults(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true });
    }

    // מחזיר את ה־scoreboard הנוכחי של החדר.
    [HttpGet("{roomCode}/scoreboard")]
    public async Task<IActionResult> Scoreboard(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        var rows = await gameDomainService.GetScoreboardAsync(room.RoomID);
        var totalQuestions = await gameDomainService.GetRoomQuestionCountAsync(room.RoomID);

        return Ok(new
        {
            roomId = room.RoomID,
            roomCode = room.RoomCode,
            totalQuestions,
            rows
        });
    }

    // מחזיר את טבלת השחקנים המובילים למסך הסטטיסטיקות.
    [HttpGet("top-players")]
    public async Task<IActionResult> TopPlayers([FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 100);
        var rows = await gameDomainService.GetTopPlayersAsync(limit);
        return Ok(rows);
    }
}
