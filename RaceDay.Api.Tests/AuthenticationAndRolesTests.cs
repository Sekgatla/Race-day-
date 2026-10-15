using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public sealed class AuthenticationAndRolesTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task RegistrationHashesPasswordAndLoginIssuesWorkingSession()
    {
        using var client = factory.CreateClient();
        var email = $"runner-{Guid.NewGuid():N}@example.test";
        const string password = "SecurePass!2026";

        var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Test",
            LastName = "Runner",
            Email = email,
            Password = password,
            Role = "Participant",
            PhoneNumber = "+27 82 123 4567"
        });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var registeredProfile = await registration.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(registeredProfile);
        Assert.Equal("Participant", registeredProfile.Role);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            var stored = await db.Users.SingleAsync(user => user.Email == email);
            Assert.NotEqual(password, stored.PasswordHash);
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<User>().VerifyHashedPassword(stored, stored.PasswordHash, password));
        }

        var token = await ApiTestHelpers.LoginAsync(client, email, password);
        ApiTestHelpers.UseBearer(client, token);
        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        Assert.NotNull(profile);
        Assert.Equal(email, profile.Email);
        Assert.Equal("Participant", profile.Role);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task InvalidCredentialsAndDuplicateEmailAreRejected()
    {
        using var client = factory.CreateClient();
        var badLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = ApiTestHelpers.OrganiserEmail,
            Password = "not-the-demo-password"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);

        var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Duplicate",
            LastName = "Account",
            Email = ApiTestHelpers.OrganiserEmail,
            Password = "AnotherSafePass!2026",
            Role = "Participant"
        });
        Assert.Equal(HttpStatusCode.Conflict, registration.StatusCode);
    }

    [Fact]
    public async Task ProtectedProfileRequiresAuthentication()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Theory]
    [InlineData("lerato.mokoena@raceday.example", "Organiser")]
    [InlineData("ayanda.dlamini@raceday.example", "Participant")]
    public async Task BothRolesCanUpdateOnlyTheirOwnProfile(string email, string expectedRole)
    {
        using var client = factory.CreateClient();
        var token = await ApiTestHelpers.LoginAsync(client, email);
        ApiTestHelpers.UseBearer(client, token);

        var update = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest
        {
            FirstName = "Updated",
            LastName = "RaceDay",
            PhoneNumber = "+27 82 555 0101"
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var profile = await update.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("Updated", profile.FirstName);
        Assert.Equal(email, profile.Email);
        Assert.Equal(expectedRole, profile.Role);
    }

    [Fact]
    public async Task RegistrationRejectsUnknownRole()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Test",
            LastName = "Runner",
            Email = $"invalid-role-{Guid.NewGuid():N}@example.test",
            Password = "AValidPassword!2026",
            Role = "Administrator"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotCreateAnEventButOrganiserCan()
    {
        using var client = factory.CreateClient();
        var participantToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.ParticipantEmail);
        ApiTestHelpers.UseBearer(client, participantToken);
        var participantAttempt = await client.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            EventName = "Forbidden participant event",
            Description = "A sufficiently detailed test event description.",
            EventDate = new DateOnly(2027, 2, 12),
            StartTime = new TimeOnly(7, 0),
            Location = "Cape Town",
            Province = "Western Cape",
            EventType = "Running",
            DistanceKm = 10,
            MaxParticipants = 100
        });
        Assert.Equal(HttpStatusCode.Forbidden, participantAttempt.StatusCode);

        var organiserToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, organiserToken);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);
        Assert.True(eventId > 0);
    }

    [Fact]
    public async Task OrganiserCannotUseParticipantOnlyEnrolmentHistory()
    {
        using var client = factory.CreateClient();
        var token = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, token);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/enrolments/me")).StatusCode);
    }

    [Fact]
    public async Task OrganiserCannotModifyAnotherOrganisersEvent()
    {
        using var client = factory.CreateClient();
        var organiserToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, organiserToken);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);

        var otherOrganiserToken = await ApiTestHelpers.LoginAsync(client, "james.naidoo@raceday.example");
        ApiTestHelpers.UseBearer(client, otherOrganiserToken);
        var update = await client.PutAsJsonAsync($"/api/events/{eventId}", new UpdateEventRequest
        {
            EventName = "Attempted takeover",
            Description = "An attempted change to an event owned by someone else.",
            EventDate = new DateOnly(2027, 2, 12),
            StartTime = new TimeOnly(7, 0),
            Location = "Cape Town",
            Province = "Western Cape",
            EventType = "Running",
            DistanceKm = 10,
            MaxParticipants = 100,
            Status = "Scheduled"
        });

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }
}
