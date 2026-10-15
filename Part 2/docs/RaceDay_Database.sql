/*
    RaceDay Event Management System
    PROG6212 PoE Part 1 - SQL Server database script

    This script is designed to run from top to bottom in SQL Server Management
    Studio on a clean SQL Server instance. It creates RaceDayDb if necessary,
    recreates the seven planning tables, applies constraints, and inserts
    realistic sample data.

    Part 1 design coverage:
      Users, Events, Categories, Enrolments, Results, Routes, EventWeather.
    Enrolments resolves the participant/event many-to-many relationship.
    Results.EnrolmentID is unique so an enrolment has at most one result.

    Planning basis: The Independent Institute of Education (The IIE) (2026),
    PROG6212 - Programming 2B: PoE Part 1 assessment breakdown.

    Demo PasswordHash values are placeholders for Part 1 seed data only.
    Part 2 must replace them with hashes produced by the selected auth library.
*/

IF DB_ID(N'RaceDayDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [RaceDayDb]');
END;
GO

USE [RaceDayDb];
GO

/* Drop in dependency order so the script can be rerun during development. */
DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.Enrolments;
DROP TABLE IF EXISTS dbo.EventWeather;
DROP TABLE IF EXISTS dbo.Routes;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.Users;
GO

CREATE TABLE dbo.Users
(
    UserID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Users PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(255) NOT NULL
        CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL
        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant')),
    PhoneNumber NVARCHAR(25) NULL,
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    IsActive BIT NOT NULL
        CONSTRAINT DF_Users_IsActive DEFAULT 1
);
GO

CREATE TABLE dbo.Events
(
    EventID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Events PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    EventDate DATE NOT NULL,
    StartTime TIME(0) NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Province NVARCHAR(50) NOT NULL,
    EventType VARCHAR(20) NOT NULL
        CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Running', 'Walking', 'Cycling')),
    DistanceKm DECIMAL(6,2) NOT NULL
        CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0),
    MaxParticipants INT NOT NULL
        CONSTRAINT DF_Events_MaxParticipants DEFAULT 1000
        CONSTRAINT CK_Events_MaxParticipants CHECK (MaxParticipants > 0),
    Status VARCHAR(20) NOT NULL
        CONSTRAINT DF_Events_Status DEFAULT 'Scheduled'
        CONSTRAINT CK_Events_Status CHECK (Status IN ('Scheduled', 'Completed', 'Cancelled')),
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Events_Organiser
        FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID)
);
GO

CREATE TABLE dbo.Categories
(
    CategoryID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Categories PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    GenderOption VARCHAR(20) NOT NULL
        CONSTRAINT CK_Categories_Gender CHECK (GenderOption IN ('Open', 'Female', 'Male')),
    MinimumAge INT NOT NULL
        CONSTRAINT DF_Categories_MinimumAge DEFAULT 16
        CONSTRAINT CK_Categories_MinimumAge CHECK (MinimumAge >= 0),
    MaximumAge INT NULL,
    EntryFee DECIMAL(10,2) NOT NULL
        CONSTRAINT CK_Categories_EntryFee CHECK (EntryFee >= 0),
    MaxEntries INT NULL
        CONSTRAINT CK_Categories_MaxEntries CHECK (MaxEntries IS NULL OR MaxEntries > 0),
    CONSTRAINT UQ_Categories_EventName UNIQUE (EventID, CategoryName),
    CONSTRAINT UQ_Categories_EventCategory UNIQUE (EventID, CategoryID),
    CONSTRAINT CK_Categories_AgeRange CHECK
        (MaximumAge IS NULL OR MaximumAge >= MinimumAge),
    CONSTRAINT FK_Categories_Event
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

CREATE TABLE dbo.Routes
(
    RouteID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Routes PRIMARY KEY,
    EventID INT NOT NULL
        CONSTRAINT UQ_Routes_Event UNIQUE,
    RouteName NVARCHAR(150) NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL
        CONSTRAINT CK_Routes_Distance CHECK (DistanceKm > 0),
    RouteDescription NVARCHAR(1000) NOT NULL,
    MapUrl NVARCHAR(500) NULL,
    ElevationGainMeters INT NULL
        CONSTRAINT CK_Routes_Elevation CHECK
            (ElevationGainMeters IS NULL OR ElevationGainMeters >= 0),
    CONSTRAINT FK_Routes_Event
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

CREATE TABLE dbo.EventWeather
(
    WeatherID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_EventWeather PRIMARY KEY,
    EventID INT NOT NULL,
    ForecastDate DATE NOT NULL,
    TemperatureC DECIMAL(4,1) NOT NULL
        CONSTRAINT CK_EventWeather_Temperature CHECK (TemperatureC BETWEEN -50 AND 60),
    Condition NVARCHAR(80) NOT NULL,
    WindSpeedKph DECIMAL(5,1) NOT NULL
        CONSTRAINT CK_EventWeather_Wind CHECK (WindSpeedKph >= 0),
    RainProbabilityPercent INT NOT NULL
        CONSTRAINT CK_EventWeather_Rain CHECK (RainProbabilityPercent BETWEEN 0 AND 100),
    Advisory NVARCHAR(500) NULL,
    RecordedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_EventWeather_RecordedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_EventWeather_EventDate UNIQUE (EventID, ForecastDate),
    CONSTRAINT FK_EventWeather_Event
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

CREATE TABLE dbo.Enrolments
(
    EnrolmentID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Enrolments PRIMARY KEY,
    EventID INT NOT NULL,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2(0) NOT NULL
        CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT SYSUTCDATETIME(),
    EmergencyContactName NVARCHAR(100) NOT NULL,
    EmergencyContactPhone NVARCHAR(25) NOT NULL,
    EnrolmentStatus VARCHAR(20) NOT NULL
        CONSTRAINT DF_Enrolments_Status DEFAULT 'Confirmed'
        CONSTRAINT CK_Enrolments_Status CHECK (EnrolmentStatus IN ('Pending', 'Confirmed', 'Withdrawn')),
    BibNumber INT NULL
        CONSTRAINT CK_Enrolments_BibNumber CHECK (BibNumber IS NULL OR BibNumber > 0),
    CONSTRAINT UQ_Enrolments_EventParticipant UNIQUE (EventID, ParticipantID),
    CONSTRAINT FK_Enrolments_Event
        FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID),
    CONSTRAINT FK_Enrolments_Participant
        FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_EventCategory
        FOREIGN KEY (EventID, CategoryID)
        REFERENCES dbo.Categories(EventID, CategoryID)
);
GO

CREATE UNIQUE INDEX UX_Enrolments_EventBibNumber
    ON dbo.Enrolments(EventID, BibNumber)
    WHERE BibNumber IS NOT NULL;
GO

CREATE TABLE dbo.Results
(
    ResultID INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_Results PRIMARY KEY,
    EnrolmentID INT NOT NULL
        CONSTRAINT UQ_Results_Enrolment UNIQUE,
    FinishPosition INT NULL
        CONSTRAINT CK_Results_FinishPosition CHECK
            (FinishPosition IS NULL OR FinishPosition > 0),
    FinishTime TIME(0) NULL,
    PaceMinPerKm DECIMAL(5,2) NULL
        CONSTRAINT CK_Results_Pace CHECK
            (PaceMinPerKm IS NULL OR PaceMinPerKm > 0),
    ResultStatus VARCHAR(20) NOT NULL
        CONSTRAINT DF_Results_Status DEFAULT 'Finished'
        CONSTRAINT CK_Results_Status CHECK (ResultStatus IN ('Finished', 'DNF', 'DNS')),
    RecordedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Results_RecordedAt DEFAULT SYSUTCDATETIME(),
    Notes NVARCHAR(500) NULL,
    CONSTRAINT FK_Results_Enrolment
        FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID),
    CONSTRAINT CK_Results_FinishData CHECK
        (
            (ResultStatus = 'Finished' AND FinishPosition IS NOT NULL AND FinishTime IS NOT NULL)
            OR
            (ResultStatus IN ('DNF', 'DNS'))
        )
);
GO

/*
    Required realistic seed data:
    - 2 Organisers
    - 2 Participants
    - 3 Events
    - Categories for every event
    - Routes and weather information
    - Sample enrolments and official results
*/

SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users
    (UserID, FirstName, LastName, Email, PasswordHash, Role, PhoneNumber)
VALUES
    (1, N'Lerato', N'Mokoena', N'lerato.mokoena@raceday.example',
        N'DEMO_HASH_REPLACE_IN_PART2_001', 'Organiser', N'+27 82 555 0101'),
    (2, N'James', N'Naidoo', N'james.naidoo@raceday.example',
        N'DEMO_HASH_REPLACE_IN_PART2_002', 'Organiser', N'+27 83 555 0102'),
    (3, N'Ayanda', N'Dlamini', N'ayanda.dlamini@raceday.example',
        N'DEMO_HASH_REPLACE_IN_PART2_003', 'Participant', N'+27 72 555 0103'),
    (4, N'Pieter', N'van Wyk', N'pieter.vanwyk@raceday.example',
        N'DEMO_HASH_REPLACE_IN_PART2_004', 'Participant', N'+27 71 555 0104');
SET IDENTITY_INSERT dbo.Users OFF;
GO

SET IDENTITY_INSERT dbo.Events ON;
INSERT INTO dbo.Events
    (EventID, OrganiserID, EventName, Description, EventDate, StartTime,
     Location, Province, EventType, DistanceKm, MaxParticipants, Status)
VALUES
    (1, 1, N'Cape Town Cycle Challenge',
        N'A fully supported coastal cycle event from the city to the Cape Peninsula and back.',
        '2026-10-11', '06:00', N'Grand Parade, Cape Town', N'Western Cape',
        'Cycling', 109.40, 12000, 'Scheduled'),
    (2, 1, N'Soweto Community Marathon',
        N'A city marathon celebrating Soweto landmarks, community spirit, and healthy living.',
        '2026-11-01', '06:30', N'Nasrec Expo Centre, Johannesburg', N'Gauteng',
        'Running', 42.20, 8000, 'Scheduled'),
    (3, 2, N'KwaZulu-Natal Coastline Walk',
        N'A scenic, accessible coastal walk supporting local conservation and active lifestyles.',
        '2026-09-27', '07:00', N'Moses Mabhida Stadium, Durban', N'KwaZulu-Natal',
        'Walking', 10.00, 2500, 'Scheduled');
SET IDENTITY_INSERT dbo.Events OFF;
GO

SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories
    (CategoryID, EventID, CategoryName, GenderOption, MinimumAge, MaximumAge, EntryFee, MaxEntries)
VALUES
    (1, 1, N'Open Cycle Challenge', 'Open', 16, NULL, 850.00, 10000),
    (2, 1, N'Junior Cycle Challenge', 'Open', 12, 17, 450.00, 2000),
    (3, 2, N'Open Marathon', 'Open', 18, NULL, 420.00, 6500),
    (4, 2, N'Women''s Marathon', 'Female', 18, NULL, 420.00, 1500),
    (5, 3, N'Open Coastal Walk', 'Open', 12, NULL, 180.00, 2200),
    (6, 3, N'Senior Coastal Walk', 'Open', 60, NULL, 120.00, 300);
SET IDENTITY_INSERT dbo.Categories OFF;
GO

SET IDENTITY_INSERT dbo.Routes ON;
INSERT INTO dbo.Routes
    (RouteID, EventID, RouteName, DistanceKm, RouteDescription, MapUrl, ElevationGainMeters)
VALUES
    (1, 1, N'Peninsula Coastal Loop', 109.40,
        N'Grand Parade through Camps Bay, Chapman''s Peak, Simon''s Town, and back along the M3.',
        N'https://maps.example/raceday/cape-town-cycle', 1450),
    (2, 2, N'Soweto Heritage Marathon Route', 42.20,
        N'Nasrec through Orlando, Vilakazi Street, Dobsonville, and returning to Nasrec.',
        N'https://maps.example/raceday/soweto-marathon', 410),
    (3, 3, N'Durban Golden Mile Out-and-Back', 10.00,
        N'Moses Mabhida Stadium along the beachfront promenade and back to the stadium.',
        N'https://maps.example/raceday/kzn-coastline-walk', 55);
SET IDENTITY_INSERT dbo.Routes OFF;
GO

INSERT INTO dbo.EventWeather
    (EventID, ForecastDate, TemperatureC, Condition, WindSpeedKph,
     RainProbabilityPercent, Advisory)
VALUES
    (1, '2026-10-10', 17.5, N'Partly cloudy', 18.0, 20,
        N'Expect a cool start; cyclists should carry a light wind layer.'),
    (1, '2026-10-11', 19.0, N'Sunny intervals', 22.0, 15,
        N'Strong coastal crosswinds are possible after Chapman''s Peak.'),
    (2, '2026-10-31', 16.0, N'Clear morning', 10.0, 10,
        N'Good running conditions with a cool start.'),
    (2, '2026-11-01', 21.0, N'Warm and clear', 12.0, 5,
        N'Carry water and use the marked hydration stations.'),
    (3, '2026-09-26', 18.0, N'Light cloud', 14.0, 25,
        N'Comfortable walking conditions; light rain remains possible.'),
    (3, '2026-09-27', 22.0, N'Partly cloudy', 16.0, 20,
        N'Use sun protection on the exposed beachfront section.');
GO

SET IDENTITY_INSERT dbo.Enrolments ON;
INSERT INTO dbo.Enrolments
    (EnrolmentID, EventID, ParticipantID, CategoryID, EnrolmentDate,
     EmergencyContactName, EmergencyContactPhone, EnrolmentStatus, BibNumber)
VALUES
    (1, 1, 3, 1, '2026-07-10 09:15:00',
        N'Thabo Dlamini', N'+27 82 555 0191', 'Confirmed', 1042),
    (2, 1, 4, 1, '2026-07-11 14:40:00',
        N'Marie van Wyk', N'+27 72 555 0192', 'Confirmed', 1043),
    (3, 2, 3, 3, '2026-07-15 10:05:00',
        N'Thabo Dlamini', N'+27 82 555 0191', 'Confirmed', 231),
    (4, 2, 4, 3, '2026-07-16 16:20:00',
        N'Marie van Wyk', N'+27 72 555 0192', 'Confirmed', 232),
    (5, 3, 3, 5, '2026-07-20 08:30:00',
        N'Thabo Dlamini', N'+27 82 555 0191', 'Confirmed', 87);
SET IDENTITY_INSERT dbo.Enrolments OFF;
GO

SET IDENTITY_INSERT dbo.Results ON;
INSERT INTO dbo.Results
    (ResultID, EnrolmentID, FinishPosition, FinishTime, PaceMinPerKm,
     ResultStatus, Notes)
VALUES
    (1, 1, 148, '04:15:00', 2.33, 'Finished',
        N'Strong finish after the coastal climb.'),
    (2, 2, 207, '04:32:00', 2.49, 'Finished',
        N'Completed safely in windy conditions.'),
    (3, 3, 36, '03:58:00', 5.64, 'Finished',
        N'Personal best marathon result.'),
    (4, 4, 51, '04:22:00', 6.21, 'Finished',
        N'Consistent pacing throughout the route.');
SET IDENTITY_INSERT dbo.Results OFF;
GO

/* Quick verification queries for SSMS demonstration during the Part 1 video. */
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;

SELECT u.Role, COUNT(*) AS UserCount
FROM dbo.Users AS u
GROUP BY u.Role;

SELECT e.EventName, COUNT(c.CategoryID) AS CategoryCount,
       COUNT(en.EnrolmentID) AS EnrolmentCount
FROM dbo.Events AS e
LEFT JOIN dbo.Categories AS c ON c.EventID = e.EventID
LEFT JOIN dbo.Enrolments AS en ON en.EventID = e.EventID
GROUP BY e.EventID, e.EventName
ORDER BY e.EventID;

SELECT e.EventName, en.BibNumber, u.FirstName, u.LastName,
       r.FinishPosition, r.FinishTime, r.ResultStatus
FROM dbo.Results AS r
INNER JOIN dbo.Enrolments AS en ON en.EnrolmentID = r.EnrolmentID
INNER JOIN dbo.Users AS u ON u.UserID = en.ParticipantID
INNER JOIN dbo.Events AS e ON e.EventID = en.EventID
ORDER BY e.EventDate, r.FinishPosition;