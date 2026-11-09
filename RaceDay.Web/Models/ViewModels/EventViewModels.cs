using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

// =======================================================
// START: EVENT VIEW MODELS
// =======================================================

public class HomeViewModel
{
    public List<EventDto> FeaturedEvents { get; set; } = new();
    public string? Error { get; set; }
}

public class EventListViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public EventQuery Query { get; set; } = new();
    public string? Error { get; set; }
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
    public List<EnrolmentDto> MyEnrolments { get; set; } = new();

    // True when the signed-in participant already holds an active enrolment.
    public bool AlreadyEnrolled { get; set; }

    public bool IsParticipant { get; set; }
    public bool IsSignedIn { get; set; }

    [Required(ErrorMessage = "Please choose a category.")]
    public int? CategoryId { get; set; }
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Event name must be between 3 and 150 characters.")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a description.")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose an event type.")]
    [RegularExpression("^(Running|Walking|Cycling)$", ErrorMessage = "Event type must be Running, Walking or Cycling.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Running";

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Event date")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the start time.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Start time must look like 06:30.")]
    [Display(Name = "Start time")]
    public string StartTime { get; set; } = "07:00";

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string Location { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Entry fee must be between 0 and 100000.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, 100000, ErrorMessage = "Maximum participants must be at least 1.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;

    [Required(ErrorMessage = "Please choose a status.")]
    [RegularExpression("^(Upcoming|Open|Closed|Completed|Cancelled)$",
        ErrorMessage = "Status must be Upcoming, Open, Closed, Completed or Cancelled.")]
    public string Status { get; set; } = "Open";

    // Kept so an update does not remove the existing banner.
    public string? BannerUrl { get; set; }

    [Display(Name = "Event banner image")]
    public IFormFile? Banner { get; set; }

    public bool IsEdit => EventId > 0;

    public static readonly string[] EventTypes = { "Running", "Walking", "Cycling" };
    public static readonly string[] Statuses = { "Upcoming", "Open", "Closed", "Completed", "Cancelled" };
}

public class OrganiserDashboardViewModel
{
    public List<EventSummaryDto> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int OpenEvents => Events.Count(e => e.Status == "Open");
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int PendingEnrolments => Events.Sum(e => e.PendingCount);
    public int ConfirmedEnrolments => Events.Sum(e => e.ConfirmedCount);
    public string? Error { get; set; }
}

public class ManageEventCategoriesViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> AllCategories { get; set; } = new();
    public HashSet<int> AssignedIds { get; set; } = new();
}

// =======================================================
// END: EVENT VIEW MODELS
// =======================================================
