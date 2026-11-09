using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Data;
using RaceDay_.Api.Dtos;
using RaceDay_.Api.Filters;
using RaceDay_.Api.Models;

namespace RaceDay_.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/categories")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Both authenticated roles can view categories for an event.
    [HttpGet]
    [SessionAuthorize]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == eventId);

        if (!eventExists)
        {
            return NotFound(new { message = "Event not found." });
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .Select(c => new
            {
                c.CategoryId,
                c.EventId,
                c.CategoryName,
                c.Description,
                c.CategoryType,
                c.MinimumAge,
                c.MaximumAge,
                c.DistanceKm,
                c.EntryFee,
                c.Capacity
            })
            .ToListAsync();

        return Ok(categories);
    }

    // Both authenticated roles can view an individual category.
    [HttpGet("{categoryId:int}")]
    [SessionAuthorize]
    public async Task<IActionResult> GetCategory(
        int eventId,
        int categoryId)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c =>
                c.EventId == eventId &&
                c.CategoryId == categoryId)
            .Select(c => new
            {
                c.CategoryId,
                c.EventId,
                c.CategoryName,
                c.Description,
                c.CategoryType,
                c.MinimumAge,
                c.MaximumAge,
                c.DistanceKm,
                c.EntryFee,
                c.Capacity
            })
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found for this event."
            });
        }

        return Ok(category);
    }

    // Only the Organiser who owns the event can create categories.
    [HttpPost]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        [FromBody] CategoryRequest request)
    {
        var validationError = ValidateCategoryRequest(request);

        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var organiserId = HttpContext.Session.GetInt32("UserId");

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e =>
                e.EventId == eventId &&
                e.OrganiserId == organiserId);

        if (eventItem == null)
        {
            return NotFound(new
            {
                message = "Event not found or you do not own this event."
            });
        }

        var categoryName = request.CategoryName.Trim();

        var duplicate = await _context.Categories.AnyAsync(c =>
            c.EventId == eventId &&
            c.CategoryName == categoryName);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "A category with this name already exists for this event."
            });
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = categoryName,
            Description = request.Description?.Trim(),
            CategoryType = request.CategoryType,
            MinimumAge = request.CategoryType == "Age"
                ? request.MinimumAge
                : null,
            MaximumAge = request.CategoryType == "Age"
                ? request.MaximumAge
                : null,
            DistanceKm = request.CategoryType == "Distance"
                ? request.DistanceKm
                : null,
            EntryFee = request.EntryFee,
            Capacity = request.Capacity
        };

        _context.Categories.Add(category);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The category could not be saved. Check for duplicate category names or invalid data."
            });
        }

        return CreatedAtAction(
            nameof(GetCategory),
            new
            {
                eventId,
                categoryId = category.CategoryId
            },
            new
            {
                category.CategoryId,
                category.EventId,
                category.CategoryName,
                category.Description,
                category.CategoryType,
                category.MinimumAge,
                category.MaximumAge,
                category.DistanceKm,
                category.EntryFee,
                category.Capacity
            });
    }

    // Only the Organiser who owns the event can update categories.
    [HttpPut("{categoryId:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateCategory(
        int eventId,
        int categoryId,
        [FromBody] CategoryRequest request)
    {
        var validationError = ValidateCategoryRequest(request);

        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var organiserId = HttpContext.Session.GetInt32("UserId");

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.EventId == eventId &&
                c.CategoryId == categoryId &&
                c.Event.OrganiserId == organiserId);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found or you do not own its event."
            });
        }

        var categoryName = request.CategoryName.Trim();

        var duplicate = await _context.Categories.AnyAsync(c =>
            c.EventId == eventId &&
            c.CategoryId != categoryId &&
            c.CategoryName == categoryName);

        if (duplicate)
        {
            return Conflict(new
            {
                message = "A category with this name already exists for this event."
            });
        }

        category.CategoryName = categoryName;
        category.Description = request.Description?.Trim();
        category.CategoryType = request.CategoryType;

        category.MinimumAge = request.CategoryType == "Age"
            ? request.MinimumAge
            : null;

        category.MaximumAge = request.CategoryType == "Age"
            ? request.MaximumAge
            : null;

        category.DistanceKm = request.CategoryType == "Distance"
            ? request.DistanceKm
            : null;

        category.EntryFee = request.EntryFee;
        category.Capacity = request.Capacity;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The category could not be updated because of a database constraint."
            });
        }

        return Ok(new
        {
            message = "Category updated successfully."
        });
    }

    // Only the Organiser who owns the event can delete categories.
    [HttpDelete("{categoryId:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> DeleteCategory(
        int eventId,
        int categoryId)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.EventId == eventId &&
                c.CategoryId == categoryId &&
                c.Event.OrganiserId == organiserId);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found or you do not own its event."
            });
        }

        _context.Categories.Remove(category);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "This category cannot be deleted because related enrolments exist."
            });
        }

        return NoContent();
    }

    private static string? ValidateCategoryRequest(
        CategoryRequest request)
    {
        if (request.CategoryType == "Age")
        {
            if (!request.MinimumAge.HasValue ||
                !request.MaximumAge.HasValue)
            {
                return "MinimumAge and MaximumAge are required for an Age category.";
            }

            if (request.MinimumAge > request.MaximumAge)
            {
                return "MinimumAge cannot exceed MaximumAge.";
            }

            if (request.DistanceKm.HasValue)
            {
                return "DistanceKm must not be supplied for an Age category.";
            }
        }
        else if (request.CategoryType == "Distance")
        {
            if (!request.DistanceKm.HasValue)
            {
                return "DistanceKm is required for a Distance category.";
            }

            if (request.MinimumAge.HasValue ||
                request.MaximumAge.HasValue)
            {
                return "MinimumAge and MaximumAge must not be supplied for a Distance category.";
            }
        }
        else
        {
            return "CategoryType must be Age or Distance.";
        }

        return null;
    }
}