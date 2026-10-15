using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class DemoSeed
{
    public const string DemoPassword = "RaceDay!2026";

    public static async Task SeedAsync(RaceDayDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var hasher = new PasswordHasher<User>();
        var organiserOne = NewUser("Lerato", "Mokoena", "lerato.mokoena@raceday.example", "Organiser", "+27 82 555 0101", hasher);
        var organiserTwo = NewUser("James", "Naidoo", "james.naidoo@raceday.example", "Organiser", "+27 83 555 0102", hasher);
        var participantOne = NewUser("Ayanda", "Dlamini", "ayanda.dlamini@raceday.example", "Participant", "+27 72 555 0103", hasher);
        var participantTwo = NewUser("Pieter", "van Wyk", "pieter.vanwyk@raceday.example", "Participant", "+27 71 555 0104", hasher);
        db.Users.AddRange(organiserOne, organiserTwo, participantOne, participantTwo);

        var capeCycle = NewEvent(organiserOne, "Cape Town Cycle Challenge",
            "A fully supported coastal cycle event from the city to the Cape Peninsula and back.",
            new DateOnly(2026, 10, 11), new TimeOnly(6, 0), "Grand Parade, Cape Town", "Western Cape",
            "Cycling", 109.40m, 12000);
        var soweto = NewEvent(organiserOne, "Soweto Community Marathon",
            "A city marathon celebrating Soweto landmarks, community spirit, and healthy living.",
            new DateOnly(2026, 11, 1), new TimeOnly(6, 30), "Nasrec Expo Centre, Johannesburg", "Gauteng",
            "Running", 42.20m, 8000);
        var coastline = NewEvent(organiserTwo, "KwaZulu-Natal Coastline Walk",
            "A scenic, accessible coastal walk supporting local conservation and active lifestyles.",
            new DateOnly(2026, 9, 27), new TimeOnly(7, 0), "Moses Mabhida Stadium, Durban", "KwaZulu-Natal",
            "Walking", 10.00m, 2500);
        db.Events.AddRange(capeCycle, soweto, coastline);
        await db.SaveChangesAsync(cancellationToken);

        var categories = new[]
        {
            NewCategory(capeCycle, "Open Cycle Challenge", "Open", 16, null, 850m, 10000),
            NewCategory(capeCycle, "Junior Cycle Challenge", "Open", 12, 17, 450m, 2000),
            NewCategory(soweto, "Open Marathon", "Open", 18, null, 420m, 6500),
            NewCategory(soweto, "Women's Marathon", "Female", 18, null, 420m, 1500),
            NewCategory(coastline, "Open Coastal Walk", "Open", 12, null, 180m, 2200),
            NewCategory(coastline, "Senior Coastal Walk", "Open", 60, null, 120m, 300)
        };
        db.Categories.AddRange(categories);

        db.Routes.AddRange(
            NewRoute(capeCycle, "Peninsula Coastal Loop", 109.40m,
                "Grand Parade through Camps Bay, Chapman's Peak, Simon's Town, and back along the M3.",
                "https://maps.example/raceday/cape-town-cycle", 1450),
            NewRoute(soweto, "Soweto Heritage Marathon Route", 42.20m,
                "Nasrec through Orlando, Vilakazi Street, Dobsonville, and returning to Nasrec.",
                "https://maps.example/raceday/soweto-marathon", 410),
            NewRoute(coastline, "Durban Golden Mile Out-and-Back", 10m,
                "Moses Mabhida Stadium along the beachfront promenade and back to the stadium.",
                "https://maps.example/raceday/kzn-coastline-walk", 55));

        db.EventWeather.AddRange(
            NewWeather(capeCycle, new DateOnly(2026, 10, 10), 17.5m, "Partly cloudy", 18m, 20,
                "Expect a cool start; cyclists should carry a light wind layer."),
            NewWeather(capeCycle, new DateOnly(2026, 10, 11), 19m, "Sunny intervals", 22m, 15,
                "Strong coastal crosswinds are possible after Chapman's Peak."),
            NewWeather(soweto, new DateOnly(2026, 10, 31), 16m, "Clear morning", 10m, 10,
                "Good running conditions with a cool start."),
            NewWeather(soweto, new DateOnly(2026, 11, 1), 21m, "Warm and clear", 12m, 5,
                "Carry water and use the marked hydration stations."),
            NewWeather(coastline, new DateOnly(2026, 9, 26), 18m, "Light cloud", 14m, 25,
                "Comfortable walking conditions; light rain remains possible."),
            NewWeather(coastline, new DateOnly(2026, 9, 27), 22m, "Partly cloudy", 16m, 20,
                "Use sun protection on the exposed beachfront section."));

        var entries = new[]
        {
            NewEnrolment(capeCycle, participantOne, categories[0], "Thabo Dlamini", "+27 82 555 0191", 1042),
            NewEnrolment(capeCycle, participantTwo, categories[0], "Marie van Wyk", "+27 72 555 0192", 1043),
            NewEnrolment(soweto, participantOne, categories[2], "Thabo Dlamini", "+27 82 555 0191", 231),
            NewEnrolment(soweto, participantTwo, categories[2], "Marie van Wyk", "+27 72 555 0192", 232),
            NewEnrolment(coastline, participantOne, categories[4], "Thabo Dlamini", "+27 82 555 0191", 87)
        };
        db.Enrolments.AddRange(entries);
        await db.SaveChangesAsync(cancellationToken);

        db.Results.AddRange(
            NewResult(entries[0], 148, new TimeOnly(4, 15), 2.33m, "Strong finish after the coastal climb."),
            NewResult(entries[1], 207, new TimeOnly(4, 32), 2.49m, "Completed safely in windy conditions."),
            NewResult(entries[2], 36, new TimeOnly(3, 58), 5.64m, "Personal best marathon result."),
            NewResult(entries[3], 51, new TimeOnly(4, 22), 6.21m, "Consistent pacing throughout the route."));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static User NewUser(string firstName, string lastName, string email, string role, string phone, IPasswordHasher<User> hasher)
    {
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = role,
            PhoneNumber = phone,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        return user;
    }

    private static RaceDayEvent NewEvent(User organiser, string name, string description, DateOnly date, TimeOnly time,
        string location, string province, string type, decimal distance, int capacity) => new()
    {
        Organiser = organiser,
        EventName = name,
        Description = description,
        EventDate = date,
        StartTime = time,
        Location = location,
        Province = province,
        EventType = type,
        DistanceKm = distance,
        MaxParticipants = capacity,
        Status = "Scheduled",
        CreatedAt = DateTime.UtcNow
    };

    private static Category NewCategory(RaceDayEvent raceEvent, string name, string gender, int minAge, int? maxAge, decimal fee, int? maxEntries) => new()
    {
        Event = raceEvent,
        CategoryName = name,
        GenderOption = gender,
        MinimumAge = minAge,
        MaximumAge = maxAge,
        EntryFee = fee,
        MaxEntries = maxEntries
    };

    private static EventRoute NewRoute(RaceDayEvent raceEvent, string name, decimal distance, string description, string? mapUrl, int elevation) => new()
    {
        Event = raceEvent,
        RouteName = name,
        DistanceKm = distance,
        RouteDescription = description,
        MapUrl = mapUrl,
        ElevationGainMeters = elevation
    };

    private static EventWeather NewWeather(RaceDayEvent raceEvent, DateOnly forecastDate, decimal temperature, string condition,
        decimal windSpeed, int rainProbability, string advisory) => new()
    {
        Event = raceEvent,
        ForecastDate = forecastDate,
        TemperatureC = temperature,
        Condition = condition,
        WindSpeedKph = windSpeed,
        RainProbabilityPercent = rainProbability,
        Advisory = advisory,
        RecordedAt = DateTime.UtcNow
    };

    private static Enrolment NewEnrolment(RaceDayEvent raceEvent, User participant, Category category,
        string emergencyName, string emergencyPhone, int bibNumber) => new()
    {
        Event = raceEvent,
        Participant = participant,
        Category = category,
        EnrolmentDate = DateTime.UtcNow,
        EmergencyContactName = emergencyName,
        EmergencyContactPhone = emergencyPhone,
        EnrolmentStatus = "Confirmed",
        BibNumber = bibNumber
    };

    private static Result NewResult(Enrolment enrolment, int position, TimeOnly finishTime, decimal pace, string notes) => new()
    {
        Enrolment = enrolment,
        FinishPosition = position,
        FinishTime = finishTime,
        PaceMinPerKm = pace,
        ResultStatus = "Finished",
        RecordedAt = DateTime.UtcNow,
        Notes = notes
    };
}
