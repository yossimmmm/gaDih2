using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// ה-controller הזה מרכז את מסלולי האדמין למשתמשים.
// הוא לא מבצע לוגיקה בעצמו, אלא מעביר הכול ל-UsersDomainService.
[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    // השירות שמדבר בפועל עם שכבת הנתונים.
    private readonly UsersDomainService usersDomainService;

    public AdminController(UsersDomainService usersDomainService)
    {
        // הזרקה דרך DI כדי להשאיר את ה-controller דק ופשוט.
        this.usersDomainService = usersDomainService;
    }

    // מחזיר את כל המשתמשים.
    // אפשר גם לסנן לפי תפקיד דרך query string.
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

    // שינוי תפקיד למשתמש קיים.
    // הנתיב כולל userId, וה-body כולל רק את התפקיד החדש.
    [HttpPost("users/{userId:int}/role")]
    public async Task<IActionResult> UpdateRole(int userId, [FromBody] UpdateRoleRequest request)
    {
        var (ok, message) = await usersDomainService.UpdateRoleAsync(userId, request.Role);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // עדכון מלא של משתמש מצד אדמין.
    [HttpPut("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] AdminUserUpdateRequest request)
    {
        var (ok, message) = await usersDomainService.UpdateUserByAdminAsync(userId, request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // מחיקת משתמש.
    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var (ok, message) = await usersDomainService.DeleteUserAsync(userId);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }
}
