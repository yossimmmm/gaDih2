using Microsoft.AspNetCore.Mvc;

namespace TriviaGame.Api.Controllers;

// מסלולי מערכת כלליים.
// כרגע יש כאן health check פשוט, כדי שה-UI או כלי בדיקה יוכלו לוודא שה-API חי.
[ApiController]
[Route("api")]
public sealed class SystemController : ControllerBase
{
    // בריאות בסיסית של השרת.
    // מחזיר ok + זמן UTC נוכחי.
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { ok = true, utc = DateTime.UtcNow });
    }
}
