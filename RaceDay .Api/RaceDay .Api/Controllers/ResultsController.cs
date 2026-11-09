using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Data;
using RaceDay_.Api.Dtos;
using RaceDay_.Api.Filters;
using RaceDay_.Api.Models;

namespace RaceDay_.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Organisers can record a result only for an enrolment
    // belonging to an event they own.
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> RecordResult(
        int enrolmentId,
        [FromBody] ResultRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var enrolment = await _context.Enrolments
            .Include(e => e.Category)
            .ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e =>
                e.EnrolmentId == enrolmentId &&
                e.Category.Event.OrganiserId == organiserId);

        if (enrolment == null)
        {
            return NotFound(new
            {
                message = "Enrolment not found or you do not own its event."
            });
        }

        if (enrolment.Result != null)
        {
            return Conflict(new
            {
                message = "A result has already been recorded for this enrolment."
            });
        }

        if (request.ResultStatus == "Finished" &&
            request.FinishTime == null)
        {
            return BadRequest(new
            {
                message = "A finish time is required for a Finished result."
            });
        }

        if (request.FinishTime.HasValue &&
            request.FinishTime.Value < TimeSpan.Zero)
        {
            return BadRequest(new
            {
                message = "Finish time cannot be negative."
            });
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            OverallPosition = request.OverallPosition,
            CategoryPosition = request.CategoryPosition,
            ResultStatus = request.ResultStatus,
            RecordedAt = DateTime.UtcNow
        };

        _context.Results.Add(result);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The result could not be saved. Check whether a result already exists for this enrolment."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            result.ResultId,
            result.EnrolmentId,
            result.FinishTime,
            result.OverallPosition,
            result.CategoryPosition,
            result.ResultStatus,
            result.RecordedAt
        });
    }

    // Participants can view only their own results.
    [HttpGet("results/my")]
    [SessionAuthorize("Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        var participantId = HttpContext.Session.GetInt32("UserId");

        if (participantId == null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        var results = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == participantId.Value)
            .OrderByDescending(r => r.RecordedAt)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                r.FinishTime,
                r.OverallPosition,
                r.CategoryPosition,
                r.ResultStatus,
                r.RecordedAt,
                r.Enrolment.Category.CategoryName,
                EventName = r.Enrolment.Category.Event.EventName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                TotalParticipants = r.Enrolment.Category.Event.Categories
                    .SelectMany(c => c.Enrolments)
                    .Count(e => e.Status == "Confirmed")
            })
            .ToListAsync();

        return Ok(results);
    }
}