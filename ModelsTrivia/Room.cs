namespace Models
{
    // מודל של חדר משחק.
    // השרת מחזיר אותו כשמייצרים חדר, מצטרפים לחדר, או טוענים חדרים ציבוריים.
    public class Room
    {
        // מזהה פנימי של החדר.
        public int RoomID { get; set; }

        // קוד קצר לשיתוף עם שחקנים אחרים.
        public string RoomCode { get; set; } = "";

        // שם ידידותי של החדר.
        public string RoomName { get; set; } = "";

        // מזהה המשתמש שיצר את החדר.
        public int HostID { get; set; }

        // האם החדר עדיין פעיל.
        public bool IsActive { get; set; } = true;

        // האם החדר מוצג ברשימת החדרים הציבוריים.
        public bool IsPublic { get; set; } = false;

        // סוג השאלות שנבחר לחדר.
        public int? QuestionTypeID { get; set; }

        // מתי החדר נוצר.
        public DateTime CreatedAt { get; set; }
    }
}
