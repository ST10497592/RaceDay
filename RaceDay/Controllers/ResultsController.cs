
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers;

// =======================================================
// START: RESULTS CONTROLLER
// Purpose: Provides secure CRUD endpoints for race results.
// =======================================================

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    // START: CONSTRUCTOR
    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }
    // END: CONSTRUCTOR

    // START: GET ALL RESULTS
    [HttpGet]
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

    // START: GET RESULT BY ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result>> GetResult(int id)
    {
        var result = await _context.Results
            .AsNoTracking()
            .Include(r => r.Enrolment)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result == null)
            return NotFound();

        if (!User.IsInRole("Organiser"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            if (result.Enrolment == null ||
    result.Enrolment.ParticipantId != userId)
            {
                return Forbid();
            }
            
        }

        return Ok(result);
    }
    // END: GET RESULT BY ID

    // START: CREATE RESULT
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<Result>> CreateResult(Result result)
    {
        result.ResultId = 0;

        var enrolmentExists = await _context.Enrolments
            .AnyAsync(e => e.EnrolmentId == result.EnrolmentId);

        if (!enrolmentExists)
        {
            return BadRequest(new
            {
                message = "The specified enrolment does not exist."
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

        result.RecordedAt = DateTime.UtcNow;

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetResult),
            new { id = result.ResultId },
            result);
    }
    // END: CREATE RESULT

    // START: UPDATE RESULT
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateResult(int id, Result result)
    {
        if (id != result.ResultId)
        {
            return BadRequest(new
            {
                message = "The URL ID does not match the result ID."
            });
        }

        var existing = await _context.Results.FindAsync(id);

        if (existing == null)
            return NotFound();

        var enrolmentExists = await _context.Enrolments
            .AnyAsync(e => e.EnrolmentId == result.EnrolmentId);

        if (!enrolmentExists)
        {
            return BadRequest(new
            {
                message = "The specified enrolment does not exist."
            });
        }

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

        existing.EnrolmentId = result.EnrolmentId;
        existing.FinishTime = result.FinishTime;
        existing.Position = result.Position;
        existing.ResultStatus = result.ResultStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: UPDATE RESULT

    // START: DELETE RESULT
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteResult(int id)
    {
        var result = await _context.Results.FindAsync(id);

        if (result == null)
            return NotFound();

        _context.Results.Remove(result);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: DELETE RESULT
}

// =======================================================
// END: RESULTS CONTROLLER
// =======================================================
