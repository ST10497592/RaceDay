using System.Net;
using System.Net.Http.Json;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: RESULTS TESTS
// Verifies that Organisers capture results, Participants can
// only view their own and that invalid results are rejected.
// =======================================================

public class ResultsTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ResultsTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private static object FinisherBody(int enrolmentId, int position, string time = "00:45:30") => new
    {
        enrolmentId,
        finishTime = time,
        position,
        resultStatus = "Finished"
    };

    private async Task<(TestUser Organiser, TestUser Participant, EventResponse Event, EnrolmentResponse Enrolment)>
        SetUpEnrolmentAsync()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var enrolment = await TestHelpers.EnrolAsync(participant, raceEvent.EventId, category.CategoryId);

        return (organiser, participant, raceEvent, enrolment);
    }

    [Fact]
    public async Task Organiser_CanRecordResult_AndEnrolmentBecomesCompleted()
    {
        var (organiser, participant, _, enrolment) = await SetUpEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync(
            "/api/results",
            FinisherBody(enrolment.EnrolmentId, position: 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ResultResponse>();

        Assert.NotNull(created);
        Assert.Equal(1, created!.Position);
        Assert.Equal("Finished", created.ResultStatus);
        Assert.Equal(new TimeSpan(0, 45, 30), created.FinishTime);

        var mine = await participant.Client
            .GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/my");

        Assert.Equal("Completed", mine!.Single().Status);
    }

    [Fact]
    public async Task Participant_CannotRecordResult_ReturnsForbidden()
    {
        var (_, participant, _, enrolment) = await SetUpEnrolmentAsync();

        var response = await participant.Client.PostAsJsonAsync(
            "/api/results",
            FinisherBody(enrolment.EnrolmentId, position: 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotRecordResult_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/results", FinisherBody(1, 1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotRecordResult_ReturnsForbidden()
    {
        var (_, _, _, enrolment) = await SetUpEnrolmentAsync();
        var other = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await other.Client.PostAsJsonAsync(
            "/api/results",
            FinisherBody(enrolment.EnrolmentId, position: 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_ForUnknownEnrolment_ReturnsBadRequest()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync(
            "/api/results",
            FinisherBody(enrolmentId: 999999, position: 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_Twice_ReturnsConflict()
    {
        var (organiser, _, _, enrolment) = await SetUpEnrolmentAsync();

        await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(enrolment.EnrolmentId, position: 1));

        var second = await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(enrolment.EnrolmentId, position: 2));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RecordResult_WithPositionAlreadyTaken_ReturnsConflict()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var first = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var firstEnrolment = await TestHelpers.EnrolAsync(first, raceEvent.EventId, category.CategoryId);
        var secondEnrolment = await TestHelpers.EnrolAsync(second, raceEvent.EventId, category.CategoryId);

        var ok = await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(firstEnrolment.EnrolmentId, position: 1));

        var duplicate = await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(secondEnrolment.EnrolmentId, position: 1));

        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task RecordResult_FinisherWithoutTime_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentId,
            position = 1,
            resultStatus = "Finished"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_WithInvalidStatus_ReturnsBadRequest()
    {
        var (organiser, _, _, enrolment) = await SetUpEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentId,
            finishTime = "00:40:00",
            position = 1,
            resultStatus = "Winner"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_DidNotFinish_ClearsTimeAndPosition()
    {
        var (organiser, _, _, enrolment) = await SetUpEnrolmentAsync();

        var response = await organiser.Client.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment.EnrolmentId,
            finishTime = "00:10:00",
            position = 5,
            resultStatus = "DidNotFinish"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ResultResponse>();

        Assert.Null(created!.Position);
        Assert.Null(created.FinishTime);
        Assert.Equal("DidNotFinish", created.ResultStatus);
    }

    [Fact]
    public async Task Participant_CanViewOwnResults_WithTotalFinishers()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var first = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var firstEnrolment = await TestHelpers.EnrolAsync(first, raceEvent.EventId, category.CategoryId);
        var secondEnrolment = await TestHelpers.EnrolAsync(second, raceEvent.EventId, category.CategoryId);

        await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(firstEnrolment.EnrolmentId, 1, "00:40:00"));
        await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(secondEnrolment.EnrolmentId, 2, "00:42:10"));

        var firstResults = await first.Client
            .GetFromJsonAsync<List<ResultResponse>>("/api/results/my");

        var secondResults = await second.Client
            .GetFromJsonAsync<List<ResultResponse>>("/api/results/my");

        Assert.Single(firstResults!);
        Assert.Equal(1, firstResults![0].Position);
        Assert.Equal(2, firstResults[0].TotalFinishers);

        Assert.Single(secondResults!);
        Assert.Equal(2, secondResults![0].Position);
        Assert.Equal(first.UserId, firstResults[0].ParticipantId);
    }

    [Fact]
    public async Task Participant_WithoutResults_GetsAnEmptyList()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var results = await participant.Client
            .GetFromJsonAsync<List<ResultResponse>>("/api/results/my");

        Assert.NotNull(results);
        Assert.Empty(results!);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsResult_ReturnsForbidden()
    {
        var (organiser, _, _, enrolment) = await SetUpEnrolmentAsync();
        var stranger = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var created = await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(enrolment.EnrolmentId, position: 1));

        var result = await created.Content.ReadFromJsonAsync<ResultResponse>();

        var response = await stranger.Client.GetAsync($"/api/results/{result!.ResultId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventResults_ArePublic_AndOrderedByPosition()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var first = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var second = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var (raceEvent, category) = await TestHelpers.CreateOpenEventWithCategoryAsync(organiser);
        var firstEnrolment = await TestHelpers.EnrolAsync(first, raceEvent.EventId, category.CategoryId);
        var secondEnrolment = await TestHelpers.EnrolAsync(second, raceEvent.EventId, category.CategoryId);

        // Recorded in reverse order on purpose.
        await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(secondEnrolment.EnrolmentId, 2, "00:50:00"));
        await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(firstEnrolment.EnrolmentId, 1, "00:45:00"));

        var anonymous = _factory.CreateClient();

        var results = await anonymous
            .GetFromJsonAsync<List<ResultResponse>>($"/api/events/{raceEvent.EventId}/results");

        Assert.Equal(2, results!.Count);
        Assert.Equal(1, results[0].Position);
        Assert.Equal(2, results[1].Position);
    }

    [Fact]
    public async Task EventResults_ForUnknownEvent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteTheirResult()
    {
        var (organiser, _, _, enrolment) = await SetUpEnrolmentAsync();

        var created = await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(enrolment.EnrolmentId, position: 1));

        var result = await created.Content.ReadFromJsonAsync<ResultResponse>();

        var delete = await organiser.Client.DeleteAsync($"/api/results/{result!.ResultId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteResult_ReturnsForbidden()
    {
        var (organiser, participant, _, enrolment) = await SetUpEnrolmentAsync();

        var created = await organiser.Client.PostAsJsonAsync(
            "/api/results", FinisherBody(enrolment.EnrolmentId, position: 1));

        var result = await created.Content.ReadFromJsonAsync<ResultResponse>();

        var delete = await participant.Client.DeleteAsync($"/api/results/{result!.ResultId}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}

// =======================================================
// END: RESULTS TESTS
// =======================================================
