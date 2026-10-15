using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    RaceDayDbContext db,
    IPasswordHasher<User> passwordHasher,
    ISessionStore sessions) : ControllerBase
{
    /// <summary>Registers an Organiser or Participant account and stores only a password hash.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register a RaceDay account", Description = "Creates an Organiser or Participant account. Passwords are hashed before storage.")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var role = request.Role.Trim();
        if (role is not (RaceDayRoles.Organiser or RaceDayRoles.Participant))
        {
            return BadRequest(new ApiError("Role must be Organiser or Participant."));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return Conflict(new ApiError("An account with this email address already exists."));
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = role,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var profile = MapProfile(user);
        return Created("/api/users/me", profile);
    }

    /// <summary>Checks credentials, creates a server-side session, and returns its opaque bearer token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Log in", Description = "Verifies the password hash and starts a 12-hour server-side session. The returned token is sent as a Bearer token on later requests.")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ApiError("Email or password is incorrect."));
        }

        var (token, expiresAt) = sessions.Create(user.UserId, user.Role);
        return Ok(new AuthResponse(token, expiresAt, MapProfile(user)));
    }

    /// <summary>Revokes the current server-side session token.</summary>
    [Authorize]
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Log out", Description = "Revokes the bearer session used by this request; subsequent requests with it receive 401.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiError>(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        var token = Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim();
        sessions.Revoke(token);
        return NoContent();
    }

    private static UserProfileDto MapProfile(User user) => new(
        user.UserId, user.FirstName, user.LastName, user.Email, user.Role, user.PhoneNumber, user.CreatedAt);
}
