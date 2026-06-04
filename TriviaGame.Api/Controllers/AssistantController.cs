using Microsoft.AspNetCore.Mvc;
using TriviaGame.Api.Contracts;
using TriviaGame.Api.Services;

namespace TriviaGame.Api.Controllers;

// ה-controller הזה הוא נקודת הכניסה ליכולות העזר של Gemini.
// הוא לא שומר נתונים בעצמו, אלא מעביר את הבקשה לשירות שמדבר עם ה-LLM.
[ApiController]
[Route("api/assistant")]
public sealed class AssistantController : ControllerBase
{
    // השירות האמיתי שמכיל את הלוגיקה:
    // בניית ה-prompt, שליחה ל-Gemini, וניתוח התשובה.
    private readonly AssistantDomainService assistantDomainService;

    public AssistantController(AssistantDomainService assistantDomainService)
    {
        // הזרקה רגילה דרך DI.
        // כך אפשר להחליף את השירות בלי לגעת ב-controller.
        this.assistantDomainService = assistantDomainService;
    }

    // מסלול לקבלת רמז קצר לשאלה פעילה.
    // ה-client שולח את השאלה הנוכחית, והשירות מחזיר hint קצר ולא את התשובה המלאה.
    [HttpPost("advice")]
    public async Task<IActionResult> Advice([FromBody] AssistantAdviceRequest request, CancellationToken cancellationToken)
    {
        // הבקשה עוברת לשכבת השירות, ושם נבנה prompt מתאים ל-Gemini.
        var (ok, message) = await assistantDomainService.GetAdviceAsync(request.Question, cancellationToken);
        return ok ? Ok(new { ok = true, advice = message }) : BadRequest(new { ok = false, message });
    }

    // מסלול צ'אט אישי.
    // כאן ה-client שולח userId, הודעה, והיסטוריה אופציונלית.
    // השירות בונה תשובה מותאמת לפי הפרופיל והסטטיסטיקות של המשתמש.
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request, CancellationToken cancellationToken)
    {
        // כל הנתונים עוברים לשירות כדי ששם תתבצע הלוגיקה.
        var (ok, message) = await assistantDomainService.GetPersonalReplyAsync(
            request.UserId,
            request.Message,
            request.History,
            cancellationToken);

        return ok ? Ok(new { ok = true, text = message }) : BadRequest(new { ok = false, message });
    }
}
