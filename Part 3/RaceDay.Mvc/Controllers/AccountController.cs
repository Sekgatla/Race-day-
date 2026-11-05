using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public sealed class AccountController(RaceDayApiClient api) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await api.PostAsync<AuthResponse>(
            "api/auth/login",
            new LoginRequest { Email = model.Email, Password = model.Password },
            cancellationToken);
        if (!result.Succeeded || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Sign-in failed.");
            return View(model);
        }

        var session = HttpContext.Session;
        session.SetString(SessionKeys.ApiToken, result.Data.Token);
        session.SetString(SessionKeys.UserId, result.Data.User.UserId.ToString());
        session.SetString(SessionKeys.Role, result.Data.User.Role);
        session.SetString(SessionKeys.Name, $"{result.Data.User.FirstName} {result.Data.User.LastName}");
        session.SetString(SessionKeys.Email, result.Data.User.Email);

        if (Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl!);
        return RedirectToAction("Index", result.Data.User.Role == RaceDayRoles.Organiser ? "Organiser" : "Participant");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);
        if (model.Role is not (RaceDayRoles.Organiser or RaceDayRoles.Participant))
        {
            ModelState.AddModelError(nameof(model.Role), "Choose Organiser or Participant.");
            return View(model);
        }

        var result = await api.PostAsync<UserProfileDto>(
            "api/auth/register",
            new RegisterRequest
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role,
                PhoneNumber = model.PhoneNumber
            },
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return View(model);
        }

        TempData["Success"] = "Your RaceDay account is ready. Sign in to continue.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SessionKeys.ApiToken)))
            await api.PostAsync<object>("api/auth/logout", null, cancellationToken);
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }
}
