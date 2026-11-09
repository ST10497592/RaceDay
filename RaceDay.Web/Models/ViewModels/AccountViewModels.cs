using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.ViewModels;

// =======================================================
// START: ACCOUNT VIEW MODELS
// =======================================================

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    [RegularExpression(@"^\+?[0-9\s().-]*$", ErrorMessage = "The phone number contains invalid characters.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Please choose a role.")]
    [RegularExpression("^(Participant|Organiser)$", ErrorMessage = "Role must be Participant or Organiser.")]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser access code")]
    public string? OrganiserCode { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    [RegularExpression(@"^\+?[0-9\s().-]*$", ErrorMessage = "The phone number contains invalid characters.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    // Read-only details shown beside the form.
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

// =======================================================
// END: ACCOUNT VIEW MODELS
// =======================================================
