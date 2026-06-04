namespace Models
{
    // מחזיק גלובלי קליל של מזהה המשתמש הנוכחי.
    // זה עזר נוח בלבד, לא מערכת אימות מלאה.
    public static class UserSession
    {
        // מזהה המשתמש המחובר כרגע, אם הוגדר כזה.
        public static int? CurrentUserID { get; set; }
    }
}
