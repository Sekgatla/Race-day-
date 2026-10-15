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
public sealed class ResultsController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Records the result for an enrolment in an event owned by the current organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SwaggerOperation(Summary = "Record an event result", Description = "Captures finish time, position, pace, and result status for an enrolment in a completed event owned by the organiser.")]
    [ProducesResponseType<ResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Create(int enrolmentId, CreateResultRequest request, CancellationToken cancellationToken)
    {
        var enrolment = await db.Enrolments.Include(item => item.Event).Include(item => item.Participant)
            .Include(item => item.Category).SingleOrDefaultAsync(item => item.EnrolmentId == enrolmentId, cancellationToken);
        if (enrolment is null) return NotFound(new ApiError("Enrolment was not found."));
        if (enrolment.Event.OrganiserId != CurrentUserId) return Forbid();
        if (enrolment.Event.Status != "Completed")
            return Conflict(new ApiError("The event must be marked Completed before results are captured."));
        if (enrolment.EnrolmentStatus != "Confirmed")
            return Conflict(new ApiError("A result can only be recorded for a confirmed enrolment."));
        if (await db.Results.AnyAsync(result => result.EnrolmentId == enrolmentId, cancellationToken))
            return Conflict(new ApiError("A result already exists for this enrolment."));
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new ApiError(validationError));

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishPosition = request.ResultStatus == "Finished" ? request.FinishPosition : null,
            FinishTime = request.ResultStatus == "Finished" ? request.FinishTime : null,
            PaceMinPerKm = request.ResultStatus == "Finished" ? request.PaceMinPerKm : null,
            ResultStatus = request.ResultStatus,
            RecordedAt = DateTime.UtcNow,
            Notes = request.Notes?.Trim()
        };
        db.Results.Add(result);
        await db.SaveChangesAsync(cancellationToken);
        result.Enrolment = enrolment;
        return CreatedAtAction(nameof(Get), new { resultId = result.ResultId }, Map(result));
    }

    /// <summary>Corrects a result for an event owned by the current organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPut("results/{resultId:int}")]
    [SwaggerOperation(Summary = "Update a result", Description = "Corrects result fields only when the current organiser owns the related event.")]
    [ProducesResponseType<ResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int resultId, UpdateResultRequest request, CancellationToken cancellationToken)
    {
        var result = await LoadResult(resultId, cancellationToken);
        if (result is null) return NotFound(new ApiError("Result was not found."));
        if (result.Enrolment.Event.OrganiserId != CurrentUserId) return Forbid();
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new ApiError(validationError));

        result.FinishPosition = request.ResultStatus == "Finished" ? request.FinishPosition : null;
        result.FinishTime = request.ResultStatus == "Finished" ? request.FinishTime : null;
        result.PaceMinPerKm = request.ResultStatus == "Finished" ? request.PaceMinPerKm : null;
        result.ResultStatus = request.ResultStatus;
        result.Notes = request.Notes?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(result));
    }

    /// <summary>Lists results belonging to the current participant, with optional date and event-type filters.</summary>
    [Authorize(Roles = RaceDayRoles.Participant)]
    [HttpGet("results/me")]
    [SwaggerOperation(Summary = "View my results", Description = "Returns the participant's personal result history, optionally filtered by fromDate, toDate, or eventType.")]
    [ProducesResponseType<IReadOnlyList<ResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ResultDto>>> Mine(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] string? eventType, CancellationToken cancellationToken)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            return BadRequest(new ApiError("fromDate must be on or before toDate."));
        if (!string.IsNullOrWhiteSpace(eventType) && eventType is not ("Running" or "Walking" or "Cycling"))
            return BadRequest(new ApiError("eventType must be Running, Walking, or Cycling."));

        var query = ResultQuery().Where(result => result.Enrolment.ParticipantId == CurrentUserId);
        if (fromDate.HasValue) query = query.Where(result => result.Enrolment.Event.EventDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(result => result.Enrolment.Event.EventDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(result => result.Enrolment.Event.EventType == eventType);
        return Ok(await query.OrderByDescending(result => result.Enrolment.Event.EventDate)
            .Select(MapProjection()).ToListAsync(cancellationToken));
    }

    /// <summary>Returns the public leaderboard for a completed event.</summary>
    [AllowAnonymous]
    [HttpGet("events/{eventId:int}/results")]
    [SwaggerOperation(Summary = "Get event leaderboard", Description = "Returns a completed event's published results, optionally filtered by categoryId.")]
    [ProducesResponseType<IReadOnlyList<ResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<ResultDto>>> ForEvent(
        int eventId, [FromQuery] int? categoryId, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.Status != "Completed")
            return Conflict(new ApiError("Results are not published until the event is marked Completed."));

        var query = ResultQuery().Where(result => result.Enrolment.EventId == eventId);
        if (categoryId.HasValue) query = query.Where(result => result.Enrolment.CategoryId == categoryId.Value);
        var rows = await query.OrderBy(result => result.FinishPosition ?? int.MaxValue)
            .ThenBy(result => result.FinishTime).Select(MapProjection()).ToListAsync(cancellationToken);
        if (rows.Count == 0) return Conflict(new ApiError("No results have been published for this event."));
        return Ok(rows);
    }

    /// <summary>Returns one result only to its participant owner or the organiser who owns the event.</summary>
    [Authorize]
    [HttpGet("results/{resultId:int}")]
    [SwaggerOperation(Summary = "Get a result", Description = "Returns a result only to its participant owner or the organiser responsible for the event.")]
    [ProducesResponseType<ResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Get(int resultId, CancellationToken cancellationToken)
    {
        var result = await LoadResult(resultId, cancellationToken);
        if (result is null) return NotFound(new ApiError("Result was not found."));
        var isParticipantOwner = result.Enrolment.ParticipantId == CurrentUserId;
        var isOrganiserOwner = User.IsInRole(RaceDayRoles.Organiser) &&
                               result.Enrolment.Event.OrganiserId == CurrentUserId;
        if (!isParticipantOwner && !isOrganiserOwner) return Forbid();
        return Ok(Map(result));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Result> ResultQuery() => db.Results.AsNoTracking()
        .Include(result => result.Enrolment).ThenInclude(enrolment => enrolment.Event)
        .Include(result => result.Enrolment).ThenInclude(enrolment => enrolment.Participant)
        .Include(result => result.Enrolment).ThenInclude(enrolment => enrolment.Category);

    private async Task<Result?> LoadResult(int resultId, CancellationToken cancellationToken) =>
        await db.Results.Include(result => result.Enrolment).ThenInclude(enrolment => enrolment.Event)
            .Include(result => result.Enrolment).ThenInclude(enrolment => enrolment.Participant)
            .Include(result => result.Enrolment).ThenInclude(enrolment => enrolment.Category)
            .SingleOrDefaultAsync(result => result.ResultId == resultId, cancellationToken);

    private static string? Validate(CreateResultRequest request)
    {
        if (request.ResultStatus is not ("Finished" or "DNF" or "DNS"))
            return "ResultStatus must be Finished, DNF, or DNS.";
        if (request.ResultStatus == "Finished" && (!request.FinishPosition.HasValue || !request.FinishTime.HasValue))
            return "Finished results require FinishPosition and FinishTime.";
        if (request.ResultStatus != "Finished" && (request.FinishPosition.HasValue || request.FinishTime.HasValue))
            return "DNF and DNS results must not include a finish position or finish time.";
        return null;
    }

    private static ResultDto Map(Result result) => new(
        result.ResultId, result.EnrolmentId, result.Enrolment.EventId,
        result.Enrolment.Event.EventName, result.Enrolment.Event.EventDate,
        result.Enrolment.ParticipantId,
        $"{result.Enrolment.Participant.FirstName} {result.Enrolment.Participant.LastName}",
        result.Enrolment.CategoryId, result.Enrolment.Category.CategoryName,
        result.FinishPosition, result.FinishTime, result.PaceMinPerKm,
        result.ResultStatus, result.RecordedAt, result.Notes);

    private static System.Linq.Expressions.Expression<Func<Result, ResultDto>> MapProjection() => result =>
        new ResultDto(result.ResultId, result.EnrolmentId, result.Enrolment.EventId,
            result.Enrolment.Event.EventName, result.Enrolment.Event.EventDate,
            result.Enrolment.ParticipantId,
            result.Enrolment.Participant.FirstName + " " + result.Enrolment.Participant.LastName,
            result.Enrolment.CategoryId, result.Enrolment.Category.CategoryName,
            result.FinishPosition, result.FinishTime, result.PaceMinPerKm,
            result.ResultStatus, result.RecordedAt, result.Notes);
}
