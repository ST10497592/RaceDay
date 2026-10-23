using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

// START: REGISTRATION REQUEST
// Purpose: Validates the data required to register a new participant.
public class RegisterRequest
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

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [MaxLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must contain at least 8 characters.")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    [RegularExpression(@"^\+?[0-9\s().-]*$", ErrorMessage = "The phone number contains invalid characters.")]
    public string? PhoneNumber { get; set; }
}
// END: REGISTRATION REQUEST