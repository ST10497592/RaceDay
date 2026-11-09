using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

// START: PROFILE DTOs
/// <summary>The profile of the signed-in user (never includes the password hash).</summary>
public class UserProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

/// <summary>Editable profile fields. Email and role cannot be changed.</summary>
public class UpdateProfileRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [MinLength(2, ErrorMessage = "First name must contain at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "First name cannot contain only whitespace.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MinLength(2, ErrorMessage = "Last name must contain at least 2 characters.")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "Last name cannot contain only whitespace.")]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    [RegularExpression(@"^\+?[0-9\s().-]*$", ErrorMessage = "The phone number contains invalid characters.")]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }
}

/// <summary>Returned after a successful image upload.</summary>
public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}
// END: PROFILE DTOs
