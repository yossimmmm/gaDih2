using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// נקודות קצה של אימות:
// login, register, שליפת המשתמש הנוכחי, forgot password, reset password.
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthDomainService authService;
    private readonly UsersDomainService usersDomainService;

    public AuthController(AuthDomainService authService, UsersDomainService usersDomainService)
    {
        // ה־controllers נשארים דקים; הלוגיקה האמיתית יושבת בשירותים.
        this.authService = authService;
        this.usersDomainService = usersDomainService;
    }

    // מאמת אימייל וסיסמה ומחזיר את נתוני המשתמש אם הפרטים נכונים.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return result.Ok ? Ok(result) : Unauthorized(result);
    }

    // יוצר חשבון משתמש חדש אחרי ולידציה ובדיקת כפילויות.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (ok, message) = await authService.RegisterAsync(request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // מחזיר את נתוני הפרופיל השמורים של ה־userId שנשלח.
    [HttpGet("me")]
    public async Task<IActionResult> Me([FromQuery] int userId)
    {
        var user = await usersDomainService.GetByIdAsync(userId);
        if (user is null)
            return Ok(new { authenticated = false, userId = 0, username = "", fullName = "", email = "", role = "User" });

        return Ok(new
        {
            authenticated = true,
            userId = user.UserID,
            username = user.Username,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role.ToString()
        });
    }

    // מתחיל את תהליך איפוס הסיסמה על ידי יצירת טוקן ושליחה במייל.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var (ok, message) = await authService.ForgotPasswordAsync(request, baseUrl);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // משלים את תהליך האיפוס בעזרת הטוקן שנשלח למשתמש.
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (ok, message) = await authService.ResetPasswordAsync(request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }
}
