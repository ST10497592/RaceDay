
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

    [Required(ErrorMessage = "A description is required.")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    [RegularExpression("^(Running|Walking|Cycling)$",
        ErrorMessage = "Event type must be Running, Walking or Cycling.")]
    public string EventType { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public TimeSpan StartTime { get; set; }

    [Required]
    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 100000, ErrorMessage = "Entry fee must be between 0 and 100000.")]
    public decimal EntryFee { get; set; }

    [Range(1, 100000, ErrorMessage = "Maximum participants must be at least 1.")]
    public int MaxParticipants { get; set; }

    [Required]
    [MaxLength(20)]
    [RegularExpression("^(Upcoming|Open|Closed|Completed|Cancelled)$",
        ErrorMessage = "Status must be Upcoming, Open, Closed, Completed or Cancelled.")]
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
