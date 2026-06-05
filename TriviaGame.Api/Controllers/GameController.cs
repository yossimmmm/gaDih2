using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// נקודות קצה של המשחק:
// התחלת משחק, שאלה נוכחית, תשובה, שמירת תוצאות, scoreboard ו-top players.
[ApiController]
[Route("api/game")]
public sealed class GameController : ControllerBase
{
    private readonly RoomsDomainService roomsDomainService;
    private readonly GameDomainService gameDomainService;

    public GameController(RoomsDomainService roomsDomainService, GameDomainService gameDomainService)
    {
        // שירות החדר בודק את מצב החדר; שירות המשחק מטפל בלוגיקה עצמה.
        this.roomsDomainService = roomsDomainService;
        this.gameDomainService = gameDomainService;
    }

    // מתחיל סיבוב משחק חדש בחדר.
    [HttpPost("{roomCode}/start")]
    public async Task<IActionResult> StartGame(string roomCode, [FromBody] StartGameRequest request)
    {
        // מאתרים את החדר לפי roomCode כי הלקוח עובד עם קוד קריא ולא עם roomId פנימי.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        // רק המשתמש שיצר את החדר רשאי להתחיל את המשחק.
        if (room.HostID != request.UserId)
            return BadRequest(new { ok = false, message = "Only host can start the game." });

        // מגבילים את מספר השאלות כדי למנוע עומס.
        var count = request.QuestionCount <= 0 ? 10 : Math.Min(request.QuestionCount, 50);

        // השירות בוחר שאלות אקראיות ושומר אותן בטבלת room_questions.
        var (ok, message, inserted) = await gameDomainService.StartGameAsync(room.RoomID, count);
        return ok
            ? Ok(new { ok = true, message, inserted })
            : BadRequest(new { ok = false, message });
    }

    // מחזיר את השאלה הנוכחית או מסמן שהמשחק הסתיים.
    [HttpGet("{roomCode}/current-question")]
    public async Task<IActionResult> GetCurrentQuestion(string roomCode)
    {
        // בודקים שהחדר קיים ופעיל לפני שמחזירים שאלה.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        // השירות מחזיר את השאלה הפעילה או null אם אין יותר שאלות.
        var q = await gameDomainService.GetCurrentQuestionAsync(room.RoomID);
        if (q is null)
        {
            // finished=true אומר ללקוח לעבור למצב סיום/תוצאות.
            return Ok(new { ok = true, finished = true });
        }

        // אם יש שאלה, מחזירים אותה עם האפשרויות שלה.
        return Ok(new { ok = true, finished = false, question = q });
    }

    // שומר תשובה של שחקן ומרענן את תוצאות החדר.
    [HttpPost("{roomCode}/answer")]
    public async Task<IActionResult> SubmitAnswer(string roomCode, [FromBody] SubmitAnswerRequest request)
    {
        // מוודאים שהתשובה נשלחת לחדר קיים ופעיל.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null || !room.IsActive)
            return NotFound(new { ok = false, message = "Room not found or inactive." });

        // השירות בודק שהאפשרות שייכת לשאלה ושומר את התשובה.
        var (ok, message) = await gameDomainService.SubmitAnswerAsync(request);
        if (!ok)
            return BadRequest(new { ok = false, message });

        // אחרי כל תשובה מעדכנים את תוצאות החדר כדי שה-scoreboard יהיה עדכני.
        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true, message });
    }

    // מאשר שמירה מלאה של תוצאות החדר.
    [HttpPost("{roomCode}/save-results")]
    public async Task<IActionResult> SaveResults(string roomCode)
    {
        // endpoint ידני לשמירת תוצאות, למקרה שהלקוח רוצה לוודא שהסיכום נשמר.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        // השמירה משתמשת ב-scoreboard הנוכחי ומכניסה/מעדכנת game_results.
        await gameDomainService.SaveRoomResultsAsync(room.RoomID);
        return Ok(new { ok = true });
    }

    // מחזיר את לוח הניקוד של החדר.
    [HttpGet("{roomCode}/scoreboard")]
    public async Task<IActionResult> Scoreboard(string roomCode)
    {
        // קודם מזהים את החדר כדי לעבוד עם roomId אמיתי במסד.
        var room = await roomsDomainService.GetRoomByCodeAsync(roomCode);
        if (room is null)
            return NotFound(new { ok = false, message = "Room not found." });

        // rows הן שורות הניקוד של השחקנים בחדר.
        var rows = await gameDomainService.GetScoreboardAsync(room.RoomID);
        // totalQuestions עוזר ללקוח להציג כמה שאלות היו במשחק.
        var totalQuestions = await gameDomainService.GetRoomQuestionCountAsync(room.RoomID);

        // מחזירים מעטפת עם פרטי החדר, כמות שאלות ושורות הניקוד.
        return Ok(new
        {
            roomId = room.RoomID,
            roomCode = room.RoomCode,
            totalQuestions,
            rows
        });
    }

    // מחזיר את טבלת המובילים של המשחק.
    [HttpGet("top-players")]
    public async Task<IActionResult> TopPlayers([FromQuery] int limit = 10)
    {
        // מגבילים את limit כדי שמשתמש לא יבקש כמות ענקית של שורות.
        limit = Math.Clamp(limit, 1, 100);
        // השירות מחזיר leaderboard גלובלי מכל תוצאות המשחקים.
        var rows = await gameDomainService.GetTopPlayersAsync(limit);
        return Ok(rows);
    }
}
