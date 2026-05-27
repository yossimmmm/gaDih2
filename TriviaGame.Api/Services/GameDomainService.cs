using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

public sealed class GameDomainService
{
    // התחלת משחק ע"י בחירת סט שאלות לחדר.
    public async Task<(bool Ok, string Message, int Inserted)> StartGameAsync(int roomId, int questionCount)
    {
        if (questionCount <= 0)
            return (false, "Question count must be positive.", 0);

        var gameDb = new GameDB();
        var inserted = await gameDb.PickQuestionsForRoomAsync(roomId, questionCount);
        return inserted <= 0
            ? (false, "No questions available. Add questions to DB first.", 0)
            : (true, "Game started.", inserted);
    }

    // שליפת שאלה נוכחית.
    public async Task<Question?> GetCurrentQuestionAsync(int roomId)
    {
        var gameDb = new GameDB();
        return await gameDb.GetCurrentQuestionAsync(roomId);
    }

    // שליחת תשובה לשאלה.
    public async Task<(bool Ok, string Message)> SubmitAnswerAsync(SubmitAnswerRequest req)
    {
        if (req.RoomPlayerId <= 0 || req.QuestionId <= 0 || req.OptionId <= 0)
            return (false, "Invalid answer payload.");

        var gameDb = new GameDB();
        var ok = await gameDb.SubmitAnswerAsync(req.RoomPlayerId, req.QuestionId, req.OptionId);
        return ok ? (true, "Answer submitted.") : (false, "Failed to submit answer.");
    }

    // שמירת תוצאות סופיות לחדר.
    public async Task SaveRoomResultsAsync(int roomId)
    {
        var gameDb = new GameDB();
        await gameDb.SaveRoomResultsAsync(roomId);
    }

    // טבלת ניקוד לחדר.
    public async Task<List<ScoreRow>> GetScoreboardAsync(int roomId)
    {
        var gameDb = new GameDB();
        return await gameDb.GetScoreboardAsync(roomId);
    }

    // מספר השאלות שהוגרלו לחדר.
    public async Task<int> GetRoomQuestionCountAsync(int roomId)
    {
        var gameDb = new GameDB();
        return await gameDb.GetRoomQuestionCountAsync(roomId);
    }

    // סטטיסטיקות משתמש.
    public async Task<(int GamesPlayed, int Wins, int Correct, int Answered)> GetUserStatsAsync(int userId)
    {
        var gameDb = new GameDB();
        return await gameDb.GetUserStatsAsync(userId);
    }

    // דירוג שחקנים מובילים.
    public async Task<List<TopPlayerRow>> GetTopPlayersAsync(int limit)
    {
        var gameDb = new GameDB();
        return await gameDb.GetTopPlayersAsync(limit);
    }

    // היסטוריית משחקים אחרונים.
    public async Task<List<(DateTime CreatedAt, string RoomName, int CorrectCount, int AnsweredCount, bool IsWinner)>> GetRecentResultsAsync(int userId, int limit)
    {
        var gameDb = new GameDB();
        return await gameDb.GetRecentUserResultsAsync(userId, limit);
    }
}
