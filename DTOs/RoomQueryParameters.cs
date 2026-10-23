namespace Hotel_Management_System_Backend_dotNet.DTOs
{
    public class RoomQueryParameters
    {
        // Pagination
        public int PageNumber { get; set; } = 1;
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 50;
        private int _pageSize = DefaultPageSize;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Clamp(value, 1, MaxPageSize); // 1..50
        }

        public DateOnly? CheckInDate { get; set; }
        public DateOnly? CheckOutDate { get; set; }
        public string SearchTerm { get; set; } = string.Empty;

		// Filtering
		public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinCapacity { get; set; }
        public List<int>? AmenityIds { get; set; }

        // Sorting
        public string SortBy { get; set; } = "price"; // "price", "rating"
        public bool IsAscending { get; set; } = true;
    }
}