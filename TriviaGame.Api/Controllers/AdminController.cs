using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly SessionTokenService sessionTokenService;
    private readonly UsersDomainService usersDomainService;

    public AdminController(SessionTokenService sessionTokenService, UsersDomainService usersDomainService)
    {
        this.sessionTokenService = sessionTokenService;
        this.usersDomainService = usersDomainService;
    }

    // רשימת משתמשים מלאה לאדמין.
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role = null)
    {
        if (!await sessionTokenService.IsAdminAsync(HttpContext))
            return Unauthorized(new { ok = false, message = "Admin access required." });

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

    // שינוי role.
    [HttpPost("users/{userId:int}/role")]
    public async Task<IActionResult> UpdateRole(int userId, [FromBody] UpdateRoleRequest request)
    {
        if (!await sessionTokenService.IsAdminAsync(HttpContext))
            return Unauthorized(new { ok = false, message = "Admin access required." });

        var (ok, message) = await usersDomainService.UpdateRoleAsync(userId, request.Role);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // עדכון משתמש מלא.
    [HttpPut("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] AdminUserUpdateRequest request)
    {
        if (!await sessionTokenService.IsAdminAsync(HttpContext))
            return Unauthorized(new { ok = false, message = "Admin access required." });

        var (ok, message) = await usersDomainService.UpdateUserByAdminAsync(userId, request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // מחיקת משתמש.
    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var current = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (current is null || current.Role != Models.UserRole.Admin)
            return Unauthorized(new { ok = false, message = "Admin access required." });

        if (current.UserID == userId)
            return BadRequest(new { ok = false, message = "Admin cannot delete own account." });

        var (ok, message) = await usersDomainService.DeleteUserAsync(userId);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }
}
