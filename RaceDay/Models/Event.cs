
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models;

[Table("Events")]
public class Event
{
    [Key]
    public int EventId { get; set; }

    public int OrganiserId { get; set; }

    [Required]
    [MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public TimeSpan StartTime { get; set; }

    [Required]
    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal EntryFee { get; set; }

    public int MaxParticipants { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? BannerUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(OrganiserId))]
    public User? Organiser { get; set; }


    // START: CATEGORY RELATIONSHIPS
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    // END: CATEGORY RELATIONSHIPS
}
