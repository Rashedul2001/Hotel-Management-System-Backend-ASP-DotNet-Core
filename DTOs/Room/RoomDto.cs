namespace Hotel_Management_System_Backend_dotNet.DTOs.Room
{

	public sealed class RoomResponseDto
	{
		public Guid Id { get; set; }
		public int RoomNumber { get; set; }
		public string Name { get; set; }
		public string Category { get; set; } 
		public int Floor { get; set; }
		public string Status { get; set; }
		public bool IsAvailable { get; set; }
		public bool IsFeatured { get; set; }
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

		public List<RoomImageDto> Images { get; set; } = [];
		public List<AmenityDto> Amenities { get; set; } = [];
	}

	public sealed class RoomImageDto
	{
		public int Id { get; set; }
		public string ImageUrl { get; set; }
		public string PublicId { get; set; }
	}

	public sealed class AmenityDto
	{
		public int Id { get; init; }
		public string Name { get; init; }
	}
}