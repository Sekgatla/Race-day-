using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public sealed class HomeController(RaceDayApiClient api, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(
        [Bind(Prefix = "Filter")] EventFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filter.FromDate))
            filter.FromDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        var parameters = new List<string>();
        parameters.Add("status=Scheduled");
        if (!string.IsNullOrWhiteSpace(filter.Search))
            parameters.Add($"search={Uri.EscapeDataString(filter.Search.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.EventType))
            parameters.Add($"eventType={Uri.EscapeDataString(filter.EventType)}");
        if (!string.IsNullOrWhiteSpace(filter.Province))
            parameters.Add($"province={Uri.EscapeDataString(filter.Province)}");
        parameters.Add($"fromDate={Uri.EscapeDataString(filter.FromDate)}");
        var endpoint = "api/events";
        if (parameters.Count > 0)
            endpoint += "?" + string.Join("&", parameters);

        var result = await api.GetAsync<List<EventDto>>(endpoint, cancellationToken);
        return View(new EventListViewModel(result.Data ?? [], filter, result.Error));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogError("Unhandled web request error {RequestId}.", Activity.Current?.Id ?? HttpContext.TraceIdentifier);
        return View();
    }
}
