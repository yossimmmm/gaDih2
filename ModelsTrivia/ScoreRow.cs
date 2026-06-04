namespace Models
{
    // שורת ניקוד של שחקן אחד בתוך חדר אחד.
    public class ScoreRow : PlayerStatsBase
    {
        // מזהה שורת השחקן בחדר, משמש בחיבורים בין טבלאות.
        public int RoomPlayerID { get; set; }

        // הכינוי שמוצג ב-scoreboard.
        public string Nickname { get; set; } = "";
    }
}
