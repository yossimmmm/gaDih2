namespace Models
{
    // רשומת הצטרפות של משתמש בתוך חדר.
    // זה האובייקט שמקשר בין שחקן לחדר מסוים.
    public class RoomPlayer
    {
        // המפתח הראשי של שורת ההצטרפות.
        public int RoomPlayerID { get; set; }

        // המפתח הזר לחדר.
        public int RoomID { get; set; }

        // המפתח הזר למשתמש.
        public int UserID { get; set; }

        // הכינוי של השחקן באותו חדר.
        public string Nickname { get; set; } = "";

        // זמן ההצטרפות לחדר.
        public System.DateTime JoinedAt { get; set; }
    }
}
