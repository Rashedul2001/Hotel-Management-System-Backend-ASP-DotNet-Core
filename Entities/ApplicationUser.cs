using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management_System_Backend_dotNet.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName{ get; set; } = string.Empty;

        [MaxLength(2048)]
        public string? ProfilePictureUrl { get; set; }

        [MaxLength(255)]
        public string? ProfilePicturePublicId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}