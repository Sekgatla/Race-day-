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
[Route("api")]
public sealed class EnrolmentsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Enrols the current participant in an event and records their selected category.</summary>
    [Authorize(Roles = RaceDayRoles.Participant)]
    [HttpPost("events/{eventId:int}/enrolments")]
    [SwaggerOperation(Summary = "Enter an event", Description = "Creates one participant enrolment for the event, including category and emergency contact information.")]
    [ProducesResponseType<EnrolmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Create(int eventId, CreateEnrolmentRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.Status != "Scheduled" || raceEvent.EventDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return Conflict(new ApiError("This event is no longer accepting enrolments."));
        if (await db.Enrolments.AnyAsync(enrolment =>
                enrolment.EventId == eventId && enrolment.ParticipantId == CurrentUserId, cancellationToken))
            return Conflict(new ApiError("You are already enrolled in this event."));

        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.CategoryId == request.CategoryId && item.EventId == eventId, cancellationToken);
        if (category is null) return NotFound(new ApiError("The selected category does not belong to this event."));

        var activeEventEntries = await db.Enrolments.CountAsync(enrolment =>
            enrolment.EventId == eventId && enrolment.EnrolmentStatus != "Withdrawn", cancellationToken);
        if (activeEventEntries >= raceEvent.MaxParticipants)
            return Conflict(new ApiError("The event is full."));
        if (category.MaxEntries.HasValue)
        {
            var activeCategoryEntries = await db.Enrolments.CountAsync(enrolment =>
                enrolment.EventId == eventId && enrolment.CategoryId == category.CategoryId &&
                enrolment.EnrolmentStatus != "Withdrawn", cancellationToken);
            if (activeCategoryEntries >= category.MaxEntries.Value)
                return Conflict(new ApiError("The selected category is full."));
        }

        var largestBib = await db.Enrolments.Where(enrolment => enrolment.EventId == eventId && enrolment.BibNumber != null)
            .Select(enrolment => (int?)enrolment.BibNumber).MaxAsync(cancellationToken);
        var enrolment = new Enrolment
        {
            EventId = eventId,
            ParticipantId = CurrentUserId,
            CategoryId = category.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            EmergencyContactName = request.EmergencyContactName.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone.Trim(),
            EnrolmentStatus = "Confirmed",
            BibNumber = (largestBib ?? 0) + 1
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync(cancellationToken);
        var saved = await LoadEnrolment(enrolment.EnrolmentId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { enrolmentId = enrolment.EnrolmentId }, Map(saved!));
    }

    /// <summary>Lists enrolments belonging to the current participant.</summary>
    [Authorize(Roles = RaceDayRoles.Participant)]
    [HttpGet("enrolments/me")]
    [SwaggerOperation(Summary = "List my enrolments", Description = "Returns the current participant's enrolments. An optional status filter may be supplied.")]
    [ProducesResponseType<IReadOnlyList<EnrolmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<EnrolmentDto>>> Mine([FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(status) && status is not ("Pending" or "Confirmed" or "Withdrawn"))
            return BadRequest(new ApiError("Status must be Pending, Confirmed, or Withdrawn."));

        var query = db.Enrolments.AsNoTracking().Where(enrolment => enrolment.ParticipantId == CurrentUserId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(enrolment => enrolment.EnrolmentStatus == status);
        return Ok(await query.Include(enrolment => enrolment.Event).Include(enrolment => enrolment.Category)
            .Include(enrolment => enrolment.Participant).OrderBy(enrolment => enrolment.Event.EventDate)
            .Select(MapProjection()).ToListAsync(cancellationToken));
    }

    /// <summary>Returns an enrolment to its participant owner or the organiser who owns its event.</summary>
    [Authorize]
    [HttpGet("enrolments/{enrolmentId:int}")]
    [SwaggerOperation(Summary = "Get an enrolment", Description = "Returns one enrolment only to its participant owner or the organiser responsible for the event.")]
    [ProducesResponseType<EnrolmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentDto>> GetById(int enrolmentId, CancellationToken cancellationToken)
    {
        var enrolment = await LoadEnrolment(enrolmentId, cancellationToken);
        if (enrolment is null) return NotFound(new ApiError("Enrolment was not found."));
        if (enrolment.ParticipantId != CurrentUserId &&
            (enrolment.Event.OrganiserId != CurrentUserId || !User.IsInRole(RaceDayRoles.Organiser)))
            return Forbid();
        return Ok(Map(enrolment));
    }

    /// <summary>Lists all enrolments for an event owned by the current organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpGet("events/{eventId:int}/enrolments")]
    [SwaggerOperation(Summary = "List event enrolments", Description = "Returns participant enrolments for an event owned by the organiser, optionally filtered by status or categoryId.")]
    [ProducesResponseType<IReadOnlyList<EnrolmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EnrolmentDto>>> ForEvent(
        int eventId, [FromQuery] string? status, [FromQuery] int? categoryId, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.OrganiserId != CurrentUserId) return Forbid();
        if (!string.IsNullOrWhiteSpace(status) && status is not ("Pending" or "Confirmed" or "Withdrawn"))
            return BadRequest(new ApiError("Status must be Pending, Confirmed, or Withdrawn."));

        var query = db.Enrolments.AsNoTracking().Where(enrolment => enrolment.EventId == eventId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(enrolment => enrolment.EnrolmentStatus == status);
        if (categoryId.HasValue) query = query.Where(enrolment => enrolment.CategoryId == categoryId.Value);
        return Ok(await query.Include(enrolment => enrolment.Event).Include(enrolment => enrolment.Category)
            .Include(enrolment => enrolment.Participant).OrderBy(enrolment => enrolment.EnrolmentDate)
            .Select(MapProjection()).ToListAsync(cancellationToken));
    }

    /// <summary>Changes the current participant's category or emergency contact before event day.</summary>
    [Authorize(Roles = RaceDayRoles.Participant)]
    [HttpPut("enrolments/{enrolmentId:int}")]
    [SwaggerOperation(Summary = "Update my enrolment", Description = "Allows a participant to change their selected category or emergency contact before the event closes enrolment.")]
    [ProducesResponseType<EnrolmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Update(int enrolmentId, UpdateEnrolmentRequest request, CancellationToken cancellationToken)
    {
        var enrolment = await db.Enrolments.Include(item => item.Event)
            .SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId, cancellationToken);
        if (enrolment is null) return NotFound(new ApiError("Enrolment was not found."));
        if (enrolment.ParticipantId != CurrentUserId) return Forbid();
        if (!AcceptingChanges(enrolment.Event))
            return Conflict(new ApiError("Enrolment changes are closed for this event."));

        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.CategoryId == request.CategoryId && item.EventId == enrolment.EventId, cancellationToken);
        if (category is null) return NotFound(new ApiError("The selected category does not belong to this event."));
        if (category.CategoryId != enrolment.CategoryId && category.MaxEntries.HasValue)
        {
            var count = await db.Enrolments.CountAsync(item =>
                item.EventId == enrolment.EventId && item.CategoryId == category.CategoryId &&
                item.EnrolmentStatus != "Withdrawn", cancellationToken);
            if (count >= category.MaxEntries.Value)
                return Conflict(new ApiError("The selected category is full."));
        }

        enrolment.CategoryId = category.CategoryId;
        enrolment.EmergencyContactName = request.EmergencyContactName.Trim();
        enrolment.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map((await LoadEnrolment(enrolmentId, cancellationToken))!));
    }

    /// <summary>Withdraws the current participant's enrolment before event day.</summary>
    [Authorize(Roles = RaceDayRoles.Participant)]
    [HttpDelete("enrolments/{enrolmentId:int}")]
    [SwaggerOperation(Summary = "Withdraw my enrolment", Description = "Marks the participant's own enrolment Withdrawn before the event begins; history is retained.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(int enrolmentId, CancellationToken cancellationToken)
    {
        var enrolment = await db.Enrolments.Include(item => item.Event)
            .SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId, cancellationToken);
        if (enrolment is null) return NotFound(new ApiError("Enrolment was not found."));
        if (enrolment.ParticipantId != CurrentUserId) return Forbid();
        if (!AcceptingChanges(enrolment.Event))
            return Conflict(new ApiError("The withdrawal deadline has passed."));

        enrolment.EnrolmentStatus = "Withdrawn";
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Enrolment?> LoadEnrolment(int enrolmentId, CancellationToken cancellationToken) =>
        await db.Enrolments.AsNoTracking().Include(item => item.Event).Include(item => item.Category)
            .Include(item => item.Participant).SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId, cancellationToken);

    private static bool AcceptingChanges(RaceDayEvent raceEvent) =>
        raceEvent.Status == "Scheduled" && raceEvent.EventDate >= DateOnly.FromDateTime(DateTime.UtcNow);

    private static EnrolmentDto Map(Enrolment enrolment) => new(
        enrolment.EnrolmentId, enrolment.EventId, enrolment.Event.EventName,
        enrolment.ParticipantId, $"{enrolment.Participant.FirstName} {enrolment.Participant.LastName}",
        enrolment.Participant.Email, enrolment.CategoryId, enrolment.Category.CategoryName,
        enrolment.EnrolmentDate, enrolment.EmergencyContactName, enrolment.EmergencyContactPhone,
        enrolment.EnrolmentStatus, enrolment.BibNumber);

    private static System.Linq.Expressions.Expression<Func<Enrolment, EnrolmentDto>> MapProjection() => enrolment =>
        new EnrolmentDto(enrolment.EnrolmentId, enrolment.EventId, enrolment.Event.EventName,
            enrolment.ParticipantId, enrolment.Participant.FirstName + " " + enrolment.Participant.LastName,
            enrolment.Participant.Email, enrolment.CategoryId, enrolment.Category.CategoryName,
            enrolment.EnrolmentDate, enrolment.EmergencyContactName, enrolment.EmergencyContactPhone,
            enrolment.EnrolmentStatus, enrolment.BibNumber);
}
