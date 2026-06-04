namespace Models
{
    // מצב משתמש גלובלי ישן מהגרסה הוותיקה של הפרויקט.
    // היום ה-API וה-MAUI עובדים בלי session אמיתי, אבל המחלקה עדיין קיימת לצורך תאימות.
    public static class UserSession
    {
        // מזהה המשתמש הנוכחי, אם נשמר.
        public static int? CurrentUserID { get; set; }
    }
}
