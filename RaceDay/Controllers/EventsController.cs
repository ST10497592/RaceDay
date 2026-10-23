
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers;

// =======================================================
// START: EVENTS CONTROLLER
// Purpose: Provides CRUD endpoints for race events.
// =======================================================

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    // START: CONSTRUCTOR
    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }
    // END: CONSTRUCTOR

    // START: GET ALL EVENTS
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        return await _context.Events
            .AsNoTracking()
            .Include(e => e.Categories)
            .ToListAsync();
    }
    // END: GET ALL EVENTS

    // START: GET EVENT BY ID
    [HttpGet("{id:int}")]
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
    [HttpPost]
    public async Task<ActionResult<Event>> CreateEvent(Event raceEvent)
    {
        raceEvent.EventId = 0;

        var organiserExists = await _context.Users
            .AnyAsync(u => u.UserId == raceEvent.OrganiserId);

        if (!organiserExists)
            return BadRequest(new { message = "The specified organiser does not exist." });

        var categoryIds = raceEvent.Categories
            .Select(c => c.CategoryId)
            .Distinct()
            .ToList();

        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToListAsync();

        if (categories.Count != categoryIds.Count)
            return BadRequest(new { message = "One or more category IDs are invalid." });

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
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEvent(int id, Event raceEvent)
    {
        if (id != raceEvent.EventId)
            return BadRequest(new { message = "The URL ID does not match the event ID." });

        var existingEvent = await _context.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (existingEvent == null)
            return NotFound();

        var organiserExists = await _context.Users
            .AnyAsync(u => u.UserId == raceEvent.OrganiserId);

        if (!organiserExists)
            return BadRequest(new { message = "The specified organiser does not exist." });

        var categoryIds = raceEvent.Categories
            .Select(c => c.CategoryId)
            .Distinct()
            .ToList();

        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.CategoryId))
            .ToListAsync();

        if (categories.Count != categoryIds.Count)
            return BadRequest(new { message = "One or more category IDs are invalid." });

        existingEvent.OrganiserId = raceEvent.OrganiserId;
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
        existingEvent.Categories = categories;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: UPDATE EVENT

    // START: DELETE EVENT
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var raceEvent = await _context.Events.FindAsync(id);

        if (raceEvent == null)
            return NotFound();

        var hasEnrolments = await _context.Enrolments
            .AnyAsync(e => e.EventId == id);

        if (hasEnrolments)
        {
            return Conflict(new
            {
                message = "This event cannot be deleted because it has enrolments."
            });
        }

        _context.Events.Remove(raceEvent);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: DELETE EVENT
}

// =======================================================
// END: EVENTS CONTROLLER
// =======================================================
