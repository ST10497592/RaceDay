
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models;

// =======================================================
// START: RESULT ENTITY
// Purpose: Represents the recorded result of an enrolment.
// =======================================================

[Table("Results")]
public class Result
{
    // START: PRIMARY KEY
    [Key]
    public int ResultId { get; set; }
    // END: PRIMARY KEY

    // START: FOREIGN KEY
    public int EnrolmentId { get; set; }
    // END: FOREIGN KEY

    // START: RESULT DETAILS
    public TimeSpan? FinishTime { get; set; }

    public int? Position { get; set; }

    [Required]
    [MaxLength(20)]
    public string ResultStatus { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; }
    // END: RESULT DETAILS

    // START: NAVIGATION PROPERTY
    [ForeignKey(nameof(EnrolmentId))]
    public Enrolment? Enrolment { get; set; }
    // END: NAVIGATION PROPERTY
}

// =======================================================
// END: RESULT ENTITY
// =======================================================
