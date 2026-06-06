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
    // שירות שעוטף את ה-API של Gemini ומכין לו prompt לפי הצורך.
    // ?????? ??? ???? ??? ????? ?????/?????? ???? Gemini ??? prompt ?????.
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
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            this.logger = logger;
        }

        // יוצר hint קצר לשאלה הנוכחית בלי לחשוף את התשובה המלאה.
        public async Task<GeminiAdviceResult> GetAdviceAsync(Question question, CancellationToken cancellationToken = default)
        {
            // ולידציה בסיסית לקלט
            // ??? ???? ????? ?? ??? ??????? ??? ????? ?? ?? ?????.
            if (question is null || question.Options.Count == 0)
                return GeminiAdviceResult.Fail("No active question to advise on.");

            // ?? ??? ???? API, ?????? ???? ??????? ???? ??? ??????.
            var apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return GeminiAdviceResult.Fail("Gemini API key is missing. Set Gemini:ApiKey or GEMINI_API_KEY.");

            var endpoint = GetGeminiEndpoint();
            var model = GetGeminiModel();

            // בניית טקסט אופציות לפורמט קריא למודל
            // ???????? ?????? ????? ????? ??? ?????? ???? ?????? ??????.
            var optionsText = string.Join(
                Environment.NewLine,
                question.Options.Select((o, index) => $"{(char)('A' + index)}. {o.OptionText}"));

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

            // ?-payload ??? ?-JSON ?-Gemini ???? ????.
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
                // ???? ?? ????? ?????? ????? ?? ?-request ?????.
                // ?????? ???? ?????? ??? helper ??? ??? ???? ????? ??????? ?-parsing.
                var url = BuildGenerateContentUrl(endpoint, model, apiKey);
                var text = await GenerateTextAsync(url, payload, "advice", cancellationToken);
                return string.IsNullOrWhiteSpace(text)
                    ? GeminiAdviceResult.Fail("Gemini returned an empty response.")
                    : GeminiAdviceResult.Ok(text.Trim());
            }
            catch (GeminiHttpException)
            {
                return GeminiAdviceResult.Fail("Gemini is unavailable right now.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gemini advice request threw an exception.");
                return GeminiAdviceResult.Fail("Failed to contact Gemini.");
            }
        }

        // מייצר תשובה אישית למשתמש לפי היסטוריה וסטטיסטיקות.
        public async Task<GeminiAssistantReply> GetPersonalAssistantReplyAsync(
            int userId,
            string userMessage,
            IReadOnlyList<GeminiChatTurn>? chatHistory = null,
            CancellationToken cancellationToken = default)
        {
            // ולידציה בסיסית לקלט
            // assistant ???? ???? ?? ?????? ?????.
            if (userId <= 0)
                return GeminiAssistantReply.Fail("You must be logged in.");
            if (string.IsNullOrWhiteSpace(userMessage))
                return GeminiAssistantReply.Fail("Message is empty.");

            var apiKey = GetGeminiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return GeminiAssistantReply.Fail("Gemini API key is missing. Set Gemini:ApiKey or GEMINI_API_KEY.");

            var endpoint = GetGeminiEndpoint();
            var model = GetGeminiModel();

            // שליפת נתוני משתמש וסטטיסטיקות
            // ?????? ????? ????? ??????????? ??? ?????? ???? ????? ?-prompt.
            var userDb = new UserDB();
            var gameDb = new GameDB();

            var user = await userDb.GetByIdAsync(userId);
            if (user is null)
                return GeminiAssistantReply.Fail("User not found.");

            var stats = await gameDb.GetUserStatsAsync(userId);
            var recentGames = await gameDb.GetRecentUserResultsAsync(userId, 10);

            // ?? ??????? ???????? ?????? ?????, ??? ?????? ???? ????? ??????.
            var recentGamesText = recentGames.Count == 0
                ? "No recent games."
                : string.Join(Environment.NewLine, recentGames.Select((g, idx) =>
                    $"{idx + 1}. {g.CreatedAt:yyyy-MM-dd HH:mm} | {g.RoomName} | Correct {g.CorrectCount}/{g.AnsweredCount} | Winner: {(g.IsWinner ? "Yes" : "No")}"));

            // ?? ????? ?????? ?????? ????? ??? ????? ?? continuity.
            var historyText = (chatHistory is null || chatHistory.Count == 0)
                ? "No previous chat history."
                : string.Join(Environment.NewLine, chatHistory.TakeLast(12).Select(h =>
                    $"{h.Role.ToUpperInvariant()}: {h.Text}"));

            // חישובים נגזרים
            var accuracy = stats.Answered == 0 ? 0 : (double)stats.Correct / stats.Answered * 100.0;
            var winningCoefficient = stats.GamesPlayed == 0 ? 0 : (double)stats.Wins / stats.GamesPlayed;
            var winningPercent = winningCoefficient * 100.0;

            // תשובת fallback דטרמיניסטית לשאלות win rate/coefficient
            // ??? ??????? ???? ????? ????? ?????? ??? ????? ???? ?-Gemini.
            var normalizedMessage = userMessage.Trim().ToLowerInvariant();
            var asksWinningCoefficient =
                normalizedMessage.Contains("cooficent") ||
                normalizedMessage.Contains("coefficient") ||
                normalizedMessage.Contains("win rate") ||
                normalizedMessage.Contains("winning rate") ||
                normalizedMessage.Contains("winning coefficient");
            if (asksWinningCoefficient)
            {
                if (stats.GamesPlayed == 0)
                {
                    return GeminiAssistantReply.Ok(
                        "You have no completed games yet, so your winning coefficient is currently 0.00 (0%).");
                }

                return GeminiAssistantReply.Ok(
                    $"Your winning coefficient is {winningCoefficient:F2} ({winningPercent:F1}%), based on {stats.Wins} wins out of {stats.GamesPlayed} games.");
            }

            // ?-prompt ????? ?????? ??????: ????? ?? ??? ??????? ????????.
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
                var url = BuildGenerateContentUrl(endpoint, model, apiKey);
                var text = await GenerateTextAsync(url, payload, "personal assistant", cancellationToken);
                return string.IsNullOrWhiteSpace(text)
                    ? GeminiAssistantReply.Fail("Gemini returned an empty response.")
                    : GeminiAssistantReply.Ok(text.Trim());
            }
            catch (GeminiHttpException)
            {
                return GeminiAssistantReply.Fail("Gemini is unavailable right now.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gemini personal assistant request threw an exception.");
                return GeminiAssistantReply.Fail("Failed to contact Gemini.");
            }
        }

        // שליפת מפתח Gemini מהגדרות או משתנה סביבה
        // שולף את מפתח ה-API מקונפיגורציה או ממשתני סביבה.
        private string? GetGeminiApiKey()
        {
            // ???? ?????? ????????????, ??? ??? - ?????? ?-env var.
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            return apiKey;
        }

        // שליפת endpoint עם ברירת מחדל
        // שולף את כתובת ה-endpoint של Gemini או ברירת מחדל.
        private string GetGeminiEndpoint()
        {
            // endpoint ????? ???? ???? ??? ??? ?? ????? ?? ???? ?? ??? ?????.
            var endpoint = configuration["Gemini:Endpoint"];
            return string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint;
        }

        // שליפת model עם ברירת מחדל
        // שולף את שם המודל או ערך ברירת מחדל.
        private string GetGeminiModel()
        {
            // ????? ???? ?????? ??? ????? ???.
            var model = configuration["Gemini:Model"];
            return string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
        }

        // בניית URL של קריאת generateContent
        // בונה את כתובת הקריאה ל-generateContent.
        private static string BuildGenerateContentUrl(string endpoint, string model, string apiKey) =>
            $"{endpoint.TrimEnd('/')}/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        // קריאה אחידה ל-Gemini והחזרת טקסט
        // שולח את ה-HTTP request ל-Gemini ומחזיר את הטקסט שחולץ מהתגובה.
        private async Task<string?> GenerateTextAsync(string url, object payload, string scope, CancellationToken cancellationToken)
        {
            // HttpClient ???? ??-factory ??? ?? ????? sockets ??? ????? ???? ??? ??? ?????.
            using var client = httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);

            // ?? Gemini ????? ????? ?? ????, ?????? ?? ???? ??????? ????.
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Gemini {Scope} request failed ({StatusCode}): {Body}", scope, (int)response.StatusCode, errorBody);
                throw new GeminiHttpException();
            }

            // ?-JSON ???? ?? Gemini ???? parsing ??? ????? ????? ??????.
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return TryExtractCandidateText(doc);
        }

        // חילוץ טקסט בטוח מתשובת Gemini
        // מחלץ טקסט מתוך מבנה ה-JSON של Gemini בצורה בטוחה.
        private static string? TryExtractCandidateText(JsonDocument doc)
        {
            // ?? ??? candidates, ??? ????? ??????.
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                return null;
            }

            // ?????? ?? candidate ?????? ?? ???? ??? ??? ?????? ???????.
            var first = candidates[0];
            if (!first.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array ||
                parts.GetArrayLength() == 0)
            {
                return null;
            }

            // ??????? ?? ???? text ?? ????.
            return parts[0].TryGetProperty("text", out var textNode)
                ? textNode.GetString()
                : null;
        }

        // חריגה פנימית להבדיל שגיאת HTTP משגיאות אחרות
        private sealed class GeminiHttpException : Exception;
    }

    public sealed record GeminiAdviceResult(bool Success, string Advice, string? ErrorMessage)
    {
        public static GeminiAdviceResult Ok(string advice) => new(true, advice, null);
        public static GeminiAdviceResult Fail(string errorMessage) => new(false, "", errorMessage);
    }

    // הודעת צ'אט בודדת עם תפקיד וטקסט
    public sealed record GeminiChatTurn(string Role, string Text);

    // אובייקט תשובת אסיסטנט אחיד
    public sealed record GeminiAssistantReply(bool Success, string Text, string? ErrorMessage)
    {
        public static GeminiAssistantReply Ok(string text) => new(true, text, null);
        public static GeminiAssistantReply Fail(string errorMessage) => new(false, "", errorMessage);
    }
}
