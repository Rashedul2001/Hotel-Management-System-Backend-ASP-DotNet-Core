namespace Hotel_Management_System_Backend_dotNet.Entities
{
    public class Room
    {
        public Guid Id { get; set; }
        public int RoomNumber { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Floor { get; set; }
        public string Status { get; set; }
        public bool IsAvailable { get; set; }
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public int BedCount { get; set; }
        public string BedType { get; set; }
        public int Size { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        public ICollection<RoomImage> Images { get; set; } = [];
        public ICollection<Amenity> Amenities { get; set; } = [];
        public ICollection<Review> Reviews { get; set; } = [];
        public ICollection<Booking> Bookings { get; set; } = [];




    }
}
