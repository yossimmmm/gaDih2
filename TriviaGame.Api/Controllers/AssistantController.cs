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
        // השירות מכיל את כל העבודה מול Gemini ואת בניית ה-prompt.
        this.assistantDomainService = assistantDomainService;
    }

    // מחזיר רמז קצר לשאלה הפעילה.
    // #advice #assistant #gemini #question - endpoint לקבלת רמז בזמן שאלה.
    [HttpPost("advice")]
    public async Task<IActionResult> Advice([FromBody] AssistantAdviceRequest request, CancellationToken cancellationToken)
    {
        // שולחים לשירות את השאלה הנוכחית כדי לקבל רמז קצר בלי לחשוף תשובה.
        var (ok, message) = await assistantDomainService.GetAdviceAsync(request.Question, cancellationToken);
        // בהצלחה השדה נקרא advice; בכשל מחזירים message רגיל.
        return ok ? Ok(new { ok = true, advice = message }) : BadRequest(new { ok = false, message });
    }

    // מחזיר תשובה אישית על בסיס הנתונים וההיסטוריה של המשתמש.
    // #assistant #chat #gemini - endpoint לשיחה אישית עם העוזר.
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken cancellationToken)
    {
        // userId מאפשר לשירות לטעון פרופיל, סטטיסטיקות והיסטוריית משחקים.
        // message הוא הטקסט הנוכחי, ו-History שומר הקשר לשיחה.
        var (ok, message) = await assistantDomainService.GetPersonalReplyAsync(
            request.UserId,
            request.Message,
            request.History,
            cancellationToken);

        // בהצלחה מחזירים text שהלקוח מציג בצ'אט.
        return ok ? Ok(new { ok = true, text = message }) : BadRequest(new { ok = false, message });
    }
}
