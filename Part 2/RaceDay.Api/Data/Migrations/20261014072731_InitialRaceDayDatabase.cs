using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialRaceDayDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserID = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Scheduled"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventID);
                    table.CheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_Events_EventType", "[EventType] IN ('Running', 'Walking', 'Cycling')");
                    table.CheckConstraint("CK_Events_MaxParticipants", "[MaxParticipants] > 0");
                    table.CheckConstraint("CK_Events_Status", "[Status] IN ('Scheduled', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Events_Organiser",
                        column: x => x.OrganiserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GenderOption = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    MinimumAge = table.Column<int>(type: "int", nullable: false, defaultValue: 16),
                    MaximumAge = table.Column<int>(type: "int", nullable: true),
                    EntryFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxEntries = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                    table.UniqueConstraint("UQ_Categories_EventCategory", x => new { x.EventID, x.CategoryID });
                    table.CheckConstraint("CK_Categories_AgeRange", "[MaximumAge] IS NULL OR [MaximumAge] >= [MinimumAge]");
                    table.CheckConstraint("CK_Categories_EntryFee", "[EntryFee] >= 0");
                    table.CheckConstraint("CK_Categories_Gender", "[GenderOption] IN ('Open', 'Female', 'Male')");
                    table.CheckConstraint("CK_Categories_MaxEntries", "[MaxEntries] IS NULL OR [MaxEntries] > 0");
                    table.CheckConstraint("CK_Categories_MinimumAge", "[MinimumAge] >= 0");
                    table.ForeignKey(
                        name: "FK_Categories_Event",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventWeather",
                columns: table => new
                {
                    WeatherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TemperatureC = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    WindSpeedKph = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RainProbabilityPercent = table.Column<int>(type: "int", nullable: false),
                    Advisory = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventWeather", x => x.WeatherID);
                    table.CheckConstraint("CK_EventWeather_Rain", "[RainProbabilityPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_EventWeather_Temperature", "[TemperatureC] BETWEEN -50 AND 60");
                    table.CheckConstraint("CK_EventWeather_Wind", "[WindSpeedKph] >= 0");
                    table.ForeignKey(
                        name: "FK_EventWeather_Event",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    RouteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    RouteDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MapUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ElevationGainMeters = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.RouteID);
                    table.CheckConstraint("CK_Routes_Distance", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_Routes_Elevation", "[ElevationGainMeters] IS NULL OR [ElevationGainMeters] >= 0");
                    table.ForeignKey(
                        name: "FK_Routes_Event",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    ParticipantID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    EnrolmentStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Confirmed"),
                    BibNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentID);
                    table.CheckConstraint("CK_Enrolments_BibNumber", "[BibNumber] IS NULL OR [BibNumber] > 0");
                    table.CheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Withdrawn')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Event",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_EventCategory",
                        columns: x => new { x.EventID, x.CategoryID },
                        principalTable: "Categories",
                        principalColumns: new[] { "EventID", "CategoryID" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Participant",
                        column: x => x.ParticipantID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentID = table.Column<int>(type: "int", nullable: false),
                    FinishPosition = table.Column<int>(type: "int", nullable: true),
                    FinishTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    PaceMinPerKm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ResultStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Finished"),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultID);
                    table.CheckConstraint("CK_Results_FinishData", "([ResultStatus] = 'Finished' AND [FinishPosition] IS NOT NULL AND [FinishTime] IS NOT NULL) OR ([ResultStatus] IN ('DNF', 'DNS'))");
                    table.CheckConstraint("CK_Results_FinishPosition", "[FinishPosition] IS NULL OR [FinishPosition] > 0");
                    table.CheckConstraint("CK_Results_Pace", "[PaceMinPerKm] IS NULL OR [PaceMinPerKm] > 0");
                    table.CheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished', 'DNF', 'DNS')");
                    table.ForeignKey(
                        name: "FK_Results_Enrolment",
                        column: x => x.EnrolmentID,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Categories_EventName",
                table: "Categories",
                columns: new[] { "EventID", "CategoryName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_EventID_CategoryID",
                table: "Enrolments",
                columns: new[] { "EventID", "CategoryID" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_ParticipantID",
                table: "Enrolments",
                column: "ParticipantID");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_EventParticipant",
                table: "Enrolments",
                columns: new[] { "EventID", "ParticipantID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Enrolments_EventBibNumber",
                table: "Enrolments",
                columns: new[] { "EventID", "BibNumber" },
                unique: true,
                filter: "[BibNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserID",
                table: "Events",
                column: "OrganiserID");

            migrationBuilder.CreateIndex(
                name: "UQ_EventWeather_EventDate",
                table: "EventWeather",
                columns: new[] { "EventID", "ForecastDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Results_Enrolment",
                table: "Results",
                column: "EnrolmentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Routes_Event",
                table: "Routes",
                column: "EventID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventWeather");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
