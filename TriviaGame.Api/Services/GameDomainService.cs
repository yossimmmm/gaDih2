using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// שכבת הדומיין של המשחק עצמו.
// כל חישוב, שליפה או שמירה של מצב משחק קורים כאן, לא ב-controller.
public sealed class GameDomainService
{
    // מתחיל משחק ע"י בחירת סט שאלות לחדר.
    public async Task<(bool Ok, string Message, int Inserted)> StartGameAsync(int roomId, int questionCount)
    {
        // השרת בוחר את השאלות ושומר אותן לחדר; הלקוח רק מבקש להתחיל.
        if (questionCount <= 0)
            return (false, "Question count must be positive.", 0);

        var gameDb = new GameDB();
        var inserted = await gameDb.PickQuestionsForRoomAsync(roomId, questionCount);
        return inserted <= 0
            ? (false, "No questions available. Add questions to DB first.", 0)
            : (true, "Game started.", inserted);
    }

    // מחזיר את השאלה הפעילה כרגע לחדר.
    public async Task<Question?> GetCurrentQuestionAsync(int roomId)
    {
        // כל מי שמבקש את אותו חדר מקבל את אותה שאלה פעילה מהשרת.
        var gameDb = new GameDB();
        return await gameDb.GetCurrentQuestionAsync(roomId);
    }

    // שומר תשובה אחת של שחקן.
    public async Task<(bool Ok, string Message)> SubmitAnswerAsync(SubmitAnswerRequest req)
    {
        // ה-UI שולח מזהים בלבד; החישוב והאימות נשמרים בשרת.
        if (req.RoomPlayerId <= 0 || req.QuestionId <= 0 || req.OptionId <= 0)
            return (false, "Invalid answer payload.");

        var gameDb = new GameDB();
        var ok = await gameDb.SubmitAnswerAsync(req.RoomPlayerId, req.QuestionId, req.OptionId);
        return ok ? (true, "Answer submitted.") : (false, "Failed to submit answer.");
    }

    // שומר את תוצאות החדר אחרי משחק או אחרי תשובה.
    public async Task SaveRoomResultsAsync(int roomId)
    {
        // אחרי המשחק, השרת ממיר את המצב הנוכחי לתוצאות קבועות.
        var gameDb = new GameDB();
        await gameDb.SaveRoomResultsAsync(roomId);
    }

    // מחזיר טבלת ניקוד מוכנה להצגה.
    public async Task<List<ScoreRow>> GetScoreboardAsync(int roomId)
    {
        // ה-UI מקבל טבלה מוכנה להצגה, בלי לחשב ניקוד מקומית.
        var gameDb = new GameDB();
        return await gameDb.GetScoreboardAsync(roomId);
    }

    // מחזיר את מספר השאלות שהוקצו לחדר.
    public async Task<int> GetRoomQuestionCountAsync(int roomId)
    {
        var gameDb = new GameDB();
        return await gameDb.GetRoomQuestionCountAsync(roomId);
    }

    // מחזיר סטטיסטיקות אישיות של משתמש.
    public async Task<(int GamesPlayed, int Wins, int Correct, int Answered)> GetUserStatsAsync(int userId)
    {
        // משתמשים ב-userId מפורש במקום session כדי שהזרימה תישאר פשוטה.
        var gameDb = new GameDB();
        return await gameDb.GetUserStatsAsync(userId);
    }

    // מחזיר את רשימת המובילים.
    public async Task<List<TopPlayerRow>> GetTopPlayersAsync(int limit)
    {
        var gameDb = new GameDB();
        return await gameDb.GetTopPlayersAsync(limit);
    }

    // מחזיר היסטוריית משחקים אחרונים למשתמש.
    public async Task<List<(DateTime CreatedAt, string RoomName, int CorrectCount, int AnsweredCount, bool IsWinner)>> GetRecentResultsAsync(int userId, int limit)
    {
        var gameDb = new GameDB();
        return await gameDb.GetRecentUserResultsAsync(userId, limit);
    }
}
