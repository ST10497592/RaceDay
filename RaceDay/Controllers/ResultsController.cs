
using System.Security.Claims;
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
// START: RESULTS CONTROLLER
// Purpose: Provides secure endpoints for race results.
// =======================================================

/// <summary>Finish times and positions captured by organisers.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResultsController : ControllerBase
{
    private static readonly string[] ValidResultStatuses =
        { "Finished", "DidNotFinish", "Disqualified" };

    private readonly RaceDayDbContext _context;

    // START: CONSTRUCTOR
    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }
    // END: CONSTRUCTOR

    // START: GET ALL RESULTS
    /// <summary>Lists results. Organisers see all results; participants only their own.</summary>
    /// <remarks>Returns 200 or 401.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Result>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Result>>> GetResults()
    {
        var query = _context.Results
            .AsNoTracking()
            .Include(r => r.Enrolment)
            .AsQueryable();

        // Organisers can view all results.
        // Participants can only view their own results.
        if (!User.IsInRole("Organiser"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            query = query.Where(r =>
                r.Enrolment != null &&
                r.Enrolment.ParticipantId == userId);
        }

        return Ok(await query.ToListAsync());
    }
    // END: GET ALL RESULTS

    // START: GET MY RESULTS
    /// <summary>Returns the signed-in user's race history: event, date, category, finish time, position and total finishers.</summary>
    /// <remarks>Returns 200 (newest event first) or 401.</remarks>
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<ResultDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ResultDetailDto>>> GetMyResults()
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var participantId = userId.Value;

        var dtos = await ResultProjection.ToDtosAsync(
            _context,
            _context.Results.Where(r => r.Enrolment!.ParticipantId == participantId));

        return Ok(dtos.OrderByDescending(r => r.EventDate).ToList());
    }
    // END: GET MY RESULTS

    // START: GET RESULT BY ID
    /// <summary>Returns one result. Participants can only view their own.</summary>
    /// <remarks>Returns 200, 401, 403 or 404.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ResultDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDetailDto>> GetResult(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var dtos = await ResultProjection.ToDtosAsync(
            _context,
            _context.Results.Where(r => r.ResultId == id));

        var dto = dtos.FirstOrDefault();

        if (dto == null)
            return NotFound();

        if (!User.IsInRole("Organiser") && dto.ParticipantId != userId.Value)
            return Forbid();

        return Ok(dto);
    }
    // END: GET RESULT BY ID

    // START: CREATE RESULT
    /// <summary>Records a result for an enrolment of the organiser's event and marks the enrolment Completed.</summary>
    /// <remarks>
    /// Body: enrolmentId, resultStatus (Finished, DidNotFinish, Disqualified), and for finishers a
    /// finishTime (hh:mm:ss) and position. Returns 201, 400, 401, 403 (not the event's organiser),
    /// or 409 (result or position already recorded).
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDetailDto>> CreateResult(Result result)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        result.ResultId = 0;

        var validationError = NormaliseAndValidate(result);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == result.EnrolmentId);

        if (enrolment == null)
        {
            return BadRequest(new
            {
                message = "The specified enrolment does not exist."
            });
        }

        if (enrolment.Event == null || enrolment.Event.OrganiserId != userId.Value)
            return Forbid();

        if (enrolment.Status == "Cancelled")
        {
            return BadRequest(new
            {
                message = "A result cannot be recorded for a cancelled enrolment."
            });
        }

        var resultExists = await _context.Results
            .AnyAsync(r => r.EnrolmentId == result.EnrolmentId);

        if (resultExists)
        {
            return Conflict(new
            {
                message = "A result already exists for this enrolment."
            });
        }

        if (await PositionTakenAsync(enrolment.EventId, result.Position, 0))
        {
            return Conflict(new
            {
                message = "That finishing position has already been given to another participant."
            });
        }

        // Only the foreign key is used from the request body.
        result.Enrolment = null;
        result.RecordedAt = DateTime.UtcNow;

        enrolment.Status = "Completed";

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        var dtos = await ResultProjection.ToDtosAsync(
            _context,
            _context.Results.Where(r => r.ResultId == result.ResultId));

        return CreatedAtAction(
            nameof(GetResult),
            new { id = result.ResultId },
            dtos.First());
    }
    // END: CREATE RESULT

    // START: UPDATE RESULT
    /// <summary>Updates a result of the organiser's event.</summary>
    /// <remarks>Body: resultId (must match the URL), enrolmentId, resultStatus, finishTime, position. Returns 204, 400, 401, 403, 404 or 409.</remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateResult(int id, Result result)
    {
        if (id != result.ResultId)
        {
            return BadRequest(new
            {
                message = "The URL ID does not match the result ID."
            });
        }

        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var validationError = NormaliseAndValidate(result);

        if (validationError != null)
            return BadRequest(new { message = validationError });

        var existing = await _context.Results.FindAsync(id);

        if (existing == null)
            return NotFound();

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == result.EnrolmentId);

        if (enrolment == null)
        {
            return BadRequest(new
            {
                message = "The specified enrolment does not exist."
            });
        }

        if (enrolment.Event == null || enrolment.Event.OrganiserId != userId.Value)
            return Forbid();

        var duplicate = await _context.Results.AnyAsync(
            r => r.EnrolmentId == result.EnrolmentId &&
                 r.ResultId != id);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "A result already exists for this enrolment."
            });
        }

        if (await PositionTakenAsync(enrolment.EventId, result.Position, id))
        {
            return Conflict(new
            {
                message = "That finishing position has already been given to another participant."
            });
        }

        existing.EnrolmentId = result.EnrolmentId;
        existing.FinishTime = result.FinishTime;
        existing.Position = result.Position;
        existing.ResultStatus = result.ResultStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: UPDATE RESULT

    // START: DELETE RESULT
    /// <summary>Deletes a result of the organiser's event.</summary>
    /// <remarks>Returns 204, 401, 403 or 404.</remarks>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var userId = User.GetUserId();

        if (userId == null)
            return Unauthorized();

        var result = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result == null)
            return NotFound();

        var raceEvent = result.Enrolment?.Event;

        if (raceEvent == null || raceEvent.OrganiserId != userId.Value)
            return Forbid();

        _context.Results.Remove(result);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: DELETE RESULT

    // START: HELPERS
    // Validates the result and clears fields that do not apply to non-finishers.
    private static string? NormaliseAndValidate(Result result)
    {
        if (!ValidResultStatuses.Contains(result.ResultStatus))
            return "Result status must be Finished, DidNotFinish or Disqualified.";

        if (result.ResultStatus == "Finished")
        {
            if (result.FinishTime == null || result.FinishTime.Value <= TimeSpan.Zero)
                return "A finish time is required for finishers.";

            if (result.Position == null || result.Position.Value < 1)
                return "A finishing position (1 or higher) is required for finishers.";
        }
        else
        {
            result.FinishTime = null;
            result.Position = null;
        }

        return null;
    }

    private async Task<bool> PositionTakenAsync(int eventId, int? position, int excludeResultId)
    {
        if (position == null)
            return false;

        return await _context.Results.AnyAsync(r =>
            r.Enrolment!.EventId == eventId &&
            r.Position == position &&
            r.ResultId != excludeResultId);
    }
    // END: HELPERS
}

// =======================================================
// END: RESULTS CONTROLLER
// =======================================================
