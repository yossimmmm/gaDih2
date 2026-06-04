namespace Models
{
    // ערכי התפקידים שמשמשים להרשאות בתוך האפליקציה.
    public enum UserRole
    {
        // משתמש רגיל שמשחק ומנהל את הפרופיל שלו.
        User = 0,
        // תפקיד ביניים שמאפשר פעולות ניהול מוגבלות.
        Manager = 1,
        // מנהל מלא עם גישה לניהול משתמשים ונתונים.
        Admin = 2
    }

    // מודל המשתמש המשותף בין שכבת הנתונים, ה־API וה־UI.
    public class User
    {
        // המפתח הראשי של המשתמש בטבלת users.
        public int UserID { get; set; }

        // שם המשתמש שמוצג במסכים ובטפסים.
        public string Username { get; set; } = "";

        // שם מלא אופציונלי להצגה בפרופיל.
        public string FullName { get; set; } = "";

        // כתובת האימייל שמשמשת להתחברות ולאיפוס סיסמה.
        public string Email { get; set; } = "";

        // hash של הסיסמה, לא הסיסמה המקורית.
        public string PasswordHash { get; set; } = "";

        // רמת ההרשאה הנוכחית של המשתמש.
        public UserRole Role { get; set; } = UserRole.User;
    }
}
