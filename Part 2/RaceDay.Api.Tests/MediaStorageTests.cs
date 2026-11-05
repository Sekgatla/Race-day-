using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public sealed class MediaStorageTests
{
    private static readonly byte[] PngHeader =
    [
        137, 80, 78, 71, 13, 10, 26, 10,
        0, 0, 0, 0
    ];

    [Fact]
    public async Task OrganiserCanUploadAndRetrieveAnEventBanner()
    {
        await using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();
        var token = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, token);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);

        using var upload = CreateImageUpload(PngHeader, "banner.png");
        var uploadResponse = await client.PutAsync($"/api/events/{eventId}/banner", upload);

        Assert.Equal(HttpStatusCode.NoContent, uploadResponse.StatusCode);
        var imageResponse = await client.GetAsync($"/api/events/{eventId}/banner");
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.Equal("image/png", imageResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngHeader, await imageResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ProfileImageRequiresLoginAndIsReturnedByTheApi()
    {
        await using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();

        using (var unauthenticatedUpload = CreateImageUpload(PngHeader, "profile.png"))
        {
            var unauthenticatedResponse = await client.PutAsync(
                "/api/users/me/profile-image",
                unauthenticatedUpload);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);
        }

        var token = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.ParticipantEmail);
        ApiTestHelpers.UseBearer(client, token);
        using var upload = CreateImageUpload(PngHeader, "profile.png");
        var uploadResponse = await client.PutAsync("/api/users/me/profile-image", upload);

        Assert.Equal(HttpStatusCode.NoContent, uploadResponse.StatusCode);
        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        Assert.NotNull(profile?.ProfileImageUrl);

        var imageResponse = await client.GetAsync("/api/users/me/profile-image");
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.Equal(PngHeader, await imageResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ParticipantCannotUploadAnEventBanner()
    {
        await using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();
        var organiserToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, organiserToken);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);

        var participantToken = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.ParticipantEmail);
        ApiTestHelpers.UseBearer(client, participantToken);
        using var upload = CreateImageUpload(PngHeader, "banner.png");
        var response = await client.PutAsync($"/api/events/{eventId}/banner", upload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadRejectsContentThatIsNotAnImage()
    {
        await using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();
        var token = await ApiTestHelpers.LoginAsync(client, ApiTestHelpers.OrganiserEmail);
        ApiTestHelpers.UseBearer(client, token);
        var eventId = await ApiTestHelpers.CreateEventAsync(client);

        using var upload = CreateImageUpload("not an image"u8.ToArray(), "banner.png");
        var response = await client.PutAsync($"/api/events/{eventId}/banner", upload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static MultipartFormDataContent CreateImageUpload(byte[] bytes, string fileName)
    {
        var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(file, "file", fileName);
        return multipart;
    }
}
