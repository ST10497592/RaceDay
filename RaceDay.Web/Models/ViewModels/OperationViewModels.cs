using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models.ViewModels;

// =======================================================
// START: ENROLMENT, RESULT AND CATEGORY VIEW MODELS
// =======================================================

public class CategoryFormViewModel : IValidatableObject
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter the category name.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters.")]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
    public string? Description { get; set; }

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinimumAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaximumAge { get; set; }

    public bool IsEdit => CategoryId > 0;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinimumAge.HasValue && MaximumAge.HasValue && MinimumAge.Value > MaximumAge.Value)
        {
            yield return new ValidationResult(
                "Minimum age cannot be higher than maximum age.",
                new[] { nameof(MaximumAge) });
        }
    }
}

public class CategoryListViewModel
{
    public List<CategoryDto> Categories { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public string StatusFilter { get; set; } = string.Empty;
}

public class MyEnrolmentsViewModel
{
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class MyResultsViewModel
{
    public List<ResultDto> Results { get; set; } = new();
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    public int ResultId { get; set; }

    [Required(ErrorMessage = "Please choose the participant.")]
    [Display(Name = "Participant")]
    public int? EnrolmentId { get; set; }

    [Required(ErrorMessage = "Please choose a result status.")]
    [RegularExpression("^(Finished|DidNotFinish|Disqualified)$",
        ErrorMessage = "Result status must be Finished, DidNotFinish or Disqualified.")]
    [Display(Name = "Result status")]
    public string ResultStatus { get; set; } = "Finished";

    // Entered as h:mm:ss (for example 1:45:30) and parsed in the controller.
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Finish time must look like 1:45:30 (hours:minutes:seconds).")]
    [Display(Name = "Finish time (h:mm:ss)")]
    public string? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    public int? Position { get; set; }

    public bool IsEdit => ResultId > 0;

    public List<EnrolmentDto> Candidates { get; set; } = new();
}

public class EventResultsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
    public List<EnrolmentDto> WithoutResult { get; set; } = new();
}

// =======================================================
// END: ENROLMENT, RESULT AND CATEGORY VIEW MODELS
// =======================================================
