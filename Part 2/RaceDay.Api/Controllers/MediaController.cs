using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Storage;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class MediaController(
    RaceDayDbContext db,
    IImageStorage imageStorage,
    ILogger<MediaController> logger) : ControllerBase
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MultipartOverheadBytes = 64 * 1024;

    [HttpPut("events/{eventId:int}/banner")]
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes + MultipartOverheadBytes)]
    public async Task<IActionResult> UploadEventBanner(
        int eventId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(
            item => item.EventId == eventId,
            cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ApiError("Event was not found."));
        }
        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Forbid();
        }

        var imageType = await ValidateImageAsync(file, cancellationToken);
        if (imageType is null)
        {
            return BadRequest(new ApiError("Choose a PNG, JPEG, or WebP image up to 5 MB."));
        }

        var key = $"events/{eventId}/banner/{Guid.NewGuid():N}{imageType.Value.Extension}";
        await using var content = file!.OpenReadStream();
        var storageError = await TryStoreAsync(key, content, imageType.Value.ContentType, cancellationToken);
        if (storageError is not null)
        {
            return storageError;
        }

        var previousKey = raceEvent.BannerImageKey;
        raceEvent.BannerImageKey = key;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(key);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            await TryDeleteAsync(previousKey);
        }
        return NoContent();
    }

    [HttpGet("events/{eventId:int}/banner")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventBanner(int eventId, CancellationToken cancellationToken)
    {
        var key = await db.Events.AsNoTracking()
            .Where(item => item.EventId == eventId)
            .Select(item => item.BannerImageKey)
            .SingleOrDefaultAsync(cancellationToken);
        if (key is null)
        {
            return NotFound(new ApiError("This event does not have a banner image."));
        }

        var (image, storageAvailable) = await TryGetAsync(key, cancellationToken);
        if (!storageAvailable)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ApiError("Image storage is temporarily unavailable. Please try again."));
        }
        if (image is null)
        {
            return NotFound(new ApiError("The banner image was not found."));
        }
        return File(image.Content, image.ContentType);
    }

    [HttpPut("users/me/profile-image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageBytes + MultipartOverheadBytes)]
    public async Task<IActionResult> UploadProfileImage(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            item => item.UserId == CurrentUserId,
            cancellationToken);
        if (user is null)
        {
            return NotFound(new ApiError("User account was not found."));
        }

        var imageType = await ValidateImageAsync(file, cancellationToken);
        if (imageType is null)
        {
            return BadRequest(new ApiError("Choose a PNG, JPEG, or WebP image up to 5 MB."));
        }

        var key = $"users/{user.UserId}/profile/{Guid.NewGuid():N}{imageType.Value.Extension}";
        await using var content = file!.OpenReadStream();
        var storageError = await TryStoreAsync(key, content, imageType.Value.ContentType, cancellationToken);
        if (storageError is not null)
        {
            return storageError;
        }

        var previousKey = user.ProfileImageKey;
        user.ProfileImageKey = key;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(key);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousKey))
        {
            await TryDeleteAsync(previousKey);
        }
        return NoContent();
    }

    [HttpGet("users/me/profile-image")]
    [Authorize]
    public async Task<IActionResult> GetProfileImage(CancellationToken cancellationToken)
    {
        var key = await db.Users.AsNoTracking()
            .Where(item => item.UserId == CurrentUserId)
            .Select(item => item.ProfileImageKey)
            .SingleOrDefaultAsync(cancellationToken);
        if (key is null)
        {
            return NotFound(new ApiError("Your profile does not have an image."));
        }

        var (image, storageAvailable) = await TryGetAsync(key, cancellationToken);
        if (!storageAvailable)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ApiError("Image storage is temporarily unavailable. Please try again."));
        }
        if (image is null)
        {
            return NotFound(new ApiError("The profile image was not found."));
        }
        return File(image.Content, image.ContentType);
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<IActionResult?> TryStoreAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            await imageStorage.StoreAsync(key, content, contentType, cancellationToken);
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not store RaceDay image {ImageKey}.", key);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ApiError("Image storage is temporarily unavailable. Please try again."));
        }
    }

    private async Task<(StoredImage? Image, bool StorageAvailable)> TryGetAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await imageStorage.GetAsync(key, cancellationToken), true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not retrieve RaceDay image {ImageKey}.", key);
            return (null, false);
        }
    }

    private async Task TryDeleteAsync(string key)
    {
        try
        {
            await imageStorage.DeleteAsync(key, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not remove previous RaceDay image {ImageKey}.", key);
        }
    }

    private static async Task<ImageType?> ValidateImageAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length is <= 0 or > MaxImageBytes)
        {
            return null;
        }

        var prefix = new byte[12];
        await using var stream = file.OpenReadStream();
        var count = await stream.ReadAsync(prefix.AsMemory(), cancellationToken);
        ImageType? actualType = count >= 8 &&
                                prefix.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            ? new ImageType("image/png", ".png")
            : count >= 3 && prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF
                ? new ImageType("image/jpeg", ".jpg")
                : count == 12 &&
                  prefix.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                  prefix.AsSpan(8, 4).SequenceEqual("WEBP"u8)
                    ? new ImageType("image/webp", ".webp")
                    : null;
        if (actualType is null)
        {
            return null;
        }

        var declaredType = file.ContentType.Split(';', 2)[0].Trim();
        return string.Equals(declaredType, actualType.Value.ContentType, StringComparison.OrdinalIgnoreCase)
            ? actualType
            : null;
    }

    private readonly record struct ImageType(string ContentType, string Extension);
}
