using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public RaceDayApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);
    }

    public Task<ApiResult<T>> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Post, path, body, cancellationToken);
    }

    public Task<ApiResult<T>> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Put, path, body, cancellationToken);
    }

    public Task<ApiResult<T>> DeleteAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Delete, path, null, cancellationToken);
    }

    public async Task<ApiResult<T>> PostFileAsync<T>(
        string path,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        var contentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var parsedContentType)
            ? parsedContentType
            : new MediaTypeHeaderValue("application/octet-stream");
        fileContent.Headers.ContentType = contentType;
        form.Add(fileContent, "file", Path.GetFileName(file.FileName));

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = form
        };

        return await SendRequestAsync<T>(request, cancellationToken);
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return await SendRequestAsync<T>(request, cancellationToken);
    }

    private async Task<ApiResult<T>> SendRequestAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            var body = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);

            T? data = default;
            if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(body))
            {
                data = JsonSerializer.Deserialize<T>(body, JsonOptions);
            }

            return new ApiResult<T>
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Data = data,
                Message = response.IsSuccessStatusCode ? null : ReadErrorMessage(body, response.StatusCode)
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.RequestTimeout,
                Message = "The RaceDay API did not respond in time. Check that the API is running and try again."
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Message = "The RaceDay API is unavailable. Start the API and check ApiSettings:BaseUrl."
            };
        }
        catch (JsonException)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                StatusCode = HttpStatusCode.BadGateway,
                Message = "The RaceDay API returned a response the website could not read."
            };
        }
    }

    private static string ReadErrorMessage(string body, HttpStatusCode statusCode)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString() ?? "The request could not be completed.";
                }

                if (root.TryGetProperty("title", out var title) &&
                    title.ValueKind == JsonValueKind.String)
                {
                    return title.GetString() ?? "The request could not be completed.";
                }

                if (root.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Object)
                {
                    var firstError = errors.EnumerateObject()
                        .SelectMany(property => property.Value.ValueKind == JsonValueKind.Array
                            ? property.Value.EnumerateArray().Select(item => item.GetString())
                            : Enumerable.Empty<string?>())
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                    if (firstError is not null)
                    {
                        return firstError;
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "Please sign in to continue.",
            HttpStatusCode.Forbidden => "Your account does not have permission to complete this action.",
            HttpStatusCode.NotFound => "The requested RaceDay record could not be found.",
            HttpStatusCode.Conflict => "This action conflicts with existing RaceDay records.",
            HttpStatusCode.BadRequest => "Check the information entered and try again.",
            _ => "The request could not be completed. Please try again."
        };
    }
}
