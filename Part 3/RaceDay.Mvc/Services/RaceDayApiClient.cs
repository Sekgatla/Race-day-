using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, path, body, cancellationToken);

    public Task<ApiResult<T>> PutAsync<T>(string path, object? body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, path, body, cancellationToken);

    public Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Delete, path, null, cancellationToken);

    public async Task<ApiResult<T>> PutFileAsync<T>(
        string path,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
        content.Add(fileContent, "file", Path.GetFileName(file.FileName));
        using var request = new HttpRequestMessage(HttpMethod.Put, path) { Content = content };
        AddBearerToken(request);
        return await SendRequestAsync<T>(request, cancellationToken);
    }

    public async Task<ApiFileResult?> GetFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddBearerToken(request);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new ApiFileResult(bytes, contentType);
    }

    public async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        AddBearerToken(request);
        return await SendRequestAsync<T>(request, cancellationToken);
    }

    private async Task<ApiResult<T>> SendRequestAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return new ApiResult<T>(true, default, null, response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResult<T>(false, default, ExtractError(errorBody), response.StatusCode);
            }

            if (response.Content.Headers.ContentLength == 0)
                return new ApiResult<T>(true, default, null, response.StatusCode);
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return new ApiResult<T>(true, data, null, response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>(
                false,
                default,
                "RaceDay could not reach the API. Check that the API is running and its address is configured.",
                HttpStatusCode.ServiceUnavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiResult<T>(
                false,
                default,
                "The API took too long to respond. Please try again.",
                HttpStatusCode.RequestTimeout);
        }
        catch (JsonException)
        {
            return new ApiResult<T>(
                false,
                default,
                "The API returned an unreadable response. Please try again later.",
                HttpStatusCode.BadGateway);
        }
    }

    private void AddBearerToken(HttpRequestMessage request)
    {
        var token = httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.ApiToken);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static string ExtractError(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
                return message.GetString() ?? "The request could not be completed.";
            if (json.RootElement.TryGetProperty("title", out var title) &&
                title.ValueKind == JsonValueKind.String)
                return title.GetString() ?? "The request could not be completed.";
        }
        catch (JsonException)
        {
            // Keep API internals out of the page; show a safe, actionable message instead.
        }
        return "The request could not be completed. Check the information and try again.";
    }
}
