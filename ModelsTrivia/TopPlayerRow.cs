namespace Models
{
    // Inherits common player stats from PlayerStatsBase for leaderboard output.
    public class TopPlayerRow : PlayerStatsBase
    {
        // Public username displayed in top players table.
        public string Username { get; set; } = "";
        // Number of games this user participated in.
        public int GamesPlayed { get; set; }
        // Number of games this user won.
        public int Wins { get; set; }
    }
}
