namespace Hotel_Management_System_Backend_dotNet.Entities
{
    public class Amenity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "WiFi", "Pool"
        public ICollection<Room> Rooms { get; set; } = new List<Room>(); // EF Core implicit many-to-many
    }
}
