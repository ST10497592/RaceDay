using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Data;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: AUTHENTICATION TESTS
// Covers user registration and login (success and failure),
// the organiser access code, hashing and token protection.
// =======================================================

public class AuthenticationTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public AuthenticationTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private static object RegistrationBody(
        string email,
        string role = "Participant",
        string? organiserCode = null,
        string password = TestHelpers.Password) => new
        {
            firstName = "Alex",
            lastName = "Runner",
            email,
            password,
            role,
            organiserCode
        };

    // ---------- Registration ----------

    [Fact]
    public async Task Register_Participant_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationBody(TestHelpers.UniqueEmail("participant")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Participant", body);
    }

    [Fact]
    public async Task Register_WithoutRole_DefaultsToParticipant()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Default",
            lastName = "Role",
            email = TestHelpers.UniqueEmail("default"),
            password = TestHelpers.Password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Participant", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("duplicate");

        var first = await client.PostAsJsonAsync("/api/auth/register", RegistrationBody(email));
        var second = await client.PostAsJsonAsync("/api/auth/register", RegistrationBody(email));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationBody("not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationBody(TestHelpers.UniqueEmail("short"), password: "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationBody(TestHelpers.UniqueEmail("admin"), role: "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_FutureDateOfBirth_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Future",
            lastName = "Baby",
            email = TestHelpers.UniqueEmail("future"),
            password = TestHelpers.Password,
            dateOfBirth = DateTime.UtcNow.AddYears(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationBody(TestHelpers.UniqueEmail("organiser"), role: "Organiser"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongCode_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationBody(
                TestHelpers.UniqueEmail("organiser"),
                role: "Organiser",
                organiserCode: "WRONG-CODE"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithValidCode_ReturnsCreatedAsOrganiser()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationBody(
                TestHelpers.UniqueEmail("organiser"),
                role: "Organiser",
                organiserCode: TestHelpers.OrganiserCode(_factory)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Organiser", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("hashed");

        var response = await client.PostAsJsonAsync("/api/auth/register", RegistrationBody(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await context.Users.SingleAsync(u => u.Email == email);

        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndRole()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("login");

        await client.PostAsJsonAsync("/api/auth/register", RegistrationBody(email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = TestHelpers.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Participant", body.User.Role);
    }

    [Fact]
    public async Task Login_IsCaseInsensitiveForEmail()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("casing");

        await client.PostAsJsonAsync("/api/auth/register", RegistrationBody(email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = email.ToUpperInvariant(),
            password = TestHelpers.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = TestHelpers.UniqueEmail("wrongpass");

        await client.PostAsJsonAsync("/api/auth/register", RegistrationBody(email));

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword!1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestHelpers.UniqueEmail("ghost"),
            password = TestHelpers.Password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestHelpers.UniqueEmail("nopass"),
            password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Protected access ----------

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/enrolments/my");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not-a-valid-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.GetAsync("/api/enrolments/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Authenticated_ReturnsOk()
    {
        var participant = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var response = await participant.Client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

// =======================================================
// END: AUTHENTICATION TESTS
// =======================================================
