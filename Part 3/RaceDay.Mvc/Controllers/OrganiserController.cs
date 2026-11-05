using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[SessionAuthorize(RaceDayRoles.Organiser)]
public sealed class OrganiserController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await api.GetAsync<List<EventDto>>("api/organiser/events", cancellationToken);
        return View(new OrganiserDashboardViewModel(result.Data ?? []));
    }

    [HttpGet]
    public IActionResult Create() => View("EventForm", new EventFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("EventForm", model);
        var created = await api.PostAsync<EventDto>("api/events", MapCreate(model), cancellationToken);
        if (!created.Succeeded || created.Data is null)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "Event could not be created.");
            return View("EventForm", model);
        }
        if (model.BannerImage is { Length: > 0 })
        {
            var upload = await api.PutFileAsync<object>(
                $"api/events/{created.Data.EventId}/banner", model.BannerImage, cancellationToken);
            if (!upload.Succeeded)
                TempData["Error"] = $"Event was created, but its banner was not uploaded: {upload.Error}";
        }
        TempData["Success"] = "Your event has been created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (result.Data is null)
            return NotFound();
        if (result.Data.OrganiserId.ToString() != HttpContext.Session.GetString(SessionKeys.UserId))
            return Forbid();
        return View("EventForm", ToForm(result.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("EventForm", model);
        var updated = await api.PutAsync<EventDto>($"api/events/{id}", MapUpdate(model), cancellationToken);
        if (!updated.Succeeded)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "Event could not be updated.");
            return View("EventForm", model);
        }
        if (model.BannerImage is { Length: > 0 })
        {
            var upload = await api.PutFileAsync<object>(
                $"api/events/{id}/banner", model.BannerImage, cancellationToken);
            if (!upload.Succeeded)
            {
                TempData["Error"] = $"Event details were saved, but the banner was not uploaded: {upload.Error}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }
        TempData["Success"] = "Event details have been saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await api.DeleteAsync<object>($"api/events/{id}", cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Event removed or cancelled." : result.Error ?? "Event could not be removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int id, CancellationToken cancellationToken)
    {
        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (eventResult.Data is null)
            return NotFound();
        if (eventResult.Data.OrganiserId.ToString() != HttpContext.Session.GetString(SessionKeys.UserId))
            return Forbid();
        var categories = await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories", cancellationToken);
        ViewBag.Event = eventResult.Data;
        return View(categories.Data ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(
        int eventId,
        int categoryId,
        CategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Check the category fields and try again.";
            return RedirectToAction(nameof(Categories), new { id = eventId });
        }
        var result = await api.PutAsync<CategoryDto>(
            $"api/categories/{categoryId}", MapCategory(model), cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Category updated." : result.Error ?? "Category could not be updated.";
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(
        int eventId,
        CategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.EventId = eventId;
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Check the category fields and try again.";
            return RedirectToAction(nameof(Categories), new { id = eventId });
        }
        var result = await api.PostAsync<CategoryDto>($"api/events/{eventId}/categories", MapCategory(model), cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Category added." : result.Error ?? "Category could not be added.";
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(
        int eventId,
        int categoryId,
        CancellationToken cancellationToken)
    {
        var result = await api.DeleteAsync<object>($"api/categories/{categoryId}", cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Category removed." : result.Error ?? "Category could not be removed.";
        return RedirectToAction(nameof(Categories), new { id = eventId });
    }

    public async Task<IActionResult> Enrolments(int id, CancellationToken cancellationToken)
    {
        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (eventResult.Data is null)
            return NotFound();
        if (eventResult.Data.OrganiserId.ToString() != HttpContext.Session.GetString(SessionKeys.UserId))
            return Forbid();
        var enrolments = await api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments", cancellationToken);
        return View(new EventEnrolmentsViewModel(eventResult.Data, enrolments.Data ?? []));
    }

    public async Task<IActionResult> Results(int id, CancellationToken cancellationToken)
    {
        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (eventResult.Data is null)
            return NotFound();
        if (eventResult.Data.OrganiserId.ToString() != HttpContext.Session.GetString(SessionKeys.UserId))
            return Forbid();
        var enrolmentsTask = api.GetAsync<List<EnrolmentDto>>($"api/events/{id}/enrolments", cancellationToken);
        var resultsTask = api.GetAsync<List<ResultDto>>($"api/events/{id}/results", cancellationToken);
        await Task.WhenAll(enrolmentsTask, resultsTask);
        var results = resultsTask.Result.Data?.ToDictionary(item => item.EnrolmentId) ?? [];
        return View(new EventResultsViewModel(
            eventResult.Data,
            enrolmentsTask.Result.Data ?? [],
            results));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(ResultFormViewModel model, CancellationToken cancellationToken)
    {
        var existing = await api.GetAsync<List<ResultDto>>(
            $"api/events/{model.EventId}/results", cancellationToken);
        var current = existing.Data?.FirstOrDefault(item => item.EnrolmentId == model.EnrolmentId);
        var body = new CreateResultRequest
        {
            FinishPosition = model.ResultStatus == "Finished" ? model.FinishPosition : null,
            FinishTime = model.ResultStatus == "Finished" ? model.FinishTime : null,
            ResultStatus = model.ResultStatus,
            Notes = model.Notes
        };
        var saved = current is null
            ? await api.PostAsync<ResultDto>($"api/enrolments/{model.EnrolmentId}/result", body, cancellationToken)
            : await api.PutAsync<ResultDto>($"api/results/{current.ResultId}", body, cancellationToken);
        TempData[saved.Succeeded ? "Success" : "Error"] =
            saved.Succeeded ? "Participant result saved." : saved.Error ?? "Result could not be saved.";
        return RedirectToAction(nameof(Results), new { id = model.EventId });
    }

    private static CreateEventRequest MapCreate(EventFormViewModel model) => new()
    {
        EventName = model.EventName,
        Description = model.Description,
        EventDate = model.EventDate,
        StartTime = model.StartTime,
        Location = model.Location,
        Province = model.Province,
        EventType = model.EventType,
        DistanceKm = model.DistanceKm,
        MaxParticipants = model.MaxParticipants
    };

    private static UpdateEventRequest MapUpdate(EventFormViewModel model) => new()
    {
        EventName = model.EventName,
        Description = model.Description,
        EventDate = model.EventDate,
        StartTime = model.StartTime,
        Location = model.Location,
        Province = model.Province,
        EventType = model.EventType,
        DistanceKm = model.DistanceKm,
        MaxParticipants = model.MaxParticipants,
        Status = model.Status
    };

    private static EventFormViewModel ToForm(EventDto item) => new()
    {
        EventId = item.EventId,
        EventName = item.EventName,
        Description = item.Description,
        EventDate = item.EventDate,
        StartTime = item.StartTime,
        Location = item.Location,
        Province = item.Province,
        EventType = item.EventType,
        DistanceKm = item.DistanceKm,
        MaxParticipants = item.MaxParticipants,
        Status = item.Status,
        BannerImageUrl = item.BannerImageUrl
    };

    private static CreateCategoryRequest MapCategory(CategoryFormViewModel model) => new()
    {
        CategoryName = model.CategoryName,
        GenderOption = model.GenderOption,
        MinimumAge = model.MinimumAge,
        MaximumAge = model.MaximumAge,
        EntryFee = model.EntryFee,
        MaxEntries = model.MaxEntries
    };
}
