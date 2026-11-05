using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize(RaceDayRoles.Participant)]
public sealed class ParticipantController(RaceDayApiClient api) : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(MyEnrolments));

    public async Task<IActionResult> MyEnrolments(CancellationToken cancellationToken)
    {
        var result = await api.GetAsync<List<EnrolmentDto>>("api/enrolments/me", cancellationToken);
        return View(result.Data ?? []);
    }

    public async Task<IActionResult> MyResults(CancellationToken cancellationToken)
    {
        var result = await api.GetAsync<List<ResultDto>>("api/results/me", cancellationToken);
        return View(result.Data ?? []);
    }

    [HttpGet]
    public async Task<IActionResult> Enrol(int id, CancellationToken cancellationToken)
    {
        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        var categories = await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories", cancellationToken);
        if (eventResult.Data is null)
            return NotFound();
        ViewBag.Event = eventResult.Data;
        ViewBag.Categories = categories.Data ?? [];
        return View(new EnrolmentFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(
        int id,
        EnrolmentFormViewModel model,
        CancellationToken cancellationToken)
    {
        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        var categories = await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories", cancellationToken);
        ViewBag.Event = eventResult.Data;
        ViewBag.Categories = categories.Data ?? [];
        if (!ModelState.IsValid || eventResult.Data is null)
            return View(model);

        var result = await api.PostAsync<EnrolmentDto>(
            $"api/events/{id}/enrolments",
            new CreateEnrolmentRequest
            {
                CategoryId = model.CategoryId,
                EmergencyContactName = model.EmergencyContactName,
                EmergencyContactPhone = model.EmergencyContactPhone
            },
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Entry could not be submitted.");
            return View(model);
        }
        TempData["Success"] = "Your event entry has been recorded.";
        return RedirectToAction(nameof(MyEnrolments));
    }

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var result = await api.GetAsync<UserProfileDto>("api/users/me", cancellationToken);
        if (result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Your profile could not be loaded.";
            return RedirectToAction(nameof(MyEnrolments));
        }
        return View(new ProfileViewModel
        {
            FirstName = result.Data.FirstName,
            LastName = result.Data.LastName,
            PhoneNumber = result.Data.PhoneNumber,
            Email = result.Data.Email,
            Role = result.Data.Role,
            ProfileImageUrl = result.Data.ProfileImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var updated = await api.PutAsync<UserProfileDto>("api/users/me", new UpdateProfileRequest
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber
        }, cancellationToken);
        if (!updated.Succeeded)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "Profile could not be saved.");
            return View(model);
        }
        if (model.ProfileImage is { Length: > 0 })
        {
            var imageResult = await api.PutFileAsync<object>(
                "api/users/me/profile-image", model.ProfileImage, cancellationToken);
            if (!imageResult.Succeeded)
            {
                ModelState.AddModelError(nameof(model.ProfileImage), imageResult.Error ?? "Profile picture could not be uploaded.");
                return View(model);
            }
        }

        HttpContext.Session.SetString(SessionKeys.Name, $"{model.FirstName} {model.LastName}");
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }
}
