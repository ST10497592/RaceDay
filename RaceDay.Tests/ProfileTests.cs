using System.Net;
using System.Net.Http.Json;
using RaceDay.Tests.TestSupport;

namespace RaceDay.Tests;

// =======================================================
// START: PROFILE TESTS
// Verifies that both roles can view and update their own
// profile and upload a profile picture.
// =======================================================

public class ProfileTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ProfileTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task BothRoles_CanViewTheirOwnProfile(string role)
    {
        var user = await TestHelpers.CreateUserAsync(_factory, role);

        var response = await user.Client.GetAsync("/api/users/me");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(user.Email, json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profile_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_CanUpdateTheirProfile()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var update = await user.Client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Name",
            phoneNumber = "+27 71 000 0000",
            dateOfBirth = new DateTime(1995, 5, 17)
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var profile = await user.Client.GetFromJsonAsync<ProfileResponse>("/api/users/me");

        Assert.Equal("Updated", profile!.FirstName);
        Assert.Equal("Name", profile.LastName);
        Assert.Equal("+27 71 000 0000", profile.PhoneNumber);
        Assert.Equal(user.Email, profile.Email);
    }

    [Fact]
    public async Task UpdateProfile_WithTooShortName_ReturnsBadRequest()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var update = await user.Client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "A",
            lastName = "Name"
        });

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithFutureDateOfBirth_ReturnsBadRequest()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var update = await user.Client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Valid",
            lastName = "Name",
            dateOfBirth = DateTime.UtcNow.AddYears(2)
        });

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var update = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Valid",
            lastName = "Name"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
    }

    [Fact]
    public async Task User_CanUploadProfilePicture_UrlIsSavedOnTheProfile()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        using var form = TestHelpers.ImageForm(TestHelpers.PngBytes, "me.png", "image/png");

        var upload = await user.Client.PostAsync("/api/users/me/profile-picture", form);

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var uploaded = await upload.Content.ReadFromJsonAsync<UploadResponse>();
        var profile = await user.Client.GetFromJsonAsync<ProfileResponse>("/api/users/me");

        Assert.Equal(uploaded!.Url, profile!.ProfilePictureUrl);
    }

    [Fact]
    public async Task UploadProfilePicture_NotAnImage_ReturnsBadRequest()
    {
        var user = await TestHelpers.CreateUserAsync(_factory, "Participant");

        var text = System.Text.Encoding.UTF8.GetBytes("definitely not an image file");
        using var form = TestHelpers.ImageForm(text, "me.png", "image/png");

        var upload = await user.Client.PostAsync("/api/users/me/profile-picture", form);

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        using var form = TestHelpers.ImageForm(TestHelpers.PngBytes, "me.png", "image/png");

        var upload = await client.PostAsync("/api/users/me/profile-picture", form);

        Assert.Equal(HttpStatusCode.Unauthorized, upload.StatusCode);
    }
}

// =======================================================
// END: PROFILE TESTS
// =======================================================
