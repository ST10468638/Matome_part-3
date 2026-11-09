using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RequireRole("Participant")]
public sealed class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var eventResult = await _api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null)
        {
            TempData["ErrorMessage"] = eventResult.Message ?? "The event could not be found.";
            return RedirectToAction("Index", "Home");
        }

        var categoriesResult = await _api.GetAsync<List<CategoryDto>>(
            $"api/events/{id}/categories", cancellationToken);

        ViewBag.Categories = categoriesResult.Data ?? new List<CategoryDto>();
        ViewBag.CategoriesMessage = categoriesResult.Message;
        return View(eventResult.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        var category = await _api.GetAsync<CategoryDto>(
            $"api/events/{eventId}/categories/{categoryId}", cancellationToken);
        if (!category.IsSuccess || category.Data is null || category.Data.EventId != eventId)
        {
            TempData["ErrorMessage"] = category.Message ?? "The selected category does not belong to this event.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.PostAsync<object>(
            "api/enrolments", new { CategoryId = categoryId }, cancellationToken);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Your enrolment has been confirmed.";
        }
        else
        {
            TempData["ErrorMessage"] = result.Message ?? "Your enrolment could not be completed.";
        }

        return RedirectToAction(nameof(Details), new { id = eventId });
    }

    public async Task<IActionResult> Enrolments(CancellationToken cancellationToken)
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>("api/enrolments/my", cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return View(result.Data ?? new List<EnrolmentDto>());
    }

    public async Task<IActionResult> Results(CancellationToken cancellationToken)
    {
        var result = await _api.GetAsync<List<ResultDto>>("api/results/my", cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Message;
        }

        var results = result.Data ?? new List<ResultDto>();
        foreach (var item in results)
        {
            item.PositionDisplay = item.OverallPosition.HasValue
                ? $"{ToOrdinal(item.OverallPosition.Value)} out of {item.TotalParticipants}"
                : "Not recorded";
        }

        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var result = await _api.GetAsync<ProfileFormViewModel>("api/profile", cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            TempData["ErrorMessage"] = result.Message ?? "Your profile could not be loaded.";
            return RedirectToAction("Index", "Home");
        }

        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var update = await _api.PutAsync<UserDto>("api/profile", new
        {
            model.FirstName,
            model.LastName,
            model.Email,
            model.PhoneNumber
        }, cancellationToken);

        if (!update.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, update.Message ?? "Your profile could not be updated.");
            return View(model);
        }

        if (model.ProfilePicture is not null)
        {
            var picture = await _api.PostFileAsync<object>(
                "api/profile/picture", model.ProfilePicture, cancellationToken);
            if (!picture.IsSuccess)
            {
                TempData["WarningMessage"] = $"Your details were saved, but the profile picture could not be uploaded: {picture.Message}";
            }
        }

        HttpContext.Session.SetString("FullName", $"{model.FirstName} {model.LastName}");
        HttpContext.Session.SetString("Email", model.Email);
        TempData["SuccessMessage"] ??= "Your profile was updated.";
        return RedirectToAction(nameof(Profile));
    }

    private static string ToOrdinal(int value)
    {
        var remainder = value % 100;
        var suffix = remainder is 11 or 12 or 13
            ? "th"
            : (value % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };

        return $"{value}{suffix}";
    }
}
