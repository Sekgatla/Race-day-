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
[Route("api/events/{eventId:int}/route")]
public sealed class RoutesController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Returns route details for a public event.</summary>
    [AllowAnonymous]
    [HttpGet]
    [SwaggerOperation(Summary = "Get event route", Description = "Returns the route name, distance, description, map link, and elevation gain.")]
    [ProducesResponseType<RouteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteDto>> Get(int eventId, CancellationToken cancellationToken)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId, cancellationToken))
            return NotFound(new ApiError("Event was not found."));
        var route = await db.Routes.AsNoTracking().SingleOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        return route is null ? NotFound(new ApiError("No route has been published for this event.")) : Ok(Map(route));
    }

    /// <summary>Creates or replaces route information for an event owned by the current organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPut]
    [SwaggerOperation(Summary = "Create or update event route", Description = "Upserts the route for an event owned by the current organiser.")]
    [ProducesResponseType<RouteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RouteDto>> Upsert(int eventId, UpsertRouteRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.OrganiserId != CurrentUserId) return Forbid();

        var route = await db.Routes.SingleOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (route is null)
        {
            route = new EventRoute { EventId = eventId };
            db.Routes.Add(route);
        }

        route.RouteName = request.RouteName.Trim();
        route.DistanceKm = request.DistanceKm;
        route.RouteDescription = request.RouteDescription.Trim();
        route.MapUrl = request.MapUrl;
        route.ElevationGainMeters = request.ElevationGainMeters;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(route));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static RouteDto Map(EventRoute route) => new(
        route.RouteId, route.EventId, route.RouteName, route.DistanceKm,
        route.RouteDescription, route.MapUrl, route.ElevationGainMeters);
}
