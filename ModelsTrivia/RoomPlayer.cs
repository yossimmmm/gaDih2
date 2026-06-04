namespace Models
{
    // ייצוג של שחקן בתוך חדר.
    // זה לא המשתמש עצמו, אלא ההשתתפות שלו בתוך חדר מסוים.
    public class RoomPlayer
    {
        // מזהה רשומת ההשתתפות בחדר.
        public int RoomPlayerID { get; set; }

        // לאיזה חדר השחקן שייך.
        public int RoomID { get; set; }

        // איזה משתמש זה.
        public int UserID { get; set; }

        // הכינוי שבחר השחקן בחדר.
        public string Nickname { get; set; } = "";

        // מתי השחקן הצטרף.
        public System.DateTime JoinedAt { get; set; }
    }
}
