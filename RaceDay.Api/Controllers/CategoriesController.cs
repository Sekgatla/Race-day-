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
public sealed class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Lists categories for one public event.</summary>
    [AllowAnonymous]
    [HttpGet("api/events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "List event categories", Description = "Returns the categories available for the specified event.")]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(int eventId, CancellationToken cancellationToken)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId, cancellationToken))
            return NotFound(new ApiError("Event was not found."));

        return Ok(await db.Categories.AsNoTracking().Where(category => category.EventId == eventId)
            .OrderBy(category => category.CategoryName).Select(MapProjection()).ToListAsync(cancellationToken));
    }

    /// <summary>Adds a category to an event owned by the authenticated organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPost("api/events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "Create an event category", Description = "Adds a unique age or gender category to an event owned by the current organiser.")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (raceEvent is null) return NotFound(new ApiError("Event was not found."));
        if (raceEvent.OrganiserId != CurrentUserId) return Forbid();
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new ApiError(validationError));
        if (await db.Categories.AnyAsync(category =>
                category.EventId == eventId && category.CategoryName == request.CategoryName.Trim(), cancellationToken))
        {
            return Conflict(new ApiError("A category with this name already exists for the event."));
        }

        var category = ToEntity(eventId, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/events/{eventId}/categories", Map(category));
    }

    /// <summary>Updates a category whose parent event belongs to the authenticated organiser.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpPut("/api/categories/{categoryId:int}")]
    [SwaggerOperation(Summary = "Update a category", Description = "Updates category rules and pricing only when the current organiser owns its event.")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int categoryId, CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.Include(item => item.Event)
            .SingleOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);
        if (category is null) return NotFound(new ApiError("Category was not found."));
        if (category.Event.OrganiserId != CurrentUserId) return Forbid();
        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new ApiError(validationError));
        if (await db.Categories.AnyAsync(item =>
                item.EventId == category.EventId && item.CategoryId != categoryId &&
                item.CategoryName == request.CategoryName.Trim(), cancellationToken))
        {
            return Conflict(new ApiError("A category with this name already exists for the event."));
        }
        if (await db.Enrolments.AnyAsync(enrolment => enrolment.CategoryId == categoryId, cancellationToken) &&
            (category.MinimumAge != request.MinimumAge || category.MaximumAge != request.MaximumAge))
        {
            return Conflict(new ApiError("Age eligibility cannot be changed after participants have enrolled."));
        }

        category.CategoryName = request.CategoryName.Trim();
        category.GenderOption = request.GenderOption;
        category.MinimumAge = request.MinimumAge;
        category.MaximumAge = request.MaximumAge;
        category.EntryFee = request.EntryFee;
        category.MaxEntries = request.MaxEntries;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(Map(category));
    }

    /// <summary>Deletes a category only if no participants have enrolled in it.</summary>
    [Authorize(Roles = RaceDayRoles.Organiser)]
    [HttpDelete("/api/categories/{categoryId:int}")]
    [SwaggerOperation(Summary = "Delete a category", Description = "Removes a category owned by the current organiser when it has no enrolments.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiError>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int categoryId, CancellationToken cancellationToken)
    {
        var category = await db.Categories.Include(item => item.Event)
            .SingleOrDefaultAsync(item => item.CategoryId == categoryId, cancellationToken);
        if (category is null) return NotFound(new ApiError("Category was not found."));
        if (category.Event.OrganiserId != CurrentUserId) return Forbid();
        if (await db.Enrolments.AnyAsync(enrolment => enrolment.CategoryId == categoryId, cancellationToken))
            return Conflict(new ApiError("A category with enrolments cannot be deleted."));

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string? Validate(CreateCategoryRequest request)
    {
        if (request.GenderOption is not ("Open" or "Female" or "Male"))
            return "GenderOption must be Open, Female, or Male.";
        if (request.MaximumAge.HasValue && request.MaximumAge.Value < request.MinimumAge)
            return "MaximumAge must be greater than or equal to MinimumAge.";
        return null;
    }

    private static Category ToEntity(int eventId, CreateCategoryRequest request) => new()
    {
        EventId = eventId,
        CategoryName = request.CategoryName.Trim(),
        GenderOption = request.GenderOption,
        MinimumAge = request.MinimumAge,
        MaximumAge = request.MaximumAge,
        EntryFee = request.EntryFee,
        MaxEntries = request.MaxEntries
    };

    private static CategoryDto Map(Category category) => new(
        category.CategoryId, category.EventId, category.CategoryName, category.GenderOption,
        category.MinimumAge, category.MaximumAge, category.EntryFee, category.MaxEntries);

    private static System.Linq.Expressions.Expression<Func<Category, CategoryDto>> MapProjection() => category =>
        new CategoryDto(category.CategoryId, category.EventId, category.CategoryName, category.GenderOption,
            category.MinimumAge, category.MaximumAge, category.EntryFee, category.MaxEntries);
}
