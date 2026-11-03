namespace Hotel_Management_System_Backend_dotNet.DTOs.Room
{

	public sealed class RoomResponseDto
	{
		public Guid Id { get; init; }
		public int RoomNumber { get; init; }
		public string Name { get; init; }
		public string Category { get; init; } 
		public int Floor { get; init; }
		public string Status { get; init; }
		public bool IsAvailable { get; init; }
		public bool IsFeatured { get; init; }
		public decimal Price { get; init; }
		public int Capacity { get; init; }
		public int BedCount { get; init; }
		public string BedType { get; init; }
		public int Size { get; init; }
		public string Description { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime UpdatedAt { get; init; }
		public double AverageRating { get; init; }
		public int TotalReviews { get; init; }

		public List<RoomImageDto> Images { get; init; } = [];
		public List<AmenityDto> Amenities { get; init; } = [];
	}

	public sealed class RoomImageDto
	{
		public int Id { get; init; }
		public string ImageUrl { get; init; }
	}

	public sealed class AmenityDto
	{
		public int Id { get; init; }
		public string Name { get; init; }
	}
}