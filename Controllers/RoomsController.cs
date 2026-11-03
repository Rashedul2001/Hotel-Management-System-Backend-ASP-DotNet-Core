using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Data;
using Microsoft.AspNetCore.Authorization;
using Hotel_Management_System_Backend_dotNet.DTOs.Room;

namespace Hotel_Management_System_Backend_dotNet.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class RoomsController(ApplicationDbContext context) : ControllerBase
	{
		private readonly ApplicationDbContext _context = context;

		[HttpGet("amenities")]
		[AllowAnonymous]
		public async Task<ActionResult<List<AmenityDto>>> GetAmenities()
		{
			var amenities = await _context.Amenities
				.AsNoTracking()
				.Select(a => new AmenityDto
				{
					Id = a.Id,
					Name = a.Name
				})
				.ToListAsync();
			return Ok(amenities);
		}


		[HttpGet]
		[AllowAnonymous]
		public async Task<ActionResult<object>> GetRooms([FromQuery] RoomQueryParameters parameters)
		{
			var query = _context.Rooms
				.AsNoTracking()
				.Include(room => room.Images)
				.Include(room => room.Amenities)
				.Include(room => room.Bookings)
				.AsQueryable();

			if (parameters.CheckInDate.HasValue && parameters.CheckOutDate.HasValue)
			{

				query = query.Where(room => !room.Bookings.Any(booking =>
					(parameters.CheckInDate <= booking.CheckOutDate && parameters.CheckOutDate >= booking.CheckInDate)
					&& (booking.Status == BookingStatus.Confirmed ||
						(booking.Status == BookingStatus.PendingPayment && booking.PaymentExpiresAt > DateTime.UtcNow))));
			}

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				query = query.Where(room => room.Name.Contains(parameters.SearchTerm) || room.Description.Contains(parameters.SearchTerm));
			}

			if (parameters.MinPrice.HasValue)
			{
				query = query.Where(room => room.Price >= parameters.MinPrice.Value);
			}

			if (parameters.MaxPrice.HasValue)
			{
				query = query.Where(room => room.Price <= parameters.MaxPrice.Value);
			}

			if (parameters.MinCapacity.HasValue)
			{
				query = query.Where(room => room.Capacity >= parameters.MinCapacity.Value);
			}
			if (!string.IsNullOrWhiteSpace(parameters.Category) && parameters.Category != "All")
			{
				query = query.Where(room => room.Category == parameters.Category);
			}

			var amenityIds = parameters.AmenityIds ?? [];

			if (amenityIds.Count == 0 && Request.Query.TryGetValue("amenityIds", out var rawAmenityIds))
			{
				amenityIds = [.. rawAmenityIds
					.SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					.Where(v => int.TryParse(v, out _))
					.Select(int.Parse)
					.Distinct()];
			}

			if (amenityIds.Count > 0)
			{
				query = query.Where(room => room.Amenities.Any(a => amenityIds.Contains(a.Id)));
			}
			var sortBy = parameters.SortBy?.Trim().ToLower()?? "";
			query = sortBy switch
			{
				"price" => parameters.IsAscending ? query.OrderBy(room => room.Price) : query.OrderByDescending(room => room.Price),
				"capacity" => parameters.IsAscending ? query.OrderBy(room => room.Capacity) : query.OrderByDescending(room => room.Capacity),
				"rating" => parameters.IsAscending ? query.OrderBy(room => room.AverageRating) : query.OrderByDescending(room => room.AverageRating),
				"name" => parameters.IsAscending ? query.OrderBy(room => room.Name) : query.OrderByDescending(room => room.Name),
				_ => query
			};

			var totalRecords = await query.CountAsync();

			var rooms = await query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize)
				.Select(room => new RoomResponseDto
				{
					Id = room.Id,
					RoomNumber = room.RoomNumber,
					Name = room.Name,
					Category = room.Category,
					Floor = room.Floor,
					Status = room.Status,
					IsAvailable = room.IsAvailable,
					Price = room.Price,
					Capacity = room.Capacity,
					BedCount = room.BedCount,
					BedType = room.BedType,
					Size = room.Size,
					Description = room.Description,
					CreatedAt = room.CreatedAt,
					UpdatedAt = room.UpdatedAt,
					AverageRating = room.AverageRating,
					TotalReviews = room.TotalReviews,
					Images = room.Images
						.Select(image => new RoomImageDto
						{
							Id = image.Id,
							ImageUrl = image.ImageUrl
						})
						.ToList(),
					Amenities = room.Amenities
						.Select(amenity => new AmenityDto
						{
							Id = amenity.Id,
							Name = amenity.Name
						})
						.ToList()
				})
				.ToListAsync();

			return Ok(new { totalRooms = totalRecords, Rooms = rooms });
		}

	}
}
