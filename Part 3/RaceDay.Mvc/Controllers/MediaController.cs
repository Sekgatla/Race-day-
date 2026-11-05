using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class MediaController(RaceDayApiClient api) : Controller
{
    [HttpGet("/media/events/{id:int}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> EventBanner(int id, CancellationToken cancellationToken)
    {
        var image = await api.GetFileAsync($"api/events/{id}/banner", cancellationToken);
        return image is null ? NotFound() : File(image.Bytes, image.ContentType);
    }

    [SessionAuthorize]
    [HttpGet("/media/profile")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProfilePicture(CancellationToken cancellationToken)
    {
        var image = await api.GetFileAsync("api/users/me/profile-image", cancellationToken);
        return image is null ? NotFound() : File(image.Bytes, image.ContentType);
    }
}
