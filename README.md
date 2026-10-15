<div align="center">
<h1>🏃 RaceDay</h1>
<p><strong>Event management and race results for South African road events</strong><br>PROG6212 · Programming 2B · Portfolio of Evidence</p>
<p>
<a href="https://github.com/Sekgatla/Race-day-/actions/workflows/part1-ci.yml"><img alt="Part 1 documentation check" src="https://github.com/Sekgatla/Race-day-/actions/workflows/part1-ci.yml/badge.svg"></a>
<a href="https://github.com/Sekgatla/Race-day-/actions/workflows/part2-ci.yml"><img alt="Part 2 API build and tests" src="https://github.com/Sekgatla/Race-day-/actions/workflows/part2-ci.yml/badge.svg"></a>
<img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white">
<img alt="SQL Server" src="https://img.shields.io/badge/Database-SQL%20Server-CC2927?logo=microsoft-sql-server&logoColor=white">
</p>
</div>

---

## Contents
- [Project Description](#project-description)
- [User Roles](#user-roles)
- [Part 1: System Planning](#part-1-system-planning)
- [Part 2: RESTful API](#part-2-restful-api)
  - [Technologies Used](#technologies-used)
  - [Project Structure](#project-structure)
  - [Database Setup](#database-setup)
  - [How to Run the API](#how-to-run-the-api)
  - [Authentication](#authentication)
  - [API Endpoints](#api-endpoints)
  - [Swagger](#swagger)
  - [Unit Testing](#unit-testing)
  - [CI/CD](#cicd)
  - [GitHub Actions Screenshot](#github-actions-screenshot)
  - [Video Presentation](#video-presentation)
- [AI Assistance and References](#ai-assistance-and-references)

## Project Description
RaceDay is a web-based event management system for South African road running, walking, and cycling events. It replaces paper registrations, disconnected spreadsheets, and manual result tracking with one API-backed platform.

The Portfolio of Evidence is built in stages: **Part 1** plans the data model and API; **Part 2** implements the RESTful API; **Part 3** will add the MVC application and Docker packaging. Part 2 follows the ERD, endpoint plan, and database design submitted in Part 1.

## User Roles
| Role | Responsibilities |
|:---|:---|
| **Organiser** | Manage owned events and categories, view enrolments, record results, and maintain event route and weather information. |
| **Participant** | Create an account, browse events, choose a category, enrol, and view personal results. |

The API checks authentication, role, and event ownership before protected operations.

## Part 1: System Planning
Part 1 planning evidence remains in the repository-root [`docs/`](docs/) folder. These documents are the specification for Part 2.

| Evidence | File |
|:---|:---|
| Entity Relationship Diagram | [RaceDay_ERD.pdf](docs/RaceDay_ERD.pdf) · [editable Graphviz source](docs/RaceDay_ERD.dot) |
| API endpoint plan | [RaceDay_API_Endpoint_Plan.md](docs/RaceDay_API_Endpoint_Plan.md) |
| SQL database design | [RaceDay_Database.sql](docs/RaceDay_Database.sql) |
| Part 1 CI screenshot | [ci-green-build.png](docs/ci-green-build.png) |
| Part 1 video | [Unlisted presentation](https://youtu.be/SWQssPZqI5k) |

The domain model contains seven entities: Users, Events, Categories, Enrolments, Results, Routes, and EventWeather. Enrolments connects participants, events, and selected categories; a result is recorded after an enrolment is completed.

## Part 2: RESTful API
The complete Part 2 source, tests, migration, and supporting documents are grouped in [`Part 2/`](Part%202/). The Part 2 workflow remains under the repository-root `.github/workflows/` folder so GitHub can detect it; it builds the solution from `Part 2/`.

### Technologies Used
| Area | Technology |
|:---|:---|
| API | ASP.NET Core Web API on .NET 8 |
| Data access | Entity Framework Core Code First |
| Database | Microsoft SQL Server / LocalDB |
| API documentation | Swagger / OpenAPI |
| Testing | xUnit and ASP.NET Core integration-test host |
| CI/CD | GitHub Actions |

### Project Structure
```text
Race-day-/
├── README.md                         # Single project and submission guide
├── docs/                             # Part 1 planning evidence
├── Part 2/                           # Complete Part 2 implementation
│   ├── .config/dotnet-tools.json
│   ├── RaceDay.sln
│   ├── RaceDay.Api/                  # Controllers, data, models, security
│   ├── RaceDay.Contracts/            # Shared request and response DTOs
│   ├── RaceDay.Api.Tests/            # Automated API tests
│   └── docs/                         # Part 2 supporting design documents
└── .github/workflows/               # Part 1 and Part 2 CI workflows
```

### Database Setup
The default connection in `Part 2/RaceDay.Api/appsettings.json` targets Windows SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

To use another SQL Server instance, set `RACEDAY_SQLSERVER_CONNECTION` before starting the API. Example in PowerShell:

```powershell
$env:RACEDAY_SQLSERVER_CONNECTION = 'Server=.\SQLEXPRESS;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true'
```

Do not commit database passwords or other credentials. The EF Core migration is included in `Part 2/RaceDay.Api/Data/Migrations/`. After configuring SQL Server or LocalDB, apply it and confirm the database tables exist:

```bash
cd "Part 2"
dotnet tool restore
dotnet restore RaceDay.sln
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
```

The test project uses an in-memory provider for automated tests; a passing CI run does not by itself verify a separate local SQL Server installation.

### How to Run the API
From the repository root:

```bash
cd "Part 2"
dotnet tool restore
dotnet restore RaceDay.sln
dotnet run --project RaceDay.Api
```

The API also applies pending migrations at startup. In Visual Studio, open `Part 2/RaceDay.sln` and set `RaceDay.Api` as the startup project.

### Authentication
RaceDay supports Organiser and Participant registration and login. Passwords are hashed with ASP.NET Core Identity's `PasswordHasher<User>`; plaintext passwords are not stored. Login returns an opaque bearer session token. The server stores a hash of the token with the user's ID, role, and expiry, and logout revokes the session.

Session state is held in memory and is cleared when the API restarts, so users must log in again after a restart. Role and ownership checks restrict Organiser-only and Participant-only actions. Unauthenticated protected requests return `401`; authenticated requests with the wrong role or ownership return `403`.

For an empty development database, the API seeds demo Organiser and Participant accounts. The demo password is `RaceDay!2026`; do not reuse it for real accounts.

### API Endpoints
| Area | Routes |
|:---|:---|
| Authentication | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` |
| Profile | `GET /api/users/me`, `PUT /api/users/me` |
| Events | `GET /api/events`, `GET /api/events/{eventId}`, `GET /api/organiser/events`, `POST /api/events`, `PUT /api/events/{eventId}`, `DELETE /api/events/{eventId}` |
| Categories | `GET/POST /api/events/{eventId}/categories`, `PUT/DELETE /api/categories/{categoryId}` |
| Enrolments | `POST /api/events/{eventId}/enrolments`, `GET /api/enrolments/me`, `GET /api/enrolments/{enrolmentId}`, `GET /api/events/{eventId}/enrolments`, `PUT/DELETE /api/enrolments/{enrolmentId}` |
| Results | `POST /api/enrolments/{enrolmentId}/result`, `PUT /api/results/{resultId}`, `GET /api/results/me`, `GET /api/events/{eventId}/results`, `GET /api/results/{resultId}` |
| Routes | `GET/PUT /api/events/{eventId}/route` |
| Weather | `GET/POST /api/events/{eventId}/weather` |

The API checks event ownership, duplicate entries, capacity, and result-publishing rules. The Part 1 endpoint plan remains the reference; explain any deliberate deviations before submission.

### Swagger
In Development, open `/api/swagger` after starting the API. Use Swagger's **Authorize** control with the bearer token returned by login to test protected calls. Demonstrate a complete flow: register, log in, create an event and categories, enrol as a Participant, view enrolments, record a result, and view personal results.

### Unit Testing
Run the test suite from the repository root:

```bash
cd "Part 2"
dotnet test RaceDay.sln --configuration Release
```

Tests cover registration and password hashing, login and logout, invalid credentials, unauthenticated requests, role and ownership restrictions, event creation, enrolment, result capture, seeded public endpoints, and Swagger. They include successful and rejected-access scenarios.

### CI/CD
The [Part 2 GitHub Actions workflow](.github/workflows/part2-ci.yml) runs on pushes and pull requests to `main`. It restores, builds, and tests `Part 2/RaceDay.sln`. GitHub requires workflow files to live in the repository-root `.github/workflows/` directory.

[![Part 2 API Build and Tests](https://github.com/Sekgatla/Race-day-/actions/workflows/part2-ci.yml/badge.svg)](https://github.com/Sekgatla/Race-day-/actions/workflows/part2-ci.yml) · [View workflow runs](https://github.com/Sekgatla/Race-day-/actions/workflows/part2-ci.yml)

The assessment guide requires at least **20 meaningful Part 2 commits**. Keep genuine implementation milestones in the history; do not manufacture filler commits.

### GitHub Actions Screenshot
The supplied screenshot is the successful **Part 1** documentation workflow:

![Successful Part 1 GitHub Actions build](docs/ci-green-build.png)

> The Part 2 workflow has passed;(doc/IMG_5128.png)

### Video Presentation
The presentations should show the running API and explain the code and design decisions. The Part 2 guide expects a voiceover walkthrough of the repository, data model, authentication, role restrictions, Swagger, tests, and green CI build.

- **Part 1:** [Unlisted presentation](https://youtu.be/SWQssPZqI5k)
- **Part 2:** Add the unlisted Part 2 YouTube link here after recording the presentation.

## AI Assistance and References
Replit Agent assisted with planning, initial implementation, tests, and documentation for Part 2. Review the work, make sure you can explain it, and disclose AI assistance as required by the assessment rules. Update this statement if other tools contributed.

- Google (2026), Gemini. https://gemini.google.com
- Graphviz (2026), Graphviz — Graph Visualization Software. https://graphviz.org/
- Lucid Software (2026), Lucidchart. https://www.lucidchart.com/
- Microsoft (2026), GitHub Actions documentation. https://docs.github.com/en/actions
- Microsoft (2026), Visio. https://www.microsoft.com/en-us/microsoft-365/visio/flowchart-software
