using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Data;
using RaceDay_.Api.Dtos;
using RaceDay_.Api.Filters;
using RaceDay_.Api.Models;

namespace RaceDay_.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // A Participant enrols using their authenticated session.
    [HttpPost]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> CreateEnrolment(
        [FromBody] EnrolmentRequest request)
    {
        var participantId = HttpContext.Session.GetInt32("UserId");

        if (participantId == null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);

        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        var now = DateTime.UtcNow;

        if (now < category.Event.RegistrationOpenDate ||
            now > category.Event.RegistrationCloseDate)
        {
            return BadRequest(new
            {
                message = "Registration for this event is not currently open."
            });
        }

        var existingEnrolment = await _context.Enrolments
            .AnyAsync(e =>
                e.ParticipantId == participantId.Value &&
                e.CategoryId == request.CategoryId);

        if (existingEnrolment)
        {
            return Conflict(new
            {
                message = "You are already enrolled in this category."
            });
        }

        var confirmedCount = await _context.Enrolments
            .CountAsync(e =>
                e.CategoryId == request.CategoryId &&
                e.Status == "Confirmed");

        if (confirmedCount >= category.Capacity)
        {
            return Conflict(new
            {
                message = "This category has reached its capacity."
            });
        }

        var eventEnrolmentCount = await _context.Enrolments
            .CountAsync(e =>
                e.Category.EventId == category.EventId &&
                e.Status == "Confirmed");

        if (category.Event.MaxParticipants.HasValue &&
            eventEnrolmentCount >= category.Event.MaxParticipants.Value)
        {
            return Conflict(new
            {
                message = "This event has reached its maximum participant limit."
            });
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId.Value,
            CategoryId = request.CategoryId,
            EnrolmentDate = now,
            Status = "Confirmed"
        };

        _context.Enrolments.Add(enrolment);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The enrolment could not be saved. Check whether you are already enrolled."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            enrolment.EnrolmentId,
            enrolment.ParticipantId,
            enrolment.CategoryId,
            enrolment.EnrolmentDate,
            enrolment.Status,
            enrolment.BibNumber
        });
    }

    // Participants can view only their own enrolments.
    [HttpGet("my")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var participantId = HttpContext.Session.GetInt32("UserId");

        if (participantId == null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.ParticipantId == participantId.Value)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.ParticipantId,
                e.CategoryId,
                e.Category.CategoryName,
                e.Category.EventId,
                EventName = e.Category.Event.EventName,
                e.EnrolmentDate,
                e.Status,
                e.BibNumber
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    // An Organiser can view enrolments only for events they own.
    [HttpGet("/api/events/{eventId:int}/enrolments")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var eventExists = await _context.Events
            .AnyAsync(e =>
                e.EventId == eventId &&
                e.OrganiserId == organiserId);

        if (!eventExists)
        {
            return NotFound(new
            {
                message = "Event not found or you do not own this event."
            });
        }

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.ParticipantId,
                ParticipantName =
                    e.Participant.FirstName + " " +
                    e.Participant.LastName,
                e.CategoryId,
                e.Category.CategoryName,
                e.EnrolmentDate,
                e.Status,
                e.BibNumber,
                ResultId = e.Result == null ? (int?)null : e.Result.ResultId,
                ResultStatus = e.Result == null ? null : e.Result.ResultStatus,
                FinishTime = e.Result == null ? (TimeSpan?)null : e.Result.FinishTime,
                OverallPosition = e.Result == null ? (int?)null : e.Result.OverallPosition
            })
            .ToListAsync();

        return Ok(enrolments);
    }
}