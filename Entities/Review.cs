namespace Hotel_Management_System_Backend_dotNet.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; } // 1 to 5
        public string Comment { get; set; } = string.Empty;
        public Guid RoomId { get; set; }
        public Room? Room { get; set; }
    }
}
