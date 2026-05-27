namespace Models
{
    public class ScoreRow
    {
        public int RoomPlayerID { get; set; }
        public int UserID { get; set; }
        public string Nickname { get; set; } = "";
        public int CorrectCount { get; set; }
        public int AnsweredCount { get; set; }
    }
}
