using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(RaceDayDbContext db) : ControllerBase
{
    /// <summary>Returns the profile belonging to the current authenticated user.</summary>
    [HttpGet("me")]
    [SwaggerOperation(Summary = "Get my profile", Description = "Returns the current user's profile without returning a password hash.")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == CurrentUserId, cancellationToken);
        return user is null ? NotFound(new ApiError("User account was not found.")) : Ok(MapProfile(user));
    }

    /// <summary>Updates the current user's name and phone number without allowing a role change.</summary>
    [HttpPut("me")]
    [SwaggerOperation(Summary = "Update my profile", Description = "Updates editable fields on the current user's account. The role and email cannot be changed here.")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.UserId == CurrentUserId, cancellationToken);
        if (user is null)
        {
            return NotFound(new ApiError("User account was not found."));
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(MapProfile(user));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static UserProfileDto MapProfile(RaceDay.Api.Models.User user) => new(
        user.UserId, user.FirstName, user.LastName, user.Email, user.Role, user.PhoneNumber, user.CreatedAt);
}
