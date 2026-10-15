using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/weather")]
public sealed class WeatherController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Lists published weather forecasts for a public event.</summary>
    [AllowAnonymous]
    [HttpGet]
    [SwaggerOperation(Summary = "Get event weather", Description = "Returns weather forecasts for the event, optionally filtered to forecastDate.")]
    [ProducesResponseType<IReadOnlyList<WeatherDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WeatherDto>>> List(
        int eventId, [FromQuery] DateOnly? forecastDate, CancellationToken cancellationToken)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId, cancellationToken))
            return NotFound(new ApiError("Event was not found."));
        var query = db.EventWeather.AsNoTracking().Where(item => item.EventId == eventId);
        if (forecastDate.HasValue) query = query.Where(item => item.ForecastDate == forecastDate.Value);
        var forecasts = await query.OrderBy(item => item.ForecastDate).Select(MapProjection())
            .ToListAsync(cancellationToken);
        return forecasts.Count == 0
            ? NotFound(new ApiError("No weather forecast is available for this event and date."))
            : Ok(forecasts);
    }

    /// <summary>Adds a weather forecast for an event owned by the current organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPost]
    [SwaggerOperation(Summary = "Add event weather", Description = "Adds a forecast or race-day advisory for an event managed by the current organiser.")]
    [ProducesResponseType<WeatherDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WeatherDto>> Create(int eventId, CreateWeatherRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.OrganiserId != CurrentUserId) return Forbid();
        if (request.ForecastDate == default) return BadRequest(new ApiError("ForecastDate is required."));
        if (await db.EventWeather.AnyAsync(item =>
                item.EventId == eventId && item.ForecastDate == request.ForecastDate, cancellationToken))
            return Conflict(new ApiError("A forecast for this date already exists."));

        var forecast = new EventWeather
        {
            EventId = eventId,
            ForecastDate = request.ForecastDate,
            TemperatureC = request.TemperatureC,
            Condition = request.Condition.Trim(),
            WindSpeedKph = request.WindSpeedKph,
            RainProbabilityPercent = request.RainProbabilityPercent,
            Advisory = request.Advisory?.Trim(),
            RecordedAt = DateTime.UtcNow
        };
        db.EventWeather.Add(forecast);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/events/{eventId}/weather?forecastDate={request.ForecastDate:yyyy-MM-dd}", Map(forecast));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static WeatherDto Map(EventWeather weather) => new(
        weather.WeatherId, weather.EventId, weather.ForecastDate, weather.TemperatureC,
        weather.Condition, weather.WindSpeedKph, weather.RainProbabilityPercent,
        weather.Advisory, weather.RecordedAt);

    private static System.Linq.Expressions.Expression<Func<EventWeather, WeatherDto>> MapProjection() => weather =>
        new WeatherDto(weather.WeatherId, weather.EventId, weather.ForecastDate,
            weather.TemperatureC, weather.Condition, weather.WindSpeedKph,
            weather.RainProbabilityPercent, weather.Advisory, weather.RecordedAt);
}
