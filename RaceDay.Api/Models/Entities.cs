namespace RaceDay.Api.Models;

public sealed class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RaceDayEvent> OrganisedEvents { get; set; } = new List<RaceDayEvent>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public sealed class RaceDayEvent
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = "";
    public string Description { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Location { get; set; } = "";
    public string Province { get; set; } = "";
    public string EventType { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; }
    public User Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public EventRoute? Route { get; set; }
    public ICollection<EventWeather> WeatherForecasts { get; set; } = new List<EventWeather>();
}

public sealed class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = "";
    public string GenderOption { get; set; } = "Open";
    public int MinimumAge { get; set; } = 16;
    public int? MaximumAge { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxEntries { get; set; }
    public RaceDayEvent Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public sealed class EventRoute
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public string RouteName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string RouteDescription { get; set; } = "";
    public string? MapUrl { get; set; }
    public int? ElevationGainMeters { get; set; }
    public RaceDayEvent Event { get; set; } = null!;
}

public sealed class EventWeather
{
    public int WeatherId { get; set; }
    public int EventId { get; set; }
    public DateOnly ForecastDate { get; set; }
    public decimal TemperatureC { get; set; }
    public string Condition { get; set; } = "";
    public decimal WindSpeedKph { get; set; }
    public int RainProbabilityPercent { get; set; }
    public string? Advisory { get; set; }
    public DateTime RecordedAt { get; set; }
    public RaceDayEvent Event { get; set; } = null!;
}

public sealed class Enrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string EmergencyContactName { get; set; } = "";
    public string EmergencyContactPhone { get; set; } = "";
    public string EnrolmentStatus { get; set; } = "Confirmed";
    public int? BibNumber { get; set; }
    public RaceDayEvent Event { get; set; } = null!;
    public User Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public sealed class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int? FinishPosition { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public decimal? PaceMinPerKm { get; set; }
    public string ResultStatus { get; set; } = "Finished";
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
}
