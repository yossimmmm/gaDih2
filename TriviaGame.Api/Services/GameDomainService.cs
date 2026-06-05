using DBL;
using Models;
using TriviaGame.Api.Contracts;

namespace TriviaGame.Api.Services;

// השירות הזה יושב בין ה־controllers לשכבת ה־DB בכל מה שקשור למשחק.
// הוא מרכז את חוקי העסק במקום לפזר אותם בין ה־endpoints.
public sealed class GameDomainService
{
    // מתחיל סיבוב חדש על ידי בחירת שאלות לחדר.
    public async Task<(bool Ok, string Message, int Inserted)> StartGameAsync(int roomId, int questionCount)
    {
        // מספר שאלות חייב להיות חיובי לפני שפונים למסד.
        if (questionCount <= 0)
            return (false, "Question count must be positive.", 0);

        var gameDb = new GameDB();

        // PickQuestionsForRoomAsync מנקה סיבוב קודם ובוחר שאלות חדשות לחדר.
        var inserted = await gameDb.PickQuestionsForRoomAsync(roomId, questionCount);

        // אם לא שובצה אף שאלה, כנראה שאין שאלות זמינות בסוג שנבחר.
        return inserted <= 0
            ? (false, "No questions available. Add questions to DB first.", 0)
            : (true, "Game started.", inserted);
    }

    // קורא את השאלה הפעילה של החדר, אם יש כזאת.
    public async Task<Question?> GetCurrentQuestionAsync(int roomId)
    {
        var gameDb = new GameDB();

        // אם אין יותר שאלות, GameDB מחזיר null וה-controller מסמן finished=true.
        return await gameDb.GetCurrentQuestionAsync(roomId);
    }

    // מאמת ושומר הגשת תשובה אחת.
    public async Task<(bool Ok, string Message)> SubmitAnswerAsync(SubmitAnswerRequest req)
    {
        // כל המזהים חייבים להיות חיוביים כדי שהתשובה תהיה משמעותית.
        if (req.RoomPlayerId <= 0 || req.QuestionId <= 0 || req.OptionId <= 0)
            return (false, "Invalid answer payload.");

        var gameDb = new GameDB();

        // GameDB בודק שה-option באמת שייך לשאלה ושומר אם התשובה נכונה.
        var ok = await gameDb.SubmitAnswerAsync(req.RoomPlayerId, req.QuestionId, req.OptionId);
        return ok ? (true, "Answer submitted.") : (false, "Failed to submit answer.");
    }

    // אחרי שהסיבוב נגמר, שומר את תוצאות החדר בטבלת התוצאות.
    public async Task SaveRoomResultsAsync(int roomId)
    {
        var gameDb = new GameDB();

        // שומר סיכום תוצאות ב-game_results כדי שיהיו סטטיסטיקות גם אחרי שהחדר נסגר.
        await gameDb.SaveRoomResultsAsync(roomId);
    }

    // מחזיר את טבלת הניקוד החיה של החדר.
    public async Task<List<ScoreRow>> GetScoreboardAsync(int roomId)
    {
        var gameDb = new GameDB();

        // ScoreRow מייצג שחקן אחד בתוך scoreboard של חדר אחד.
        return await gameDb.GetScoreboardAsync(roomId);
    }

    // מחזיר כמה שאלות שובצו לחדר.
    public async Task<int> GetRoomQuestionCountAsync(int roomId)
    {
        var gameDb = new GameDB();

        // המספר הזה עוזר ללקוח להציג ניקוד מתוך סך שאלות.
        return await gameDb.GetRoomQuestionCountAsync(roomId);
    }

    // מחזיר סטטיסטיקה מצטברת של משתמש אחד.
    public async Task<(int GamesPlayed, int Wins, int Correct, int Answered)> GetUserStatsAsync(int userId)
    {
        var gameDb = new GameDB();

        // הסטטיסטיקה מחושבת מכל השורות של המשתמש בטבלת game_results.
        return await gameDb.GetUserStatsAsync(userId);
    }

    // מחזיר את רשימת ה־leaderboard למסך הסטטיסטיקות.
    public async Task<List<TopPlayerRow>> GetTopPlayersAsync(int limit)
    {
        var gameDb = new GameDB();

        // TopPlayerRow מייצג שחקן בדירוג גלובלי, לא בחדר ספציפי.
        return await gameDb.GetTopPlayersAsync(limit);
    }

    // מחזיר את שורות תוצאות המשחק האחרונות של משתמש אחד.
    public async Task<List<(DateTime CreatedAt, string RoomName, int CorrectCount, int AnsweredCount, bool IsWinner)>> GetRecentResultsAsync(int userId, int limit)
    {
        var gameDb = new GameDB();

        // ההיסטוריה האחרונה משמשת את מסך הפרופיל ואת העוזר החכם.
        return await gameDb.GetRecentUserResultsAsync(userId, limit);
    }
}
