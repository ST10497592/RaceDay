
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.DTOs;
using RaceDay.Extensions;
using RaceDay.Models;

namespace RaceDay.Controllers;

// =======================================================
// START: ENROLMENTS CONTROLLER
// Purpose: Provides secure endpoints for event enrolments.
// =======================================================

/// <summary>Participant enrolments in events.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private static readonly string[] ValidStatuses =
        { "Pending", "Confirmed", "Cancelled", "Completed" };

    private static readonly string[] ValidPaymentStatuses =
        { "Pending", "Paid", "Failed", "Refunded" };

    private readonly RaceDayDbContext _context;

    // START: CONSTRUCTOR
    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }
    // END: CONSTRUCTOR

    // START: GET ALL ENROLMENTS
    /// <summary>Lists every enrolment for the signed-in organiser's events.</summary>
    /// <remarks>Organiser only. Returns 200, 401 or 403.</remarks>
    [HttpGet]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentDetailDto>>> GetEnrolments()
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var organiserId = userId.Value;

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Where(e => e.Event!.OrganiserId == organiserId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => EnrolmentDetailDto.From(e)).ToList());
    }
    // END: GET ALL ENROLMENTS

    // START: GET MY ENROLMENTS
    /// <summary>Lists the signed-in user's own enrolments, newest event first.</summary>
    /// <remarks>Returns 200 (with result information when recorded) or 401.</remarks>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<EnrolmentDetailDto>>> GetMyEnrolments()
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var participantId = userId.Value;

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.Event!.EventDate)
            .ToListAsync();

        var enrolmentIds = enrolments.Select(e => e.EnrolmentId).ToList();

        var results = await _context.Results
            .AsNoTracking()
            .Where(r => enrolmentIds.Contains(r.EnrolmentId))
            .ToDictionaryAsync(r => r.EnrolmentId);

        var dtos = new List<EnrolmentDetailDto>();

        foreach (var enrolment in enrolments)
        {
            results.TryGetValue(enrolment.EnrolmentId, out var result);
            dtos.Add(EnrolmentDetailDto.From(enrolment, result));
        }

        return Ok(dtos);
    }
    // END: GET MY ENROLMENTS

    // START: GET ENROLMENT BY ID
    /// <summary>Returns one enrolment. Participants can only see their own; organisers only for their events.</summary>
    /// <remarks>Returns 200, 401, 403 or 404.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EnrolmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentDetailDto>> GetEnrolment(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var enrolment = await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound();

        if (!CanAccess(enrolment, userId.Value))
            return Forbid();

        var result = await _context.Results
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EnrolmentId == id);

        return Ok(EnrolmentDetailDto.From(enrolment, result));
    }
    // END: GET ENROLMENT BY ID

    // START: CREATE ENROLMENT
    /// <summary>Enrols the signed-in participant in an event under a chosen category.</summary>
    /// <remarks>
    /// Body: eventId, categoryId (status and paymentStatus are always recorded as Pending until
    /// the organiser confirms). The participant is taken from the token. Returns 201, 400 (event
    /// closed or category not offered), 401, 403 (not a Participant), 404 or 409 (already enrolled or event full).
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDetailDto>> CreateEnrolment(
        Enrolment enrolment)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        enrolment.EnrolmentId = 0;

        // The participant ID must come from the authenticated token.
        enrolment.ParticipantId = userId.Value;

        var participantExists = await _context.Users.AnyAsync(
            u => u.UserId == userId.Value &&
                 u.Role == "Participant" &&
                 u.IsActive);

        if (!participantExists)
        {
            return BadRequest(new
            {
                message = "The authenticated participant does not exist or is inactive."
            });
        }

        var raceEvent = await _context.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == enrolment.EventId);

        if (raceEvent == null)
            return NotFound(new { message = "The specified event does not exist." });

        if (raceEvent.Status != "Open")
        {
            return BadRequest(new
            {
                message = "This event is not open for enrolment."
            });
        }

        if (raceEvent.EventDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new
            {
                message = "This event has already taken place."
            });
        }

        if (!raceEvent.Categories.Any(c => c.CategoryId == enrolment.CategoryId))
        {
            return BadRequest(new
            {
                message = "The selected category is not offered for this event."
            });
        }

        var duplicate = await _context.Enrolments.AnyAsync(
            e => e.ParticipantId == userId.Value &&
                 e.EventId == enrolment.EventId);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "This participant is already enrolled in this event."
            });
        }

        var activeEnrolments = await _context.Enrolments.CountAsync(
            e => e.EventId == raceEvent.EventId && e.Status != "Cancelled");

        if (activeEnrolments >= raceEvent.MaxParticipants)
        {
            return Conflict(new
            {
                message = "This event is full."
            });
        }

        // Participants cannot confirm or pay for themselves: the organiser does that.
        enrolment.Status = "Pending";
        enrolment.PaymentStatus = "Pending";
        enrolment.EnrolmentDate = DateTime.UtcNow;

        // Only the foreign keys are used from the request body.
        enrolment.Event = null;
        enrolment.Participant = null;
        enrolment.Category = null;

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var saved = await _context.Enrolments
            .AsNoTracking()
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);

        return CreatedAtAction(
            nameof(GetEnrolment),
            new { id = saved.EnrolmentId },
            EnrolmentDetailDto.From(saved));
    }
    // END: CREATE ENROLMENT

    // START: UPDATE ENROLMENT STATUS
    /// <summary>Lets the organiser of the event confirm/cancel an enrolment and record its payment state.</summary>
    /// <remarks>
    /// Body: status (Pending, Confirmed, Cancelled, Completed) and paymentStatus (Pending, Paid, Failed, Refunded).
    /// Returns 200, 400, 401, 403 (not the event's organiser) or 404.
    /// </remarks>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EnrolmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentDetailDto>> UpdateEnrolmentStatus(
        int id,
        UpdateEnrolmentStatusRequest request)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound();

        if (enrolment.Event == null || enrolment.Event.OrganiserId != userId.Value)
            return Forbid();

        enrolment.Status = request.Status;
        enrolment.PaymentStatus = request.PaymentStatus;

        await _context.SaveChangesAsync();

        return Ok(EnrolmentDetailDto.From(enrolment));
    }
    // END: UPDATE ENROLMENT STATUS

    // START: UPDATE ENROLMENT
    /// <summary>Replaces an enrolment's event, participant, category and statuses (organiser of the event only).</summary>
    /// <remarks>Returns 204, 400, 401, 403, 404 or 409 (participant already enrolled in that event).</remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        if (!ValidStatuses.Contains(enrolment.Status) ||
            !ValidPaymentStatuses.Contains(enrolment.PaymentStatus))
        {
            return BadRequest(new
            {
                message = "The enrolment status or payment status is not valid."
            });
        }

        var existing = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (existing == null)
            return NotFound();

        if (existing.Event == null || existing.Event.OrganiserId != userId.Value)
            return Forbid();

        var participantExists = await _context.Users.AnyAsync(
            u => u.UserId == enrolment.ParticipantId &&
                 u.Role == "Participant");

        if (!participantExists)
            return BadRequest(new { message = "The specified participant does not exist." });

        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == enrolment.EventId &&
                           e.OrganiserId == userId.Value);

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
    /// <summary>Cancels (removes) an enrolment. Participants can cancel their own; organisers those of their events.</summary>
    /// <remarks>Returns 204, 401, 403, 404 or 409 (a result already exists for the enrolment).</remarks>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEnrolment(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound();

        if (!CanAccess(enrolment, userId.Value))
            return Forbid();

        var hasResult = await _context.Results
            .AnyAsync(r => r.EnrolmentId == id);

        if (hasResult)
        {
            return Conflict(new
            {
                message = "This enrolment cannot be cancelled because it has a result."
            });
        }

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: DELETE ENROLMENT

    // Participants may access their own enrolments; organisers those of their own events.
    private bool CanAccess(Enrolment enrolment, int userId)
    {
        if (User.IsInRole("Organiser"))
            return enrolment.Event != null && enrolment.Event.OrganiserId == userId;

        return enrolment.ParticipantId == userId;
    }
}

// =======================================================
// END: ENROLMENTS CONTROLLER
// =======================================================
