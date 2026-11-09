using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

// =======================================================
// START: RACEDAY API CLIENT
// Purpose: The ONLY place in the MVC application that talks to
// the RaceDay API (including image uploads, which the API then
// forwards to Azure Blob Storage). Every method returns an
// ApiResult with a friendly error message - never a raw exception.
// =======================================================

public class RaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(HttpClient http, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    // START: AUTH
    public Task<ApiResult<LoginResponseDto>> LoginAsync(string email, string password)
    {
        return SendAsync<LoginResponseDto>(HttpMethod.Post, "api/auth/login", new { email, password });
    }

    public Task<ApiResult<EmptyResponse>> RegisterAsync(RegisterApiRequest request)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Post, "api/auth/register", request);
    }

    public Task<ApiResult<EmptyResponse>> LogoutAsync()
    {
        return SendAsync<EmptyResponse>(HttpMethod.Post, "api/auth/logout");
    }
    // END: AUTH

    // START: PROFILE
    public Task<ApiResult<ProfileDto>> GetProfileAsync()
    {
        return SendAsync<ProfileDto>(HttpMethod.Get, "api/users/me");
    }

    public Task<ApiResult<EmptyResponse>> UpdateProfileAsync(
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime? dateOfBirth)
    {
        var body = new
        {
            firstName,
            lastName,
            phoneNumber,
            dateOfBirth
        };

        return SendAsync<EmptyResponse>(HttpMethod.Put, "api/users/me", body);
    }

    public Task<ApiResult<UploadResultDto>> UploadProfilePictureAsync(IFormFile file)
    {
        return UploadAsync("api/users/me/profile-picture", file);
    }
    // END: PROFILE

    // START: EVENTS
    public Task<ApiResult<List<EventDto>>> GetEventsAsync(EventQuery? query = null)
    {
        var parts = new List<string>();

        if (query != null)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
                parts.Add("search=" + Uri.EscapeDataString(query.Search.Trim()));

            if (!string.IsNullOrWhiteSpace(query.Type))
                parts.Add("type=" + Uri.EscapeDataString(query.Type));

            if (query.FromDate.HasValue)
                parts.Add("fromDate=" + query.FromDate.Value.ToString("yyyy-MM-dd"));

            if (query.ToDate.HasValue)
                parts.Add("toDate=" + query.ToDate.Value.ToString("yyyy-MM-dd"));

            if (query.Upcoming)
                parts.Add("upcoming=true");
        }

        var url = "api/events";

        if (parts.Count > 0)
            url += "?" + string.Join("&", parts);

        return SendAsync<List<EventDto>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<EventDto>> GetEventAsync(int id)
    {
        return SendAsync<EventDto>(HttpMethod.Get, "api/events/" + id);
    }

    public Task<ApiResult<List<EventSummaryDto>>> GetMyEventsAsync()
    {
        return SendAsync<List<EventSummaryDto>>(HttpMethod.Get, "api/events/mine");
    }

    public Task<ApiResult<EventDto>> CreateEventAsync(EventFormData form, string? bannerUrl)
    {
        return SendAsync<EventDto>(HttpMethod.Post, "api/events", BuildEventPayload(0, form, bannerUrl));
    }

    // The API replaces BannerUrl on update, so the current banner must be sent back.
    public Task<ApiResult<EmptyResponse>> UpdateEventAsync(int id, EventFormData form, string? bannerUrl)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Put, "api/events/" + id, BuildEventPayload(id, form, bannerUrl));
    }

    public Task<ApiResult<EmptyResponse>> DeleteEventAsync(int id)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Delete, "api/events/" + id);
    }

    public Task<ApiResult<UploadResultDto>> UploadEventBannerAsync(int id, IFormFile file)
    {
        return UploadAsync("api/events/" + id + "/banner", file);
    }

    public Task<ApiResult<List<CategoryDto>>> GetEventCategoriesAsync(int eventId)
    {
        return SendAsync<List<CategoryDto>>(HttpMethod.Get, "api/events/" + eventId + "/categories");
    }

    public Task<ApiResult<EmptyResponse>> AddCategoryToEventAsync(int eventId, int categoryId)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Post, "api/events/" + eventId + "/categories/" + categoryId);
    }

    public Task<ApiResult<EmptyResponse>> RemoveCategoryFromEventAsync(int eventId, int categoryId)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Delete, "api/events/" + eventId + "/categories/" + categoryId);
    }

    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId)
    {
        return SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/events/" + eventId + "/enrolments");
    }

    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId)
    {
        return SendAsync<List<ResultDto>>(HttpMethod.Get, "api/events/" + eventId + "/results");
    }

    private static object BuildEventPayload(int id, EventFormData form, string? bannerUrl)
    {
        return new
        {
            eventId = id,
            eventName = form.EventName,
            description = form.Description,
            eventType = form.EventType,
            eventDate = form.EventDate.Date,
            startTime = form.StartTime,
            location = form.Location,
            entryFee = form.EntryFee,
            maxParticipants = form.MaxParticipants,
            status = form.Status,
            bannerUrl
        };
    }
    // END: EVENTS

    // START: CATEGORIES
    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync()
    {
        return SendAsync<List<CategoryDto>>(HttpMethod.Get, "api/categories");
    }

    public Task<ApiResult<CategoryDto>> GetCategoryAsync(int id)
    {
        return SendAsync<CategoryDto>(HttpMethod.Get, "api/categories/" + id);
    }

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(CategoryDto category)
    {
        var body = new
        {
            categoryName = category.CategoryName,
            description = category.Description,
            minimumAge = category.MinimumAge,
            maximumAge = category.MaximumAge
        };

        return SendAsync<CategoryDto>(HttpMethod.Post, "api/categories", body);
    }

    public Task<ApiResult<EmptyResponse>> UpdateCategoryAsync(int id, CategoryDto category)
    {
        var body = new
        {
            categoryId = id,
            categoryName = category.CategoryName,
            description = category.Description,
            minimumAge = category.MinimumAge,
            maximumAge = category.MaximumAge
        };

        return SendAsync<EmptyResponse>(HttpMethod.Put, "api/categories/" + id, body);
    }

    public Task<ApiResult<EmptyResponse>> DeleteCategoryAsync(int id)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Delete, "api/categories/" + id);
    }
    // END: CATEGORIES

    // START: ENROLMENTS
    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int eventId, int categoryId)
    {
        return SendAsync<EnrolmentDto>(HttpMethod.Post, "api/enrolments", new { eventId, categoryId });
    }

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync()
    {
        return SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/enrolments/my");
    }

    public Task<ApiResult<EmptyResponse>> CancelEnrolmentAsync(int enrolmentId)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Delete, "api/enrolments/" + enrolmentId);
    }

    public Task<ApiResult<EnrolmentDto>> UpdateEnrolmentStatusAsync(int enrolmentId, string status, string paymentStatus)
    {
        return SendAsync<EnrolmentDto>(
            HttpMethod.Patch,
            "api/enrolments/" + enrolmentId + "/status",
            new { status, paymentStatus });
    }
    // END: ENROLMENTS

    // START: RESULTS
    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync()
    {
        return SendAsync<List<ResultDto>>(HttpMethod.Get, "api/results/my");
    }

    public Task<ApiResult<ResultDto>> CreateResultAsync(
        int enrolmentId,
        string resultStatus,
        TimeSpan? finishTime,
        int? position)
    {
        var body = new
        {
            enrolmentId,
            resultStatus,
            finishTime,
            position
        };

        return SendAsync<ResultDto>(HttpMethod.Post, "api/results", body);
    }

    public Task<ApiResult<EmptyResponse>> UpdateResultAsync(
        int resultId,
        int enrolmentId,
        string resultStatus,
        TimeSpan? finishTime,
        int? position)
    {
        var body = new
        {
            resultId,
            enrolmentId,
            resultStatus,
            finishTime,
            position
        };

        return SendAsync<EmptyResponse>(HttpMethod.Put, "api/results/" + resultId, body);
    }

    public Task<ApiResult<EmptyResponse>> DeleteResultAsync(int resultId)
    {
        return SendAsync<EmptyResponse>(HttpMethod.Delete, "api/results/" + resultId);
    }
    // END: RESULTS

    // START: HTTP HELPERS
    private async Task<ApiResult<UploadResultDto>> UploadAsync(string url, IFormFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

            content.Add(fileContent, "file", Path.GetFileName(file.FileName));

            return await SendAsync<UploadResultDto>(HttpMethod.Post, url, null, content);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not read the uploaded file.");
            return ApiResult<UploadResultDto>.Failure(
                HttpStatusCode.BadRequest,
                "The selected file could not be read. Please try again.");
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body = null,
        HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);

            if (content != null)
            {
                request.Content = content;
            }
            else if (body != null)
            {
                request.Content = JsonContent.Create(body, body.GetType(), null, JsonOptions);
            }

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(text))
                    return ApiResult<T>.Ok(response.StatusCode, default);

                try
                {
                    var data = JsonSerializer.Deserialize<T>(text, JsonOptions);
                    return ApiResult<T>.Ok(response.StatusCode, data);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Unexpected response from the API for {Url}.", url);
                    return ApiResult<T>.Failure(
                        HttpStatusCode.BadGateway,
                        "RaceDay received an unexpected reply. Please try again.");
                }
            }

            var message = await ExtractErrorAsync(response);

            return ApiResult<T>.Failure(response.StatusCode, message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "The API could not be reached for {Url}.", url);
            return ApiResult<T>.Failure(
                HttpStatusCode.ServiceUnavailable,
                "RaceDay is temporarily unavailable. Please try again in a moment.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "The API timed out for {Url}.", url);
            return ApiResult<T>.Failure(
                HttpStatusCode.GatewayTimeout,
                "RaceDay took too long to respond. Please try again.");
        }
    }

    // Turns any error response into a short, readable sentence.
    private static async Task<string> ExtractErrorAsync(HttpResponseMessage response)
    {
        var fallback = FallbackMessage(response.StatusCode);

        string text;

        try
        {
            text = await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return fallback;
        }

        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return fallback;

            var messages = new List<string>();

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                AddIfFriendly(messages, message.GetString());
            }

            if (root.TryGetProperty("errors", out var errors))
            {
                CollectErrors(errors, messages);
            }

            if (messages.Count == 0 &&
                root.TryGetProperty("detail", out var detail) &&
                detail.ValueKind == JsonValueKind.String)
            {
                AddIfFriendly(messages, detail.GetString());
            }

            if (messages.Count == 0)
                return fallback;

            return string.Join(" ", messages.Distinct().Take(4));
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static void CollectErrors(JsonElement errors, List<string> messages)
    {
        if (errors.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in errors.EnumerateObject())
            {
                CollectErrors(property.Value, messages);
            }
        }
        else if (errors.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in errors.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    AddIfFriendly(messages, item.GetString());
            }
        }
        else if (errors.ValueKind == JsonValueKind.String)
        {
            AddIfFriendly(messages, errors.GetString());
        }
    }

    // Technical messages (JSON paths, stack traces) are never shown to users.
    private static void AddIfFriendly(List<string> messages, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var value = text.Trim();

        if (value.Contains("Path:") ||
            value.Contains("LineNumber") ||
            value.Contains("System.") ||
            value.Contains("JSON value") ||
            value.Length > 300)
        {
            return;
        }

        messages.Add(value);
    }

    private static string FallbackMessage(HttpStatusCode status)
    {
        switch (status)
        {
            case HttpStatusCode.BadRequest:
                return "Some of the information provided is not valid. Please check it and try again.";
            case HttpStatusCode.Unauthorized:
                return "Your session has expired or the details are incorrect. Please sign in again.";
            case HttpStatusCode.Forbidden:
                return "You do not have permission to do that.";
            case HttpStatusCode.NotFound:
                return "We could not find what you were looking for.";
            case HttpStatusCode.Conflict:
                return "That action conflicts with existing information.";
            case HttpStatusCode.RequestEntityTooLarge:
                return "The selected file is too large.";
            case HttpStatusCode.ServiceUnavailable:
                return "This feature is temporarily unavailable. Please try again later.";
            default:
                return "Something went wrong on our side. Please try again.";
        }
    }
    // END: HTTP HELPERS
}

// =======================================================
// END: RACEDAY API CLIENT
// =======================================================
