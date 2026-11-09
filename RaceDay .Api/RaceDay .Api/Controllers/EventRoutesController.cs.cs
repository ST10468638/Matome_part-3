using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_.Api.Data;
using RaceDay_.Api.Dtos;
using RaceDay_.Api.Filters;
using RaceDay_.Api.Models;

namespace RaceDay_.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/route")]
public class EventRoutesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventRoutesController(RaceDayDbContext context)
    {
        _context = context;
    }

    // Authenticated users can view an event's route.
    [HttpGet]
    [SessionAuthorize]
    public async Task<IActionResult> GetEventRoute(int eventId)
    {
        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == eventId);

        if (!eventExists)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        var route = await _context.EventRoutes
            .AsNoTracking()
            .Where(r => r.EventId == eventId)
            .Select(r => new
            {
                r.EventRouteId,
                r.EventId,
                r.RouteName,
                r.DistanceKm,
                r.StartLocation,
                r.FinishLocation,
                r.RouteUrl,
                r.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (route == null)
        {
            return NotFound(new
            {
                message = "No route has been created for this event."
            });
        }

        return Ok(route);
    }

    // Only the event's Organiser can create its route.
    [HttpPost]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> CreateEventRoute(
        int eventId,
        [FromBody] EventRouteRequest request)
    {
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

        var routeAlreadyExists = await _context.EventRoutes
            .AnyAsync(r => r.EventId == eventId);

        if (routeAlreadyExists)
        {
            return Conflict(new
            {
                message = "A route already exists for this event."
            });
        }

        var route = new EventRoute
        {
            EventId = eventId,
            RouteName = request.RouteName.Trim(),
            DistanceKm = request.DistanceKm,
            StartLocation = request.StartLocation.Trim(),
            FinishLocation = request.FinishLocation.Trim(),
            RouteUrl = request.RouteUrl?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.EventRoutes.Add(route);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The route could not be saved. Check the event and route details."
            });
        }

        return CreatedAtAction(
            nameof(GetEventRoute),
            new { eventId },
            new
            {
                route.EventRouteId,
                route.EventId,
                route.RouteName,
                route.DistanceKm,
                route.StartLocation,
                route.FinishLocation,
                route.RouteUrl,
                route.CreatedAt
            });
    }

    // Only the event's Organiser can update its route.
    [HttpPut]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> UpdateEventRoute(
        int eventId,
        [FromBody] EventRouteRequest request)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var route = await _context.EventRoutes
            .FirstOrDefaultAsync(r =>
                r.EventId == eventId &&
                r.Event.OrganiserId == organiserId);

        if (route == null)
        {
            return NotFound(new
            {
                message = "Route not found or you do not own this event."
            });
        }

        route.RouteName = request.RouteName.Trim();
        route.DistanceKm = request.DistanceKm;
        route.StartLocation = request.StartLocation.Trim();
        route.FinishLocation = request.FinishLocation.Trim();
        route.RouteUrl = request.RouteUrl?.Trim();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The route could not be updated."
            });
        }

        return Ok(new
        {
            message = "Event route updated successfully."
        });
    }

    // Only the event's Organiser can delete its route.
    [HttpDelete]
    [SessionAuthorize("Organiser")]
    public async Task<IActionResult> DeleteEventRoute(int eventId)
    {
        var organiserId = HttpContext.Session.GetInt32("UserId");

        var route = await _context.EventRoutes
            .FirstOrDefaultAsync(r =>
                r.EventId == eventId &&
                r.Event.OrganiserId == organiserId);

        if (route == null)
        {
            return NotFound(new
            {
                message = "Route not found or you do not own this event."
            });
        }

        _context.EventRoutes.Remove(route);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}