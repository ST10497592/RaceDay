namespace RaceDay.Web.Models.Api;

// =======================================================
// START: API MODELS
// Purpose: Shapes of the JSON returned by the RaceDay API.
// The MVC application only deserialises these - it contains
// no database access and no business rules.
// =======================================================

public class AuthUserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthUserDto User { get; set; } = new();
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }

    // Display text only.
    public string AgeRangeText
    {
        get
        {
            if (MinimumAge.HasValue && MaximumAge.HasValue)
                return $"Ages {MinimumAge}-{MaximumAge}";

            if (MinimumAge.HasValue)
                return $"Ages {MinimumAge}+";

            if (MaximumAge.HasValue)
                return $"Up to {MaximumAge}";

            return "All ages";
        }
    }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? BannerUrl { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

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

public class EnrolmentDto
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
}

public class ResultDto
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

public class ProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class UploadResultDto
{
    public string Url { get; set; } = string.Empty;
}

/// <summary>Used for API calls whose response body is not needed.</summary>
public class EmptyResponse
{
}

// START: REQUEST HELPERS
public class RegisterApiRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Role { get; set; } = "Participant";
    public string? OrganiserCode { get; set; }
}

public class EventFormData
{
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EventQuery
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool Upcoming { get; set; }
}
// END: REQUEST HELPERS

// =======================================================
// END: API MODELS
// =======================================================
