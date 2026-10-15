using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public sealed class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RaceDayEvent> Events => Set<RaceDayEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventRoute> Routes => Set<EventRoute>();
    public DbSet<EventWeather> EventWeather => Set<EventWeather>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", table =>
            {
                table.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");
            });
            entity.HasKey(user => user.UserId).HasName("PK_Users");
            entity.Property(user => user.UserId).HasColumnName("UserID").UseIdentityColumn();
            entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            entity.Property(user => user.FirstName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.LastName).HasMaxLength(50).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(user => user.Role).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(25);
            entity.Property(user => user.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(user => user.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<RaceDayEvent>(entity =>
        {
            entity.ToTable("Events", table =>
            {
                table.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Running', 'Walking', 'Cycling')");
                table.HasCheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
                table.HasCheckConstraint("CK_Events_MaxParticipants", "[MaxParticipants] > 0");
                table.HasCheckConstraint("CK_Events_Status", "[Status] IN ('Scheduled', 'Completed', 'Cancelled')");
            });
            entity.HasKey(e => e.EventId).HasName("PK_Events");
            entity.Property(e => e.EventId).HasColumnName("EventID").UseIdentityColumn();
            entity.Property(e => e.OrganiserId).HasColumnName("OrganiserID");
            entity.Property(e => e.EventName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.EventDate).HasColumnType("date");
            entity.Property(e => e.StartTime).HasColumnType("time(0)");
            entity.Property(e => e.Location).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Province).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventType).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            entity.Property(e => e.DistanceKm).HasPrecision(6, 2);
            entity.Property(e => e.MaxParticipants).HasDefaultValue(1000);
            entity.Property(e => e.Status).HasColumnType("varchar(20)").HasMaxLength(20).HasDefaultValue("Scheduled");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.Organiser).WithMany(user => user.OrganisedEvents)
                .HasForeignKey(e => e.OrganiserId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Events_Organiser");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", table =>
            {
                table.HasCheckConstraint("CK_Categories_Gender", "[GenderOption] IN ('Open', 'Female', 'Male')");
                table.HasCheckConstraint("CK_Categories_MinimumAge", "[MinimumAge] >= 0");
                table.HasCheckConstraint("CK_Categories_EntryFee", "[EntryFee] >= 0");
                table.HasCheckConstraint("CK_Categories_MaxEntries", "[MaxEntries] IS NULL OR [MaxEntries] > 0");
                table.HasCheckConstraint("CK_Categories_AgeRange", "[MaximumAge] IS NULL OR [MaximumAge] >= [MinimumAge]");
            });
            entity.HasKey(category => category.CategoryId).HasName("PK_Categories");
            entity.Property(category => category.CategoryId).HasColumnName("CategoryID").UseIdentityColumn();
            entity.Property(category => category.EventId).HasColumnName("EventID");
            entity.Property(category => category.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(category => category.GenderOption).HasColumnType("varchar(20)").HasMaxLength(20).IsRequired();
            entity.Property(category => category.MinimumAge).HasDefaultValue(16);
            entity.Property(category => category.EntryFee).HasPrecision(10, 2);
            entity.HasIndex(category => new { category.EventId, category.CategoryName })
                .IsUnique().HasDatabaseName("UQ_Categories_EventName");
            entity.HasAlternateKey(category => new { category.EventId, category.CategoryId })
                .HasName("UQ_Categories_EventCategory");
            entity.HasOne(category => category.Event).WithMany(e => e.Categories)
                .HasForeignKey(category => category.EventId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Categories_Event");
        });

        modelBuilder.Entity<EventRoute>(entity =>
        {
            entity.ToTable("Routes", table =>
            {
                table.HasCheckConstraint("CK_Routes_Distance", "[DistanceKm] > 0");
                table.HasCheckConstraint("CK_Routes_Elevation", "[ElevationGainMeters] IS NULL OR [ElevationGainMeters] >= 0");
            });
            entity.HasKey(route => route.RouteId).HasName("PK_Routes");
            entity.Property(route => route.RouteId).HasColumnName("RouteID").UseIdentityColumn();
            entity.Property(route => route.EventId).HasColumnName("EventID");
            entity.HasIndex(route => route.EventId).IsUnique().HasDatabaseName("UQ_Routes_Event");
            entity.Property(route => route.RouteName).HasMaxLength(150).IsRequired();
            entity.Property(route => route.DistanceKm).HasPrecision(6, 2);
            entity.Property(route => route.RouteDescription).HasMaxLength(1000).IsRequired();
            entity.Property(route => route.MapUrl).HasMaxLength(500);
            entity.HasOne(route => route.Event).WithOne(e => e.Route)
                .HasForeignKey<EventRoute>(route => route.EventId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Routes_Event");
        });

        modelBuilder.Entity<EventWeather>(entity =>
        {
            entity.ToTable("EventWeather", table =>
            {
                table.HasCheckConstraint("CK_EventWeather_Temperature", "[TemperatureC] BETWEEN -50 AND 60");
                table.HasCheckConstraint("CK_EventWeather_Wind", "[WindSpeedKph] >= 0");
                table.HasCheckConstraint("CK_EventWeather_Rain", "[RainProbabilityPercent] BETWEEN 0 AND 100");
            });
            entity.HasKey(weather => weather.WeatherId).HasName("PK_EventWeather");
            entity.Property(weather => weather.WeatherId).HasColumnName("WeatherID").UseIdentityColumn();
            entity.Property(weather => weather.EventId).HasColumnName("EventID");
            entity.Property(weather => weather.ForecastDate).HasColumnType("date");
            entity.Property(weather => weather.TemperatureC).HasPrecision(4, 1);
            entity.Property(weather => weather.Condition).HasMaxLength(80).IsRequired();
            entity.Property(weather => weather.WindSpeedKph).HasPrecision(5, 1);
            entity.Property(weather => weather.Advisory).HasMaxLength(500);
            entity.Property(weather => weather.RecordedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(weather => new { weather.EventId, weather.ForecastDate })
                .IsUnique().HasDatabaseName("UQ_EventWeather_EventDate");
            entity.HasOne(weather => weather.Event).WithMany(e => e.WeatherForecasts)
                .HasForeignKey(weather => weather.EventId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_EventWeather_Event");
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments", table =>
            {
                table.HasCheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Withdrawn')");
                table.HasCheckConstraint("CK_Enrolments_BibNumber", "[BibNumber] IS NULL OR [BibNumber] > 0");
            });
            entity.HasKey(enrolment => enrolment.EnrolmentId).HasName("PK_Enrolments");
            entity.Property(enrolment => enrolment.EnrolmentId).HasColumnName("EnrolmentID").UseIdentityColumn();
            entity.Property(enrolment => enrolment.EventId).HasColumnName("EventID");
            entity.Property(enrolment => enrolment.ParticipantId).HasColumnName("ParticipantID");
            entity.Property(enrolment => enrolment.CategoryId).HasColumnName("CategoryID");
            entity.Property(enrolment => enrolment.EnrolmentDate).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(enrolment => enrolment.EmergencyContactName).HasMaxLength(100).IsRequired();
            entity.Property(enrolment => enrolment.EmergencyContactPhone).HasMaxLength(25).IsRequired();
            entity.Property(enrolment => enrolment.EnrolmentStatus).HasColumnType("varchar(20)").HasMaxLength(20).HasDefaultValue("Confirmed");
            entity.HasIndex(enrolment => new { enrolment.EventId, enrolment.ParticipantId })
                .IsUnique().HasDatabaseName("UQ_Enrolments_EventParticipant");
            entity.HasIndex(enrolment => new { enrolment.EventId, enrolment.BibNumber })
                .IsUnique().HasFilter("[BibNumber] IS NOT NULL").HasDatabaseName("UX_Enrolments_EventBibNumber");
            entity.HasOne(enrolment => enrolment.Event).WithMany(e => e.Enrolments)
                .HasForeignKey(enrolment => enrolment.EventId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Enrolments_Event");
            entity.HasOne(enrolment => enrolment.Participant).WithMany(user => user.Enrolments)
                .HasForeignKey(enrolment => enrolment.ParticipantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Enrolments_Participant");
            entity.HasOne(enrolment => enrolment.Category).WithMany(category => category.Enrolments)
                .HasForeignKey(enrolment => new { enrolment.EventId, enrolment.CategoryId })
                .HasPrincipalKey(category => new { category.EventId, category.CategoryId })
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Enrolments_EventCategory");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results", table =>
            {
                table.HasCheckConstraint("CK_Results_FinishPosition", "[FinishPosition] IS NULL OR [FinishPosition] > 0");
                table.HasCheckConstraint("CK_Results_Pace", "[PaceMinPerKm] IS NULL OR [PaceMinPerKm] > 0");
                table.HasCheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished', 'DNF', 'DNS')");
                table.HasCheckConstraint("CK_Results_FinishData", "([ResultStatus] = 'Finished' AND [FinishPosition] IS NOT NULL AND [FinishTime] IS NOT NULL) OR ([ResultStatus] IN ('DNF', 'DNS'))");
            });
            entity.HasKey(result => result.ResultId).HasName("PK_Results");
            entity.Property(result => result.ResultId).HasColumnName("ResultID").UseIdentityColumn();
            entity.Property(result => result.EnrolmentId).HasColumnName("EnrolmentID");
            entity.HasIndex(result => result.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_Enrolment");
            entity.Property(result => result.FinishTime).HasColumnType("time(0)");
            entity.Property(result => result.PaceMinPerKm).HasPrecision(5, 2);
            entity.Property(result => result.ResultStatus).HasColumnType("varchar(20)").HasMaxLength(20).HasDefaultValue("Finished");
            entity.Property(result => result.RecordedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(result => result.Notes).HasMaxLength(500);
            entity.HasOne(result => result.Enrolment).WithOne(enrolment => enrolment.Result)
                .HasForeignKey<Result>(result => result.EnrolmentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Results_Enrolment");
        });
    }
}
