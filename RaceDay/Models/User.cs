
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RaceDay.Models;

[Table("Users")]
public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    // The hash must never be serialised in an API response.
    [JsonIgnore]
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    // URL of the profile picture stored in Azure Blob Storage.
    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }
}
