using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using DBL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TriviaGame.Services
{
    public sealed class GeminiAdviceService
    {
        // כתובת ברירת מחדל ל-API של Gemini
        private const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta";
        // מודל ברירת מחדל לשיחות
        private const string DefaultModel = "gemini-2.5-flash-lite";

        // שירות יצירת HttpClient
        private readonly IHttpClientFactory httpClientFactory;
        // קונפיגורציה כללית (appsettings + env vars)
        private readonly IConfiguration configuration;
        // לוג לאבחון תקלות
        private readonly ILogger<GeminiAdviceService> logger;

        public GeminiAdviceService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GeminiAdviceService> logger)
        {
            // הזרקת תלויות מה-DI כדי לשמור את השירות מבודד וניתן לבדיקה.
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            this.logger = logger;
        }

        // מחזיר רמז קצר לשאלת טריוויה אחת (בלי לגלות תשובה ישירה).
        public async Task<GeminiAdviceResult> GetAdviceAsync(Question question, CancellationToken cancellationToken = default)
        {
            // ולידציה בסיסית לקלט
            if (question is null || question.Options.Count == 0)
                return GeminiAdviceResult.Fail("No active question to advise on.");

            // שליפת מפתח API; בלי מפתח אי אפשר לפנות ל-Gemini
            var apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return GeminiAdviceResult.Fail("Gemini API key is missing. Set Gemini:ApiKey or GEMINI_API_KEY.");

            // שליפת endpoint/model מתוך קונפיג או ברירות מחדל
            var endpoint = GetGeminiEndpoint();
            var model = GetGeminiModel();

            // בניית טקסט אופציות לפורמט קריא למודל
            var optionsText = string.Join(
                Environment.NewLine,
                question.Options.Select((o, index) => $"{(char)('A' + index)}. {o.OptionText}"));

            // פרומפט קצר ומוגדר היטב כדי לקבל "רמז" ולא תשובה ישירה.
            var prompt = $"""
You are a live trivia coach.
Give one short hint (max 70 words) for the current question.
Do not reveal the exact correct option and do not claim certainty.
Focus on reasoning/elimination clues and keep it practical.

Question:
{question.QuestionText}

Options:
{optionsText}
""";

            var payload = new
            {
                // פורמט contents של Gemini: רשימת הודעות עם parts
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
                    // temperature נמוך יחסית כדי למנוע "המצאות" ולשמור על עקביות.
                    temperature = 0.4,
                    // מגביל את אורך התשובה כדי להשאיר אותה קצרה ושימושית במשחק.
                    maxOutputTokens = 120
                }
            };

            try
            {
                // בניית URL מלא לקריאת generateContent
                var url = BuildGenerateContentUrl(endpoint, model, apiKey);
                // שליחה ל-Gemini וחילוץ טקסט תשובה
                var text = await GenerateTextAsync(url, payload, "advice", cancellationToken);
                return string.IsNullOrWhiteSpace(text)
                    ? GeminiAdviceResult.Fail("Gemini returned an empty response.")
                    : GeminiAdviceResult.Ok(text.Trim());
            }
            catch (GeminiHttpException)
            {
                // כשל HTTP ידוע מול Gemini
                return GeminiAdviceResult.Fail("Gemini is unavailable right now.");
            }
            catch (Exception ex)
            {
                // כל חריגה אחרת נרשמת בלוג ומוחזרת הודעת שגיאה כללית
                logger.LogError(ex, "Gemini advice request threw an exception.");
                return GeminiAdviceResult.Fail("Failed to contact Gemini.");
            }
        }

        public async Task<GeminiAssistantReply> GetPersonalAssistantReplyAsync(
            int userId,
            string userMessage,
            IReadOnlyList<GeminiChatTurn>? chatHistory = null,
            CancellationToken cancellationToken = default)
        {
            // צ'אט אישי שמחזיר תשובות רק לפי נתוני המשתמש המחובר.
            // ולידציה בסיסית לקלט
            if (userId <= 0)
                return GeminiAssistantReply.Fail("You must be logged in.");
            if (string.IsNullOrWhiteSpace(userMessage))
                return GeminiAssistantReply.Fail("Message is empty.");

            var apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return GeminiAssistantReply.Fail("Gemini API key is missing. Set Gemini:ApiKey or GEMINI_API_KEY.");

            // endpoint/model ניתנים לשינוי דרך קונפיג בלי שינוי קוד.
            var endpoint = GetGeminiEndpoint();
            var model = GetGeminiModel();

            // שליפת נתוני משתמש וסטטיסטיקות
            // גישת DB לצורך פרופיל משתמש + סטטיסטיקות משחק.
            var userDb = new UserDB();
            var gameDb = new GameDB();

            var user = await userDb.GetByIdAsync(userId);
            if (user is null)
                return GeminiAssistantReply.Fail("User not found.");

            var stats = await gameDb.GetUserStatsAsync(userId);
            var recentGames = await gameDb.GetRecentUserResultsAsync(userId, 10);

            // פורמט טקסטואלי למשחקים אחרונים כדי לתת הקשר ברור למודל.
            var recentGamesText = recentGames.Count == 0
                ? "No recent games."
                : string.Join(Environment.NewLine, recentGames.Select((g, idx) =>
                    $"{idx + 1}. {g.CreatedAt:yyyy-MM-dd HH:mm} | {g.RoomName} | Correct {g.CorrectCount}/{g.AnsweredCount} | Winner: {(g.IsWinner ? "Yes" : "No")}"));

            // היסטוריית שיחה מקוצרת כדי לשמור הקשר מבלי לנפח טוקנים
            var historyText = (chatHistory is null || chatHistory.Count == 0)
                ? "No previous chat history."
                : string.Join(Environment.NewLine, chatHistory.TakeLast(12).Select(h =>
                    $"{h.Role.ToUpperInvariant()}: {h.Text}"));

            // חישובים נגזרים
            // חישובים נגזרים לשאלות כמו accuracy / winning-coefficient.
            var accuracy = stats.Answered == 0 ? 0 : (double)stats.Correct / stats.Answered * 100.0;
            var winningCoefficient = stats.GamesPlayed == 0 ? 0 : (double)stats.Wins / stats.GamesPlayed;
            var winningPercent = winningCoefficient * 100.0;

            // תשובת fallback דטרמיניסטית לשאלות win rate/coefficient
            // מנרמלים הודעת משתמש לזיהוי ביטויי "winning coefficient" בלי תלות case.
            var normalizedMessage = userMessage.Trim().ToLowerInvariant();
            var asksWinningCoefficient =
                normalizedMessage.Contains("cooficent") ||
                normalizedMessage.Contains("coefficient") ||
                normalizedMessage.Contains("win rate") ||
                normalizedMessage.Contains("winning rate") ||
                normalizedMessage.Contains("winning coefficient");
            if (asksWinningCoefficient)
            {
                // אם אין משחקים כלל - מחזירים תשובה מספרית ברורה
                if (stats.GamesPlayed == 0)
                {
                    return GeminiAssistantReply.Ok(
                        "You have no completed games yet, so your winning coefficient is currently 0.00 (0%).");
                }

                return GeminiAssistantReply.Ok(
                    $"Your winning coefficient is {winningCoefficient:F2} ({winningPercent:F1}%), based on {stats.Wins} wins out of {stats.GamesPlayed} games.");
            }

            // בניית prompt אישי שמוגבל לנתוני המשתמש המחובר בלבד
            // פרומפט אישי עשיר: כולל פרופיל, סטטיסטיקות, היסטוריה והודעה נוכחית.
            var prompt = $"""
You are a personal trivia assistant for a single authenticated player.
Only answer based on the user data below.
If data is missing, explicitly say it is unavailable.
Be concise, clear, and practical.

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
- Winning Coefficient (wins/games): {winningCoefficient:F4}
- Winning Percent: {winningPercent:F1}%

Recent Games (latest first):
{recentGamesText}

Recent Chat History:
{historyText}

Current user message:
{userMessage.Trim()}
""";

            var payload = new
            {
                // payload תקני של Gemini generateContent
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
                    // טמפרטורה נמוכה לשמירה על תשובות יציבות ועקביות
                    temperature = 0.25,
                    // טווח גדול יותר מצ'אט עצה כדי לאפשר תשובות מפורטות מעט.
                    maxOutputTokens = 300
                }
            };

            try
            {
                // קריאת assistant מול Gemini
                var url = BuildGenerateContentUrl(endpoint, model, apiKey);
                var text = await GenerateTextAsync(url, payload, "personal assistant", cancellationToken);
                return string.IsNullOrWhiteSpace(text)
                    ? GeminiAssistantReply.Fail("Gemini returned an empty response.")
                    : GeminiAssistantReply.Ok(text.Trim());
            }
            catch (GeminiHttpException)
            {
                // כשל תקשורת/סטטוס מול Gemini
                return GeminiAssistantReply.Fail("Gemini is unavailable right now.");
            }
            catch (Exception ex)
            {
                // טיפול כללי בתקלות בלתי צפויות
                logger.LogError(ex, "Gemini personal assistant request threw an exception.");
                return GeminiAssistantReply.Fail("Failed to contact Gemini.");
            }
        }

        // שליפת מפתח Gemini מהגדרות או משתנה סביבה
        private string? GetGeminiApiKey()
        {
            // קודם קוראים מהקונפיג המקומי; אם אין, נופלים ל-ENV לפרודקשן.
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            return apiKey;
        }

        // שליפת endpoint עם ברירת מחדל
        private string GetGeminiEndpoint()
        {
            // אם אין ערך בקונפיג - משתמשים ב-endpoint ברירת מחדל של Google.
            var endpoint = configuration["Gemini:Endpoint"];
            return string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint;
        }

        // שליפת model עם ברירת מחדל
        private string GetGeminiModel()
        {
            // אם לא הוגדר מודל ספציפי - נופלים ל-model ברירת המחדל של השירות.
            var model = configuration["Gemini:Model"];
            return string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
        }

        // בניית URL של קריאת generateContent
        // יוצר URL תקין ל-generateContent כולל Escape ל-model ול-apiKey.
        private static string BuildGenerateContentUrl(string endpoint, string model, string apiKey) =>
            $"{endpoint.TrimEnd('/')}/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        // קריאה אחידה ל-Gemini והחזרת טקסט
        private async Task<string?> GenerateTextAsync(string url, object payload, string scope, CancellationToken cancellationToken)
        {
            // יצירת HttpClient דרך factory (תואם DI)
            using var client = httpClientFactory.CreateClient();
            // שליחת בקשת POST עם JSON
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // קריאת גוף שגיאה לצרכי דיבוג ולוג
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Gemini {Scope} request failed ({StatusCode}): {Body}", scope, (int)response.StatusCode, errorBody);
                throw new GeminiHttpException();
            }

            // קריאת JSON תשובה וניתוחו
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return TryExtractCandidateText(doc);
        }

        // חילוץ טקסט בטוח מתשובת Gemini
        private static string? TryExtractCandidateText(JsonDocument doc)
        {
            // בדיקת מבנה בסיסי: חייב להיות candidates[0] כדי להמשיך חילוץ.
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var first = candidates[0];
            // שליפה מהמבנה: candidates[0].content.parts[0].text
            if (!first.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array ||
                parts.GetArrayLength() == 0)
            {
                return null;
            }

            // שלב סופי: חילוץ הטקסט עצמו מה-part הראשון.
            return parts[0].TryGetProperty("text", out var textNode)
                ? textNode.GetString()
                : null;
        }

        // חריגה פנימית להבדיל שגיאת HTTP משגיאות אחרות
        private sealed class GeminiHttpException : Exception;
    }

    public sealed record GeminiAdviceResult(bool Success, string Advice, string? ErrorMessage)
    {
        // מחזיר תשובת הצלחה למסלול עצת שאלה.
        public static GeminiAdviceResult Ok(string advice) => new(true, advice, null);
        // מחזיר תשובת כשל אחידה למסלול עצת שאלה.
        public static GeminiAdviceResult Fail(string errorMessage) => new(false, "", errorMessage);
    }

    // הודעת צ'אט בודדת עם תפקיד וטקסט
    public sealed record GeminiChatTurn(string Role, string Text);

    // אובייקט תשובת אסיסטנט אחיד
    public sealed record GeminiAssistantReply(bool Success, string Text, string? ErrorMessage)
    {
        // מחזיר תשובת הצלחה למסלול האסיסטנט האישי.
        public static GeminiAssistantReply Ok(string text) => new(true, text, null);
        // מחזיר תשובת כשל אחידה למסלול האסיסטנט האישי.
        public static GeminiAssistantReply Fail(string errorMessage) => new(false, "", errorMessage);
    }
}
