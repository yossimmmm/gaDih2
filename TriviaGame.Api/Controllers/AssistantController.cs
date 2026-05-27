using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

[ApiController]
[Route("api/assistant")]
public sealed class AssistantController : ControllerBase
{
    private readonly SessionTokenService sessionTokenService;
    private readonly AssistantDomainService assistantDomainService;

    public AssistantController(SessionTokenService sessionTokenService, AssistantDomainService assistantDomainService)
    {
        this.sessionTokenService = sessionTokenService;
        this.assistantDomainService = assistantDomainService;
    }

    // קבלת hint לשאלה פעילה.
    [HttpPost("advice")]
    public async Task<IActionResult> Advice([FromBody] AssistantAdviceRequest request, CancellationToken cancellationToken)
    {
        var (ok, message) = await assistantDomainService.GetAdviceAsync(request.Question, cancellationToken);
        return ok ? Ok(new { ok = true, advice = message }) : BadRequest(new { ok = false, message });
    }

    // קבלת תשובת assistant מותאמת למשתמש המחובר.
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken cancellationToken)
    {
        var user = await sessionTokenService.TryGetUserAsync(HttpContext);
        if (user is null)
            return Unauthorized(new { ok = false, message = "Unauthorized." });

        var (ok, message) = await assistantDomainService.GetPersonalReplyAsync(
            user.UserID,
            request.Message,
            request.History,
            cancellationToken);

        return ok ? Ok(new { ok = true, text = message }) : BadRequest(new { ok = false, message });
    }
}
