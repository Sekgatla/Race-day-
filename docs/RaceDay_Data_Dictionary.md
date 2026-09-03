# RaceDay Part 1 Data Dictionary

This data dictionary explains the purpose of each field in the seven-table RaceDay database. It complements the ERD and the executable SQL Server script.

## Users

| Field | Purpose |
|---|---|
| `UserID` | Surrogate primary key for an organiser or participant account. |
| `FirstName`, `LastName` | Person's name used in profiles and event records. |
| `Email` | Login and contact address; unique per account. |
| `PasswordHash` | Stored password hash placeholder for the future authentication layer. |
| `Role` | Authorisation role constrained to `Organiser` or `Participant`. |
| `PhoneNumber` | Optional contact number for event communication. |
| `CreatedAt` | Account creation timestamp. |
| `IsActive` | Allows an account to be deactivated without deleting its history. |

## Events

| Field | Purpose |
|---|---|
| `EventID` | Primary key for a race-day event. |
| `OrganiserID` | Foreign key identifying the organiser who owns the event. |
| `EventName` | Public event name. |
| `EventDate` | Scheduled date of the event. |
| `Venue` | Main event location. |
| `Description` | Public event information and instructions. |
| `Status` | Lifecycle state such as `Draft`, `Published`, `Cancelled`, or `Completed`. |
| `CreatedAt` | Timestamp used for audit and ordering. |

## Categories

| Field | Purpose |
|---|---|
| `CategoryID` | Primary key for an event category. |
| `EventID` | Foreign key linking the category to one event. |
| `CategoryName` | Display name such as 10 km Run or 5 km Walk. |
| `DistanceKm` | Numeric distance used for entry and result reporting. |
| `MaximumEntries` | Optional capacity for the category. |
| `EntryFee` | Entry price stored as a fixed-precision monetary value. |

## Enrolments

| Field | Purpose |
|---|---|
| `EnrolmentID` | Primary key for a participant's event entry. |
| `EventID` | Foreign key for the selected event. |
| `CategoryID` | Foreign key for the selected category. |
| `ParticipantID` | Foreign key for the participant account. |
| `EnrolmentDate` | Date and time when the entry was created. |
| `Status` | Entry state such as `Pending`, `Confirmed`, `Withdrawn`, or `Completed`. |
| `RaceNumber` | Optional number assigned for race-day identification. |

## Results

| Field | Purpose |
|---|---|
| `ResultID` | Primary key for an official result. |
| `EnrolmentID` | Unique foreign key ensuring one result per enrolment. |
| `FinishTime` | Recorded finish time. |
| `FinishPosition` | Overall or category position recorded by the organiser. |
| `ResultStatus` | State such as `Official`, `DNF`, or `DQ`. |
| `RecordedAt` | Timestamp for result capture and correction auditing. |

## Routes

| Field | Purpose |
|---|---|
| `RouteID` | Primary key for a route. |
| `EventID` | Unique foreign key for the event's optional route. |
| `RouteName` | Human-readable route name. |
| `DistanceKm` | Published route distance. |
| `ElevationGainM` | Optional elevation gain in metres. |
| `MapUrl` | Optional link to a route map or course file. |

## EventWeather

| Field | Purpose |
|---|---|
| `WeatherID` | Primary key for a weather record. |
| `EventID` | Foreign key for the related event. |
| `ForecastDate` | Date represented by the forecast. |
| `Condition` | Description such as sunny, cloudy, or rain. |
| `TemperatureC` | Forecast temperature in degrees Celsius. |
| `WindSpeedKph` | Forecast wind speed in kilometres per hour. |
| `RainProbability` | Percentage chance of rain constrained to 0–100. |

## Integrity rules

- Every foreign key must reference an existing parent record.
- A category belongs to one event, and an enrolment selects a category for its event.
- A participant may enter multiple events, while a result belongs to at most one enrolment.
- Email addresses and race numbers follow the uniqueness rules defined in the SQL script.
- Role and status values are constrained so that invalid workflow states cannot be inserted accidentally.