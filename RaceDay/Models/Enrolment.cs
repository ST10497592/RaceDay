
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models;

// =======================================================
// START: ENROLMENT ENTITY
// Purpose: Represents a participant's enrolment in an event.
// =======================================================

[Table("Enrolments")]
public class Enrolment
{
    // START: PRIMARY KEY
    [Key]
    public int EnrolmentId { get; set; }
    // END: PRIMARY KEY

    // START: FOREIGN KEYS
    public int EventId { get; set; }

    public int ParticipantId { get; set; }

    public int CategoryId { get; set; }
    // END: FOREIGN KEYS

    // START: ENROLMENT DETAILS
    public DateTime EnrolmentDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = string.Empty;
    // END: ENROLMENT DETAILS

    // START: NAVIGATION PROPERTIES
    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [ForeignKey(nameof(ParticipantId))]
    public User? Participant { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }
    // END: NAVIGATION PROPERTIES
}

// =======================================================
// END: ENROLMENT ENTITY
// =======================================================
