namespace Models
{
    // רמות הרשאה במערכת
    public enum UserRole
    {
        // משתמש רגיל
        User = 0,
        // מנהל ביניים (למשל יצירת חדרים ועוד)
        Manager = 1,
        // מנהל מערכת מלא
        Admin = 2
    }

    public class User
    {
        // מזהה ייחודי של משתמש במסד
        public int UserID { get; set; }
        // שם משתמש לתצוגה ולהתחברות לוגית
        public string Username { get; set; } = "";
        // שם מלא של המשתמש
        public string FullName { get; set; } = "";
        // כתובת אימייל ייחודית
        public string Email { get; set; } = "";
        // ערך hash של הסיסמה (לא סיסמה גולמית)
        public string PasswordHash { get; set; } = "";
        // רמת ההרשאה של המשתמש
        public UserRole Role { get; set; } = UserRole.User;
    }
}
