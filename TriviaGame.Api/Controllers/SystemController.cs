using Microsoft.AspNetCore.Mvc;

namespace TriviaGame.Api.Controllers;

// נקודות קצה כלליות של המערכת.
// כרגע יש health check פשוט כדי שה־UI או בדיקת פריסה יוכלו לוודא שה־API חי.
[ApiController]
[Route("api")]
public sealed class SystemController : ControllerBase
{
    // נקודת health בסיסית לקריאה בלבד.
    // מחזירה ok + זמן UTC נוכחי.
    [HttpGet("health")]
    public IActionResult Health()
    {
        // endpoint זה נשאר פשוט ופתוח כדי לבדוק מהר שהשרת רץ.
        return Ok(new { ok = true, utc = DateTime.UtcNow });
    }
}
