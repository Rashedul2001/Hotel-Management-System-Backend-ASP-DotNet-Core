using Bogus;
using Hotel_Management_System_Backend_dotNet.Common.Constants;
using Hotel_Management_System_Backend_dotNet.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hotel_Management_System_Backend_dotNet.Data.MockData
{
	public static class MockDataSeeder
	{
		public static async Task SeedRoomsAsync(ApplicationDbContext context)
		{
			// 1. Seed Master Amenities first to prevent duplicate entries
			var availableAmenities = await SeedAndGetAmenitiesAsync(context);

			var currentRoomCount = await context.Rooms.CountAsync();
			var roomIdCounter = await context.Rooms.MaxAsync(r => (int?)r.RoomNumber) ?? 0;
			roomIdCounter++;
			if (currentRoomCount >= 50)
			{
				return;
			}

			int roomsToGenerate = 50 - currentRoomCount;

			var roomCategory = RoomCategories.AllCategories; 
			var bedTypes = new[] { "Single", "Double", "Queen", "King" };
			var statuses = Statuses.AllStatuses;
			Console.WriteLine($"Generating {roomsToGenerate} mock rooms...");

			var roomFaker = new Faker<Room>()
				.RuleFor(r => r.Id, f => Guid.NewGuid())
				.RuleFor(r => r.RoomNumber, f => roomIdCounter++)
				.RuleFor(r => r.Name, f => $"{f.Address.City()} {f.PickRandom("Suite", "Room", "Retreat")}")
				.RuleFor(r => r.Category, f => f.PickRandom(roomCategory))
				.RuleFor(r => r.Floor, f => f.Random.Int(1, 15))
				.RuleFor(r => r.Status, f => f.PickRandom(statuses))
				.RuleFor(r => r.IsAvailable, f => f.Random.Bool())
				.RuleFor(r => r.Price, f => Math.Round(f.Random.Decimal(50, 1000), 2))
				.RuleFor(r => r.Capacity, f => f.Random.Int(1, 6))
				.RuleFor(r => r.BedCount, f => f.Random.Int(1, 3))
				.RuleFor(r => r.BedType, f => f.PickRandom(bedTypes))
				.RuleFor(r => r.Size, f => f.Random.Int(25, 120))
				.RuleFor(r => r.Description, f => f.Lorem.Paragraph(2))
				.RuleFor(r => r.CreatedAt, f => f.Date.Past(1).ToUniversalTime())
				.RuleFor(r => r.UpdatedAt, (f, r) => f.Date.Recent(30, r.CreatedAt).ToUniversalTime())

				// Assign 2 to 5 random Images
				.RuleFor(r => r.Images, (f, r) =>
				{
					var images = new List<RoomImage>();
					var imageCount = f.Random.Int(2, 5);
					for (int i = 0; i < imageCount; i++)
					{
						images.Add(new RoomImage
						{
							ImageUrl = "https://media.istockphoto.com/id/2213411469/photo/photorealistic-3d-render-of-a-premium-hotel-bedroom.jpg?s=612x612&w=0&k=20&c=0orkasa78xjFbY-qiltD58oT6yqhCCeM632clCdpSDo=",
							PublicId = $"mock_room_{f.Random.AlphaNumeric(10)}",
							RoomId = r.Id
						});
					}
					return images;
				})

				// Assign a random subset of our master Amenities list
				.RuleFor(r => r.Amenities, f => f.PickRandom(availableAmenities, f.Random.Int(3, 7)).ToList())

				// Assign 0 to 10 random Reviews
				.RuleFor(r => r.Reviews, (f, r) =>
				{
					var reviews = new List<Review>();
					var reviewCount = f.Random.Int(0, 10);
					for (int i = 0; i < reviewCount; i++)
					{
						reviews.Add(new Review
						{
							UserId = Guid.NewGuid(), // Mocking random users
							Rating = f.Random.Int(1, 5),
							Comment = f.Lorem.Sentences(f.Random.Int(1, 3)),
							RoomId = r.Id
						});
					}
					return reviews;
				});

			var generatedRooms = roomFaker.Generate(roomsToGenerate);

			// Calculate the Average Rating and Total Reviews dynamically based on the generated Reviews
			foreach (var room in generatedRooms)
			{
				room.TotalReviews = room.Reviews.Count;
				room.AverageRating = room.Reviews.Any()
					? Math.Round(room.Reviews.Average(rev => rev.Rating), 1)
					: 0;
			}

			await context.Rooms.AddRangeAsync(generatedRooms);
			await context.SaveChangesAsync();
		}

		// Helper method to ensure amenities exist and return them
		private static async Task<List<Amenity>> SeedAndGetAmenitiesAsync(ApplicationDbContext context)
		{
			var amenities = await context.Amenities.ToListAsync();

			if (amenities.Any())
			{
				return amenities;
			}

			var defaultAmenities = new List<string>
			{
				"Free WiFi", "Air Conditioning", "Flat-Screen TV", "Mini Bar",
				"Room Service", "Balcony", "Safe", "Gym Access", "Coffee Maker", "Ocean View"
			};

			foreach (var amenityName in defaultAmenities)
			{
				var amenity = new Amenity { Name = amenityName };
				context.Amenities.Add(amenity);
				amenities.Add(amenity);
			}

			await context.SaveChangesAsync();
			return amenities;
		}
	}
}