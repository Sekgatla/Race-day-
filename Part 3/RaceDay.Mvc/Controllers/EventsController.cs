using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public sealed class EventsController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var eventResult = await api.GetAsync<EventDto>($"api/events/{id}", cancellationToken);
        if (eventResult.StatusCode == System.Net.HttpStatusCode.NotFound)
            return NotFound();
        if (eventResult.Data is null)
        {
            TempData["Error"] = eventResult.Error ?? "Event details could not be loaded.";
            return RedirectToAction("Index", "Home");
        }

        var categoriesTask = api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories", cancellationToken);
        var routeTask = api.GetAsync<RouteDto>($"api/events/{id}/route", cancellationToken);
        var weatherTask = api.GetAsync<List<WeatherDto>>($"api/events/{id}/weather", cancellationToken);
        await Task.WhenAll(categoriesTask, routeTask, weatherTask);

        return View(new EventDetailsViewModel(
            eventResult.Data,
            categoriesTask.Result.Data ?? [],
            routeTask.Result.Data,
            weatherTask.Result.Data ?? []));
    }
}
