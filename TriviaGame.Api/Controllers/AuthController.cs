using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// ה-controller הזה מטפל בכל מה שקשור לזהות המשתמש:
// login, register, טעינת המשתמש הנוכחי, ואיפוס סיסמה.
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthDomainService authService;
    private readonly UsersDomainService usersDomainService;

    public AuthController(AuthDomainService authService, UsersDomainService usersDomainService)
    {
        // הזרקת השירותים מאפשרת ל-controller להישאר דק: הוא לא עושה את הלוגיקה בעצמו.
        this.authService = authService;
        this.usersDomainService = usersDomainService;
    }

    // ה-UI שולח email + password, והשרת מחזיר אם ההתחברות הצליחה ואת פרטי המשתמש.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return result.Ok ? Ok(result) : Unauthorized(result);
    }

    // הרשמה יוצרת רשומת משתמש חדשה במסד, אחרי בדיקות תקינות.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (ok, message) = await authService.RegisterAsync(request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // המובייל כבר מחזיק userId מקומי, אז הוא מבקש את רשומת המשתמש ישירות לפי id.
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

    // זרימת forgot-password: יוצרים token ושולחים מייל עם קישור לאיפוס.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var (ok, message) = await authService.ForgotPasswordAsync(request, baseUrl);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // המסלול הזה מקבל token וסיסמה חדשה ומבצע את האיפוס בפועל.
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (ok, message) = await authService.ResetPasswordAsync(request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }
}
