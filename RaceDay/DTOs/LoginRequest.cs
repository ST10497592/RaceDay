using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

// START: LOGIN REQUEST
// Purpose: Validates the credentials submitted during login.
public class LoginRequest
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [MaxLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;
}
// END: LOGIN REQUEST