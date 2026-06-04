namespace Models
{
    // חדר טריוויה אחד - הלובי שבו שחקנים מצטרפים ומשחקים יחד.
    public class Room
    {
        // המפתח הראשי של החדר בטבלת rooms.
        public int RoomID { get; set; }

        // קוד קצר להצטרפות לחדר.
        public string RoomCode { get; set; } = "";

        // שם קריא שמופיע למשתמשים.
        public string RoomName { get; set; } = "";

        // מזהה המשתמש שיצר את החדר.
        public int HostID { get; set; }

        // האם החדר עדיין פעיל.
        public bool IsActive { get; set; } = true;

        // האם החדר גלוי ברשימת החדרים הציבוריים.
        public bool IsPublic { get; set; } = false;

        // סינון אופציונלי לסוג שאלות מסוים.
        public int? QuestionTypeID { get; set; }

        // זמן היצירה של החדר.
        public DateTime CreatedAt { get; set; }
    }
}
