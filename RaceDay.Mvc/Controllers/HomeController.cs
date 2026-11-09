using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class HomeController : Controller
{
    private readonly RaceDayApiClient _api;

    public HomeController(RaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? search, string? eventType, CancellationToken cancellationToken)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role is null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (role == "Organiser")
        {
            return RedirectToAction("Index", "Organiser");
        }

        var result = await _api.GetAsync<List<EventDto>>("api/events", cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(new List<EventDto>());
        }

        var events = (result.Data ?? new List<EventDto>())
            .Where(item => item.EventDate.Date >= DateTime.Now.Date);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            events = events.Where(item =>
                item.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.City.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Venue.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            events = events.Where(item => string.Equals(
                item.EventType, eventType, StringComparison.OrdinalIgnoreCase));
        }

        ViewBag.Search = search;
        ViewBag.EventType = eventType;
        return View(events.OrderBy(item => item.EventDate).ToList());
    }

    public IActionResult Error()
    {
        return View();
    }
}
