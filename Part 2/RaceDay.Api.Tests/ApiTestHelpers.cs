using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

internal static class ApiTestHelpers
{
    public static async Task<string> LoginAsync(HttpClient client, string email, string password = DemoPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth.Token;
    }

    public static void UseBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<int> CreateEventAsync(HttpClient client, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            EventName = name ?? $"Test Run {Guid.NewGuid():N}",
            Description = "A sufficiently detailed test event description.",
            EventDate = new DateOnly(2027, 2, 12),
            StartTime = new TimeOnly(7, 0),
            Location = "Green Point, Cape Town",
            Province = "Western Cape",
            EventType = "Running",
            DistanceKm = 10,
            MaxParticipants = 100
        });
        response.EnsureSuccessStatusCode();
        var raceEvent = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.NotNull(raceEvent);
        return raceEvent.EventId;
    }

    public static async Task<int> CreateCategoryAsync(HttpClient client, int eventId, string? name = null)
    {
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories", new CreateCategoryRequest
        {
            CategoryName = name ?? $"Open {Guid.NewGuid():N}",
            GenderOption = "Open",
            MinimumAge = 16,
            EntryFee = 100,
            MaxEntries = 50
        });
        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        return category.CategoryId;
    }

    public const string DemoPassword = "RaceDay!2026";
    public const string OrganiserEmail = "lerato.mokoena@raceday.example";
    public const string ParticipantEmail = "ayanda.dlamini@raceday.example";
}
