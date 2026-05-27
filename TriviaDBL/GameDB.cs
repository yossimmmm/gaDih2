// Implements:
// PickQuestionsForRoomAsync(roomId, count)
// GetCurrentQuestionAsync(roomId)
// SubmitAnswerAsync(roomPlayerId, questionId, optionId)
// GetScoreboardAsync(roomId)

using Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DBL
{
    public class GameDB
    {
        // מחרוזת חיבור למסד
        private const string ConnStr =
            "server=localhost;user id=root;password=999GtaS999An;persistsecurityinfo=True;database=trivia_game";

        // ---------------------------
        // PickQuestionsForRoomAsync
        // Clears existing room_questions + room answers, then picks random questions
        // and inserts them with question_order 1..count.
        // Returns number inserted.
        // ---------------------------
        // בחירת שאלות לחדר חדש ואיפוס נתוני שאלות/תשובות ישנים בחדר
        public async Task<int> PickQuestionsForRoomAsync(int roomId, int count)
        {
            // ולידציה בסיסית לקלט
            if (roomId <= 0 || count <= 0) 
                return 0;

            if (count > 50)
                throw new ArgumentException("Cannot pick more than 50 questions");

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // טרנזקציה כדי שכל שלבי בחירת השאלות יהיו אטומיים
                await using var tx = await conn.BeginTransactionAsync();

                try
                {
                    // מחיקת תשובות ישנות של החדר
                    {
                        const string delAnswers = @"DELETE FROM player_answers WHERE room_id = @room_id;";
                        await using var cmd = new MySqlCommand(delAnswers, conn, (MySqlTransaction)tx);
                        cmd.Parameters.AddWithValue("@room_id", roomId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // מחיקת שאלות ישנות של החדר
                    {
                        const string delRoomQs = @"DELETE FROM room_questions WHERE room_id = @room_id;";
                        await using var cmd = new MySqlCommand(delRoomQs, conn, (MySqlTransaction)tx);
                        cmd.Parameters.AddWithValue("@room_id", roomId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // שליפת סוג שאלות מועדף לפי הגדרות החדר
                    int? questionTypeId = null;
                    {
                        const string roomSql = @"SELECT question_type_id FROM rooms WHERE room_id = @room_id LIMIT 1;";
                        await using var cmd = new MySqlCommand(roomSql, conn, (MySqlTransaction)tx);
                        // שולחים room_id כפרמטר מאובטח
                        cmd.Parameters.AddWithValue("@room_id", roomId);
                        // מחזירים סוג שאלות מועדף אם הוגדר בחדר
                        var obj = await cmd.ExecuteScalarAsync();
                        if (obj != null && obj != DBNull.Value)
                            questionTypeId = Convert.ToInt32(obj);
                    }

                    // שליפה אקראית של שאלות למסגרת המשחק
                    List<int> qids = new();
                    {
                        const string pickSql = @"
SELECT question_id
FROM questions
WHERE (@qtype IS NULL OR question_type_id = @qtype)
ORDER BY RAND()
LIMIT @cnt;";
                        await using var cmd = new MySqlCommand(pickSql, conn, (MySqlTransaction)tx);
                        // כמות שאלות להגרלה
                        cmd.Parameters.AddWithValue("@cnt", count);
                        // אם qtype null -> הסינון הופך ל"כל הקטגוריות"
                        cmd.Parameters.AddWithValue("@qtype", (object?)questionTypeId ?? DBNull.Value);

                        await using var reader = await cmd.ExecuteReaderAsync();
                        // צבירת מזהי שאלות לרשימה זמנית
                        while (await reader.ReadAsync())
                            qids.Add(reader.GetInt32("question_id"));
                    }

                    if (qids.Count == 0)
                    {
                        await tx.CommitAsync();
                        return 0;
                    }

                    // שמירת סדר השאלות בחדר
                    int inserted = 0;
                    for (int i = 0; i < qids.Count; i++)
                    {
                        const string insSql = @"
INSERT INTO room_questions (room_id, question_id, question_order, time_limit_sec)
VALUES (@room_id, @qid, @ord, 15);";

                        await using var cmd = new MySqlCommand(insSql, conn, (MySqlTransaction)tx);
                        // קישור שאלה לחדר + סדר שאלה
                        cmd.Parameters.AddWithValue("@room_id", roomId);
                        cmd.Parameters.AddWithValue("@qid", qids[i]);
                        cmd.Parameters.AddWithValue("@ord", i + 1);

                        inserted += await cmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    return inserted;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while picking questions: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error picking questions: {ex.Message}", ex);
            }
        }

        // ---------------------------
        // GetCurrentQuestionAsync
        // Current question is the earliest one not fully answered and not expired.
        // When a question is first served, started_at is set.
        // ---------------------------
        // שליפת השאלה הנוכחית לחדר כולל התחלת טיימר אם זו שאלה חדשה
        public async Task<Question?> GetCurrentQuestionAsync(int roomId)
        {
            // ולידציה בסיסית למזהה חדר
            if (roomId <= 0)
                return null;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // שליפת מספר שחקנים פעילים לצורך קביעת התקדמות שאלה
                var playersCount = await GetRoomPlayersCountAsync(conn, roomId);
                // שליפת השאלה הפעילה הנוכחית
                var current = await TryGetCurrentRoomQuestionAsync(conn, roomId, playersCount);
                if (current is null)
                    return null;

                // טעינת גוף שאלה + אופציות
                var q = await LoadQuestionAsync(conn, current.Value.QuestionId, current.Value.TimeLimitSec, current.Value.StartedAt);
                if (q is null)
                    return null;

                q.Options = await LoadQuestionOptionsAsync(conn, q.QuestionID);

                return q;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching current question: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching current question: {ex.Message}", ex);
            }
        }

        // ---------------------------
        // SubmitAnswerAsync
        // Ensures only one answer per (room_player_id, question_id) by:
        // - deleting any previous row(s)
        // - inserting new answer with is_correct derived from option
        // Returns true if inserted.
        // ---------------------------
        // שמירת תשובת שחקן לשאלה (עם מניעת כפילויות לאותו שחקן ושאלה)
        public async Task<bool> SubmitAnswerAsync(int roomPlayerId, int questionId, int optionId)
        {
            // ולידציה בסיסית לקלט תשובה
            if (roomPlayerId <= 0 || questionId <= 0 || optionId <= 0) 
                return false;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();
                // טרנזקציה כדי לשמור עקביות בין מחיקה/הוספה
                await using var tx = await conn.BeginTransactionAsync();

                try
                {
                    int roomId;
                    // שליפת room_id של השחקן וגם אימות שהשחקן קיים
                    {
                        const string rpSql = @"
SELECT room_id
FROM room_players
WHERE room_player_id = @rpid
LIMIT 1;";
                        await using var cmd = new MySqlCommand(rpSql, conn, (MySqlTransaction)tx);
                        cmd.Parameters.AddWithValue("@rpid", roomPlayerId);
                        var obj = await cmd.ExecuteScalarAsync();
                        if (obj == null || obj == DBNull.Value)
                        {
                            await tx.RollbackAsync();
                            return false;
                        }
                        roomId = Convert.ToInt32(obj);
                    }

                    // אימות שהאפשרות שייכת לשאלה ושליפת דגל תשובה נכונה
                    bool isCorrect;
                    {
                        const string chkSql = @"
SELECT is_correct
FROM question_options
WHERE option_id = @oid AND question_id = @qid
LIMIT 1;";
                        await using var cmd = new MySqlCommand(chkSql, conn, (MySqlTransaction)tx);
                        // אימות option_id תואם לשאלה
                        cmd.Parameters.AddWithValue("@oid", optionId);
                        cmd.Parameters.AddWithValue("@qid", questionId);

                        var obj = await cmd.ExecuteScalarAsync();
                        if (obj == null || obj == DBNull.Value)
                        {
                            await tx.RollbackAsync();
                            return false;
                        }

                        isCorrect = Convert.ToInt32(obj) == 1;
                    }

                    // מחיקת תשובה קודמת של אותו שחקן לאותה שאלה
                    {
                        const string delSql = @"
DELETE FROM player_answers
WHERE room_player_id = @rpid AND question_id = @qid;";
                        await using var cmd = new MySqlCommand(delSql, conn, (MySqlTransaction)tx);
                        // מבטיחים answer יחיד לשחקן בכל שאלה
                        cmd.Parameters.AddWithValue("@rpid", roomPlayerId);
                        cmd.Parameters.AddWithValue("@qid", questionId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // הכנסת התשובה החדשה למסד
                    {
                        const string insSql = @"
INSERT INTO player_answers (room_id, room_player_id, question_id, selected_option_id, is_correct, answered_at)
VALUES (@room_id, @rpid, @qid, @oid, @isc, NOW());";
                        await using var cmd = new MySqlCommand(insSql, conn, (MySqlTransaction)tx);
                        // שמירת room_id הכרחית לצבירת סטטיסטיקה ברמת חדר
                        cmd.Parameters.AddWithValue("@room_id", roomId);
                        cmd.Parameters.AddWithValue("@rpid", roomPlayerId);
                        cmd.Parameters.AddWithValue("@qid", questionId);
                        cmd.Parameters.AddWithValue("@oid", optionId);
                        cmd.Parameters.AddWithValue("@isc", isCorrect ? 1 : 0);

                        var rows = await cmd.ExecuteNonQueryAsync();
                        await tx.CommitAsync();
                        return rows == 1;
                    }
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while submitting answer: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error submitting answer: {ex.Message}", ex);
            }
        }

        // ---------------------------
        // GetScoreboardAsync
        // For each room_player: counts correct + total answered
        // ---------------------------
        // שליפת טבלת ניקוד מלאה של החדר
        public async Task<List<ScoreRow>> GetScoreboardAsync(int roomId)
        {
            // יצירת אוסף תוצאות להחזרה
            var result = new List<ScoreRow>();
            if (roomId <= 0) 
                return result;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // חישוב ניקוד לכל שחקן בחדר
                const string sql = @"
SELECT
  rp.room_player_id,
  rp.user_id,
  rp.nickname,
  COALESCE(SUM(pa.is_correct), 0) AS correct_count,
  COALESCE(COUNT(pa.player_answer_id), 0) AS answered_count
FROM room_players rp
LEFT JOIN player_answers pa
  ON pa.room_player_id = rp.room_player_id
  AND pa.room_id = rp.room_id
WHERE rp.room_id = @room_id
GROUP BY rp.room_player_id, rp.user_id, rp.nickname
ORDER BY correct_count DESC, answered_count DESC, rp.nickname ASC;";

                await using var cmd = new MySqlCommand(sql, conn);
                // מזהה חדר עבורו מחשבים scoreboard
                cmd.Parameters.AddWithValue("@room_id", roomId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // המרת תוצאת SQL לשורת ניקוד במודל
                    result.Add(new ScoreRow
                    {
                        RoomPlayerID = reader.GetInt32("room_player_id"),
                        UserID = reader.GetInt32("user_id"),
                        Nickname = reader.GetString("nickname"),
                        CorrectCount = Convert.ToInt32(reader["correct_count"]),
                        AnsweredCount = Convert.ToInt32(reader["answered_count"])
                    });
                }

                return result;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching scoreboard: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching scoreboard: {ex.Message}", ex);
            }
        }
        
        // בדיקה האם שחקן מסוים כבר ענה על השאלה הנוכחית
        public async Task<bool> HasPlayerAnsweredAsync(int roomId, int roomPlayerId, int questionId)
        {
            if (roomId <= 0 || roomPlayerId <= 0 || questionId <= 0)
                return false;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

            const string sql = @"
SELECT 1
FROM player_answers
WHERE room_id = @room_id
  AND room_player_id = @rpid
  AND question_id = @qid
LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@room_id", roomId);
            cmd.Parameters.AddWithValue("@rpid", roomPlayerId);
            cmd.Parameters.AddWithValue("@qid", questionId);

            var obj = await cmd.ExecuteScalarAsync();
            return obj != null;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while checking answer status: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking answer status: {ex.Message}", ex);
            }
        }

        // ספירת כמות השאלות המשויכות לחדר
        public async Task<int> GetRoomQuestionCountAsync(int roomId)
        {
            if (roomId <= 0)
                return 0;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
SELECT COUNT(*)
FROM room_questions
WHERE room_id = @room_id;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@room_id", roomId);

                var obj = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(obj);
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while counting room questions: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error counting room questions: {ex.Message}", ex);
            }
        }

        // ספירת כמות התשובות ששחקן נתן בחדר
        public async Task<int> GetPlayerAnswerCountAsync(int roomId, int roomPlayerId)
        {
            if (roomId <= 0 || roomPlayerId <= 0)
                return 0;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
SELECT COUNT(*)
FROM player_answers
WHERE room_id = @room_id
  AND room_player_id = @room_player_id;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@room_id", roomId);
                cmd.Parameters.AddWithValue("@room_player_id", roomPlayerId);

                var obj = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(obj);
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while counting player answers: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error counting player answers: {ex.Message}", ex);
            }
        }

        // שמירת תוצאות סופיות של חדר לניתוח ביצועי משתמשים בהמשך
        public async Task SaveRoomResultsAsync(int roomId)
        {
            // אם מזהה חדר לא תקין אין מה לשמור
            if (roomId <= 0)
                return;

            try
            {
                var rows = await GetScoreboardAsync(roomId);
                if (rows.Count == 0)
                    return;

                // שליפת מידע עזר לקביעת מנצחים
                var totalQuestions = await GetRoomQuestionCountAsync(roomId);
                var isSinglePlayer = rows.Count == 1;
                var maxCorrect = GetMaxCorrect(rows);

                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // שמירת תוצאות לכל משתמש בחדר (insert/update)
                const string sql = @"
INSERT INTO game_results (room_id, user_id, correct_count, answered_count, is_winner)
VALUES (@room_id, @user_id, @correct_count, @answered_count, @is_winner)
ON DUPLICATE KEY UPDATE
  correct_count = VALUES(correct_count),
  answered_count = VALUES(answered_count),
  is_winner = VALUES(is_winner);";

                foreach (var row in rows)
                {
                    // קביעת האם המשתמש נחשב מנצח לפי חוקי המשחק
                    var isWinner = IsWinnerRow(row, isSinglePlayer, totalQuestions, maxCorrect);

                    await using var cmd = new MySqlCommand(sql, conn);
                    // עדכון תוצאה לכל משתמש בחדר הנוכחי
                    cmd.Parameters.AddWithValue("@room_id", roomId);
                    cmd.Parameters.AddWithValue("@user_id", row.UserID);
                    cmd.Parameters.AddWithValue("@correct_count", row.CorrectCount);
                    cmd.Parameters.AddWithValue("@answered_count", row.AnsweredCount);
                    cmd.Parameters.AddWithValue("@is_winner", isWinner ? 1 : 0);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while saving room results: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving room results: {ex.Message}", ex);
            }
        }

        // שליפת סטטיסטיקת משתמש מצטברת (משחקים, ניצחונות, תשובות נכונות וכו')
        public async Task<(int GamesPlayed, int Wins, int Correct, int Answered)> GetUserStatsAsync(int userId)
        {
            if (userId <= 0)
                return (0, 0, 0, 0);

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
SELECT
  COUNT(*) AS games_played,
  COALESCE(SUM(is_winner), 0) AS wins,
  COALESCE(SUM(correct_count), 0) AS correct_count,
  COALESCE(SUM(answered_count), 0) AS answered_count
FROM game_results
WHERE user_id = @user_id;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@user_id", userId);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return (0, 0, 0, 0);

                return (
                    reader.GetInt32("games_played"),
                    Convert.ToInt32(reader["wins"]),
                    Convert.ToInt32(reader["correct_count"]),
                    Convert.ToInt32(reader["answered_count"])
                );
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching user stats: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching user stats: {ex.Message}", ex);
            }
        }

        // שליפת טבלת שחקנים מובילים לפי דירוג ביצועים
        public async Task<List<TopPlayerRow>> GetTopPlayersAsync(int limit)
        {
            var result = new List<TopPlayerRow>();
            if (limit <= 0)
                return result;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                const string sql = @"
SELECT
  u.user_id,
  u.username,
  COALESCE(COUNT(gr.game_result_id), 0) AS games_played,
  COALESCE(SUM(gr.is_winner), 0) AS wins,
  COALESCE(SUM(gr.correct_count), 0) AS correct_count,
  COALESCE(SUM(gr.answered_count), 0) AS answered_count
FROM users u
LEFT JOIN game_results gr ON gr.user_id = u.user_id
GROUP BY u.user_id, u.username
ORDER BY
  (CASE WHEN COALESCE(SUM(gr.answered_count), 0) = 0 THEN 0
        ELSE (COALESCE(SUM(gr.correct_count), 0) / COALESCE(SUM(gr.answered_count), 0)) END) DESC,
  COALESCE(SUM(gr.correct_count), 0) DESC,
  COALESCE(SUM(gr.is_winner), 0) DESC,
  COALESCE(COUNT(gr.game_result_id), 0) DESC,
  u.username ASC
LIMIT @limit;";

                await using var cmd = new MySqlCommand(sql, conn);
                // פרמטר limit מגן מפני שליפת יתר
                cmd.Parameters.AddWithValue("@limit", limit);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new TopPlayerRow
                    {
                        UserID = reader.GetInt32("user_id"),
                        Username = reader.GetString("username"),
                        GamesPlayed = Convert.ToInt32(reader["games_played"]),
                        Wins = Convert.ToInt32(reader["wins"]),
                        CorrectCount = Convert.ToInt32(reader["correct_count"]),
                        AnsweredCount = Convert.ToInt32(reader["answered_count"])
                    });
                }

                return result;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching top players: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching top players: {ex.Message}", ex);
            }
        }

        public async Task<List<(DateTime CreatedAt, string RoomName, int CorrectCount, int AnsweredCount, bool IsWinner)>> GetRecentUserResultsAsync(int userId, int limit)
        {
            // רשימת תוצאות אחרונות לפי משתמש לצורך הצגת היסטוריה אישית
            var result = new List<(DateTime CreatedAt, string RoomName, int CorrectCount, int AnsweredCount, bool IsWinner)>();
            if (userId <= 0 || limit <= 0)
                return result;

            try
            {
                await using var conn = new MySqlConnection(ConnStr);
                await conn.OpenAsync();

                // שליפת משחקים אחרונים מטבלת game_results + שם חדר אם קיים
                const string sql = @"
SELECT
  gr.created_at,
  COALESCE(r.room_name, 'Unknown Room') AS room_name,
  gr.correct_count,
  gr.answered_count,
  gr.is_winner
FROM game_results gr
LEFT JOIN rooms r ON r.room_id = gr.room_id
WHERE gr.user_id = @user_id
ORDER BY gr.created_at DESC
LIMIT @limit;";

                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.Parameters.AddWithValue("@limit", limit);

                // המרה לשכבת המודל של האפליקציה (tuple פשוט)
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add((
                        reader.GetDateTime("created_at"),
                        reader.GetString("room_name"),
                        Convert.ToInt32(reader["correct_count"]),
                        Convert.ToInt32(reader["answered_count"]),
                        Convert.ToInt32(reader["is_winner"]) == 1
                    ));
                }

                return result;
            }
            catch (MySqlException ex)
            {
                throw new Exception($"Database error while fetching recent user results: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching recent user results: {ex.Message}", ex);
            }
        }

        // חישוב מקסימום תשובות נכונות מכל שורות הניקוד
        private static int GetMaxCorrect(List<ScoreRow> rows)
        {
            var maxCorrect = 0;
            foreach (var row in rows)
            {
                if (row.CorrectCount > maxCorrect)
                    maxCorrect = row.CorrectCount;
            }

            return maxCorrect;
        }

        // קביעת מנצח לפי כללי חדר יחיד/מרובה משתתפים
        private static bool IsWinnerRow(ScoreRow row, bool isSinglePlayer, int totalQuestions, int maxCorrect)
        {
            if (isSinglePlayer)
            {
                return totalQuestions > 0
                       && row.AnsweredCount == totalQuestions
                       && row.CorrectCount == totalQuestions;
            }

            return row.CorrectCount == maxCorrect;
        }

        // שליפת כמות שחקנים בחדר
        private static async Task<int> GetRoomPlayersCountAsync(MySqlConnection conn, int roomId)
        {
            const string playersSql = @"SELECT COUNT(*) FROM room_players WHERE room_id = @room_id;";
            await using var cmd = new MySqlCommand(playersSql, conn);
            cmd.Parameters.AddWithValue("@room_id", roomId);
            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        // מציאת שאלה פעילה/חדשה ראשונה בחדר והתחלת טיימר כשצריך
        private static async Task<(int QuestionId, int TimeLimitSec, DateTime? StartedAt)?> TryGetCurrentRoomQuestionAsync(MySqlConnection conn, int roomId, int playersCount)
        {
            // רשימת שאלות החדר כולל ספירת תשובות לכל שאלה
            const string listSql = @"
SELECT rq.question_id, rq.time_limit_sec, rq.started_at,
       COALESCE(a.answers_count, 0) AS answers_count
FROM room_questions rq
LEFT JOIN (
    SELECT question_id, COUNT(DISTINCT room_player_id) AS answers_count
    FROM player_answers
    WHERE room_id = @room_id
    GROUP BY question_id
) a ON a.question_id = rq.question_id
WHERE rq.room_id = @room_id
ORDER BY rq.question_order ASC;";

            await using var cmd = new MySqlCommand(listSql, conn);
            cmd.Parameters.AddWithValue("@room_id", roomId);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var candidateQid = reader.GetInt32("question_id");
                var candidateTimeLimit = reader.GetInt32("time_limit_sec");
                var candidateStartedAt = reader.IsDBNull(reader.GetOrdinal("started_at"))
                    ? (DateTime?)null
                    : reader.GetDateTime("started_at");
                var answersCount = reader.GetInt32("answers_count");

                if (candidateStartedAt.HasValue)
                {
                    // דילוג על שאלה שפג לה הזמן או שכבר נענתה ע"י כולם
                    var expiresAt = candidateStartedAt.Value.AddSeconds(candidateTimeLimit);
                    if (DateTime.Now > expiresAt)
                        continue;
                    if (answersCount >= playersCount)
                        continue;

                    return (candidateQid, candidateTimeLimit, candidateStartedAt);
                }

                // שאלה חדשה: מתחילים לה timer בצד שרת ואז מחזירים אותה
                await reader.CloseAsync();
                await MarkQuestionStartedAsync(conn, roomId, candidateQid);
                return (candidateQid, candidateTimeLimit, DateTime.Now);
            }

            return null;
        }

        // סימון שאלה כהתחילה רק אם עדיין לא סומנה
        private static async Task MarkQuestionStartedAsync(MySqlConnection conn, int roomId, int questionId)
        {
            const string startSql = @"
UPDATE room_questions
SET started_at = NOW()
WHERE room_id = @room_id AND question_id = @qid AND started_at IS NULL;";
            await using var startCmd = new MySqlCommand(startSql, conn);
            startCmd.Parameters.AddWithValue("@room_id", roomId);
            startCmd.Parameters.AddWithValue("@qid", questionId);
            await startCmd.ExecuteNonQueryAsync();
        }

        // טעינת נתוני שאלה בודדת
        private static async Task<Question?> LoadQuestionAsync(MySqlConnection conn, int questionId, int timeLimitSec, DateTime? startedAt)
        {
            // שליפת נתוני שאלה בסיסיים מהמסד
            const string qSql = @"
SELECT question_id, question_text, question_type_id, difficulty, created_by
FROM questions
WHERE question_id = @qid
LIMIT 1;";

            await using var cmd = new MySqlCommand(qSql, conn);
            cmd.Parameters.AddWithValue("@qid", questionId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new Question
            {
                QuestionID = reader.GetInt32("question_id"),
                QuestionText = reader.GetString("question_text"),
                QuestionTypeID = reader.GetInt32("question_type_id"),
                Difficulty = reader.GetString("difficulty"),
                CreatedBy = reader.GetInt32("created_by"),
                TimeLimitSec = timeLimitSec,
                StartedAt = startedAt,
                Options = new List<QuestionOption>()
            };
        }

        // טעינת אופציות לשאלה
        private static async Task<List<QuestionOption>> LoadQuestionOptionsAsync(MySqlConnection conn, int questionId)
        {
            // שליפת אופציות תשובה לשאלה בסדר אקראי
            const string optSql = @"
SELECT option_id, question_id, option_text, is_correct
FROM question_options
WHERE question_id = @qid
ORDER BY RAND();";

            var options = new List<QuestionOption>();
            await using var cmd = new MySqlCommand(optSql, conn);
            cmd.Parameters.AddWithValue("@qid", questionId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                options.Add(new QuestionOption
                {
                    OptionID = reader.GetInt32("option_id"),
                    QuestionID = reader.GetInt32("question_id"),
                    OptionText = reader.GetString("option_text"),
                    IsCorrect = reader.GetBoolean("is_correct")
                });
            }

            return options;
        }

    }
}
