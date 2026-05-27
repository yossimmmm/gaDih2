using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly SessionTokenService sessionTokenService;
    private readonly UsersDomainService usersDomainService;
    private readonly GameDomainService gameDomainService;

    public UsersController(
        SessionTokenService sessionTokenService,
        UsersDomainService usersDomainService,
        GameDomainService gameDomainService)
    {
        this.sessionTokenService = sessionTokenService;
        this.usersDomainService = usersDomainService;
        this.gameDomainService = gameDomainService;
    }

    // שליפת פרופיל משתמש מחובר.
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

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

    // עדכון פרופיל.
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        var (ok, message) = await usersDomainService.UpdateProfileAsync(user.UserID, request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // שינוי סיסמה.
    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        var (ok, message) = await usersDomainService.ChangePasswordAsync(user.UserID, request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // סטטיסטיקות שחקן.
    [HttpGet("me/stats")]
    public async Task<IActionResult> GetStats()
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        var stats = await gameDomainService.GetUserStatsAsync(user.UserID);
        return Ok(new
        {
            gamesPlayed = stats.GamesPlayed,
            wins = stats.Wins,
            correct = stats.Correct,
            answered = stats.Answered
        });
    }

    // היסטוריית משחקים אחרונים.
    [HttpGet("me/recent-results")]
    public async Task<IActionResult> GetRecentResults([FromQuery] int limit = 10)
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        limit = Math.Clamp(limit, 1, 50);
        var rows = await gameDomainService.GetRecentResultsAsync(user.UserID, limit);
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
