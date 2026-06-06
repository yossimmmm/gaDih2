using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// #login #register #me #forgot-password #reset-password #logout #cookie #email #token

// נקודות קצה של אימות משתמשים:
// התחברות, הרשמה, me, שכחתי סיסמה, איפוס סיסמה.
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthDomainService authService;
    private readonly UsersDomainService usersDomainService;

    public AuthController(AuthDomainService authService, UsersDomainService usersDomainService)
    {
        // ה-controller נשאר דק; הלוגיקה האמיתית יושבת בשירותים.
        this.authService = authService;
        this.usersDomainService = usersDomainService;
    }

    // התחברות עם אימייל וסיסמה.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // מעבירים את פרטי ההתחברות לשירות האימות.
        // ה-controller לא בודק סיסמה בעצמו כדי שכל חוקי האימות יהיו במקום אחד.
        var result = await authService.LoginAsync(request);

        // אם ההתחברות הצליחה מחזירים 200; אם לא, מחזירים 401 כי זו בעיית אימות.
        return result.Ok ? Ok(result) : Unauthorized(result);
    }

    // הרשמה של משתמש חדש אחרי בדיקת תקינות.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // השירות בודק תקינות, כפילויות של אימייל/שם משתמש, ויוצר משתמש חדש.
        var (ok, message) = await authService.RegisterAsync(request);

        // הצלחה חוזרת כ-200, וקלט לא תקין חוזר כ-400.
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // מחזיר את המשתמש המחובר לפי userId.
    [HttpGet("me")]
    public async Task<IActionResult> Me([FromQuery] int userId)
    {
        // הלקוח שולח userId, והשרת מנסה למצוא את המשתמש המתאים.
        var user = await usersDomainService.GetByIdAsync(userId);
        if (user is null)
        {
            // כאן מחזירים authenticated=false במקום שגיאה כדי שהלקוח יוכל להציג מצב "לא מחובר".
            return Ok(new { authenticated = false, userId = 0, username = "", fullName = "", email = "", role = "User" });
        }

        // מחזירים רק פרטים בטוחים להצגה, בלי PasswordHash.
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

    // מתחיל תהליך איפוס סיסמה ושולח מייל עם קישור.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // #forgot-password #email #reset-token #reset-link
        // בונים baseUrl מתוך הבקשה הנוכחית כדי שקישור האיפוס יוביל לאותו שרת.
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // השירות יוצר טוקן איפוס, שומר hash שלו במסד, ושולח מייל.
        var (ok, message) = await authService.ForgotPasswordAsync(request, baseUrl);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }

    // משלים את איפוס הסיסמה בעזרת הטוקן שנשלח במייל.
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // #reset-password #token #password-hash #expiry
        // השירות מאמת את הטוקן ואת הסיסמה החדשה, ואז מעדכן את ה-hash במסד.
        var (ok, message) = await authService.ResetPasswordAsync(request);
        return ok ? Ok(new { ok = true, message }) : BadRequest(new { ok = false, message });
    }
}
