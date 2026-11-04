namespace Hotel_Management_System_Backend_dotNet.DTOs.Room
{
	public class RoomQueryParameters
	{
		// Pagination
		private const int DefaultPageSize = 10;
		private const int MaxPageSize = 100;
		private int _pageSize = DefaultPageSize;
		public int PageNumber { get; set; } = 1;

		public int PageSize
		{
			get => _pageSize;
			set => _pageSize = Math.Clamp(value, 10, MaxPageSize);
		}

		public DateOnly? CheckInDate { get; set; }
		public DateOnly? CheckOutDate { get; set; }
		public string? SearchTerm { get; set; }

		// Filtering
		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }
		public int? MinCapacity { get; set; }
		public string Category { get; set; } = "All";
		public List<int>? AmenityIds { get; set; }

		// Sorting
		public string? SortBy { get; set; } 
		public bool IsAscending { get; set; } = true;
		public string? Status{ get; set; }
	}
}