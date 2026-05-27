using Microsoft.AspNetCore.Mvc;

namespace TriviaGame.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class SystemController : ControllerBase
{
    // בריאות מערכת בסיסית.
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { ok = true, utc = DateTime.UtcNow });
    }
}
