using System.Net.Http.Json;
using System.Text.Json;
using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// השירות הזה מדבר עם Gemini.
// הוא לוקח נתוני שאלה או נתוני משתמש, בונה prompt, שולח HTTP, ואז מחלץ טקסט מהתגובה.
public sealed class AssistantDomainService
{
    // ברירת מחדל אם לא הוגדר endpoint אחר בקונפיגורציה.
    private const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta";

    // המודל המוגדר כברירת מחדל.
    private const string DefaultModel = "gemini-2.5-flash-lite";

    // factory ליצירת HttpClient.
    // זה מאפשר יצירה מנוהלת של לקוח HTTP בלי לבנות אותו ידנית בכל פעם.
    private readonly IHttpClientFactory httpClientFactory;

    // הקונפיגורציה של האפליקציה:
    // מכאן קוראים API key, endpoint, model וכו'.
    private readonly IConfiguration configuration;

    // לוגים של כשלים בבקשות.
    private readonly ILogger<AssistantDomainService> logger;

    public AssistantDomainService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AssistantDomainService> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
        this.logger = logger;
    }

    // מחזיר רמז קצר לשאלה פעילה.
    // הלקוח שולח אובייקט Question, והשירות מחזיר hint שלא חושף את התשובה המלאה.
    public async Task<(bool Ok, string Message)> GetAdviceAsync(Question question, CancellationToken cancellationToken = default)
    {
        // אם אין שאלה פעילה או שאין אפשרויות, אין על מה לייצר hint.
        if (question is null || question.Options.Count == 0)
            return (false, "No active question to advise on.");

        // קוראים את מפתח ה-API מהקונפיגורציה או ממשתנה סביבה.
        var apiKey = GetGeminiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "Gemini API key is missing.");

        var endpoint = GetGeminiEndpoint();
        var model = GetGeminiModel();

        // ממירים את האפשרויות לטקסט קריא עבור ה-prompt.
        var optionsText = string.Join(Environment.NewLine, question.Options.Select((o, i) => $"{(char)('A' + i)}. {o.OptionText}"));

        // ה-prompt אומר ל-Gemini לתת רמז קצר בלבד ולא לחשוף את התשובה.
        var prompt = $"""
You are a live trivia coach.
Give one short hint (max 70 words) for the current question.
Do not reveal the exact correct option.

Question:
{question.QuestionText}

Options:
{optionsText}
""";

        // הפורמט ש-Gemini מצפה לקבל.
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.4,
                maxOutputTokens = 120
            }
        };

        try
        {
            // בונים URL מלא לבקשה ל-Gemini.
            var url = BuildGenerateContentUrl(endpoint, model, apiKey);

            // שולחים את ה-payload ומחלצים טקסט מהתגובה.
            var text = await GenerateTextAsync(url, payload, "advice", cancellationToken);
            return string.IsNullOrWhiteSpace(text)
                ? (false, "Gemini returned an empty response.")
                : (true, text.Trim());
        }
        catch (Exception ex)
        {
            // אם משהו נכשל ברשת או בפענוח, רושמים לוג ומחזירים הודעת כשל ברורה.
            logger.LogError(ex, "Gemini advice request failed.");
            return (false, "Gemini is unavailable right now.");
        }
    }

    // מחזיר תשובה אישית למשתמש מחובר.
    // כאן השירות משלב פרופיל, סטטיסטיקות, היסטוריה אחרונה, והודעת המשתמש.
    public async Task<(bool Ok, string Message)> GetPersonalReplyAsync(
        int userId,
        string userMessage,
        List<AssistantChatMessage>? history,
        CancellationToken cancellationToken = default)
    {
        // בלי userId תקין אין הקשר למשתמש, ולכן עוצרים.
        if (userId <= 0)
            return (false, "You must be logged in.");
        if (string.IsNullOrWhiteSpace(userMessage))
            return (false, "Message is empty.");

        // שוב קוראים API key לפני כל שיחה.
        var apiKey = GetGeminiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "Gemini API key is missing.");

        // כדי לבנות הקשר אישי, שולפים את פרטי המשתמש והנתונים הסטטיסטיים שלו.
        var userDb = new UserDB();
        var gameDb = new GameDB();
        var user = await userDb.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        var stats = await gameDb.GetUserStatsAsync(userId);
        var recent = await gameDb.GetRecentUserResultsAsync(userId, 10);

        // ממירים את התוצאות האחרונות לטקסט שנוח להכניס ל-prompt.
        var recentText = recent.Count == 0
            ? "No recent games."
            : string.Join(Environment.NewLine, recent.Select((g, i) =>
                $"{i + 1}. {g.CreatedAt:yyyy-MM-dd HH:mm} | {g.RoomName} | Correct {g.CorrectCount}/{g.AnsweredCount} | Winner: {(g.IsWinner ? "Yes" : "No")}"));

        // גם היסטוריית צ'אט קודמת נכנסת ל-prompt אם קיימת.
        var historyText = history is null || history.Count == 0
            ? "No previous chat history."
            : string.Join(Environment.NewLine, history.TakeLast(12).Select(h => $"{h.Role.ToUpperInvariant()}: {h.Text}"));

        // חישובי עזר שנכנסים לקונטקסט של הבוט.
        var accuracy = stats.Answered == 0 ? 0 : (double)stats.Correct / stats.Answered * 100.0;
        var winCoeff = stats.GamesPlayed == 0 ? 0 : (double)stats.Wins / stats.GamesPlayed;
        var winPercent = winCoeff * 100.0;

        // לפעמים המשתמש שואל במפורש על "winning coefficient".
        // במקרה כזה אין צורך לשלוח ל-Gemini, אפשר לענות ישירות מהנתונים.
        var normalized = userMessage.Trim().ToLowerInvariant();
        var asksWinCoeff = normalized.Contains("cooficent")
                           || normalized.Contains("coefficient")
                           || normalized.Contains("win rate")
                           || normalized.Contains("winning rate")
                           || normalized.Contains("winning coefficient");
        if (asksWinCoeff)
        {
            if (stats.GamesPlayed == 0)
                return (true, "You have no completed games yet, so your winning coefficient is currently 0.00 (0%).");
            return (true, $"Your winning coefficient is {winCoeff:F2} ({winPercent:F1}%), based on {stats.Wins} wins out of {stats.GamesPlayed} games.");
        }

        // ה-prompt כאן כולל את כל ההקשר האישי כדי ש-Gemini יחזיר תשובה מותאמת.
        var prompt = $"""
You are a personal trivia assistant for one authenticated player.
Only answer based on the data below.
Be concise and practical.

User Profile:
- UserId: {user.UserID}
- Username: {user.Username}
- Full Name: {user.FullName}
- Email: {user.Email}

User Aggregate Stats:
- Games Played: {stats.GamesPlayed}
- Wins: {stats.Wins}
- Correct Answers: {stats.Correct}
- Total Answers: {stats.Answered}
- Accuracy Percent: {accuracy:F1}%
- Winning Coefficient: {winCoeff:F4}
- Winning Percent: {winPercent:F1}%

Recent Games (latest first):
{recentText}

Recent Chat History:
{historyText}

Current user message:
{userMessage.Trim()}
""";

        // אותו מבנה payload כמו ברמזים, רק עם יותר הקשר ויותר טוקנים.
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.25,
                maxOutputTokens = 300
            }
        };

        try
        {
            // בונים את ה-URL ושולחים את הבקשה.
            var url = BuildGenerateContentUrl(GetGeminiEndpoint(), GetGeminiModel(), apiKey);
            var text = await GenerateTextAsync(url, payload, "assistant", cancellationToken);
            return string.IsNullOrWhiteSpace(text)
                ? (false, "Gemini returned an empty response.")
                : (true, text.Trim());
        }
        catch (Exception ex)
        {
            // לוג כשלים, והחזרת הודעת שגיאה יציבה ל-client.
            logger.LogError(ex, "Gemini personal assistant request failed.");
            return (false, "Gemini is unavailable right now.");
        }
    }

    // מחלץ את מפתח ה-API מהקונפיגורציה.
    // קודם appsettings, ואז משתנה סביבה אם צריך.
    private string? GetGeminiApiKey()
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        return apiKey;
    }

    // endpoint ניתן להגדרה חיצונית כדי לעבוד מול סביבות שונות.
    private string GetGeminiEndpoint()
    {
        var endpoint = configuration["Gemini:Endpoint"];
        return string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint;
    }

    // גם המודל עצמו נקבע דרך קונפיגורציה, עם ברירת מחדל בטוחה.
    private string GetGeminiModel()
    {
        var model = configuration["Gemini:Model"];
        return string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
    }

    // מרכיב את ה-URL הסופי לבקשת generateContent.
    private static string BuildGenerateContentUrl(string endpoint, string model, string apiKey) =>
        $"{endpoint.TrimEnd('/')}/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

    // שולח את ה-payload ל-Gemini ומחזיר את הטקסט שהמודל ייצר.
    private async Task<string?> GenerateTextAsync(string url, object payload, string scope, CancellationToken cancellationToken)
    {
        // HttpClient נוצר מה-factory כדי לשמור על ניהול נכון של connections.
        using var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);

        // אם ה-HTTP נכשל, אנחנו לא זורקים ישר חריגה ללקוח אלא מחזירים null ומוסיפים לוג.
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Gemini {Scope} request failed ({StatusCode}): {Body}", scope, (int)response.StatusCode, body);
            return null;
        }

        // קוראים את ה-JSON של Gemini כ-stream.
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return TryExtractCandidateText(doc);
    }

    // Gemini מחזיר מבנה JSON עמוק.
    // הפונקציה הזו יורדת לשדות הרלוונטיים ומחזירה את הטקסט של ה-candidate הראשון.
    private static string? TryExtractCandidateText(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
        {
            return null;
        }

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array ||
            parts.GetArrayLength() == 0)
        {
            return null;
        }

        return parts[0].TryGetProperty("text", out var textNode)
            ? textNode.GetString()
            : null;
    }
}
