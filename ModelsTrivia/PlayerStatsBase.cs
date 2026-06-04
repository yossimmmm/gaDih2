namespace Models
{
    // בסיס משותף לשורות סטטיסטיקה.
    // גם scoreboard של חדר וגם top players משתמשים באותם שדות בסיסיים.
    public abstract class PlayerStatsBase
    {
        // לאיזה משתמש הסטטיסטיקה שייכת.
        public int UserID { get; set; }

        // כמה תשובות נכונות היו לו.
        public int CorrectCount { get; set; }

        // כמה תשובות הוא נתן בסך הכול.
        public int AnsweredCount { get; set; }
    }
}
