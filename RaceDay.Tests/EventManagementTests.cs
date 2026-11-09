using System.Net;
using System.Net.Http.Json;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: EVENT MANAGEMENT TESTS
// Verifies that Organisers can manage their own events,
// that other roles are rejected and that validation works.
// =======================================================

public class EventManagementTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EventManagementTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private static object EventBody(
        int? eventId = null,
        string eventName = "Valid Event",
        string eventType = "Running") => new
        {
            eventId = eventId ?? 0,
            eventName,
            description = "A valid description.",
            eventType,
            eventDate = DateTime.UtcNow.Date.AddDays(20),
            startTime = "08:30:00",
            location = "Test Park",
            entryFee = 150,
            maxParticipants = 200,
            status = "Open"
        };

    [Fact]
    public async Task GetEvents_Anonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_AndOwnsIt()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<EventResponse>();

        Assert.NotNull(created);
        Assert.Equal(organiser.UserId, created!.OrganiserId);
    }

    [Fact]
    public async Task CreateEvent_IgnoresOrganiserIdSentInTheBody()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/events", new
        {
            organiserId = 987654,
            eventName = "Spoofed Organiser",
            description = "The organiser id must come from the token.",
            eventType = "Cycling",
            eventDate = DateTime.UtcNow.Date.AddDays(5),
            startTime = "06:00:00",
            location = "Test Park",
            entryFee = 0,
            maxParticipants = 10,
            status = "Upcoming"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(organiser.UserId, created!.OrganiserId);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEvent_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync(
            "/api/events",
            EventBody(eventType: "Swimming"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_MissingName_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync(
            "/api/events",
            EventBody(eventName: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var update = await organiser.Client.PutAsJsonAsync(
            $"/api/events/{created.EventId}",
            EventBody(created.EventId, "Renamed Event"));

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var fetched = await organiser.Client.GetFromJsonAsync<EventResponse>(
            $"/api/events/{created.EventId}");

        Assert.Equal("Renamed Event", fetched!.EventName);
    }

    [Fact]
    public async Task Update_MismatchedIds_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var update = await organiser.Client.PutAsJsonAsync(
            $"/api/events/{created.EventId}",
            EventBody(created.EventId + 1));

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(owner);

        var update = await other.Client.PutAsJsonAsync(
            $"/api/events/{created.EventId}",
            EventBody(created.EventId, "Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateEvent_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var update = await participant.Client.PutAsJsonAsync(
            $"/api/events/{created.EventId}",
            EventBody(created.EventId));

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.Client.DeleteAsync($"/api/events/{created.EventId}");
        var fetch = await organiser.Client.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetch.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var created = await TestHelpers.CreateEventAsync(owner);

        var delete = await other.Client.DeleteAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var created = await TestHelpers.CreateEventAsync(organiser);

        var delete = await participant.Client.DeleteAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_ReturnsConflict()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var delete = await organiser.Client.DeleteAsync($"/api/events/{raceEvent.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task GetEvents_FilterByType_ReturnsOnlyMatchingEvents()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var cycling = await TestHelpers.CreateEventAsync(organiser, eventType: "Cycling");
        var running = await TestHelpers.CreateEventAsync(organiser, eventType: "Running");

        var response = await organiser.Client.GetAsync(
            $"/api/events?type=Cycling&organiserId={organiser.UserId}");

        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(cycling.EventName, json);
        Assert.DoesNotContain(running.EventName, json);
    }

    [Fact]
    public async Task GetEvents_UpcomingFilter_ExcludesPastEvents()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var future = await TestHelpers.CreateEventAsync(organiser, daysAhead: 15);
        var past = await TestHelpers.CreateEventAsync(organiser, daysAhead: -15);

        var response = await organiser.Client.GetAsync(
            $"/api/events?upcoming=true&organiserId={organiser.UserId}");

        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains(future.EventName, json);
        Assert.DoesNotContain(past.EventName, json);
    }

    [Fact]
    public async Task Organiser_MyEvents_ReturnsOnlyTheirOwnEvents()
    {
        var first = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var second = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var firstEvent = await TestHelpers.CreateEventAsync(first);
        var secondEvent = await TestHelpers.CreateEventAsync(second);

        var response = await first.Client.GetAsync("/api/events/mine");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(firstEvent.EventName, json);
        Assert.DoesNotContain(secondEvent.EventName, json);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserEventList_ReturnsForbidden()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Event categories ----------

    [Fact]
    public async Task Organiser_CanAssignAndRemoveCategory()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser);

        var assign = await organiser.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/categories/{category.CategoryId}", null);

        var listed = await organiser.Client.GetStringAsync(
            $"/api/events/{raceEvent.EventId}/categories");

        var remove = await organiser.Client.DeleteAsync(
            $"/api/events/{raceEvent.EventId}/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);
        Assert.Contains(category.CategoryName, listed);
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
    }

    [Fact]
    public async Task AssignCategory_Twice_ReturnsConflict()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var again = await organiser.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/categories/{category.CategoryId}", null);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotAssignCategory_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);
        var category = await TestHelpers.CreateCategoryAsync(organiser);

        var response = await participant.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/categories/{category.CategoryId}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveCategory_WithEnrolments_ReturnsConflict()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var response = await organiser.Client.DeleteAsync(
            $"/api/events/{raceEvent.EventId}/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---------- Banner upload ----------

    [Fact]
    public async Task Organiser_CanUploadBanner_UrlIsSavedOnTheEvent()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);

        using var form = TestHelpers.ImageForm(TestHelpers.PngBytes, "banner.png", "image/png");

        var upload = await organiser.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/banner", form);

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var uploaded = await upload.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.StartsWith("https://", uploaded!.Url);

        var fetched = await organiser.Client.GetStringAsync($"/api/events/{raceEvent.EventId}");
        Assert.Contains(uploaded.Url, fetched);
    }

    [Fact]
    public async Task UploadBanner_NotAnImage_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);

        var notAnImage = System.Text.Encoding.UTF8.GetBytes("this is just text, not an image");
        using var form = TestHelpers.ImageForm(notAnImage, "banner.png", "image/png");

        var upload = await organiser.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/banner", form);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WrongExtension_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);

        using var form = TestHelpers.ImageForm(TestHelpers.PngBytes, "script.exe", "image/png");

        var upload = await organiser.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/banner", form);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);

        using var form = TestHelpers.ImageForm(TestHelpers.PngBytes, "banner.png", "image/png");

        var upload = await participant.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/banner", form);

        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUploadBannerForAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var raceEvent = await TestHelpers.CreateEventAsync(owner);

        using var form = TestHelpers.ImageForm(TestHelpers.PngBytes, "banner.png", "image/png");

        var upload = await other.Client.PostAsync(
            $"/api/events/{raceEvent.EventId}/banner", form);

        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
    }
}

// =======================================================
// END: EVENT MANAGEMENT TESTS
// =======================================================
