using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RequireRole("Organiser")]
public sealed class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var eventsResult = await _api.GetAsync<List<EventDto>>("api/events/mine", cancellationToken);
        if (!eventsResult.IsSuccess)
        {
            TempData["ErrorMessage"] = eventsResult.Message;
            return View(new List<EventSummaryViewModel>());
        }

        var summaries = new List<EventSummaryViewModel>();
        foreach (var eventItem in eventsResult.Data ?? new List<EventDto>())
        {
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>(
                $"api/events/{eventItem.EventId}/enrolments", cancellationToken);
            summaries.Add(new EventSummaryViewModel
            {
                Event = eventItem,
                TotalEnrolments = enrolments.IsSuccess ? enrolments.Data?.Count ?? 0 : 0
            });
        }

        return View(summaries.OrderBy(item => item.Event.EventDate).ToList());
    }

    public async Task<IActionResult> Events(CancellationToken cancellationToken)
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine", cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
            return View(new List<EventDto>());
        }

        return View(result.Data ?? new List<EventDto>());
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var created = await _api.PostAsync<EventDto>("api/events", ToEventRequest(model), cancellationToken);
        if (!created.IsSuccess || created.Data is null)
        {
            ModelState.AddModelError(string.Empty, created.Message ?? "The event could not be created.");
            return View("EventForm", model);
        }

        if (model.BannerImage is not null)
        {
            var imageResult = await _api.PostFileAsync<object>(
                $"api/events/{created.Data.EventId}/banner", model.BannerImage, cancellationToken);
            if (!imageResult.IsSuccess)
            {
                TempData["WarningMessage"] = $"The event was created, but the banner could not be uploaded: {imageResult.Message}";
            }
        }

        TempData["SuccessMessage"] ??= "Event created successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id, CancellationToken cancellationToken)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (!result.IsSuccess || result.Data is null || result.Data.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = result.Message ?? "The event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        var item = result.Data;
        return View("EventForm", new EventFormViewModel
        {
            EventId = item.EventId,
            EventName = item.EventName,
            Description = item.Description ?? string.Empty,
            EventType = item.EventType,
            DistanceKm = item.DistanceKm,
            EventDate = item.EventDate,
            Venue = item.Venue,
            City = item.City,
            Province = item.Province,
            RegistrationOpenDate = item.RegistrationOpenDate,
            RegistrationCloseDate = item.RegistrationCloseDate,
            MaxParticipants = item.MaxParticipants,
            CurrentBannerImageUrl = item.BannerImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.EventId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var updated = await _api.PutAsync<object>($"api/events/{id}", ToEventRequest(model), cancellationToken);
        if (!updated.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, updated.Message ?? "The event could not be updated.");
            return View("EventForm", model);
        }

        if (model.BannerImage is not null)
        {
            var imageResult = await _api.PostFileAsync<object>(
                $"api/events/{id}/banner", model.BannerImage, cancellationToken);
            if (!imageResult.IsSuccess)
            {
                TempData["WarningMessage"] = $"The event details were saved, but the banner could not be uploaded: {imageResult.Message}";
            }
        }

        TempData["SuccessMessage"] ??= "Event updated successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken cancellationToken)
    {
        var result = await _api.DeleteAsync<object>($"api/events/{id}", cancellationToken);
        SetResultMessage(result.IsSuccess, result.Message, "Event deleted successfully.");
        return RedirectToAction(nameof(Events));
    }

    public async Task<IActionResult> Categories(int eventId, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}", cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null || eventResult.Data.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = eventResult.Message ?? "The event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        var categoriesResult = await _api.GetAsync<List<CategoryDto>>(
            $"api/events/{eventId}/categories", cancellationToken);
        ViewBag.Event = eventResult.Data;
        if (!categoriesResult.IsSuccess)
        {
            TempData["ErrorMessage"] = categoriesResult.Message;
        }

        return View(categoriesResult.Data ?? new List<CategoryDto>());
    }

    [HttpGet]
    public async Task<IActionResult> CreateCategory(int eventId, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}", cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null || eventResult.Data.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = eventResult.Message ?? "The event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        ViewBag.EventName = eventResult.Data.EventName;
        return View("CategoryForm", new CategoryFormViewModel { EventId = eventId });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        var result = await _api.PostAsync<CategoryDto>(
            $"api/events/{model.EventId}/categories", ToCategoryRequest(model), cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "The category could not be saved.");
            return View("CategoryForm", model);
        }

        TempData["SuccessMessage"] = "Category added successfully.";
        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}", cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null ||
            eventResult.Data.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = eventResult.Message ?? "The event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        var result = await _api.GetAsync<CategoryDto>(
            $"api/events/{eventId}/categories/{categoryId}", cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = result.Message ?? "The category could not be found.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        var item = result.Data;
        return View("CategoryForm", new CategoryFormViewModel
        {
            EventId = eventId,
            CategoryId = item.CategoryId,
            CategoryName = item.CategoryName,
            Description = item.Description,
            CategoryType = item.CategoryType,
            MinimumAge = item.MinimumAge,
            MaximumAge = item.MaximumAge,
            DistanceKm = item.DistanceKm,
            EntryFee = item.EntryFee,
            Capacity = item.Capacity ?? 100
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(int eventId, int categoryId, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (categoryId != model.CategoryId || eventId != model.EventId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        var result = await _api.PutAsync<object>(
            $"api/events/{eventId}/categories/{categoryId}", ToCategoryRequest(model), cancellationToken);
        SetResultMessage(result.IsSuccess, result.Message, "Category updated successfully.");
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        var result = await _api.DeleteAsync<object>(
            $"api/events/{eventId}/categories/{categoryId}", cancellationToken);
        SetResultMessage(result.IsSuccess, result.Message, "Category deleted successfully.");
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    public async Task<IActionResult> Enrolments(int eventId, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}", cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null || eventResult.Data.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = eventResult.Message ?? "The event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        var result = await _api.GetAsync<List<EnrolmentDto>>(
            $"api/events/{eventId}/enrolments", cancellationToken);
        ViewBag.Event = eventResult.Data;
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return View(result.Data ?? new List<EnrolmentDto>());
    }

    public async Task<IActionResult> Results(int eventId, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}", cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null || eventResult.Data.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = eventResult.Message ?? "The event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        var result = await _api.GetAsync<List<EnrolmentDto>>(
            $"api/events/{eventId}/enrolments", cancellationToken);
        ViewBag.Event = eventResult.Data;
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        return View(result.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> RecordResult(ResultFormViewModel model, int eventId, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{eventId}", cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null ||
            eventResult.Data.OrganiserId != HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = eventResult.Message ?? "The event could not be found.";
            return RedirectToAction(nameof(Events));
        }

        if (eventResult.Data.EventDate > DateTime.Now)
        {
            TempData["ErrorMessage"] = "Results can only be recorded after the event has concluded.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>(
            $"api/events/{eventId}/enrolments", cancellationToken);
        if (!enrolments.IsSuccess || enrolments.Data is null ||
            !enrolments.Data.Any(item => item.EnrolmentId == model.EnrolmentId))
        {
            TempData["ErrorMessage"] = enrolments.Message ?? "The selected participant is not enrolled in this event.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Check the result details and try again.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        var result = await _api.PostAsync<object>(
            $"api/enrolments/{model.EnrolmentId}/result",
            new
            {
                model.FinishTime,
                model.OverallPosition,
                model.CategoryPosition,
                model.ResultStatus
            },
            cancellationToken);
        SetResultMessage(result.IsSuccess, result.Message, "Result recorded successfully.");
        return RedirectToAction(nameof(Results), new { eventId });
    }

    private static object ToEventRequest(EventFormViewModel model)
    {
        return new
        {
            model.EventName,
            model.Description,
            model.EventType,
            model.DistanceKm,
            model.EventDate,
            model.Venue,
            model.City,
            model.Province,
            model.RegistrationOpenDate,
            model.RegistrationCloseDate,
            model.MaxParticipants
        };
    }

    private static object ToCategoryRequest(CategoryFormViewModel model)
    {
        return new
        {
            model.CategoryName,
            model.Description,
            model.CategoryType,
            MinimumAge = model.CategoryType == "Age" ? model.MinimumAge : null,
            MaximumAge = model.CategoryType == "Age" ? model.MaximumAge : null,
            DistanceKm = model.CategoryType == "Distance" ? model.DistanceKm : null,
            model.EntryFee,
            model.Capacity
        };
    }

    private void SetResultMessage(bool success, string? message, string successMessage)
    {
        if (success)
        {
            TempData["SuccessMessage"] = successMessage;
        }
        else
        {
            TempData["ErrorMessage"] = message ?? "The action could not be completed.";
        }
    }
}
