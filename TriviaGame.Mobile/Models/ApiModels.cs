namespace TriviaGame.Mobile.Models;

// תוצאת קריאה כללית ל־API.
// כל endpoint מחזיר את המעטפת הזאת או משהו דומה, כדי שה־UI יטפל בהצלחה או בכישלון באותו אופן.
public sealed class ApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = "";
    public T? Data { get; init; }

    // בונה תשובה מוצלחת עם הנתונים שהגיעו מהשרת.
    public static ApiResult<T> Ok(T? data, int statusCode = 200, string message = "") =>
        new() { Success = true, StatusCode = statusCode, Message = message, Data = data };

    // בונה תשובה שנכשלה עם הודעת שגיאה.
    public static ApiResult<T> Fail(string message, int statusCode = 0) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Data = default };
}

// תשובת login.
// ה־API מחזיר את שדות הזהות הבסיסיים, וה־MAUI שומר אותם בזיכרון המקומי.
public sealed class AuthResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string Role { get; set; } = "User";
}

// פרטי המשתמש המחובר.
// זה מה שממלא את אזור Auth + Profile במסך הראשי.
public sealed class CurrentUserResponse
{
    public bool Authenticated { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "User";
}

// שורת סוג שאלה לרשימת Picker.
public sealed class QuestionTypeRow
{
    public int QuestionTypeID { get; set; }
    public string TypeName { get; set; } = "";
    public override string ToString() => $"{TypeName} ({QuestionTypeID})";
}

// שורת חדר ברשימת חדרים ציבוריים.
public sealed class RoomRow
{
    public int RoomID { get; set; }
    public string RoomCode { get; set; } = "";
    public string RoomName { get; set; } = "";
    public int HostID { get; set; }
    public bool IsActive { get; set; }
    public bool IsPublic { get; set; }
    public int? QuestionTypeID { get; set; }
}

// שורת שחקן בחדר.
public sealed class RoomPlayerRow
{
    public int RoomPlayerID { get; set; }
    public int RoomID { get; set; }
    public int UserID { get; set; }
    public string Nickname { get; set; } = "";
}

// שורת אפשרות תשובה להצגה ב־CollectionView.
public sealed class QuestionOptionRow
{
    public int OptionID { get; set; }
    public int QuestionID { get; set; }
    public string OptionText { get; set; } = "";
}

// שורת שאלה מלאה עם אפשרויות.
public sealed class QuestionRow
{
    public int QuestionID { get; set; }
    public string QuestionText { get; set; } = "";
    public int TimeLimitSec { get; set; }
    public DateTime? StartedAt { get; set; }
    public List<QuestionOptionRow> Options { get; set; } = new();
}

// תשובת השרת כאשר שואלים על השאלה הפעילה.
public sealed class CurrentQuestionResponse
{
    public bool Ok { get; set; }
    public bool Finished { get; set; }
    public QuestionRow? Question { get; set; }
}

// שורת ניקוד ב־scoreboard.
public sealed class ScoreRow
{
    public int UserID { get; set; }
    public int CorrectCount { get; set; }
    public int AnsweredCount { get; set; }
    public string Nickname { get; set; } = "";
}

// תשובת scoreboard מלאה.
public sealed class ScoreboardResponse
{
    public int RoomId { get; set; }
    public string RoomCode { get; set; } = "";
    public int TotalQuestions { get; set; }
    public List<ScoreRow> Rows { get; set; } = new();
}

// שורת שחקן מוביל.
public sealed class TopPlayerRow
{
    public int UserID { get; set; }
    public string Username { get; set; } = "";
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int CorrectCount { get; set; }
    public int AnsweredCount { get; set; }
}

// סטטיסטיקות אישיות של משתמש.
public sealed class UserStatsResponse
{
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Correct { get; set; }
    public int Answered { get; set; }
}

// תשובה פשוטה לפעולות create/update/delete.
public sealed class ApiSimpleResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string Text { get; set; } = "";
}

// תשובת join room, כולל החדר והשחקן שנוצרו/נמצאו.
public sealed class JoinRoomResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public RoomRow? Room { get; set; }
    public RoomPlayerRow? Player { get; set; }
}
