namespace Hotel_Management_System_Backend_dotNet.Entities
{
    public class RoomImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty; // Cloudinary identifier for deletions
        public Guid RoomId { get; set; }
        public Room? Room { get; set; }
    }
}
