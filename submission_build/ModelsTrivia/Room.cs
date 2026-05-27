namespace Models
{
    public class Room
    {
        public int RoomID { get; set; }
        public string RoomCode { get; set; } = "";
        public string RoomName { get; set; } = "";
        public int HostID { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = false;
        public int? QuestionTypeID { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
