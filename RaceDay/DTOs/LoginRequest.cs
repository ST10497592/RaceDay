using System.ComponentModel.DataAnnotations;

namespace RaceDay.DTOs;

// START: LOGIN REQUEST
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
// END: LOGIN REQUEST