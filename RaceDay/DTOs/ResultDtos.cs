namespace RaceDay.DTOs;

// START: RESULT DTO
/// <summary>
/// A race result together with the event, category and participant it belongs to.
/// TotalFinishers is the number of finishers recorded for the event, so the UI can
/// show "47th out of 312 finishers".
/// </summary>
public class ResultDetailDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
// END: RESULT DTO
