namespace Models
{
    // רמת הרשאה של משתמש במערכת.
    // היא משמשת כדי להחליט איזה מסכים ופעולות מותרות לכל משתמש.
    public enum UserRole
    {
        // משתמש רגיל.
        User = 0,
        // משתמש עם הרשאות ניהול חלקיות.
        Manager = 1,
        // משתמש עם הרשאות מלאות.
        Admin = 2
    }

    // מודל משתמש.
    // זהו הייצוג הבסיסי שעובר בין ה-DB, ה-API, והלקוח.
    public class User
    {
        // מזהה פנימי של המשתמש.
        public int UserID { get; set; }

        // שם המשתמש שמוצג ב-login ובמסכים.
        public string Username { get; set; } = "";

        // שם מלא לקריאה אנושית.
        public string FullName { get; set; } = "";

        // כתובת המייל של המשתמש.
        public string Email { get; set; } = "";

        // hash של הסיסמה, לא הסיסמה עצמה.
        public string PasswordHash { get; set; } = "";

        // התפקיד של המשתמש במערכת.
        public UserRole Role { get; set; } = UserRole.User;
    }
}
