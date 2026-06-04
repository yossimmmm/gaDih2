using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// נקודות קצה של העוזר החכם שמבוססות על Gemini.
[ApiController]
[Route("api/assistant")]
public sealed class AssistantController : ControllerBase
{
    private readonly AssistantDomainService assistantDomainService;

    public AssistantController(AssistantDomainService assistantDomainService)
    {
        this.assistantDomainService = assistantDomainService;
    }

    // מחזיר רמז קצר לשאלה הפעילה.
    [HttpPost("advice")]
    public async Task<IActionResult> Advice([FromBody] AssistantAdviceRequest request, CancellationToken cancellationToken)
    {
        var (ok, message) = await assistantDomainService.GetAdviceAsync(request.Question, cancellationToken);
        return ok ? Ok(new { ok = true, advice = message }) : BadRequest(new { ok = false, message });
    }

    // מחזיר תשובה אישית על בסיס הנתונים וההיסטוריה של המשתמש.
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken cancellationToken)
    {
        var (ok, message) = await assistantDomainService.GetPersonalReplyAsync(
            request.UserId,
            request.Message,
            request.History,
            cancellationToken);

        return ok ? Ok(new { ok = true, text = message }) : BadRequest(new { ok = false, message });
    }
}
