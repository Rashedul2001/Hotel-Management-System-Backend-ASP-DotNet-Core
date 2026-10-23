using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Data;
using Microsoft.AspNetCore.Authorization;
using Hotel_Management_System_Backend_dotNet.DTOs;

namespace Hotel_Management_System_Backend_dotNet.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class RoomsController(ApplicationDbContext context) : ControllerBase
	{
		private readonly ApplicationDbContext _context = context;
		[HttpGet]
		[AllowAnonymous]
		public async Task<ActionResult<IEnumerable<Room>>> GetRooms([FromQuery] RoomQueryParameters parameters)
		{

			var query = _context.Rooms
				.Include(room => room.Images)
				.Include(room => room.Amenities)
				.Include(room => room.Bookings)
				.AsQueryable();

			//Filtering
			if (parameters.CheckInDate.HasValue && parameters.CheckOutDate.HasValue)
			{
				query = query.Where(room => !room.Bookings.Any(booking =>
					(parameters.CheckInDate <= booking.CheckOutDate && parameters.CheckOutDate >= booking.CheckInDate)
					&& (booking.Status == BookingStatus.Confirmed ||
						(booking.Status == BookingStatus.PendingPayment && booking.PaymentExpiresAt > DateTime.UtcNow)))
				);
			}
			if (parameters.SearchTerm != null && !string.IsNullOrEmpty(parameters.SearchTerm))
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
			if (parameters.AmenityIds != null && parameters.AmenityIds.Count != 0)
			{
				query = query.Where(room => room.Amenities.Any(amenity => parameters.AmenityIds.Contains(amenity.Id)));
			}
			//Sorting
			query = parameters.SortBy switch
			{
				"Price" => parameters.IsAscending ? query.OrderBy(room => room.Price) : query.OrderByDescending(room => room.Price),
				"Capacity" => parameters.IsAscending ? query.OrderBy(room => room.Capacity) : query.OrderByDescending(room => room.Capacity),
				"Rating" => parameters.IsAscending ? query.OrderBy(room => room.AverageRating) : query.OrderByDescending(room => room.AverageRating),
				"Name" => parameters.IsAscending ? query.OrderBy(room => room.Name) : query.OrderByDescending(room => room.Name),
				_ => query
			};

			//TODO: later include bookings ratings and exclude room properties that are not needed in the response depending on user authority 
			// and images amenities if not included by default 
			query = query
				.Skip((parameters.PageNumber - 1) * parameters.PageSize)
				.Take(parameters.PageSize);

			var totalRecords = await query.CountAsync();
			var rooms = await query.ToListAsync();
			return Ok(new { TotalRecords = totalRecords, Data = rooms });
		}

	

	
	}
}