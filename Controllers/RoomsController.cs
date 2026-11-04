using Hotel_Management_System_Backend_dotNet.Data;
using Hotel_Management_System_Backend_dotNet.DTOs.Room;
using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Entities.Authorization;
using Hotel_Management_System_Backend_dotNet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Management_System_Backend_dotNet.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class RoomsController(
		ApplicationDbContext context,
		ICloudinaryService cloudinaryService) : ControllerBase
	{
		private readonly ApplicationDbContext _context = context;
		private readonly ICloudinaryService _cloudinaryService = cloudinaryService;

		private const long MaxRoomImageSize = 10 * 1024 * 1024;

		private static readonly HashSet<string> AllowedImageTypes =
		[
			"image/jpeg",
			"image/png",
			"image/webp"
		];

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

		[HttpPost("amenities")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<ActionResult<AmenityDto>> CreateAmenity(
			[FromBody] CreateOrUpdateAmenityDto dto,
			CancellationToken cancellationToken)
		{
			var amenityName = dto.Name.Trim();

			var alreadyExists = await _context.Amenities
				.AnyAsync(a => a.Name.ToLower() == amenityName.ToLower(), cancellationToken);

			if (alreadyExists)
			{
				return Conflict(new { message = "Amenity already exists." });
			}

			var amenity = new Amenity
			{
				Name = amenityName
			};

			_context.Amenities.Add(amenity);
			await _context.SaveChangesAsync(cancellationToken);

			return StatusCode(StatusCodes.Status201Created, new AmenityDto
			{
				Id = amenity.Id,
				Name = amenity.Name
			});
		}


		[HttpGet]
		[AllowAnonymous]
		public async Task<ActionResult<object>> GetRooms(
	[FromQuery] RoomQueryParameters parameters)
		{
			var isAdmin = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);

			var query = _context.Rooms
				.AsNoTracking()
				.Include(room => room.Images)
				.Include(room => room.Amenities)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(room =>
					room.Name.ToLower().Contains(searchTerm) ||
					room.Description.ToLower().Contains(searchTerm));
				if (isAdmin)
				{
					query = query.Where(room =>
						room.RoomNumber.ToString().Contains(searchTerm));
				}
			}


			if (!string.IsNullOrWhiteSpace(parameters.Category) &&
				parameters.Category != "All")
			{
				query = query.Where(room =>
					room.Category == parameters.Category);
			}

			if (!string.IsNullOrWhiteSpace(parameters.Status))
			{
				query = query.Where(room =>
					room.Status == parameters.Status);
			}
			// Only show rooms available for the requested dates.
			if (parameters.CheckInDate.HasValue &&
				parameters.CheckOutDate.HasValue)
			{
				var checkIn = parameters.CheckInDate.Value;
				var checkOut = parameters.CheckOutDate.Value;
				var now = DateTime.UtcNow;

				query = query.Where(room =>
					!room.Bookings.Any(booking =>
						checkIn < booking.CheckOutDate &&
						checkOut > booking.CheckInDate &&
						(
							booking.Status == BookingStatus.Confirmed ||
							(
								booking.Status == BookingStatus.PendingPayment &&
								booking.PaymentExpiresAt > now
							)
						)
					));
			}

			if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
			{
				var searchTerm = parameters.SearchTerm.Trim().ToLower();
				query = query.Where(room =>
					room.Name.ToLower().Contains(searchTerm) ||
					room.Description.ToLower().Contains(searchTerm));
			}

			if (parameters.MinPrice.HasValue)
			{
				query = query.Where(room =>
					room.Price >= parameters.MinPrice.Value);
			}

			if (parameters.MaxPrice.HasValue)
			{
				query = query.Where(room =>
					room.Price <= parameters.MaxPrice.Value);
			}

			if (parameters.MinCapacity.HasValue)
			{
				query = query.Where(room =>
					room.Capacity >= parameters.MinCapacity.Value);
			}

			if (!string.IsNullOrWhiteSpace(parameters.Category) &&
				parameters.Category != "All")
			{
				query = query.Where(room =>
					room.Category == parameters.Category);
			}

			var amenityIds = ParseAmenityIds(parameters.AmenityIds);

			if (amenityIds.Count > 0)
			{
				query = query.Where(room =>
					room.Amenities.Any(amenity =>
						amenityIds.Contains(amenity.Id)));
			}

			// ==========================================
			// SORTING
			// ==========================================

			var sortBy = parameters.SortBy?.Trim().ToLowerInvariant()
				?? string.Empty;

			query = sortBy switch
			{
				"price" => parameters.IsAscending
					? query.OrderBy(room => room.Price).ThenBy(room => room.Id)
					: query.OrderByDescending(room => room.Price).ThenBy(room => room.Id),

				"capacity" => parameters.IsAscending
					? query.OrderBy(room => room.Capacity).ThenBy(room => room.Id)
					: query.OrderByDescending(room => room.Capacity).ThenBy(room => room.Id),

				"rating" => parameters.IsAscending
					? query.OrderBy(room => room.AverageRating).ThenBy(room => room.Id)
					: query.OrderByDescending(room => room.AverageRating).ThenBy(room => room.Id),

				"name" => parameters.IsAscending
					? query.OrderBy(room => room.Name).ThenBy(room => room.Id)
					: query.OrderByDescending(room => room.Name).ThenBy(room => room.Id),

				_ => query.OrderBy(room => room.Id)
			};

			// ==========================================
			// PAGINATION
			// ==========================================

			var totalRecords = await query.CountAsync();

			var pageNumber = Math.Max(1, parameters.PageNumber);
			var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

			// ==========================================
			// RESPONSE PROJECTION
			// ==========================================

			var rooms = isAdmin
				? await query
					.Skip((pageNumber - 1) * pageSize)
					.Take(pageSize)
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
								ImageUrl = image.ImageUrl,
								PublicId = image.PublicId
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
					.ToListAsync()
				: await query
					.Skip((pageNumber - 1) * pageSize)
					.Take(pageSize)
					.Select(room => new RoomResponseDto
					{
						Id = room.Id,
						Name = room.Name,
						Category = room.Category,
						Price = room.Price,
						Capacity = room.Capacity,
						BedCount = room.BedCount,
						BedType = room.BedType,
						Size = room.Size,
						Description = room.Description,
						AverageRating = room.AverageRating,
						TotalReviews = room.TotalReviews,
						Images = room.Images
							.Select(image => new RoomImageDto
							{
								Id = image.Id,
								ImageUrl = image.ImageUrl,
								PublicId = image.PublicId
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

			return Ok(new
			{
				totalRooms = totalRecords,
				pageNumber,
				pageSize,
				totalPages = (int)Math.Ceiling(
					totalRecords / (double)pageSize),
				rooms
			});
		}

		[HttpGet("{id:guid}")]
		[AllowAnonymous]
		public async Task<ActionResult<RoomResponseDto>> GetRoomById(Guid id)
		{
			var room = await _context.Rooms
				.AsNoTracking()
				.Include(r => r.Images)
				.Include(r => r.Amenities)
				.FirstOrDefaultAsync(r => r.Id == id);

			if (room is null)
			{
				return NotFound(new { message = "Room not found." });
			}
			var roomResponse = new RoomResponseDto
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
						ImageUrl = image.ImageUrl,
						PublicId = image.PublicId
					})
					.ToList(),
				Amenities = room.Amenities
					.Select(amenity => new AmenityDto
					{
						Id = amenity.Id,
						Name = amenity.Name
					})
					.ToList()
			};

			return Ok(roomResponse);
		}

		[HttpPost]
		[Authorize(Policy = "AdminOnly")]
		public async Task<ActionResult<RoomResponseDto>> CreateRoom(
			[FromForm] CreateOrupdateRoomDto dto,
			CancellationToken cancellationToken)
		{
			if (await _context.Rooms.AnyAsync(r => r.RoomNumber == dto.RoomNumber, cancellationToken))
			{
				return Conflict(new { message = "Room number already exists." });
			}

			var amenityIds = dto.AmenityIds.Distinct().ToList();
			var amenities = await _context.Amenities
				.Where(a => amenityIds.Contains(a.Id))
				.ToListAsync(cancellationToken);

			if (amenities.Count != amenityIds.Count)
			{
				var foundIds = amenities.Select(a => a.Id).ToHashSet();
				var missingIds = amenityIds.Where(id => !foundIds.Contains(id)).ToList();
				return BadRequest(new { message = "Some amenities were not found.", missingAmenityIds = missingIds });
			}

			var uploadedImages = new List<CloudinaryUploadResult>();

			try
			{
				foreach (var file in dto.Images)
				{
					if (file is null || file.Length == 0)
					{
						return BadRequest(new { message = "Please select valid image files." });
					}

					if (file.Length > MaxRoomImageSize)
					{
						return BadRequest(new { message = "Each room image must be 10 MB or smaller." });
					}

					if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
					{
						return BadRequest(new { message = "Only JPEG, PNG, and WebP images are allowed." });
					}

					await using var stream = file.OpenReadStream();
					var uploadResult = await _cloudinaryService.UploadRoomImageAsync(
						stream,
						file.FileName,
						cancellationToken);

					uploadedImages.Add(uploadResult);
				}

				var now = DateTime.UtcNow;
				var room = new Room
				{
					Id = Guid.NewGuid(),
					RoomNumber = dto.RoomNumber,
					Name = dto.Name.Trim(),
					Category = dto.Category.Trim(),
					Floor = dto.Floor,
					Status = dto.Status.Trim(),
					IsAvailable = dto.IsAvailable,
					Price = dto.Price,
					Capacity = dto.Capacity,
					BedCount = dto.BedCount,
					BedType = dto.BedType.Trim(),
					Size = dto.Size,
					Description = dto.Description.Trim(),
					CreatedAt = now,
					UpdatedAt = now,
					Amenities = amenities,
					Images = uploadedImages
						.Select(uploaded => new RoomImage
						{
							ImageUrl = uploaded.SecureUrl,
							PublicId = uploaded.PublicId
						})
						.ToList()
				};

				_context.Rooms.Add(room);
				await _context.SaveChangesAsync(cancellationToken);

				var response = await _context.Rooms
					.AsNoTracking()
					.Include(r => r.Images)
					.Include(r => r.Amenities)
					.Where(r => r.Id == room.Id)
					.Select(r => new RoomResponseDto
					{
						Id = r.Id,
						RoomNumber = r.RoomNumber,
						Name = r.Name,
						Category = r.Category,
						Floor = r.Floor,
						Status = r.Status,
						IsAvailable = r.IsAvailable,
						Price = r.Price,
						Capacity = r.Capacity,
						BedCount = r.BedCount,
						BedType = r.BedType,
						Size = r.Size,
						Description = r.Description,
						CreatedAt = r.CreatedAt,
						UpdatedAt = r.UpdatedAt,
						AverageRating = r.AverageRating,
						TotalReviews = r.TotalReviews,
						Images = r.Images
							.Select(image => new RoomImageDto
							{
								Id = image.Id,
								ImageUrl = image.ImageUrl,
								PublicId = image.PublicId
							})
							.ToList(),
						Amenities = r.Amenities
							.Select(amenity => new AmenityDto
							{
								Id = amenity.Id,
								Name = amenity.Name
							})
							.ToList()
					})
					.FirstAsync(cancellationToken);

				return CreatedAtAction(nameof(GetRoomById), new { id = room.Id }, response);
			}
			catch
			{
				await CleanupUploadedImagesAsync(uploadedImages, cancellationToken);
				throw;
			}
		}

		private async Task CleanupUploadedImagesAsync(
			IEnumerable<CloudinaryUploadResult> uploadedImages,
			CancellationToken cancellationToken)
		{
			foreach (var uploadedImage in uploadedImages)
			{
				if (string.IsNullOrWhiteSpace(uploadedImage.PublicId))
				{
					continue;
				}

				try
				{
					await _cloudinaryService.DeleteImageAsync(uploadedImage.PublicId, cancellationToken);
				}
				catch
				{
					// Intentionally ignored to avoid masking the original failure.
				}
			}
		}

		[HttpPut("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<ActionResult<RoomResponseDto>> UpdateRoom(Guid id, [FromBody] CreateOrupdateRoomDto dto)
		{
			var room = await _context.Rooms
				.Include(r => r.Amenities)
				.FirstOrDefaultAsync(r => r.Id == id);

			if (room is null)
			{
				return NotFound(new { message = "Room not found." });
			}

			if (await _context.Rooms.AnyAsync(r => r.Id != id && r.RoomNumber == dto.RoomNumber))
			{
				return Conflict(new { message = "Room number already exists." });
			}

			var amenityIds = dto.AmenityIds.Distinct().ToList();
			var amenities = await _context.Amenities
				.Where(a => amenityIds.Contains(a.Id))
				.ToListAsync();

			if (amenities.Count != amenityIds.Count)
			{
				var foundIds = amenities.Select(a => a.Id).ToHashSet();
				var missingIds = amenityIds.Where(amenityId => !foundIds.Contains(amenityId)).ToList();
				return BadRequest(new { message = "Some amenities were not found.", missingAmenityIds = missingIds });
			}

			room.RoomNumber = dto.RoomNumber;
			room.Name = dto.Name.Trim();
			room.Category = dto.Category.Trim();
			room.Floor = dto.Floor;
			room.Status = dto.Status.Trim();
			room.IsAvailable = dto.IsAvailable;
			room.Price = dto.Price;
			room.Capacity = dto.Capacity;
			room.BedCount = dto.BedCount;
			room.BedType = dto.BedType.Trim();
			room.Size = dto.Size;
			room.Description = dto.Description.Trim();
			room.UpdatedAt = DateTime.UtcNow;

			room.Amenities.Clear();
			foreach (var amenity in amenities)
			{
				room.Amenities.Add(amenity);
			}

			await _context.SaveChangesAsync();

			var response = await _context.Rooms
				.AsNoTracking()
				.Include(r => r.Images)
				.Include(r => r.Amenities)
				.Where(r => r.Id == room.Id)
				.Select(r => new RoomResponseDto
				{
					Id = r.Id,
					RoomNumber = r.RoomNumber,
					Name = r.Name,
					Category = r.Category,
					Floor = r.Floor,
					Status = r.Status,
					IsAvailable = r.IsAvailable,
					Price = r.Price,
					Capacity = r.Capacity,
					BedCount = r.BedCount,
					BedType = r.BedType,
					Size = r.Size,
					Description = r.Description,
					CreatedAt = r.CreatedAt,
					UpdatedAt = r.UpdatedAt,
					AverageRating = r.AverageRating,
					TotalReviews = r.TotalReviews,
					Images = r.Images
						.Select(image => new RoomImageDto
						{
							Id = image.Id,
							ImageUrl = image.ImageUrl,
							PublicId = image.PublicId
						})
						.ToList(),
					Amenities = r.Amenities
						.Select(amenity => new AmenityDto
						{
							Id = amenity.Id,
							Name = amenity.Name
						})
						.ToList()
				})
				.FirstAsync();

			return Ok(response);
		}

		[HttpDelete("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> DeleteRoom(Guid id, CancellationToken cancellationToken)
		{
			var room = await _context.Rooms
				.Include(r => r.Bookings)
				.Include(r => r.Images)
				.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

			if (room is null)
			{
				return NotFound(new { message = "Room not found." });
			}

			if (room.Bookings.Count > 0)
			{
				return Conflict(new { message = "Room cannot be deleted because it has booking history." });
			}

			foreach (var image in room.Images)
			{
				if (string.IsNullOrWhiteSpace(image.PublicId))
				{
					continue;
				}

				await _cloudinaryService.DeleteImageAsync(image.PublicId, cancellationToken);
			}

			_context.Rooms.Remove(room);
			await _context.SaveChangesAsync(cancellationToken);

			return NoContent();
		}

		[HttpGet("{roomId:guid}/images")]
		[AllowAnonymous]
		public async Task<ActionResult<List<RoomImageDto>>> GetRoomImages(Guid roomId)
		{
			var roomExists = await _context.Rooms.AsNoTracking().AnyAsync(r => r.Id == roomId);
			if (!roomExists)
			{
				return NotFound(new { message = "Room not found." });
			}

			var images = await _context.RoomImages
				.AsNoTracking()
				.Where(i => i.RoomId == roomId)
				.OrderBy(i => i.Id)
				.Select(i => new RoomImageDto
				{
					Id = i.Id,
					ImageUrl = i.ImageUrl
				})
				.ToListAsync();

			return Ok(images);
		}

		[HttpPost("{roomId:guid}/images")]
		[Authorize(Policy = "AdminOnly")]
		[RequestSizeLimit(MaxRoomImageSize)]
		public async Task<ActionResult<RoomImageDto>> AddRoomImage(
			Guid roomId,
			IFormFile file,
			CancellationToken cancellationToken)
		{
			if (file is null || file.Length == 0)
			{
				return BadRequest(new { message = "Please select an image." });
			}

			if (file.Length > MaxRoomImageSize)
			{
				return BadRequest(new { message = "Room image must be 10 MB or smaller." });
			}

			if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
			{
				return BadRequest(new { message = "Only JPEG, PNG, and WebP images are allowed." });
			}

			var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);
			if (room is null)
			{
				return NotFound(new { message = "Room not found." });
			}

			await using var stream = file.OpenReadStream();
			var uploadResult = await _cloudinaryService.UploadRoomImageAsync(stream, file.FileName, cancellationToken);

			var image = new RoomImage
			{
				ImageUrl = uploadResult.SecureUrl,
				PublicId = uploadResult.PublicId,
				RoomId = roomId
			};

			_context.RoomImages.Add(image);
			await _context.SaveChangesAsync(cancellationToken);

			return CreatedAtAction(nameof(GetRoomImages), new { roomId }, new RoomImageDto
			{
				Id = image.Id,
				ImageUrl = image.ImageUrl
			});
		}

		[HttpDelete("{roomId:guid}/images/{imageId:int}")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> DeleteRoomImage(Guid roomId, int imageId, CancellationToken cancellationToken)
		{
			var image = await _context.RoomImages
				.FirstOrDefaultAsync(i => i.Id == imageId && i.RoomId == roomId, cancellationToken);

			if (image is null)
			{
				return NotFound(new { message = "Room image not found." });
			}

			if (!string.IsNullOrWhiteSpace(image.PublicId))
			{
				await _cloudinaryService.DeleteImageAsync(image.PublicId, cancellationToken);
			}

			_context.RoomImages.Remove(image);
			await _context.SaveChangesAsync(cancellationToken);

			return NoContent();
		}

		private List<int> ParseAmenityIds(List<int>? amenityIdsFromModel)
		{
			if (amenityIdsFromModel is { Count: > 0 })
			{
				return amenityIdsFromModel.Distinct().ToList();
			}

			if (!Request.Query.TryGetValue("amenityIds", out var rawAmenityIds))
			{
				return [];
			}

			return [.. rawAmenityIds
				.SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.Select(v => (isValid: int.TryParse(v, out var parsedId), parsedId))
				.Where(x => x.isValid)
				.Select(x => x.parsedId)
				.Distinct()];
		}

	}
}
