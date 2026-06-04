using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// ה-controller הזה מטפל בכל מה שקשור להרצת משחק:
// התחלה, שאלה נוכחית, שליחת תשובה, שמירת תוצאות, ניקוד, ודירוג מובילים.
[ApiController]
[Route("api/game")]
public sealed class GameController : ControllerBase
{
    private readonly RoomsDomainService roomsDomainService;
    private readonly GameDomainService gameDomainService;

    public GameController(RoomsDomainService roomsDomainService, GameDomainService gameDomainService)
    {
        // חדרים ומשחקים תלויים אחד בשני, ולכן ה-controller מקבל את שני השירותים.
        this.roomsDomainService = roomsDomainService;
        this.gameDomainService = gameDomainService;
    }

    // רק המארח יכול להתחיל את המשחק, ולכן בודקים את userId מול HostID של החדר.
    [HttpPost("{roomCode}/start")]
    public async Task<IActionResult> StartGame(string roomCode, [FromBody] StartGameRequest request)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        if (room.HostID != request.UserId)
            return BadRequest(new { ok = false, message = "Only host can start the game." });

        // מגבילים את מספר השאלות כדי לא לקבל בקשה לא הגיונית מהלקוח.
        var count = request.QuestionCount <= 0 ? 10 : Math.Min(request.QuestionCount, 50);
        var (ok, message, inserted) = await gameDomainService.StartGameAsync(room.RoomID, count);
        return ok
            ? Ok(new { ok = true, message, inserted })
            : BadRequest(new { ok = false, message });
    }

    // ה-UI שואל את השרת איזו שאלה פעילה כרגע בחדר.
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

    // שליחת תשובה אחת: ה-UI שולח רק מזהים, והשרת מחשב את התוצאה.
    [HttpPost("{roomCode}/answer")]
    public async Task<IActionResult> SubmitAnswer(string roomCode, [FromBody] SubmitAnswerRequest request)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        var (ok, message) = await gameDomainService.SubmitAnswerAsync(request);
        if (!ok)
            return BadRequest(new { ok = false, message });

        // אחרי תשובה טובה, שומרים גם את תוצאות החדר כדי שהניקוד וההיסטוריה יהיו מעודכנים.
        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true, message });
    }

    // מסלול תחזוקה שמאפשר לשמור תוצאות גם ידנית אם צריך.
    [HttpPost("{roomCode}/save-results")]
    public async Task<IActionResult> SaveResults(string roomCode)
    {
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true });
    }

    // טבלת הניקוד נשלחת ל-UI כ-rows מוכנים להצגה.
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

    // דירוג השחקנים המובילים הוא קריאת read-only כללית.
    [HttpGet("top-players")]
    public async Task<IActionResult> TopPlayers([FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 100);
        var rows = await gameDomainService.GetTopPlayersAsync(limit);
        return Ok(rows);
    }
}
