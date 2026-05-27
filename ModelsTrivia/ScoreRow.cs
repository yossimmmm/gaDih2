namespace Models
{
    // Inherits common player stats from PlayerStatsBase for room scoreboard output.
    public class ScoreRow : PlayerStatsBase
    {
        // Unique id of the room participant row.
        public int RoomPlayerID { get; set; }
        // Display nickname in the room.
        public string Nickname { get; set; } = "";
    }
}
