using Hotel_Management_System_Backend_dotNet.Common.Constants;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management_System_Backend_dotNet.DTOs.Room
{
    public sealed class CreateOrupdateRoomDto
    {
        [Range(1, int.MaxValue)]
        public int RoomNumber { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Range(1, 200)]
        public int Floor { get; set; }

        [Required]
        [StringLength(50)]
        public  string Status { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        [Range(typeof(decimal), "0.01", "1000000")]
        public decimal Price { get; set; }

        [Range(1, 20)]
        public int Capacity { get; set; }

        [Range(1, 10)]
        public int BedCount { get; set; }

        [Required]
        [StringLength(50)]
        public string BedType { get; set; } = string.Empty;

        [Range(1, 2000)]
        public int Size { get; set; }

        [Required]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        public List<int> AmenityIds { get; set; } = [];
        public List<int> ExistingImageIds { get; set; } = [];
        public List<IFormFile> Images { get; set; } = [];
    }
}
