using System.Net;
using System.Net.Http.Json;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: ROLE ACCESS TESTS
// Verifies that Organiser-only and Participant-only
// functionality rejects the wrong role (403) and anonymous
// callers (401) across the API.
// =======================================================

public class RoleAccessTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public RoleAccessTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private static object CategoryBody() => new
    {
        categoryName = "Category " + Guid.NewGuid().ToString("N")[..8],
        description = "Test category",
        minimumAge = 12,
        maximumAge = 40
    };

    // ---------- Categories ----------

    [Fact]
    public async Task Anonymous_CanViewCategories()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewCategories()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateCategory()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/categories", CategoryBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_ReturnsForbidden()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsJsonAsync("/api/categories", CategoryBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateCategory_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/categories", CategoryBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteCategory_ReturnsForbidden()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");
        var category = await TestHelpers.CreateCategoryAsync(organiser);

        var response = await participant.Client.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Organiser-only and Participant-only areas ----------

    [Fact]
    public async Task Organiser_CannotUseParticipantOnlyEnrolmentEndpoint()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.PostAsJsonAsync("/api/enrolments", new
        {
            eventId = 1,
            categoryId = 1,
            status = "Pending",
            paymentStatus = "Pending"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotListAllEnrolments_ReturnsForbidden()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.GetAsync("/api/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanListEnrolmentsOfTheirEvents()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");

        var response = await organiser.Client.GetAsync("/api/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotListEnrolments_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/enrolments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotViewResultsList_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/results/my");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_RoleClaim_MatchesTheRegisteredRole()
    {
        var organiser = await TestHelpers.CreateUserAsync(_factory, "Organiser");
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var organiserProfile = await organiser.Client
            .GetFromJsonAsync<ProfileResponse>("/api/users/me");

        var participantProfile = await participant.Client
            .GetFromJsonAsync<ProfileResponse>("/api/users/me");

        Assert.Equal("Organiser", organiserProfile!.Role);
        Assert.Equal("Participant", participantProfile!.Role);
    }
}

// =======================================================
// END: ROLE ACCESS TESTS
// =======================================================
