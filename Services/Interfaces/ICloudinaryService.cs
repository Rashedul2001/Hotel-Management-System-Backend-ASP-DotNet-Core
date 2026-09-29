namespace Hotel_Management_System_Backend_dotNet.Services.Interfaces;

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadProfilePictureAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default);
}

public sealed record CloudinaryUploadResult(
    string SecureUrl,
    string PublicId);