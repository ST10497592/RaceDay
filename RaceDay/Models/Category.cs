
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models;

// =======================================================
// START: CATEGORY ENTITY
// Purpose: Represents a category for a race event.
// =======================================================

[Table("Categories")]
public class Category
{
    // START: PRIMARY KEY
    [Key]
    public int CategoryId { get; set; }
    // END: PRIMARY KEY

    // START: CATEGORY DETAILS
    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? MinimumAge { get; set; }

    public int? MaximumAge { get; set; }
    // END: CATEGORY DETAILS



    // START: EVENT RELATIONSHIPS
    public ICollection<Event> Events { get; set; } = new List<Event>();
    // END: EVENT RELATIONSHIPS

}

// =======================================================
// END: CATEGORY ENTITY
// =======================================================
