# RaceDay Event Management System

RaceDay is a planned event management system for South African running, walking, and cycling events. The idea is to replace paper registrations, separate spreadsheets, and manual result tracking with one organised system for event organisers and participants.

## User roles

### Organiser

Organisers can create and manage events, set up event categories, view enrolments, capture results, and maintain route and weather information.

### Participant

Participants can create an account, browse events, choose a category, enrol in an event, and view their own results.

## Part 1 work

This repository contains my Part 1 planning work:

- [`docs/RaceDay_ERD.pdf`](docs/RaceDay_ERD.pdf) — the seven-entity ERD.
- [`docs/RaceDay_ERD.dot`](docs/RaceDay_ERD.dot) — editable ERD source.
- [`docs/RaceDay_API_Endpoint_Plan.md`](docs/RaceDay_API_Endpoint_Plan.md) — planned REST endpoints and access roles.
- [`docs/RaceDay_Database.sql`](docs/RaceDay_Database.sql) — SQL Server database script with constraints and sample data.

The seven entities are Users, Events, Categories, Enrolments, Results, Routes, and EventWeather. Enrolments connects participants to events, while Results stores the result for an enrolment.

## Database

The database script is written for Microsoft SQL Server and can be opened in SQL Server Management Studio. It creates the `RaceDayDb` database, sets up the relationships and constraints, and inserts sample organisers, participants, events, categories, enrolments, routes, weather records, and results.

The script can be run from top to bottom and includes verification queries at the end.

## GitHub Actions

The repository includes a small workflow in [`.github/workflows/part1-ci.yml`](.github/workflows/part1-ci.yml). It checks that the main Part 1 files are present and that the endpoint plan and SQL script contain the expected sections.

## Video

Part 1 walkthrough: **[YouTube link](PASTE_YOUR_UNLISTED_YOUTUBE_LINK_HERE)**

## Repository structure

```text
Race-day-/
├── README.md
├── docs/
│   ├── RaceDay_ERD.dot
│   ├── RaceDay_ERD.pdf
│   ├── RaceDay_API_Endpoint_Plan.md
│   └── RaceDay_Database.sql
└── .github/
    └── workflows/
        └── part1-ci.yml
```

Parts 2 and 3 will build on this plan with the API and MVC application.