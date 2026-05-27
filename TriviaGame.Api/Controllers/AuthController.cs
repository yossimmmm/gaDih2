using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthDomainService authService;
    private readonly SessionTokenService sessionTokenService;
    private readonly UsersDomainService usersDomainService;

    public AuthController(
        AuthDomainService authService,
        SessionTokenService sessionTokenService,
        UsersDomainService usersDomainService)
    {
        this.authService = authService;
        this.sessionTokenService = sessionTokenService;
        this.usersDomainService = usersDomainService;
    }

    // התחברות משתמש והחזרת token למובייל + כתיבת cookie לתאימות web.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (!result.Ok)
            return Unauthorized(result);

        // שומרים גם cookie כדי לשמר תאימות ל-web flows הקיימים.
        Response.Cookies.Append("session_token", result.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Ok(result);
    }

    // רישום משתמש חדש.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (ok, message) = await authService.RegisterAsync(request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // התנתקות לפי token מה-header/cookie.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = sessionTokenService.TryReadToken(HttpContext);
        await authService.LogoutAsync(token);
        Response.Cookies.Delete("session_token");
        return Ok(new { ok = true });
    }

    // מצב משתמש מחובר נוכחי.
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
        {
            return Ok(new CurrentUserResponse(false, 0, "", "", "", "User"));
        }

        return Ok(new CurrentUserResponse(
            true,
            user.UserID,
            user.Username,
            user.FullName,
            user.Email,
            user.Role.ToString()));
    }

    // יצירת reset-token ושליחת מייל.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var (ok, message) = await authService.ForgotPasswordAsync(request, baseUrl);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // איפוס סיסמה לפי token.
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (ok, message) = await authService.ResetPasswordAsync(request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }
}
