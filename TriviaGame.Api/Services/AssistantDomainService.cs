using System.Net.Http.Json;
using System.Text.Json;
using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

public sealed class AssistantDomainService
{
    private const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta";
    private const string DefaultModel = "gemini-2.5-flash-lite";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;
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

    // עוזר לשאלה פעילה: מחזיר hint קצר בלי לחשוף תשובה.
    public async Task<(bool Ok, string Message)> GetAdviceAsync(Question question, CancellationToken cancellationToken = default)
    {
        if (question is null || question.Options.Count == 0)
            return (false, "No active question to advise on.");

        var apiKey = GetGeminiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "Gemini API key is missing.");

        var endpoint = GetGeminiEndpoint();
        var model = GetGeminiModel();
        var optionsText = string.Join(Environment.NewLine, question.Options.Select((o, i) => $"{(char)('A' + i)}. {o.OptionText}"));

        var prompt = $"""
You are a live trivia coach.
Give one short hint (max 70 words) for the current question.
Do not reveal the exact correct option.

Question:
{question.QuestionText}

Options:
{optionsText}
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
                temperature = 0.4,
                maxOutputTokens = 120
            }
        };

        try
        {
            var url = BuildGenerateContentUrl(endpoint, model, apiKey);
            var text = await GenerateTextAsync(url, payload, "advice", cancellationToken);
            return string.IsNullOrWhiteSpace(text)
                ? (false, "Gemini returned an empty response.")
                : (true, text.Trim());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gemini advice request failed.");
            return (false, "Gemini is unavailable right now.");
        }
    }

    // צ'אט אישי מבוסס נתוני משתמש.
    public async Task<(bool Ok, string Message)> GetPersonalReplyAsync(
        int userId,
        string userMessage,
        List<AssistantChatMessage>? history,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return (false, "You must be logged in.");
        if (string.IsNullOrWhiteSpace(userMessage))
            return (false, "Message is empty.");

        var apiKey = GetGeminiApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "Gemini API key is missing.");

        var userDb = new UserDB();
        var gameDb = new GameDB();
        var user = await userDb.GetByIdAsync(userId);
        if (user is null)
            return (false, "User not found.");

        var stats = await gameDb.GetUserStatsAsync(userId);
        var recent = await gameDb.GetRecentUserResultsAsync(userId, 10);
        var recentText = recent.Count == 0
            ? "No recent games."
            : string.Join(Environment.NewLine, recent.Select((g, i) =>
                $"{i + 1}. {g.CreatedAt:yyyy-MM-dd HH:mm} | {g.RoomName} | Correct {g.CorrectCount}/{g.AnsweredCount} | Winner: {(g.IsWinner ? "Yes" : "No")}"));
        var historyText = history is null || history.Count == 0
            ? "No previous chat history."
            : string.Join(Environment.NewLine, history.TakeLast(12).Select(h => $"{h.Role.ToUpperInvariant()}: {h.Text}"));

        var accuracy = stats.Answered == 0 ? 0 : (double)stats.Correct / stats.Answered * 100.0;
        var winCoeff = stats.GamesPlayed == 0 ? 0 : (double)stats.Wins / stats.GamesPlayed;
        var winPercent = winCoeff * 100.0;

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
            var url = BuildGenerateContentUrl(GetGeminiEndpoint(), GetGeminiModel(), apiKey);
            var text = await GenerateTextAsync(url, payload, "assistant", cancellationToken);
            return string.IsNullOrWhiteSpace(text)
                ? (false, "Gemini returned an empty response.")
                : (true, text.Trim());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gemini personal assistant request failed.");
            return (false, "Gemini is unavailable right now.");
        }
    }

    private string? GetGeminiApiKey()
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        return apiKey;
    }

    private string GetGeminiEndpoint()
    {
        var endpoint = configuration["Gemini:Endpoint"];
        return string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint;
    }

    private string GetGeminiModel()
    {
        var model = configuration["Gemini:Model"];
        return string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
    }

    private static string BuildGenerateContentUrl(string endpoint, string model, string apiKey) =>
        $"{endpoint.TrimEnd('/')}/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

    private async Task<string?> GenerateTextAsync(string url, object payload, string scope, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Gemini {Scope} request failed ({StatusCode}): {Body}", scope, (int)response.StatusCode, body);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return TryExtractCandidateText(doc);
    }

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
