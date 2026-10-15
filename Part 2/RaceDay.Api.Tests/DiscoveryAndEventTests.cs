using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public sealed class DiscoveryAndEventTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task SwaggerAndPublicEventDiscoveryAreAvailable()
    {
        using var client = factory.CreateClient();
        var swagger = await client.GetAsync("/api/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);

        var events = await client.GetFromJsonAsync<EventDto[]>("/api/events?eventType=Running");
        Assert.NotNull(events);
        Assert.NotEmpty(events);
        Assert.All(events, raceEvent => Assert.Equal("Running", raceEvent.EventType));
    }

    [Fact]
    public async Task PublicCategoryRouteAndWeatherEndpointsReturnSeededData()
    {
        using var client = factory.CreateClient();
        var categories = await client.GetFromJsonAsync<CategoryDto[]>("/api/events/1/categories");
        Assert.NotNull(categories);
        Assert.NotEmpty(categories);

        var route = await client.GetFromJsonAsync<RouteDto>("/api/events/1/route");
        Assert.NotNull(route);
        Assert.Equal(1, route.EventId);

        var weather = await client.GetFromJsonAsync<WeatherDto[]>("/api/events/1/weather");
        Assert.NotNull(weather);
        Assert.NotEmpty(weather);
    }

    [Fact]
    public async Task EventDeletePreservesEntriesByCancellingInstead()
    {
        using var client = factory.CreateClient();
        var organiserToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, organiserToken);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(client, eventId);

        var participantToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.ParticipantEmail);
        ApiTestHelpers.UseBearer(client, participantToken);
        var entry = await client.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new CreateEnrolmentRequest
        {
            CategoryId = categoryId,
            EmergencyContactName = "Test Contact",
            EmergencyContactPhone = "+27 82 111 1111"
        });
        Assert.Equal(HttpStatusCode.Created, entry.StatusCode);

        ApiTestHelpers.UseBearer(client, organiserToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/events/{eventId}")).StatusCode);
        var cancelled = await client.GetFromJsonAsync<EventDto>($"/api/events/{eventId}");
        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled.Status);
    }
}
