namespace Models
{
    // שורת דירוג לשחקנים המובילים במערכת.
    // ה-API משתמש בזה כשמציגים top players.
    public class TopPlayerRow : PlayerStatsBase
    {
        // שם המשתמש.
        public string Username { get; set; } = "";

        // כמה משחקים המשתמש שיחק.
        public int GamesPlayed { get; set; }

        // כמה משחקים המשתמש ניצח.
        public int Wins { get; set; }
    }
}
