using System.ComponentModel.DataAnnotations;
using RaceDay.Models;

namespace RaceDay.DTOs;

// START: ENROLMENT DTOs
/// <summary>
/// Flattened enrolment, including event, participant, category and (when
/// recorded) result information. Never exposes the participant's password hash.
/// </summary>
public class EnrolmentDetailDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventLocation { get; set; } = string.Empty;
    public string EventStatus { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    public int? ResultId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string? ResultStatus { get; set; }

    public static EnrolmentDetailDto From(Enrolment enrolment, Result? result = null)
    {
        return new EnrolmentDetailDto
        {
            EnrolmentId = enrolment.EnrolmentId,
            EventId = enrolment.EventId,
            EventName = enrolment.Event?.EventName ?? string.Empty,
            EventDate = enrolment.Event?.EventDate ?? default,
            EventLocation = enrolment.Event?.Location ?? string.Empty,
            EventStatus = enrolment.Event?.Status ?? string.Empty,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = enrolment.Participant == null
                ? string.Empty
                : enrolment.Participant.FirstName + " " + enrolment.Participant.LastName,
            ParticipantEmail = enrolment.Participant?.Email ?? string.Empty,
            CategoryId = enrolment.CategoryId,
            CategoryName = enrolment.Category?.CategoryName ?? string.Empty,
            EnrolmentDate = enrolment.EnrolmentDate,
            Status = enrolment.Status,
            PaymentStatus = enrolment.PaymentStatus,
            ResultId = result?.ResultId,
            FinishTime = result?.FinishTime,
            Position = result?.Position,
            ResultStatus = result?.ResultStatus
        };
    }
}

/// <summary>Request used by an organiser to confirm / cancel an enrolment or update its payment state.</summary>
public class UpdateEnrolmentStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Pending|Confirmed|Cancelled|Completed)$",
        ErrorMessage = "Status must be Pending, Confirmed, Cancelled or Completed.")]
    public string Status { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payment status is required.")]
    [RegularExpression("^(Pending|Paid|Failed|Refunded)$",
        ErrorMessage = "Payment status must be Pending, Paid, Failed or Refunded.")]
    public string PaymentStatus { get; set; } = string.Empty;
}
// END: ENROLMENT DTOs
