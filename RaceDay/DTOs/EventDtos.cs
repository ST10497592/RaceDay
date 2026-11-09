namespace RaceDay.DTOs;

// START: EVENT SUMMARY DTO
/// <summary>
/// Compact view of an event used by the organiser dashboard and management pages,
/// including enrolment totals.
/// </summary>
public class EventSummaryDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? BannerUrl { get; set; }
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
}
// END: EVENT SUMMARY DTO
