using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public sealed class EventFilterViewModel
{
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Province { get; set; }
    public string? FromDate { get; set; }
}

public sealed record EventListViewModel(
    IReadOnlyList<EventDto> Events,
    EventFilterViewModel Filter,
    string? Error = null);

public sealed record EventDetailsViewModel(
    EventDto Event,
    IReadOnlyList<CategoryDto> Categories,
    RouteDto? Route,
    IReadOnlyList<WeatherDto> Weather);

public sealed class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required, StringLength(150, MinimumLength = 3)]
    public string EventName { get; set; } = "";

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; set; } = "";

    [Required, DataType(DataType.Date)]
    public DateOnly EventDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

    [Required, DataType(DataType.Time)]
    public TimeOnly StartTime { get; set; } = new(7, 0);

    [Required, StringLength(150, MinimumLength = 2)]
    public string Location { get; set; } = "";

    [Required, StringLength(50, MinimumLength = 2)]
    public string Province { get; set; } = "Gauteng";

    [Required]
    public string EventType { get; set; } = "Running";

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; set; } = 10;

    [Range(1, 1000000)]
    public int MaxParticipants { get; set; } = 500;

    public string Status { get; set; } = "Scheduled";
    public string? BannerImageUrl { get; set; }

    [Display(Name = "Event banner (PNG, JPEG, or WebP; max 5 MB)")]
    public IFormFile? BannerImage { get; set; }
}

public sealed class EnrolmentFormViewModel
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string EmergencyContactName { get; set; } = "";

    [Required, Phone, StringLength(25, MinimumLength = 7)]
    public string EmergencyContactPhone { get; set; } = "";
}

public sealed class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; set; } = "";

    [Required]
    public string GenderOption { get; set; } = "Open";

    [Range(0, 120)]
    public int MinimumAge { get; set; } = 16;

    [Range(0, 120)]
    public int? MaximumAge { get; set; }

    [Range(typeof(decimal), "0", "999999.99")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000)]
    public int? MaxEntries { get; set; }
}

public sealed class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? FinishPosition { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly? FinishTime { get; set; }

    [Required]
    public string ResultStatus { get; set; } = "Finished";

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed record OrganiserDashboardViewModel(IReadOnlyList<EventDto> Events);

public sealed record EventEnrolmentsViewModel(EventDto Event, IReadOnlyList<EnrolmentDto> Enrolments);

public sealed record EventResultsViewModel(
    EventDto Event,
    IReadOnlyList<EnrolmentDto> Enrolments,
    IReadOnlyDictionary<int, ResultDto> Results);
