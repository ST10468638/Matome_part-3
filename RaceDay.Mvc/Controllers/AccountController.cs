using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class AccountController : Controller
{
    private readonly RaceDayApiClient _api;

    public AccountController(RaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("Role") is not null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.PostAsync<LoginApiResponse>(
            "api/auth/login",
            new { model.Email, model.Password },
            cancellationToken);

        if (!result.IsSuccess || result.Data?.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Sign in was unsuccessful.");
            return View(model);
        }

        SetUserSession(result.Data.User);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetString("Role") is not null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (model.Role is not ("Participant" or "Organiser"))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose a valid account type.");
        }

        if (model.Role == "Organiser" && string.IsNullOrWhiteSpace(model.OrganiserInviteCode))
        {
            ModelState.AddModelError(nameof(model.OrganiserInviteCode), "The organiser invitation code is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var registration = await _api.PostAsync<UserDto>(
            "api/auth/register",
            new
            {
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.PhoneNumber,
                model.Role,
                model.OrganiserInviteCode
            },
            cancellationToken);

        if (!registration.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, registration.Message ?? "The account could not be registered.");
            return View(model);
        }

        var login = await _api.PostAsync<LoginApiResponse>(
            "api/auth/login",
            new { model.Email, model.Password },
            cancellationToken);

        if (!login.IsSuccess || login.Data?.User is null)
        {
            TempData["SuccessMessage"] = "Your account was created. Sign in to continue.";
            return RedirectToAction(nameof(Login));
        }

        SetUserSession(login.Data.User);
        TempData["SuccessMessage"] = "Your RaceDay account has been created.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (HttpContext.Session.GetString("Role") is not null)
        {
            await _api.PostAsync<JsonElementResponse>("api/auth/logout", new { }, cancellationToken);
        }

        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private void SetUserSession(UserDto user)
    {
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("FullName", $"{user.FirstName} {user.LastName}");
        HttpContext.Session.SetString("Email", user.Email);
    }

    private sealed class JsonElementResponse
    {
        public string? Message { get; set; }
    }
}
