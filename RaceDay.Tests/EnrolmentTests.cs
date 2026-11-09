using System.Net;
using System.Net.Http.Json;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: ENROLMENT TESTS
// Verifies that Participants can enrol, that the enrolment
// is recorded correctly and that the business rules and
// role restrictions are enforced.
// =======================================================

public class EnrolmentTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public EnrolmentTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecordedAsPending()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = category.CategoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();

        Assert.NotNull(created);
        Assert.Equal(raceEvent.EventId, created!.EventId);
        Assert.Equal(participant.UserId, created.ParticipantId);
        Assert.Equal(category.CategoryName, created.CategoryName);
        Assert.Equal("Pending", created.Status);
        Assert.Equal("Pending", created.PaymentStatus);
    }

    [Fact]
    public async Task Enrolment_AppearsInTheParticipantsOwnList()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var created = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var mine = await participant.Client
            .GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/my");

        Assert.NotNull(mine);
        Assert.Contains(mine!, e => e.EnrolmentId == created.EnrolmentId);
    }

    [Fact]
    public async Task Participant_CannotSeeAnotherParticipantsEnrolments()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var first = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var created = await TestHelpers.EnrolAsync(first, raceEvent.EventId, category.CategoryId);

        var secondList = await second.Client
            .GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/my");

        var direct = await second.Client.GetAsync($"/api/enrolments/{created.EnrolmentId}");

        Assert.DoesNotContain(secondList!, e => e.EnrolmentId == created.EnrolmentId);
        Assert.Equal(HttpStatusCode.Forbidden, direct.StatusCode);
    }

    [Fact]
    public async Task ParticipantId_ComesFromTheToken_NotTheRequestBody()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = category.CategoryId,
            participantId = 424242,
            status = "Confirmed",
            paymentStatus = "Paid"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();

        // Participants cannot confirm or pay for themselves.
        Assert.Equal(participant.UserId, created!.ParticipantId);
        Assert.Equal("Pending", created.Status);
        Assert.Equal("Pending", created.PaymentStatus);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var second = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = category.CategoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryNotOfferedForTheEvent_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);
        var unassignedCategory = await TestHelpers.CreateCategoryAsync(organiser);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = unassignedCategory.CategoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAClosedEvent_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) =
            await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, status: "Closed");

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = category.CategoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAnEventThatAlreadyTookPlace_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) =
            await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, daysAhead: -3);

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = category.CategoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAFullEvent_ReturnsConflict()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var first = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) =
            await TestHelpers.CreateOpenEventWithCategoryAsync(organiser, maxParticipants: 1);

        await TestHelpers.EnrolAsync(first, raceEvent.EventId, category.CategoryId);

        var response = await second.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = category.CategoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownEvent_ReturnsNotFound()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = 999999,
            categoryId = 1,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotEnrol_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = 1,
            categoryId = 1,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var response = await organiser.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = raceEvent.EventId,
            categoryId = category.CategoryId,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Organiser view of enrolments ----------

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForTheirEvent_WithoutLeakingPasswordHashes()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);

        var created = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var response = await organiser.Client.GetAsync($"/api/events/{raceEvent.EventId}/enrolments");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(participant.Email, json);
        Assert.Contains(category.CategoryName, json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);

        var list = await organiser.Client
            .GetFromJsonAsync<List<EnrolmentResponse>>($"/api/events/{raceEvent.EventId}/enrolments");

        Assert.Single(list!);
        Assert.Equal(created.EnrolmentId, list![0].EnrolmentId);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var raceEvent = await TestHelpers.CreateEventAsync(organiser);

        var response = await participant.Client.GetAsync($"/api/events/{raceEvent.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_ReturnsForbidden()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var raceEvent = await TestHelpers.CreateEventAsync(owner);

        var response = await other.Client.GetAsync($"/api/events/{raceEvent.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanConfirmAnEnrolment()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var created = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var response = await TestHelpers.PatchJsonAsync(
            organiser.Client,
            $"/api/enrolments/{created.EnrolmentId}/status",
            new { status = "Confirmed", paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal("Confirmed", updated!.Status);
        Assert.Equal("Paid", updated.PaymentStatus);

        var mine = await participant.Client
            .GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/my");

        Assert.Equal("Confirmed", mine!.Single(e => e.EnrolmentId == created.EnrolmentId).Status);
    }

    [Fact]
    public async Task Participant_CannotConfirmTheirOwnEnrolment_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var created = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var response = await TestHelpers.PatchJsonAsync(
            participant.Client,
            $"/api/enrolments/{created.EnrolmentId}/status",
            new { status = "Confirmed", paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotChangeEnrolmentStatus_ReturnsForbidden()
    {
        var owner = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(owner);
        var created = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var response = await TestHelpers.PatchJsonAsync(
            other.Client,
            $"/api/enrolments/{created.EnrolmentId}/status",
            new { status = "Confirmed", paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidValue_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var created = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var response = await TestHelpers.PatchJsonAsync(
            organiser.Client,
            $"/api/enrolments/{created.EnrolmentId}/status",
            new { status = "Maybe", paymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Cancelling ----------

    [Fact]
    public async Task Participant_CanCancelTheirOwnEnrolment()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var created = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        var response = await participant.Client.DeleteAsync($"/api/enrolments/{created.EnrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var mine = await participant.Client
            .GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/my");

        Assert.DoesNotContain(mine!, e => e.EnrolmentId == created.EnrolmentId);
    }

    [Fact]
    public async Task Participant_CannotCancelAnotherParticipantsEnrolment_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var owner = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var other = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var created = await TestHelpers.EnrolAsync(owner, raceEvent.EventId, category.CategoryId);

        var response = await other.Client.DeleteAsync($"/api/enrolments/{created.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

// =======================================================
// END: ENROLMENT TESTS
// =======================================================
