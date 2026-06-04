namespace Models
{
    // ערכי ההרשאה שמשתמשים בהם במערכת.
    public enum UserRole
    {
        // משתמש רגיל.
        User = 0,
        // אדמין עם הרשאות ניהול.
        Admin = 1
    }

    // מודל המשתמש המשותף בין שכבת הנתונים, ה-API והלקוח.
    public class User
    {
        // המפתח הראשי של המשתמש בטבלת users.
        public int UserID { get; set; }

        // שם המשתמש שמוצג במסכים ובטפסים.
        public string Username { get; set; } = "";

        // שם מלא אופציונלי להצגה בפרופיל.
        public string FullName { get; set; } = "";

        // כתובת המייל של המשתמש לצורכי התחברות ואיפוס סיסמה.
        public string Email { get; set; } = "";

        // hash של הסיסמה, לא הסיסמה עצמה.
        public string PasswordHash { get; set; } = "";

        // רמת ההרשאה של המשתמש.
        public UserRole Role { get; set; } = UserRole.User;
    }
}
