namespace Models
{
    // בסיס משותף לשורות סטטיסטיקה.
    // גם scoreboard וגם top players משתמשים בשדות האלה.
    public abstract class PlayerStatsBase
    {
        // המשתמש שאליו שורת הסטטיסטיקה שייכת.
        public int UserID { get; set; }

        // מספר התשובות הנכונות.
        public int CorrectCount { get; set; }

        // מספר התשובות הכולל שנשלחו.
        public int AnsweredCount { get; set; }
    }
}
