using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using Microsoft.Extensions.Logging;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for processing user profile photos and expense receipts.
/// Handles image loading, cropping, square conversion, compression, and storage.
/// </summary>
public class PhotoProcessingService
{
    private readonly ILogger<PhotoProcessingService>? _logger;
    private readonly string _storagePath;

    public PhotoProcessingService(
        ILogger<PhotoProcessingService>? logger = null,
        string? storagePath = null)
    {
        _logger = logger;
        _storagePath = storagePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyDesk", "Photos");

        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    /// <summary>
    /// Crop image to specified rectangle and convert to square.
    /// </summary>
    public async Task<(Stream croppedImage, int width, int height, string contentType)> CropAndSquareAsync(
        Stream originalImage,
        string originalContentType,
        int cropLeft,
        int cropTop,
        int cropWidth,
        int cropHeight,
        int outputDimension = 500)
    {
        try
        {
            _logger?.LogInformation("Cropping image: left={L}, top={T}, width={W}, height={H}, output={D}",
                cropLeft, cropTop, cropWidth, cropHeight, outputDimension);

            using var image = await Image.LoadAsync(originalImage);

            // Validate crop bounds
            var actualCropWidth = Math.Min(cropWidth, image.Width - cropLeft);
            var actualCropHeight = Math.Min(cropHeight, image.Height - cropTop);

            if (actualCropWidth <= 0 || actualCropHeight <= 0)
                throw new ArgumentException("Invalid crop dimensions");

            // Crop to specified rectangle
            image.Mutate(x => x.Crop(new Rectangle(cropLeft, cropTop, actualCropWidth, actualCropHeight)));

            // Resize to square (maintains aspect ratio by padding)
            var squareDim = Math.Max(image.Width, image.Height);
            var squareImage = new Image<Rgba32>(squareDim, squareDim);

            // Center the cropped image in the square
            var xOffset = (squareDim - image.Width) / 2;
            var yOffset = (squareDim - image.Height) / 2;

            squareImage.Mutate(x => x.DrawImage(image, new Point(xOffset, yOffset), 1f));

            // Resize to target dimension
            squareImage.Mutate(x => x.Resize(
                new ResizeOptions
                {
                    Size = new Size(outputDimension, outputDimension),
                    Mode = ResizeMode.Crop,
                    Sampler = KnownResamplers.Lanczos3
                }));

            // Encode to stream
            var outputStream = new MemoryStream();
            var contentType = originalContentType.ToLower() switch
            {
                "image/png" => await EncodePngAsync(squareImage, outputStream),
                _ => await EncodeJpegAsync(squareImage, outputStream, quality: 85)
            };

            outputStream.Position = 0;
            _logger?.LogInformation("Crop completed successfully: {W}x{H}", outputDimension, outputDimension);

            return (outputStream, outputDimension, outputDimension, contentType);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error cropping image");
            throw;
        }
    }

    /// <summary>
    /// Convert any image to square by padding with white background.
    /// </summary>
    public async Task<(Stream squareImage, int dimension, string contentType)> ConvertToSquareAsync(
        Stream originalImage,
        string originalContentType,
        int outputDimension = 500)
    {
        try
        {
            _logger?.LogInformation("Converting to square: input type={T}, output dim={D}",
                originalContentType, outputDimension);

            using var image = await Image.LoadAsync(originalImage);
            var squareDim = Math.Max(image.Width, image.Height);

            // Create square canvas
            var squareImage = new Image<Rgba32>(squareDim, squareDim, SixLabors.ImageSharp.Color.White);

            // Center original image
            var xOffset = (squareDim - image.Width) / 2;
            var yOffset = (squareDim - image.Height) / 2;

            squareImage.Mutate(x => x.DrawImage(image, new Point(xOffset, yOffset), 1f));

            // Resize to target
            squareImage.Mutate(x => x.Resize(
                new ResizeOptions
                {
                    Size = new Size(outputDimension, outputDimension),
                    Mode = ResizeMode.Crop
                }));

            var outputStream = new MemoryStream();
            var contentType = originalContentType.ToLower() switch
            {
                "image/png" => await EncodePngAsync(squareImage, outputStream),
                _ => await EncodeJpegAsync(squareImage, outputStream, quality: 85)
            };

            outputStream.Position = 0;
            _logger?.LogInformation("Square conversion completed: {W}x{H}", outputDimension, outputDimension);

            return (outputStream, outputDimension, contentType);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error converting to square");
            throw;
        }
    }

    /// <summary>
    /// Create thumbnail for display in lists.
    /// </summary>
    public async Task<Stream> CreateThumbnailAsync(
        Stream originalImage,
        string originalContentType,
        int thumbnailDimension = 100)
    {
        try
        {
            _logger?.LogInformation("Creating thumbnail: dimension={D}", thumbnailDimension);

            using var image = await Image.LoadAsync(originalImage);

            image.Mutate(x => x.Resize(
                new ResizeOptions
                {
                    Size = new Size(thumbnailDimension, thumbnailDimension),
                    Mode = ResizeMode.Crop,
                    Sampler = KnownResamplers.Lanczos3
                }));

            var outputStream = new MemoryStream();

            if (originalContentType.Contains("png", StringComparison.OrdinalIgnoreCase))
                await image.SaveAsPngAsync(outputStream);
            else
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 80 });

            outputStream.Position = 0;
            _logger?.LogInformation("Thumbnail created successfully");

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating thumbnail");
            throw;
        }
    }

    /// <summary>
    /// Validate image file (type, size, dimensions).
    /// </summary>
    public async Task<(bool isValid, string? error)> ValidateImageAsync(
        Stream imageStream,
        string contentType,
        long maxSizeBytes = 5242880,  // 5 MB
        int minDimension = 100)
    {
        try
        {
            // Check content type
            if (!IsValidImageContentType(contentType))
                return (false, $"Invalid content type: {contentType}. Supported: image/jpeg, image/png");

            // Check file size
            if (imageStream.Length > maxSizeBytes)
                return (false, $"File too large. Maximum size: {maxSizeBytes / 1048576} MB");

            // Check dimensions
            try
            {
                imageStream.Position = 0;
                using var image = await Image.LoadAsync(imageStream);

                if (image.Width < minDimension || image.Height < minDimension)
                    return (false, $"Image too small. Minimum dimension: {minDimension}x{minDimension}");
            }
            catch
            {
                return (false, "Invalid image file or corrupted");
            }

            _logger?.LogInformation("Image validation passed");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error validating image");
            return (false, "Error validating image");
        }
    }

    /// <summary>
    /// Save processed image to file system.
    /// </summary>
    public async Task<string> SaveImageAsync(
        Stream imageStream,
        string tenantId,
        string userId,
        string fileName)
    {
        try
        {
            var directory = Path.Combine(_storagePath, tenantId, userId);
            Directory.CreateDirectory(directory);

            var newFileName = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName)}.jpg";
            var filePath = Path.Combine(directory, newFileName);

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await imageStream.CopyToAsync(fileStream);

            _logger?.LogInformation("Image saved: {Path}", filePath);
            return $"/tenant/{tenantId}/photos/{userId}/{newFileName}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving image");
            throw;
        }
    }

    private static bool IsValidImageContentType(string contentType) =>
        contentType switch
        {
            "image/jpeg" or "image/jpg" or "image/png" => true,
            _ => false
        };

    private static async Task<string> EncodeJpegAsync(
        Image image,
        Stream outputStream,
        int quality = 85)
    {
        await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = quality });
        return "image/jpeg";
    }

    private static async Task<string> EncodePngAsync(
        Image image,
        Stream outputStream)
    {
        await image.SaveAsPngAsync(outputStream);
        return "image/png";
    }
}
