using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Hotel_Management_System_Backend_dotNet.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public  string FullName { get; set; } = string.Empty; 
        public string? ProfilePictureUrl { get; set; }
        [Required]
        public  DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
