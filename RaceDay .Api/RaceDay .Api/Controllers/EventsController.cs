
using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Data;
using RaceDay_.Api.Dtos;
using RaceDay_.Api.Filters;
using RaceDay_.Api.Models;
using RaceDay_.Api.Services;

namespace RaceDay_.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly IConfiguration _configuration;

    public EventsController(
        RaceDayDbContext context,
        IBlobStorageService blobStorage,
        IConfiguration configuration)
    {
        _context = context;
        _blobStorage = blobStorage;
        _configuration = configuration;
    }

    [HttpGet]
    [SessionAuthorize]
    public async Task<IActionResult> GetEvents(
        CancellationToken cancellationToken)
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(ToResponse));
    }

    [HttpGet("mine")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> GetMyEvents(
        CancellationToken cancellationToken)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        var events = await _context.Events
            .AsNoTracking()
            .Where(e => e.OrganiserId == organiserId.Value)
            .OrderBy(e => e.EventDate)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [SessionAuthorize]
    public async Task<IActionResult> GetEvent(
        int id,
        CancellationToken cancellationToken)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.EventId == id,
                cancellationToken);

        if (eventItem is null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        return Ok(ToResponse(eventItem));
    }

    [HttpPost]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateEvent(
        [FromBody] EventRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RegistrationOpenDate >= request.RegistrationCloseDate ||
            request.RegistrationCloseDate > request.EventDate)
        {
            return BadRequest(new
            {
                message = "Registration must open before it closes, and registration must close on or before the event date."
            });
        }

        var organiserId = HttpContext.Session.GetInt32("UserId");

        if (organiserId is null)
        {
            return Unauthorized(new
            {
                message = "Authentication is required."
            });
        }

        var eventItem = new Event
        {
            OrganiserId = organiserId.Value,
            EventName = request.EventName.Trim(),
            Description = request.Description?.Trim(),
            EventType = request.EventType,
            DistanceKm = request.DistanceKm,
            EventDate = request.EventDate,
            Venue = request.Venue.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            RegistrationOpenDate = request.RegistrationOpenDate,
            RegistrationCloseDate = request.RegistrationCloseDate,
            MaxParticipants = request.MaxParticipants,
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventItem);

        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = eventItem.EventId },
            ToResponse(eventItem));
    }

    [HttpPut("{id:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] EventRequest request,
        CancellationToken cancellationToken)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var eventItem = await _context.Events.FirstOrDefaultAsync(
            e => e.EventId == id &&
                 e.OrganiserId == organiserId,
            cancellationToken);

        if (eventItem is null)
        {
            return NotFound(new
            {
                message = "Event not found or you do not own this event."
            });
        }

        if (request.RegistrationOpenDate >= request.RegistrationCloseDate ||
            request.RegistrationCloseDate > request.EventDate)
        {
            return BadRequest(new
            {
                message = "Registration must open before it closes, and registration must close on or before the event date."
            });
        }

        eventItem.EventName = request.EventName.Trim();
        eventItem.Description = request.Description?.Trim();
        eventItem.EventType = request.EventType;
        eventItem.DistanceKm = request.DistanceKm;
        eventItem.EventDate = request.EventDate;
        eventItem.Venue = request.Venue.Trim();
        eventItem.City = request.City.Trim();
        eventItem.Province = request.Province.Trim();
        eventItem.RegistrationOpenDate = request.RegistrationOpenDate;
        eventItem.RegistrationCloseDate = request.RegistrationCloseDate;
        eventItem.MaxParticipants = request.MaxParticipants;

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "Event updated successfully."
        });
    }

    [HttpDelete("{id:int}")]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> DeleteEvent(
        int id,
        CancellationToken cancellationToken)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var eventItem = await _context.Events.FirstOrDefaultAsync(
            e => e.EventId == id &&
                 e.OrganiserId == organiserId,
            cancellationToken);

        if (eventItem is null)
        {
            return NotFound(new
            {
                message = "Event not found or you do not own this event."
            });
        }

        _context.Events.Remove(eventItem);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "This event cannot be deleted because related records exist. Remove or manage those records first."
            });
        }

        await _blobStorage.DeleteIfExistsAsync(
            _configuration["AzureBlobStorage:EventBannerContainer"]
                ?? "event-banners",
            eventItem.BannerBlobName,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/banner")]
    [SessionAuthorize("Organiser")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageUploadValidator.MaximumFileSize + 64 * 1024)]
    public async Task<IActionResult> UploadBanner(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var eventItem = await _context.Events.FirstOrDefaultAsync(
            e => e.EventId == id &&
                 e.OrganiserId == organiserId,
            cancellationToken);

        if (eventItem is null)
        {
            return NotFound(new
            {
                message = "Event not found or you do not own this event."
            });
        }

        if (!await ImageUploadValidator.IsValidImageAsync(
                file,
                cancellationToken))
        {
            return BadRequest(new
            {
                message = "Choose a valid JPEG, PNG or WebP image up to 5 MB."
            });
        }

        var containerName =
            _configuration["AzureBlobStorage:EventBannerContainer"]
            ?? "event-banners";

        var oldBlobName = eventItem.BannerBlobName;

        string newBlobName;

        try
        {
            newBlobName = await _blobStorage.UploadAsync(
                containerName,
                file,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Azure Blob Storage is not configured. Set the API storage connection string before uploading a banner."
                });
        }
        catch (RequestFailedException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Azure Blob Storage could not store the banner. Check the storage account configuration."
                });
        }

        eventItem.BannerBlobName = newBlobName;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _blobStorage.DeleteIfExistsAsync(
                containerName,
                newBlobName,
                cancellationToken);

            throw;
        }

        await _blobStorage.DeleteIfExistsAsync(
            containerName,
            oldBlobName,
            cancellationToken);

        return Ok(new
        {
            bannerImageUrl = _blobStorage.GetReadUrl(
                containerName,
                newBlobName)
        });
    }

    private EventResponse ToResponse(Event eventItem)
    {
        var containerName =
            _configuration["AzureBlobStorage:EventBannerContainer"]
            ?? "event-banners";

        return new EventResponse
        {
            EventId = eventItem.EventId,
            OrganiserId = eventItem.OrganiserId,
            EventName = eventItem.EventName,
            Description = eventItem.Description,
            EventType = eventItem.EventType,
            DistanceKm = eventItem.DistanceKm,
            EventDate = eventItem.EventDate,
            Venue = eventItem.Venue,
            City = eventItem.City,
            Province = eventItem.Province,
            RegistrationOpenDate = eventItem.RegistrationOpenDate,
            RegistrationCloseDate = eventItem.RegistrationCloseDate,
            MaxParticipants = eventItem.MaxParticipants,
            CreatedAt = eventItem.CreatedAt,
            BannerImageUrl = _blobStorage.GetReadUrl(
                containerName,
                eventItem.BannerBlobName)
        };
    }
}
