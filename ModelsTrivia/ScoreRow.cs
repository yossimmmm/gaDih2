namespace Models
{
    // שורת ניקוד עבור חדר מסוים.
    // זו תוצאה שמוצגת ב-scoreboard.
    public class ScoreRow : PlayerStatsBase
    {
        // מזהה השחקן בתוך החדר.
        public int RoomPlayerID { get; set; }

        // הכינוי של השחקן.
        public string Nickname { get; set; } = "";
    }
}
