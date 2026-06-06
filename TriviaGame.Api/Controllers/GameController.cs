using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// × ×§×•×“×•×ª ×§×¦×” ×©×œ ×”×ž×©×—×§:
// ×”×ª×—×œ×ª ×ž×©×—×§, ×©××œ×” × ×•×›×—×™×ª, ×ª×©×•×‘×”, ×©×ž×™×¨×ª ×ª×•×¦××•×ª, scoreboard ×•-top players.
[ApiController]
[Route("api/game")]
public sealed class GameController : ControllerBase
{
    private readonly RoomsDomainService roomsDomainService;
    private readonly GameDomainService gameDomainService;

    public GameController(RoomsDomainService roomsDomainService, GameDomainService gameDomainService)
    {
        // ×©×™×¨×•×ª ×”×—×“×¨ ×‘×•×“×§ ××ª ×ž×¦×‘ ×”×—×“×¨; ×©×™×¨×•×ª ×”×ž×©×—×§ ×ž×˜×¤×œ ×‘×œ×•×’×™×§×” ×¢×¦×ž×”.
        this.roomsDomainService = roomsDomainService;
        this.gameDomainService = gameDomainService;
    }

    // ×ž×ª×—×™×œ ×¡×™×‘×•×‘ ×ž×©×—×§ ×—×“×© ×‘×—×“×¨.
    [HttpPost("{roomCode}/start")]
    public async Task<IActionResult> StartGame(string roomCode, [FromBody] StartGameRequest request)
    {
        // ×ž××ª×¨×™× ××ª ×”×—×“×¨ ×œ×¤×™ roomCode ×›×™ ×”×œ×§×•×— ×¢×•×‘×“ ×¢× ×§×•×“ ×§×¨×™× ×•×œ× ×¢× roomId ×¤× ×™×ž×™.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        // ×¨×§ ×”×ž×©×ª×ž×© ×©×™×¦×¨ ××ª ×”×—×“×¨ ×¨×©××™ ×œ×”×ª×—×™×œ ××ª ×”×ž×©×—×§.
        if (room.HostID != request.UserId)
            return BadRequest(new { ok = false, message = "Only host can start the game." });

        // ×ž×’×‘×™×œ×™× ××ª ×ž×¡×¤×¨ ×”×©××œ×•×ª ×›×“×™ ×œ×ž× ×•×¢ ×¢×•×ž×¡.
        var count = request.QuestionCount <= 0 ? 10 : Math.Min(request.QuestionCount, 50);

        // ×”×©×™×¨×•×ª ×‘×•×—×¨ ×©××œ×•×ª ××§×¨××™×•×ª ×•×©×•×ž×¨ ××•×ª×Ÿ ×‘×˜×‘×œ×ª room_questions.
        var (ok, message, inserted) = await gameDomainService.StartGameAsync(room.RoomID, count);
        return ok
            ? Ok(new { ok = true, message, inserted })
            : BadRequest(new { ok = false, message });
    }

    // ×ž×—×–×™×¨ ××ª ×”×©××œ×” ×”× ×•×›×—×™×ª ××• ×ž×¡×ž×Ÿ ×©×”×ž×©×—×§ ×”×¡×ª×™×™×.
    [HttpGet("{roomCode}/current-question")]
    public async Task<IActionResult> GetCurrentQuestion(string roomCode)
    {
        // ×‘×•×“×§×™× ×©×”×—×“×¨ ×§×™×™× ×•×¤×¢×™×œ ×œ×¤× ×™ ×©×ž×—×–×™×¨×™× ×©××œ×”.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        // ×”×©×™×¨×•×ª ×ž×—×–×™×¨ ××ª ×”×©××œ×” ×”×¤×¢×™×œ×” ××• null ×× ××™×Ÿ ×™×•×ª×¨ ×©××œ×•×ª.
        var q = await gameDomainService.GetCurrentQuestionAsync(room.RoomID);
        if (q is null)
        {
            // finished=true ××•×ž×¨ ×œ×œ×§×•×— ×œ×¢×‘×•×¨ ×œ×ž×¦×‘ ×¡×™×•×/×ª×•×¦××•×ª.
            return Ok(new { ok = true, finished = true });
        }

        // ×× ×™×© ×©××œ×”, ×ž×—×–×™×¨×™× ××•×ª×” ×¢× ×”××¤×©×¨×•×™×•×ª ×©×œ×”.
        return Ok(new { ok = true, finished = false, question = q });
    }

    // ×©×•×ž×¨ ×ª×©×•×‘×” ×©×œ ×©×—×§×Ÿ ×•×ž×¨×¢× ×Ÿ ××ª ×ª×•×¦××•×ª ×”×—×“×¨.
    [HttpPost("{roomCode}/answer")]
    public async Task<IActionResult> SubmitAnswer(string roomCode, [FromBody] SubmitAnswerRequest request)
    {
        // ×ž×•×•×“××™× ×©×”×ª×©×•×‘×” × ×©×œ×—×ª ×œ×—×“×¨ ×§×™×™× ×•×¤×¢×™×œ.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        // ×”×©×™×¨×•×ª ×‘×•×“×§ ×©×”××¤×©×¨×•×ª ×©×™×™×›×ª ×œ×©××œ×” ×•×©×•×ž×¨ ××ª ×”×ª×©×•×‘×”.
        var (ok, message) = await gameDomainService.SubmitAnswerAsync(request);
        if (!ok)
            return BadRequest(new { ok = false, message });

        // ××—×¨×™ ×›×œ ×ª×©×•×‘×” ×ž×¢×“×›× ×™× ××ª ×ª×•×¦××•×ª ×”×—×“×¨ ×›×“×™ ×©×”-scoreboard ×™×”×™×” ×¢×“×›× ×™.
        // שומרים את התוצאות הסופיות בטבלת game_results כדי שהסטטיסטיקות יישארו זמינות גם אחרי שהחדר נסגר.
        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true, message });
    }

    // ×ž××©×¨ ×©×ž×™×¨×” ×ž×œ××” ×©×œ ×ª×•×¦××•×ª ×”×—×“×¨.
    [HttpPost("{roomCode}/save-results")]
    public async Task<IActionResult> SaveResults(string roomCode)
    {
        // endpoint ×™×“× ×™ ×œ×©×ž×™×¨×ª ×ª×•×¦××•×ª, ×œ×ž×§×¨×” ×©×”×œ×§×•×— ×¨×•×¦×” ×œ×•×•×“× ×©×”×¡×™×›×•× × ×©×ž×¨.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        // ×”×©×ž×™×¨×” ×ž×©×ª×ž×©×ª ×‘-scoreboard ×”× ×•×›×—×™ ×•×ž×›× ×™×¡×”/×ž×¢×“×›× ×ª game_results.
        // שומרים את התוצאות הסופיות בטבלת game_results כדי שהסטטיסטיקות יישארו זמינות גם אחרי שהחדר נסגר.
        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true });
    }

    // ×ž×—×–×™×¨ ××ª ×œ×•×— ×”× ×™×§×•×“ ×©×œ ×”×—×“×¨.
    [HttpGet("{roomCode}/scoreboard")]
    public async Task<IActionResult> Scoreboard(string roomCode)
    {
        // ×§×•×“× ×ž×–×”×™× ××ª ×”×—×“×¨ ×›×“×™ ×œ×¢×‘×•×“ ×¢× roomId ××ž×™×ª×™ ×‘×ž×¡×“.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        // rows ×”×Ÿ ×©×•×¨×•×ª ×”× ×™×§×•×“ ×©×œ ×”×©×—×§× ×™× ×‘×—×“×¨.
        var rows = await gameDomainService.GetScoreboardAsync(room.RoomID);
        // totalQuestions ×¢×•×–×¨ ×œ×œ×§×•×— ×œ×”×¦×™×’ ×›×ž×” ×©××œ×•×ª ×”×™×• ×‘×ž×©×—×§.
        var totalQuestions = await gameDomainService.GetRoomQuestionCountAsync(room.RoomID);

        // ×ž×—×–×™×¨×™× ×ž×¢×˜×¤×ª ×¢× ×¤×¨×˜×™ ×”×—×“×¨, ×›×ž×•×ª ×©××œ×•×ª ×•×©×•×¨×•×ª ×”× ×™×§×•×“.
        return Ok(new
        {
            roomId = room.RoomID,
            roomCode = room.RoomCode,
            totalQuestions,
            rows
        });
    }

    // ×ž×—×–×™×¨ ××ª ×˜×‘×œ×ª ×”×ž×•×‘×™×œ×™× ×©×œ ×”×ž×©×—×§.
    [HttpGet("top-players")]
    public async Task<IActionResult> TopPlayers([FromQuery] int limit = 10)
    {
        // ×ž×’×‘×™×œ×™× ××ª limit ×›×“×™ ×©×ž×©×ª×ž×© ×œ× ×™×‘×§×© ×›×ž×•×ª ×¢× ×§×™×ª ×©×œ ×©×•×¨×•×ª.
        limit = Math.Clamp(limit, 1, 100);
        // ×”×©×™×¨×•×ª ×ž×—×–×™×¨ leaderboard ×’×œ×•×‘×œ×™ ×ž×›×œ ×ª×•×¦××•×ª ×”×ž×©×—×§×™×.
        var rows = await gameDomainService.GetTopPlayersAsync(limit);
        return Ok(rows);
    }
}

