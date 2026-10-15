using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public sealed class EnrolmentAndResultTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task ParticipantEnrolsAndEnrolmentIsVisibleToEventOrganiser()
    {
        using var client = factory.CreateClient();
        var organiserToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, organiserToken);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(client, eventId);

        var participantToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.ParticipantEmail);
        ApiTestHelpers.UseBearer(client, participantToken);
        var entryResponse = await client.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new CreateEnrolmentRequest
        {
            CategoryId = categoryId,
            EmergencyContactName = "Thabo Dlamini",
            EmergencyContactPhone = "+27 82 555 0191"
        });
        Assert.Equal(HttpStatusCode.Created, entryResponse.StatusCode);
        var enrolment = await entryResponse.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.NotNull(enrolment);
        Assert.Equal(categoryId, enrolment.CategoryId);
        Assert.Equal("Confirmed", enrolment.EnrolmentStatus);

        var duplicate = await client.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new CreateEnrolmentRequest
        {
            CategoryId = categoryId,
            EmergencyContactName = "Thabo Dlamini",
            EmergencyContactPhone = "+27 82 555 0191"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        ApiTestHelpers.UseBearer(client, organiserToken);
        var organiserView = await client.GetFromJsonAsync<EnrolmentDto[]>($"/api/events/{eventId}/enrolments");
        Assert.NotNull(organiserView);
        Assert.Contains(organiserView, item => item.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task OrganiserRecordsResultAndParticipantCanSeePersonalHistory()
    {
        using var client = factory.CreateClient();
        var organiserToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, organiserToken);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);
        var categoryId = await ApiTestHelpers.CreateCategoryAsync(client, eventId);

        var participantToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.ParticipantEmail);
        ApiTestHelpers.UseBearer(client, participantToken);
        var entryResponse = await client.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new CreateEnrolmentRequest
        {
            CategoryId = categoryId,
            EmergencyContactName = "Thabo Dlamini",
            EmergencyContactPhone = "+27 82 555 0191"
        });
        var enrolment = await entryResponse.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.NotNull(enrolment);

        ApiTestHelpers.UseBearer(client, organiserToken);
        var completion = new UpdateEventRequest
        {
            EventName = "Completed test run",
            Description = "A sufficiently detailed completed test event.",
            EventDate = new DateOnly(2027, 2, 12),
            StartTime = new TimeOnly(7, 0),
            Location = "Cape Town",
            Province = "Western Cape",
            EventType = "Running",
            DistanceKm = 10,
            MaxParticipants = 100,
            Status = "Completed"
        };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/events/{eventId}", completion)).StatusCode);

        var resultResponse = await client.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result",
            new CreateResultRequest
            {
                FinishPosition = 3,
                FinishTime = new TimeOnly(0, 56, 12),
                PaceMinPerKm = 5.62m,
                ResultStatus = "Finished",
                Notes = "Clean test result."
            });
        Assert.Equal(HttpStatusCode.Created, resultResponse.StatusCode);

        ApiTestHelpers.UseBearer(client, participantToken);
        var history = await client.GetFromJsonAsync<ResultDto[]>("/api/results/me");
        Assert.NotNull(history);
        Assert.Contains(history, result => result.EventId == eventId && result.FinishPosition == 3);
    }

    [Fact]
    public async Task ParticipantCannotCaptureTheirOwnResult()
    {
        using var client = factory.CreateClient();
        var token = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.ParticipantEmail);
        ApiTestHelpers.UseBearer(client, token);
        var response = await client.PostAsJsonAsync("/api/enrolments/1/result", new CreateResultRequest
        {
            FinishPosition = 1,
            FinishTime = new TimeOnly(1, 0),
            ResultStatus = "Finished"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
