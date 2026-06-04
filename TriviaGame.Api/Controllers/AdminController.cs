using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// נקודות קצה לניהול משתמשים ברמת אדמין.
[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly UsersDomainService usersDomainService;

    public AdminController(UsersDomainService usersDomainService)
    {
        // controller נשאר דק ומעביר הכול לשירות המשתמשים.
        this.usersDomainService = usersDomainService;
    }

    // מחזיר את רשימת המשתמשים עם אפשרות סינון לפי role.
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role = null)
    {
        var users = await usersDomainService.GetAllUsersAsync();
        var filtered = string.IsNullOrWhiteSpace(role)
            ? users
            : users.Where(u => string.Equals(u.Role.ToString(), role, StringComparison.OrdinalIgnoreCase)).ToList();

        return Ok(filtered.Select(u => new
        {
            userId = u.UserID,
            username = u.Username,
            fullName = u.FullName,
            email = u.Email,
            role = u.Role.ToString()
        }));
    }

    // משנה את תפקיד המשתמש.
    [HttpPost("users/{userId:int}/role")]
    public async Task<IActionResult> UpdateRole(int userId, [FromBody] UpdateRoleRequest request)
    {
        var (ok, message) = await usersDomainService.UpdateRoleAsync(userId, request.Role);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // מעדכן משתמש מלא מתוך מסך הניהול.
    [HttpPut("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] AdminUserUpdateRequest request)
    {
        var (ok, message) = await usersDomainService.UpdateUserByAdminAsync(userId, request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // מוחק משתמש.
    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var (ok, message) = await usersDomainService.DeleteUserAsync(userId);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }
}
