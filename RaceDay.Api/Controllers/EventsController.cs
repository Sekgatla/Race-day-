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
[Route("api/events")]
public sealed class EventsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Lists public events, optionally filtered by type, province, date range, status, or text.</summary>
    [AllowAnonymous]
    [HttpGet]
    [SwaggerOperation(Summary = "Browse events", Description = "Returns public event details and optional filters: eventType, province, fromDate, toDate, status, and search.")]
    [ProducesResponseType<IReadOnlyList<EventDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> List(
        [FromQuery] string? eventType,
        [FromQuery] string? province,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            return BadRequest(new ApiError("fromDate must be on or before toDate."));
        }
        if (!string.IsNullOrWhiteSpace(eventType) && !ValidEventType(eventType))
        {
            return BadRequest(new ApiError("eventType must be Running, Walking, or Cycling."));
        }
        if (!string.IsNullOrWhiteSpace(status) && status is not ("Scheduled" or "Completed" or "Cancelled"))
        {
            return BadRequest(new ApiError("status must be Scheduled, Completed, or Cancelled."));
        }

        var query = db.Events.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType == eventType);
        if (!string.IsNullOrWhiteSpace(province)) query = query.Where(e => e.Province == province);
        if (fromDate.HasValue) query = query.Where(e => e.EventDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.EventDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.EventName.Contains(term) || e.Location.Contains(term) || e.Province.Contains(term));
        }

        var events = await query.OrderBy(e => e.EventDate)
            .Select(e => new EventDto(
                e.EventId, e.OrganiserId, e.Organiser.FirstName + " " + e.Organiser.LastName,
                e.EventName, e.Description, e.EventDate, e.StartTime, e.Location, e.Province,
                e.EventType, e.DistanceKm, e.MaxParticipants, e.Status,
                e.Categories.Count(), e.Enrolments.Count()))
            .ToListAsync(cancellationToken);
        return Ok(events);
    }

    /// <summary>Returns public details for one event, including its organiser and counts.</summary>
    [AllowAnonymous]
    [HttpGet("{eventId:int}")]
    [SwaggerOperation(Summary = "Get event details", Description = "Returns one event's public fields, organiser name, category count, and enrolment count.")]
    [ProducesResponseType<EventDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Get(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await EventProjection().SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        return raceEvent is null ? NotFound(new ApiError("Event was not found.")) : Ok(raceEvent);
    }

    /// <summary>Lists events created by the authenticated organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpGet("/api/organiser/events")]
    [SwaggerOperation(Summary = "List my events", Description = "Returns only events owned by the current organiser. An optional status filter may be supplied.")]
    [ProducesResponseType<IReadOnlyList<EventDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> Mine([FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(status) && status is not ("Scheduled" or "Completed" or "Cancelled"))
            return BadRequest(new ApiError("status must be Scheduled, Completed, or Cancelled."));

        var query = EventProjection().Where(e => e.OrganiserId == CurrentUserId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
        return Ok(await query.OrderBy(e => e.EventDate).ToListAsync(cancellationToken));
    }

    /// <summary>Creates an event owned by the authenticated organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPost]
    [SwaggerOperation(Summary = "Create an event", Description = "Creates a running, walking, or cycling event owned by the authenticated organiser.")]
    [ProducesResponseType<EventDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> Create(CreateEventRequest request, CancellationToken cancellationToken)
    {
        if (request.EventDate == default)
        {
            return BadRequest(new ApiError("EventDate is required."));
        }
        if (!ValidEventType(request.EventType))
        {
            return BadRequest(new ApiError("EventType must be Running, Walking, or Cycling."));
        }

        var user = await db.Users.SingleAsync(candidate => candidate.UserId == CurrentUserId, cancellationToken);
        var raceEvent = new RaceDayEvent
        {
            Organiser = user,
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate,
            StartTime = request.StartTime,
            Location = request.Location.Trim(),
            Province = request.Province.Trim(),
            EventType = request.EventType,
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(raceEvent);
        await db.SaveChangesAsync(cancellationToken);
        var dto = await EventProjection().SingleAsync(e => e.EventId == raceEvent.EventId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { eventId = raceEvent.EventId }, dto);
    }

    /// <summary>Updates an event owned by the authenticated organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPut("{eventId:int}")]
    [SwaggerOperation(Summary = "Update an event", Description = "Updates an event owned by the current organiser. Valid statuses are Scheduled, Completed, and Cancelled.")]
    [ProducesResponseType<EventDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Update(int eventId, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        if (request.EventDate == default)
        {
            return BadRequest(new ApiError("EventDate is required."));
        }
        if (!ValidEventType(request.EventType))
        {
            return BadRequest(new ApiError("EventType must be Running, Walking, or Cycling."));
        }
        if (request.Status is not ("Scheduled" or "Completed" or "Cancelled"))
        {
            return BadRequest(new ApiError("Status must be Scheduled, Completed, or Cancelled."));
        }

        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.OrganiserId != CurrentUserId) return Forbid();

        raceEvent.EventName = request.EventName.Trim();
        raceEvent.Description = request.Description.Trim();
        raceEvent.EventDate = request.EventDate;
        raceEvent.StartTime = request.StartTime;
        raceEvent.Location = request.Location.Trim();
        raceEvent.Province = request.Province.Trim();
        raceEvent.EventType = request.EventType;
        raceEvent.DistanceKm = request.DistanceKm;
        raceEvent.MaxParticipants = request.MaxParticipants;
        raceEvent.Status = request.Status;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await EventProjection().SingleAsync(e => e.EventId == eventId, cancellationToken));
    }

    /// <summary>Deletes an event with no entries, or cancels it when active entries exist.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpDelete("{eventId:int}")]
    [SwaggerOperation(Summary = "Delete or cancel an event", Description = "Hard-deletes an event with no enrolments. If entries exist, marks it Cancelled to preserve participant and result history.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.OrganiserId != CurrentUserId) return Forbid();

        var hasResults = await db.Results.AnyAsync(result => result.Enrolment.EventId == eventId, cancellationToken);
        if (hasResults)
        {
            return Conflict(new ApiError("This event has published results and must be retained."));
        }

        if (await db.Enrolments.AnyAsync(enrolment => enrolment.EventId == eventId, cancellationToken))
        {
            raceEvent.Status = "Cancelled";
            await db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        var categories = await db.Categories.Where(category => category.EventId == eventId).ToListAsync(cancellationToken);
        var route = await db.Routes.SingleOrDefaultAsync(existing => existing.EventId == eventId, cancellationToken);
        var weather = await db.EventWeather.Where(item => item.EventId == eventId).ToListAsync(cancellationToken);
        db.Categories.RemoveRange(categories);
        if (route is not null) db.Routes.Remove(route);
        db.EventWeather.RemoveRange(weather);
        db.Events.Remove(raceEvent);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static bool ValidEventType(string value) => value is "Running" or "Walking" or "Cycling";

    private IQueryable<EventDto> EventProjection() => db.Events.AsNoTracking()
        .Select(e => new EventDto(
            e.EventId, e.OrganiserId, e.Organiser.FirstName + " " + e.Organiser.LastName,
            e.EventName, e.Description, e.EventDate, e.StartTime, e.Location, e.Province,
            e.EventType, e.DistanceKm, e.MaxParticipants, e.Status,
            e.Categories.Count(), e.Enrolments.Count()));
}
