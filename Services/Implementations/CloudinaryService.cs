using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Hotel_Management_System_Backend_dotNet.Data.Configuration;
using Hotel_Management_System_Backend_dotNet.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Hotel_Management_System_Backend_dotNet.Services.Implementations;

public sealed class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(
        IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.CloudName) ||
            string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary configuration is incomplete.");
        }

        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public Task<CloudinaryUploadResult> UploadProfilePictureAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var transformation = new Transformation()
            .Width(512)
            .Height(512)
            .Crop("fill")
            .Gravity("face")
            .Quality("auto")
            .FetchFormat("auto");

        return UploadImageAsync(
            fileStream,
            fileName,
            "velora/users/profile-pictures",
            transformation,
            cancellationToken);
    }

    public Task<CloudinaryUploadResult> UploadRoomImageAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var transformation = new Transformation()
            .Width(1600)
            .Height(1000)
            .Crop("fill")
            .Quality("auto")
            .FetchFormat("auto");

        return UploadImageAsync(
            fileStream,
            fileName,
            "velora/rooms",
            transformation,
            cancellationToken);
    }

    public async Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var deleteParams = new DeletionParams(publicId);

        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Error != null)
        {
            throw new InvalidOperationException(
                $"Cloudinary deletion failed: {result.Error.Message}");
        }
    }

    private async Task<CloudinaryUploadResult> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string folder,
        Transformation transformation,
        CancellationToken cancellationToken)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = folder,
            UseFilename = false,
            UniqueFilename = true,
            Transformation = transformation
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error != null)
        {
            throw new InvalidOperationException(
                $"Cloudinary upload failed: {result.Error.Message}");
        }

        return new CloudinaryUploadResult(
            result.SecureUrl.ToString(),
            result.PublicId);
    }
}
