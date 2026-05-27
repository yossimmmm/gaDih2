namespace Models
{
    public class RoomPlayer
    {
        public int RoomPlayerID { get; set; }
        public int RoomID { get; set; }
        public int UserID { get; set; }
        public string Nickname { get; set; } = "";
        public System.DateTime JoinedAt { get; set; }
    }
}
