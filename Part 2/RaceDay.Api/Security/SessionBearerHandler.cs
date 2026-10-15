using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Security;

public sealed class SessionBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return AuthenticateResult.NoResult();
        }

        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Use a bearer session token.");
        }

        var token = header["Bearer ".Length..].Trim();
        var sessions = Context.RequestServices.GetRequiredService<ISessionStore>();
        if (!sessions.TryGet(token, out var session) || session is null)
        {
            return AuthenticateResult.Fail("The session is missing, expired, or has been signed out.");
        }

        var db = Context.RequestServices.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == session.UserId, Context.RequestAborted);
        if (user is null || !user.IsActive || user.Role != session.Role)
        {
            sessions.Revoke(token);
            return AuthenticateResult.Fail("The account is not active.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Response.WriteAsJsonAsync(new ApiError("A valid bearer session token is required."));
    }
}
