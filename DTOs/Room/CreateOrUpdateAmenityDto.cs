using System.ComponentModel.DataAnnotations;

namespace Hotel_Management_System_Backend_dotNet.DTOs.Room;

public sealed class CreateOrUpdateAmenityDto
{
	[Required]
	[StringLength(100)]
	public string Name { get; init; } = string.Empty;
}
