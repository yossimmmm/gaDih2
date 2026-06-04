using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// נקודות קצה של פרופיל משתמש, סיסמה, סטטיסטיקות והיסטוריית משחקים.
[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly UsersDomainService usersDomainService;
    private readonly GameDomainService gameDomainService;

    public UsersController(UsersDomainService usersDomainService, GameDomainService gameDomainService)
    {
        // ה-controller הזה רק מעביר לשירותים.
        this.usersDomainService = usersDomainService;
        this.gameDomainService = gameDomainService;
    }

    // מחזיר את הפרופיל של המשתמש לפי userId.
    [HttpGet("me")]
    public async Task<IActionResult> GetMe([FromQuery] int userId)
    {
        var user = await usersDomainService.GetByIdAsync(userId);
        if (user is null)
            return NotFound(new { ok = false, message = "User not found." });

        return Ok(new
        {
            ok = true,
            userId = user.UserID,
            username = user.Username,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role.ToString()
        });
    }

    // מעדכן username, full name ואימייל.
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var (ok, message) = await usersDomainService.UpdateProfileAsync(request.UserId, request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // משנה את הסיסמה של המשתמש אחרי בדיקת הסיסמה הישנה.
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var (ok, message) = await usersDomainService.ChangePasswordAsync(request.UserId, request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // מחזיר סטטיסטיקות מצטברות למשתמש.
    [HttpGet("me/stats")]
    public async Task<IActionResult> GetStats([FromQuery] int userId)
    {
        var stats = await gameDomainService.GetUserStatsAsync(userId);
        return Ok(new
        {
            gamesPlayed = stats.GamesPlayed,
            wins = stats.Wins,
            correct = stats.Correct,
            answered = stats.Answered
        });
    }

    // מחזיר את תוצאות המשחק האחרונות של המשתמש.
    [HttpGet("me/recent-results")]
    public async Task<IActionResult> GetRecentResults([FromQuery] int userId, [FromQuery] int limit = 10)
    {
        limit = Math.Clamp(limit, 1, 50);
        var rows = await gameDomainService.GetRecentResultsAsync(userId, limit);
        return Ok(rows.Select(r => new
        {
            createdAt = r.CreatedAt,
            roomName = r.RoomName,
            correctCount = r.CorrectCount,
            answeredCount = r.AnsweredCount,
            isWinner = r.IsWinner
        }));
    }
}
