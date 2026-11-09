using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RaceDay.Tests.TestSupport;

// =======================================================
// START: TEST HELPERS
// Shared builders used by the API tests.
// =======================================================

public sealed record TestUser(HttpClient Client, int UserId, string Email, string Role);

public sealed record LoginUser(int UserId, string Role);

public sealed record LoginResponse(string Token, LoginUser User);

public sealed record EventResponse(int EventId, int OrganiserId, string EventName, string Status);

public sealed record CategoryResponse(int CategoryId, string CategoryName);

public sealed record EnrolmentResponse(
    int EnrolmentId,
    int EventId,
    int ParticipantId,
    string ParticipantName,
    string CategoryName,
    string Status,
    string PaymentStatus,
    int? ResultId);

public sealed record ResultResponse(
    int ResultId,
    int EnrolmentId,
    int ParticipantId,
    int? Position,
    int TotalFinishers,
    string ResultStatus,
    TimeSpan? FinishTime);

public sealed record ProfileResponse(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? PhoneNumber,
    string? ProfilePictureUrl);

public sealed record UploadResponse(string Url);

public static class TestHelpers
{
    public const string Password = "Password123!";

    // The first bytes of a real PNG file (signature + padding) so the upload validator accepts it.
    public static readonly byte[] PngBytes =
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01
    };

    public static string UniqueEmail(string prefix) =>
        $"{prefix}.{Guid.NewGuid():N}@raceday.test";

    public static string OrganiserCode(RaceDayApiFactory factory) =>
        factory.Services.GetRequiredService<IConfiguration>()["Registration:OrganiserCode"]
        ?? throw new InvalidOperationException("The organiser code is not configured.");

    // Registers and logs in a user, returning a client that sends the JWT.
    public static async Task<TestUser> CreateUserAsync(RaceDayApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail(role.ToLowerInvariant());
        var code = role == "Organiser" ? OrganiserCode(factory) : null;

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email,
            password = Password,
            role,
            organiserCode = code
        });

        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = Password
        });

        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("The login response was empty.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.Token);

        return new TestUser(client, body.User.UserId, email, role);
    }

    public static async Task<EventResponse> CreateEventAsync(
        TestUser organiser,
        string status = "Open",
        int daysAhead = 10,
        int maxParticipants = 100,
        string eventType = "Running")
    {
        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            eventName = "Test Event " + Guid.NewGuid().ToString("N")[..6],
            description = "An event created by the automated tests.",
            eventType,
            eventDate = DateTime.UtcNow.Date.AddDays(daysAhead),
            startTime = "07:00:00",
            location = "Test Park",
            entryFee = 100,
            maxParticipants,
            status
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EventResponse>()
            ?? throw new InvalidOperationException("The event response was empty.");
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(TestUser organiser)
    {
        var response = await organiser.Client.PostAsJsonAsync("/api/categories", new
        {
            categoryName = "Category " + Guid.NewGuid().ToString("N")[..8],
            description = "A category created by the automated tests.",
            minimumAge = 10
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CategoryResponse>()
            ?? throw new InvalidOperationException("The category response was empty.");
    }

    public static async Task<(EventResponse Event, CategoryResponse Category)> CreateOpenEventWithCategoryAsync(
        TestUser organiser,
        int maxParticipants = 100,
        string status = "Open",
        int daysAhead = 10)
    {
        var raceEvent = await CreateEventAsync(
            organiser,
            status: status,
            daysAhead: daysAhead,
            maxParticipants: maxParticipants);

        var category = await CreateCategoryAsync(organiser);

        var assign = await organiser.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/categories/{category.CategoryId}",
            null);

        assign.EnsureSuccessStatusCode();

        return (raceEvent, category);
    }

    public static async Task<EnrolmentResponse> EnrolAsync(
        TestUser participant,
        int eventId,
        int categoryId)
    {
        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId,
            categoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<EnrolmentResponse>()
            ?? throw new InvalidOperationException("The enrolment response was empty.");
    }

    public static Task<HttpResponseMessage> PatchJsonAsync(
        HttpClient client,
        string url,
        object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };

        return client.SendAsync(request);
    }

    public static MultipartFormDataContent ImageForm(
        byte[] bytes,
        string fileName,
        string contentType)
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);

        return form;
    }
}

// =======================================================
// END: TEST HELPERS
// =======================================================
