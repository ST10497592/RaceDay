
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Extensions;
using RaceDay.Models;
using RaceDay.Services;

namespace RaceDay.Controllers;

// =======================================================
// START: EVENTS CONTROLLER
// Purpose: Provides CRUD endpoints for race events, their
// categories, banner upload, enrolments and results.
// =======================================================

/// <summary>Race events, their categories, enrolments and results.</summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    // START: CONSTRUCTOR
    public EventsController(
        RaceDayDbContext context,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }
    // END: CONSTRUCTOR

    // START: GET ALL EVENTS
    /// <summary>Lists events, optionally filtered. Public.</summary>
    /// <remarks>
    /// Optional query parameters: search (name, location or description), type
    /// (Running, Walking, Cycling), status, organiserId, upcoming (only future
    /// Open/Upcoming events), fromDate and toDate. Returns 200.
    /// </remarks>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<Event>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int? organiserId,
        [FromQuery] bool? upcoming,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var query = _context.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(e =>
                e.EventName.Contains(term) ||
                e.Location.Contains(term) ||
                (e.Description != null && e.Description.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var eventType = type.Trim();
            query = query.Where(e => e.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var eventStatus = status.Trim();
            query = query.Where(e => e.Status == eventStatus);
        }

        if (organiserId.HasValue)
        {
            var organiser = organiserId.Value;
            query = query.Where(e => e.OrganiserId == organiser);
        }

        if (upcoming == true)
        {
            var today = DateTime.UtcNow.Date;

            query = query.Where(e =>
                e.EventDate >= today &&
                (e.Status == "Open" || e.Status == "Upcoming"));
        }

        if (fromDate.HasValue)
        {
            var fromValue = fromDate.Value.Date;
            query = query.Where(e => e.EventDate >= fromValue);
        }

        if (toDate.HasValue)
        {
            var toValue = toDate.Value.Date;
            query = query.Where(e => e.EventDate <= toValue);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.StartTime)
            .ToListAsync();

        return Ok(events);
    }
    // END: GET ALL EVENTS

    // START: GET MY EVENTS
    /// <summary>Lists the signed-in organiser's events with enrolment totals (dashboard data).</summary>
    /// <remarks>Organiser only. Returns 200, 401 (not signed in) or 403 (not an Organiser).</remarks>
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EventSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetMyEvents()
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var organiserId = userId.Value;

        var events = await _context.Events
            .AsNoTracking()
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryDto
            {
                EventId = e.EventId,
                EventName = e.EventName,
                EventType = e.EventType,
                EventDate = e.EventDate,
                StartTime = e.StartTime,
                Location = e.Location,
                EntryFee = e.EntryFee,
                MaxParticipants = e.MaxParticipants,
                Status = e.Status,
                BannerUrl = e.BannerUrl,
                CategoryCount = e.Categories.Count,
                EnrolmentCount = _context.Enrolments
                    .Count(x => x.EventId == e.EventId),
                PendingCount = _context.Enrolments
                    .Count(x => x.EventId == e.EventId && x.Status == "Pending"),
                ConfirmedCount = _context.Enrolments
                    .Count(x => x.EventId == e.EventId && x.Status == "Confirmed")
            })
            .ToListAsync();

        return Ok(events);
    }
    // END: GET MY EVENTS

    // START: GET EVENT BY ID
    /// <summary>Returns one event with its categories. Public.</summary>
    /// <remarks>Returns 200, or 404 when the event does not exist.</remarks>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> GetEvent(int id)
    {
        var raceEvent = await _context.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent == null)
            return NotFound();

        return Ok(raceEvent);
    }
    // END: GET EVENT BY ID

    // START: CREATE EVENT
    /// <summary>Creates an event owned by the signed-in organiser.</summary>
    /// <remarks>
    /// Body: eventName, description, eventType (Running, Walking, Cycling), eventDate, startTime,
    /// location, entryFee, maxParticipants, status and optional bannerUrl. The organiser is always
    /// taken from the token. Returns 201, 400, 401 or 403.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Event>> CreateEvent(Event raceEvent)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        raceEvent.EventId = 0;
        raceEvent.OrganiserId = userId.Value;
        raceEvent.CreatedAt = DateTime.UtcNow;

        var organiserExists = await _context.Users
            .AnyAsync(u => u.UserId == raceEvent.OrganiserId
                        && u.Role == "Organiser"
                        && u.IsActive);

        if (!organiserExists)
        {
            return BadRequest(new
            {
                message = "The specified active organiser does not exist."
            });
        }

        var categoryIds = raceEvent.Categories
            .Select(c => c.CategoryId)
            .Distinct()
            .ToList();

        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToListAsync();

        if (categories.Count != categoryIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more category IDs are invalid."
            });
        }

        raceEvent.Categories = categories;
        _context.Events.Add(raceEvent);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = raceEvent.EventId },
            raceEvent);
    }
    // END: CREATE EVENT

    // START: UPDATE EVENT
    /// <summary>Updates an event owned by the signed-in organiser.</summary>
    /// <remarks>
    /// Body: the event fields (eventId must match the URL). Categories are managed with the
    /// /categories endpoints and are not changed here. Returns 204, 400, 401, 403 (not the owner) or 404.
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, Event raceEvent)
    {
        if (id != raceEvent.EventId)
        {
            return BadRequest(new
            {
                message = "The URL ID does not match the event ID."
            });
        }

        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var existingEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (existingEvent == null)
            return NotFound();

        if (existingEvent.OrganiserId != userId.Value)
            return Forbid();

        existingEvent.EventName = raceEvent.EventName;
        existingEvent.Description = raceEvent.Description;
        existingEvent.EventType = raceEvent.EventType;
        existingEvent.EventDate = raceEvent.EventDate;
        existingEvent.StartTime = raceEvent.StartTime;
        existingEvent.Location = raceEvent.Location;
        existingEvent.EntryFee = raceEvent.EntryFee;
        existingEvent.MaxParticipants = raceEvent.MaxParticipants;
        existingEvent.Status = raceEvent.Status;
        existingEvent.BannerUrl = raceEvent.BannerUrl;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: UPDATE EVENT

    // START: DELETE EVENT
    /// <summary>Deletes an event owned by the signed-in organiser (only when it has no enrolments).</summary>
    /// <remarks>Returns 204, 401, 403 (not the owner), 404 or 409 (event has enrolments).</remarks>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var raceEvent = await _context.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent == null)
            return NotFound();

        if (raceEvent.OrganiserId != userId.Value)
            return Forbid();

        var hasEnrolments = await _context.Enrolments
            .AnyAsync(e => e.EventId == id);

        if (hasEnrolments)
        {
            return Conflict(new
            {
                message = "This event cannot be deleted because it has enrolments."
            });
        }

        // Remove the event/category links first so the join rows are deleted too.
        raceEvent.Categories.Clear();
        _context.Events.Remove(raceEvent);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: DELETE EVENT

    // START: UPLOAD BANNER
    /// <summary>Uploads the event banner to Azure Blob Storage and saves its URL on the event.</summary>
    /// <remarks>
    /// Multipart form field "file" (JPG, PNG, GIF or WEBP, max 5 MB). Returns 200 with the URL,
    /// 400 invalid image, 401, 403 (not the owner), 404, or 503 if storage is unavailable.
    /// </remarks>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UploadResponse>> UploadBanner(int id, IFormFile file)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent == null)
            return NotFound();

        if (raceEvent.OrganiserId != userId.Value)
            return Forbid();

        var validationError = await ImageUploadValidator.ValidateAsync(file);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        try
        {
            var url = await _blobStorage.UploadImageAsync(file, "banners");

            raceEvent.BannerUrl = url;
            await _context.SaveChangesAsync();

            return Ok(new UploadResponse { Url = url });
        }
        catch (StorageUnavailableException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }
    // END: UPLOAD BANNER

    // START: EVENT CATEGORIES
    /// <summary>Lists the categories offered for an event. Public.</summary>
    /// <remarks>Returns 200, or 404 when the event does not exist.</remarks>
    [HttpGet("{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Category>>> GetEventCategories(int eventId)
    {
        var raceEvent = await _context.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (raceEvent == null)
            return NotFound();

        return Ok(raceEvent.Categories);
    }

    /// <summary>Assigns an existing category to an event owned by the signed-in organiser.</summary>
    /// <remarks>Returns 201, 401, 403 (not the owner), 404 (event or category) or 409 (already assigned).</remarks>
    [HttpPost("{eventId:int}/categories/{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignCategory(int eventId, int categoryId)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var raceEvent = await _context.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (raceEvent == null)
            return NotFound(new { message = "The event does not exist." });

        if (raceEvent.OrganiserId != userId.Value)
            return Forbid();

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category == null)
            return NotFound(new { message = "The category does not exist." });

        if (raceEvent.Categories.Any(c => c.CategoryId == categoryId))
        {
            return Conflict(new
            {
                message = "This category is already assigned to the event."
            });
        }

        raceEvent.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { message = "Category assigned to the event." });
    }

    /// <summary>Removes a category from an event owned by the signed-in organiser.</summary>
    /// <remarks>Returns 204, 401, 403, 404, or 409 when participants are already enrolled in that category.</remarks>
    [HttpDelete("{eventId:int}/categories/{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveCategory(int eventId, int categoryId)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var raceEvent = await _context.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (raceEvent == null)
            return NotFound(new { message = "The event does not exist." });

        if (raceEvent.OrganiserId != userId.Value)
            return Forbid();

        var category = raceEvent.Categories
            .FirstOrDefault(c => c.CategoryId == categoryId);

        if (category == null)
            return NotFound(new { message = "The category is not assigned to this event." });

        var inUse = await _context.Enrolments
            .AnyAsync(e => e.EventId == eventId && e.CategoryId == categoryId);

        if (inUse)
        {
            return Conflict(new
            {
                message = "This category cannot be removed because participants are enrolled in it."
            });
        }

        raceEvent.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: EVENT CATEGORIES

    // START: EVENT ENROLMENTS
    /// <summary>Lists all enrolments (with result information) for an event owned by the signed-in organiser.</summary>
    /// <remarks>Returns 200, 401, 403 (not the owner / not an Organiser) or 404.</remarks>
    [HttpGet("{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentDetailDto>>> GetEventEnrolments(int eventId)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var raceEvent = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (raceEvent == null)
            return NotFound();

        if (raceEvent.OrganiserId != userId.Value)
            return Forbid();

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment!.EventId == eventId)
            .ToDictionaryAsync(r => r.EnrolmentId);

        var dtos = new List<EnrolmentDetailDto>();

        foreach (var enrolment in enrolments)
        {
            results.TryGetValue(enrolment.EnrolmentId, out var result);
            dtos.Add(EnrolmentDetailDto.From(enrolment, result));
        }

        return Ok(dtos);
    }
    // END: EVENT ENROLMENTS

    // START: EVENT RESULTS
    /// <summary>Lists the published results of an event, best position first. Public.</summary>
    /// <remarks>Returns 200, or 404 when the event does not exist.</remarks>
    [HttpGet("{eventId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultDetailDto>>> GetEventResults(int eventId)
    {
        var exists = await _context.Events.AnyAsync(e => e.EventId == eventId);

        if (!exists)
            return NotFound();

        var dtos = await ResultProjection.ToDtosAsync(
            _context,
            _context.Results.Where(r => r.Enrolment!.EventId == eventId));

        return Ok(dtos
            .OrderBy(r => r.Position ?? int.MaxValue)
            .ThenBy(r => r.FinishTime)
            .ToList());
    }
    // END: EVENT RESULTS
}

// =======================================================
// END: EVENTS CONTROLLER
// =======================================================
