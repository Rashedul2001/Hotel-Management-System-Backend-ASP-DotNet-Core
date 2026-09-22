using Hotel_Management_System_Backend_dotNet.Entities;
using Hotel_Management_System_Backend_dotNet.Services;
using Hotel_Management_System_Backend_dotNet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Management_System_Backend_dotNet.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(
    UserManager<ApplicationUser> userManager,
    ICloudinaryService cloudinaryService) : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ICloudinaryService _cloudinaryService = cloudinaryService;

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    [HttpPut("picture")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UpdateProfilePicture(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Please select an image."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "Profile picture must be 5 MB or smaller."
            });
        }

        if (!AllowedImageTypes.Contains(
                file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new
            {
                message =
                    "Only JPEG, PNG, and WebP images are allowed."
            });
        }

        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        await using var stream = file.OpenReadStream();

        var uploadResult =
            await _cloudinaryService.UploadProfilePictureAsync(
                stream,
                file.FileName,
                cancellationToken);

        var oldPublicId = user.ProfilePicturePublicId;

        user.ProfilePictureUrl = uploadResult.SecureUrl;
        user.ProfilePicturePublicId = uploadResult.PublicId;

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            // The new Cloudinary asset was created, but the database
            // update failed. A production implementation should also
            // attempt to clean up the newly uploaded asset here.

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Could not update profile picture.",
                    errors = updateResult.Errors
                });
        }

        if (!string.IsNullOrWhiteSpace(oldPublicId))
        {
            try
            {
                await _cloudinaryService.DeleteImageAsync(
                    oldPublicId,
                    cancellationToken);
            }
            catch
            {
                // Log this failure in production.
                // Do not invalidate an otherwise successful profile update.
            }
        }

        return Ok(new
        {
            profilePictureUrl = user.ProfilePictureUrl
        });
    }
}