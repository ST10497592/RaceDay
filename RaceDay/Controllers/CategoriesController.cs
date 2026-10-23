
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers;

// =======================================================
// START: CATEGORIES CONTROLLER
// Purpose: Provides CRUD endpoints for event categories.
// =======================================================

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    // START: CONSTRUCTOR
    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }
    // END: CONSTRUCTOR

    // START: GET ALL CATEGORIES
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        return await _context.Categories
            .AsNoTracking()
            .ToListAsync();
    }
    // END: GET ALL CATEGORIES

    // START: GET CATEGORY BY ID
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Category>> GetCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        return Ok(category);
    }
    // END: GET CATEGORY BY ID

    // START: CREATE CATEGORY
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        category.CategoryId = 0;

        _context.Categories.Add(category);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Unable to create the category. Check that its name is unique."
            });
        }

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.CategoryId },
            category);
    }
    // END: CREATE CATEGORY

    // START: UPDATE CATEGORY
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateCategory(int id, Category category)
    {
        if (id != category.CategoryId)
        {
            return BadRequest(new
            {
                message = "The ID in the URL does not match the category ID."
            });
        }

        var existingCategory = await _context.Categories.FindAsync(id);

        if (existingCategory == null)
            return NotFound();

        existingCategory.CategoryName = category.CategoryName;
        existingCategory.Description = category.Description;
        existingCategory.MinimumAge = category.MinimumAge;
        existingCategory.MaximumAge = category.MaximumAge;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Unable to update the category. Check that its name is unique."
            });
        }

        return NoContent();
    }
    // END: UPDATE CATEGORY

    // START: DELETE CATEGORY
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound();

        var hasEvents = await _context.Events
            .AnyAsync(e => e.Categories.Any(c => c.CategoryId == id));

        var hasEnrolments = await _context.Enrolments
            .AnyAsync(e => e.CategoryId == id);

        if (hasEvents || hasEnrolments)
        {
            return Conflict(new
            {
                message = "This category cannot be deleted because it is linked to events or enrolments."
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    // END: DELETE CATEGORY
}

// =======================================================
// END: CATEGORIES CONTROLLER
// =======================================================
