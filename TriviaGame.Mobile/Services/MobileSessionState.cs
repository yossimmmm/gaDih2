using TriviaGame.Mobile.Models;

namespace TriviaGame.Mobile.Services;

// שומר מצב מקומי של אפליקציית MAUI.
// ב-MAUI אין Cookie של דפדפן כמו באתר, לכן צריך אובייקט אחד בזיכרון
// שמחזיק מי מחובר, באיזה חדר הוא נמצא, ואיזו שאלה פתוחה כרגע.
public sealed class MobileSessionState
{
    // המשתמש שהתחבר דרך Login.
    // אם הערך null או Authenticated=false, הדפים יודעים שאין משתמש מחובר.
    public CurrentUserResponse? CurrentUser { get; set; }

    // החדר הפעיל שהמשתמש יצר או הצטרף אליו.
    // PlayPage משתמש בזה כדי לדעת לאיזה roomCode לשלוח start/question/answer.
    public RoomRow? CurrentRoom { get; set; }

    // הרשומה שמייצגת את המשתמש כשחקן בתוך החדר.
    // זה שונה מ-UserId: UserId מזהה חשבון, RoomPlayerID מזהה שחקן במשחק מסוים.
    public RoomPlayerRow? CurrentPlayer { get; set; }

    // השאלה הנוכחית שהשרת החזיר.
    // Submit Answer צריך את QuestionID מתוך האובייקט הזה.
    public QuestionRow? CurrentQuestion { get; set; }

    // אפשרות התשובה שנבחרה במסך.
    // היא נשמרת כאן רק אחרי בחירה ב-UI, ונשלחת לשרת רק בלחיצה על Submit.
    public QuestionOptionRow? SelectedOption { get; set; }

    // בדיקה קצרה שכל הדפים יכולים להשתמש בה לפני פעולות שדורשות התחברות.
    public bool IsLoggedIn => CurrentUser?.Authenticated == true && CurrentUser.UserId > 0;

    // מנקה רק את מצב המשחק, אבל לא מנתק את המשתמש.
    // משתמשים בזה כשעוזבים חדר או כשעוברים לחדר אחר.
    public void ClearGame()
    {
        CurrentRoom = null;
        CurrentPlayer = null;
        CurrentQuestion = null;
        SelectedOption = null;
    }

    // מנקה הכל בזמן Logout.
    // אחרי זה האפליקציה חוזרת למצב כאילו נפתחה מחדש.
    public void Logout()
    {
        CurrentUser = null;
        ClearGame();
    }
}
