
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers;

// =======================================================
// START: ENROLMENTS CONTROLLER
// Purpose: Provides secure CRUD endpoints for enrolments.
// =======================================================

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    // START: CONSTRUCTOR
    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }
    // END: CONSTRUCTOR

    // START: GET ALL ENROLMENTS
    [HttpGet]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<IEnumerable<Enrolment>>> GetEnrolments()
    {
        return await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .ToListAsync();
    }
    // END: GET ALL ENROLMENTS

    // START: GET ENROLMENT BY ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Enrolment>> GetEnrolment(int id)
    {
        var enrolment = await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound();

        var isOrganiser = User.IsInRole("Organiser");
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!isOrganiser &&
            (!int.TryParse(userIdClaim, out var userId) ||
             enrolment.ParticipantId != userId))
        {
            return Forbid();
        }

        return Ok(enrolment);
    }
    // END: GET ENROLMENT BY ID

    // START: CREATE ENROLMENT
    [HttpPost]
    [Authorize(Roles = "Participant")]
    public async Task<ActionResult<Enrolment>> CreateEnrolment(
        Enrolment enrolment)
    {
        enrolment.EnrolmentId = 0;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        // The participant ID must come from the authenticated token.
        enrolment.ParticipantId = userId;

        var participantExists = await _context.Users.AnyAsync(
            u => u.UserId == userId &&
                 u.Role == "Participant" &&
                 u.IsActive);

        if (!participantExists)
        {
            return BadRequest(new
            {
                message = "The authenticated participant does not exist or is inactive."
            });
        }

        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == enrolment.EventId);

        if (!eventExists)
            return BadRequest(new { message = "The specified event does not exist." });

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == enrolment.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "The specified category does not exist." });

        var duplicate = await _context.Enrolments.AnyAsync(
            e => e.ParticipantId == userId &&
                 e.EventId == enrolment.EventId);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "This participant is already enrolled in this event."
            });
        }

        enrolment.EnrolmentDate = DateTime.UtcNow;

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEnrolment),
            new { id = enrolment.EnrolmentId },
            enrolment);
    }
    // END: CREATE ENROLMENT

    // START: UPDATE ENROLMENT
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEnrolment(
        int id,
        Enrolment enrolment)
    {
        if (id != enrolment.EnrolmentId)
        {
            return BadRequest(new
            {
                message = "The URL ID does not match the enrolment ID."
            });
        }

        var existing = await _context.Enrolments.FindAsync(id);

        if (existing == null)
            return NotFound();

        var participantExists = await _context.Users.AnyAsync(
            u => u.UserId == enrolment.ParticipantId &&
                 u.Role == "Participant");

        if (!participantExists)
            return BadRequest(new { message = "The specified participant does not exist." });

        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == enrolment.EventId);

        if (!eventExists)
            return BadRequest(new { message = "The specified event does not exist." });

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == enrolment.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "The specified category does not exist." });

        var duplicate = await _context.Enrolments.AnyAsync(
            e => e.EnrolmentId != id &&
                 e.ParticipantId == enrolment.ParticipantId &&
                 e.EventId == enrolment.EventId);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "This participant is already enrolled in this event."
            });
        }

        existing.EventId = enrolment.EventId;
        existing.ParticipantId = enrolment.ParticipantId;
        existing.CategoryId = enrolment.CategoryId;
        existing.Status = enrolment.Status;
        existing.PaymentStatus = enrolment.PaymentStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: UPDATE ENROLMENT

    // START: DELETE ENROLMENT
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEnrolment(int id)
    {
        var enrolment = await _context.Enrolments.FindAsync(id);

        if (enrolment == null)
            return NotFound();

        var hasResult = await _context.Results
            .AnyAsync(r => r.EnrolmentId == id);

        if (hasResult)
        {
            return Conflict(new
            {
                message = "This enrolment cannot be deleted because it has a result."
            });
        }

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: DELETE ENROLMENT
}

// =======================================================
// END: ENROLMENTS CONTROLLER
// =======================================================
