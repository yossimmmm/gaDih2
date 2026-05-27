namespace Models
{
    public class TopPlayerRow
    {
        public int UserID { get; set; }
        public string Username { get; set; } = "";
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int CorrectCount { get; set; }
        public int AnsweredCount { get; set; }
    }
}
