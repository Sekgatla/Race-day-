using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

public static class RaceDayRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public sealed class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string FirstName { get; init; } = "";

    [Required, StringLength(50, MinimumLength = 1)]
    public string LastName { get; init; } = "";

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; init; } = "";

    [Required, StringLength(100, MinimumLength = 10)]
    public string Password { get; init; } = "";

    [Required]
    public string Role { get; init; } = "";

    [StringLength(25)]
    public string? PhoneNumber { get; init; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";
}

public sealed record UserProfileDto(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? PhoneNumber,
    DateTime CreatedAt);

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserProfileDto User);

public sealed class UpdateProfileRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string FirstName { get; init; } = "";

    [Required, StringLength(50, MinimumLength = 1)]
    public string LastName { get; init; } = "";

    [StringLength(25)]
    public string? PhoneNumber { get; init; }
}

public class CreateEventRequest
{
    [Required, StringLength(150, MinimumLength = 3)]
    public string EventName { get; init; } = "";

    [Required, StringLength(1000, MinimumLength = 10)]
    public string Description { get; init; } = "";

    public DateOnly EventDate { get; init; }
    public TimeOnly StartTime { get; init; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Location { get; init; } = "";

    [Required, StringLength(50, MinimumLength = 2)]
    public string Province { get; init; } = "";

    [Required]
    public string EventType { get; init; } = "";

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; init; }

    [Range(1, 1000000)]
    public int MaxParticipants { get; init; } = 1000;
}

public sealed class UpdateEventRequest : CreateEventRequest
{
    [Required]
    public string Status { get; init; } = "Scheduled";
}

public sealed record EventDto(
    int EventId,
    int OrganiserId,
    string OrganiserName,
    string EventName,
    string Description,
    DateOnly EventDate,
    TimeOnly StartTime,
    string Location,
    string Province,
    string EventType,
    decimal DistanceKm,
    int MaxParticipants,
    string Status,
    int CategoryCount,
    int EnrolmentCount);

public sealed class CreateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; init; } = "";

    [Required]
    public string GenderOption { get; init; } = "Open";

    [Range(0, 120)]
    public int MinimumAge { get; init; } = 16;

    [Range(0, 120)]
    public int? MaximumAge { get; init; }

    [Range(typeof(decimal), "0", "999999.99")]
    public decimal EntryFee { get; init; }

    [Range(1, 1000000)]
    public int? MaxEntries { get; init; }
}

public sealed record CategoryDto(
    int CategoryId,
    int EventId,
    string CategoryName,
    string GenderOption,
    int MinimumAge,
    int? MaximumAge,
    decimal EntryFee,
    int? MaxEntries);

public sealed class CreateEnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string EmergencyContactName { get; init; } = "";

    [Required, StringLength(25, MinimumLength = 7)]
    public string EmergencyContactPhone { get; init; } = "";
}

public sealed record EnrolmentDto(
    int EnrolmentId,
    int EventId,
    string EventName,
    int ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string EmergencyContactName,
    string EmergencyContactPhone,
    string EnrolmentStatus,
    int? BibNumber);

public sealed class UpdateEnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string EmergencyContactName { get; init; } = "";

    [Required, StringLength(25, MinimumLength = 7)]
    public string EmergencyContactPhone { get; init; } = "";
}

public class CreateResultRequest
{
    [Range(1, int.MaxValue)]
    public int? FinishPosition { get; init; }

    public TimeOnly? FinishTime { get; init; }

    [Range(typeof(decimal), "0.01", "999.99")]
    public decimal? PaceMinPerKm { get; init; }

    [Required]
    public string ResultStatus { get; init; } = "Finished";

    [StringLength(500)]
    public string? Notes { get; init; }
}

public sealed record ResultDto(
    int ResultId,
    int EnrolmentId,
    int EventId,
    string EventName,
    DateOnly EventDate,
    int ParticipantId,
    string ParticipantName,
    int CategoryId,
    string CategoryName,
    int? FinishPosition,
    TimeOnly? FinishTime,
    decimal? PaceMinPerKm,
    string ResultStatus,
    DateTime RecordedAt,
    string? Notes);

public sealed class UpdateResultRequest : CreateResultRequest { }

public sealed class UpsertRouteRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string RouteName { get; init; } = "";

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; init; }

    [Required, StringLength(1000, MinimumLength = 10)]
    public string RouteDescription { get; init; } = "";

    [Url, StringLength(500)]
    public string? MapUrl { get; init; }

    [Range(0, 100000)]
    public int? ElevationGainMeters { get; init; }
}

public sealed record RouteDto(
    int RouteId,
    int EventId,
    string RouteName,
    decimal DistanceKm,
    string RouteDescription,
    string? MapUrl,
    int? ElevationGainMeters);

public sealed class CreateWeatherRequest
{
    public DateOnly ForecastDate { get; init; }

    [Range(typeof(decimal), "-50", "60")]
    public decimal TemperatureC { get; init; }

    [Required, StringLength(80, MinimumLength = 2)]
    public string Condition { get; init; } = "";

    [Range(typeof(decimal), "0", "999.9")]
    public decimal WindSpeedKph { get; init; }

    [Range(0, 100)]
    public int RainProbabilityPercent { get; init; }

    [StringLength(500)]
    public string? Advisory { get; init; }
}

public sealed record WeatherDto(
    int WeatherId,
    int EventId,
    DateOnly ForecastDate,
    decimal TemperatureC,
    string Condition,
    decimal WindSpeedKph,
    int RainProbabilityPercent,
    string? Advisory,
    DateTime RecordedAt);

public sealed record ApiError(string Message, IReadOnlyDictionary<string, string[]>? Errors = null);
