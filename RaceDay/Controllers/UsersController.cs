using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Extensions;
using RaceDay.Models;
using RaceDay.Services;

namespace RaceDay.Controllers;

// =======================================================
// START: USERS CONTROLLER
// Purpose: Lets the signed-in user view and update their own
// profile and upload a profile picture (Azure Blob Storage).
// =======================================================

/// <summary>Profile of the signed-in user (both roles).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public UsersController(
        RaceDayDbContext context,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    // START: GET PROFILE
    /// <summary>Returns the profile of the signed-in user.</summary>
    /// <remarks>Returns 200 with the profile, or 401 when not signed in.</remarks>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var user = await FindCurrentUserAsync(track: false);

        if (user == null)
            return Unauthorized();

        return Ok(ToDto(user));
    }
    // END: GET PROFILE

    // START: UPDATE PROFILE
    /// <summary>Updates the signed-in user's name, phone number and date of birth.</summary>
    /// <remarks>Body: firstName, lastName, optional phoneNumber and dateOfBirth. Returns 200, 400 or 401.</remarks>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileRequest request)
    {
        var user = await FindCurrentUserAsync(track: true);

        if (user == null)
            return Unauthorized();

        if (request.DateOfBirth.HasValue &&
            request.DateOfBirth.Value.Date >= DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(
                nameof(request.DateOfBirth),
                "Date of birth must be in the past.");

            return ValidationProblem(ModelState);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();
        user.DateOfBirth = request.DateOfBirth?.Date;

        await _context.SaveChangesAsync();

        return Ok(ToDto(user));
    }
    // END: UPDATE PROFILE

    // START: UPLOAD PROFILE PICTURE
    /// <summary>Uploads a profile picture to Azure Blob Storage and saves its URL on the user.</summary>
    /// <remarks>
    /// Multipart form field "file" (JPG, PNG, GIF or WEBP, max 5 MB).
    /// Returns 200 with the image URL, 400 for an invalid image, 401, or 503 if storage is unavailable.
    /// </remarks>
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UploadResponse>> UploadProfilePicture(IFormFile file)
    {
        var user = await FindCurrentUserAsync(track: true);

        if (user == null)
            return Unauthorized();

        var validationError = await ImageUploadValidator.ValidateAsync(file);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        try
        {
            var url = await _blobStorage.UploadImageAsync(file, "profiles");

            user.ProfilePictureUrl = url;
            await _context.SaveChangesAsync();

            return Ok(new UploadResponse { Url = url });
        }
        catch (StorageUnavailableException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }
    // END: UPLOAD PROFILE PICTURE

    private async Task<User?> FindCurrentUserAsync(bool track)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return null;

        IQueryable<User> query = _context.Users;

        if (!track)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            u => u.UserId == userId.Value && u.IsActive);
    }

    private static UserProfileDto ToDto(User user)
    {
        return new UserProfileDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            ProfilePictureUrl = user.ProfilePictureUrl
        };
    }
}

// =======================================================
// END: USERS CONTROLLER
// =======================================================
