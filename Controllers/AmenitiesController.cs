using Hotel_Management_System_Backend_dotNet.Data;
using Hotel_Management_System_Backend_dotNet.DTOs.Room;
using Hotel_Management_System_Backend_dotNet.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Management_System_Backend_dotNet.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AmenitiesController(ApplicationDbContext context) : ControllerBase
{
	private readonly ApplicationDbContext _context = context;

	[HttpGet]
	[AllowAnonymous]
	public async Task<ActionResult<List<AmenityDto>>> GetAmenities()
	{
		var amenities = await _context.Amenities
			.AsNoTracking()
			.OrderBy(a => a.Name)
			.Select(a => new AmenityDto
			{
				Id = a.Id,
				Name = a.Name
			})
			.ToListAsync();

		return Ok(amenities);
	}

	[HttpGet("{id:int}")]
	[AllowAnonymous]
	public async Task<ActionResult<AmenityDto>> GetAmenityById(int id)
	{
		var amenity = await _context.Amenities
			.AsNoTracking()
			.Where(a => a.Id == id)
			.Select(a => new AmenityDto
			{
				Id = a.Id,
				Name = a.Name
			})
			.FirstOrDefaultAsync();

		if (amenity is null)
		{
			return NotFound(new { message = "Amenity not found." });
		}

		return Ok(amenity);
	}

	[HttpPost]
	[Authorize(Policy = "AdminOnly")]
	public async Task<ActionResult<AmenityDto>> CreateAmenity([FromBody] CreateOrUpdateAmenityDto dto)
	{
		var normalizedName = dto.Name.Trim();

		if (await _context.Amenities.AnyAsync(a => a.Name.ToLower() == normalizedName.ToLower()))
		{
			return Conflict(new { message = "Amenity already exists." });
		}

		var amenity = new Amenity
		{
			Name = normalizedName
		};

		_context.Amenities.Add(amenity);
		await _context.SaveChangesAsync();

		return CreatedAtAction(nameof(GetAmenityById), new { id = amenity.Id }, new AmenityDto
		{
			Id = amenity.Id,
			Name = amenity.Name
		});
	}

	[HttpPut("{id:int}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<ActionResult<AmenityDto>> UpdateAmenity(int id, [FromBody] CreateOrUpdateAmenityDto dto)
	{
		var amenity = await _context.Amenities.FirstOrDefaultAsync(a => a.Id == id);
		if (amenity is null)
		{
			return NotFound(new { message = "Amenity not found." });
		}

		var normalizedName = dto.Name.Trim();

		if (await _context.Amenities.AnyAsync(a => a.Id != id && a.Name.ToLower() == normalizedName.ToLower()))
		{
			return Conflict(new { message = "Amenity already exists." });
		}

		amenity.Name = normalizedName;
		await _context.SaveChangesAsync();

		return Ok(new AmenityDto
		{
			Id = amenity.Id,
			Name = amenity.Name
		});
	}

	[HttpDelete("{id:int}")]
	[Authorize(Policy = "AdminOnly")]
	public async Task<IActionResult> DeleteAmenity(int id)
	{
		var amenity = await _context.Amenities
			.Include(a => a.Rooms)
			.FirstOrDefaultAsync(a => a.Id == id);

		if (amenity is null)
		{
			return NotFound(new { message = "Amenity not found." });
		}

		if (amenity.Rooms.Count > 0)
		{
			return Conflict(new { message = "Amenity cannot be deleted because it is assigned to rooms." });
		}

		_context.Amenities.Remove(amenity);
		await _context.SaveChangesAsync();

		return NoContent();
	}
}
