# RaceDay Event Management System

> **Assessment scope:** PROG6212 Programming 2B — PoE Part 1

## Project status

RaceDay Part 1 is complete as a planning and database-design submission. The repository contains the system plan, seven-entity ERD, SQL Server database script, REST endpoint plan, Harvard Anglia references, and automated documentation checks.

Parts 2 and 3 are intentionally out of scope for this submission. They will implement the API and MVC application described by the Part 1 plan.

RaceDay is a web-based event management system for South African road running, walking, and cycling events. It replaces paper registrations, disconnected spreadsheets, and manual result tracking with one planned system for organisers and participants.

## User roles

### Organiser

An Organiser can:

- Create, edit, cancel, and manage events.
- Create and manage event categories.
- View participant enrolments for managed events.
- Capture and correct finish times, finishing positions, and result statuses.
- Maintain route and race-day weather information.

### Participant

A Participant can:

- Create an account and log in.
- Browse upcoming events and categories.
- Enrol in an event by selecting a category.
- View their own enrolments.
- View their race results and performance history.

## Part 1: System Planning and Database

Part 1 is the planning and database stage. The API and MVC application are intentionally not included yet.

| Deliverable | Location | Description |
|---|---|---|
| Entity Relationship Diagram | [`docs/RaceDay_ERD.pdf`](docs/RaceDay_ERD.pdf) | Seven-entity relational design with keys, attributes, relationships, and cardinality. |
| ERD source | [`docs/RaceDay_ERD.dot`](docs/RaceDay_ERD.dot) | Editable Graphviz source for the exported ERD. |
| API Endpoint Plan | [`docs/RaceDay_API_Endpoint_Plan.md`](docs/RaceDay_API_Endpoint_Plan.md) | REST API blueprint covering all required resources, roles, request bodies, and responses. |
| SQL Database Script | [`docs/RaceDay_Database.sql`](docs/RaceDay_Database.sql) | SQL Server script that creates, constrains, and seeds the RaceDay database. |
| Harvard Anglia references | [`docs/RaceDay_References.md`](docs/RaceDay_References.md) | Reference list for the assessment guide used as the planning source. |

## Repository structure

```text
RaceDay/
├── README.md
├── docs/
│   ├── RaceDay_ERD.dot
│   ├── RaceDay_ERD.pdf
│   ├── RaceDay_API_Endpoint_Plan.md
│   ├── RaceDay_Database.sql
│   └── RaceDay_References.md
└── .github/
    └── workflows/
        └── part1-ci.yml
```

## Database setup

1. Open Microsoft SQL Server Management Studio.
2. Open [`docs/RaceDay_Database.sql`](docs/RaceDay_Database.sql).
3. Execute the complete script from the first line to the last line.
4. The script creates `RaceDayDb`, recreates the tables, applies keys and constraints, and inserts realistic sample records.
5. Review the verification queries at the end of the script to show the tables, role counts, event categories, enrolments, and results.

The script is designed to be rerunnable for Part 1 development. The sample `PasswordHash` values are clearly labelled placeholders and must be replaced by the authentication implementation in Part 2.

## CI/CD validation

The [`part1-ci.yml`](.github/workflows/part1-ci.yml) workflow checks that:

- The `docs` and `.github/workflows` folders exist.
- The ERD PDF and editable source exist.
- The API endpoint plan and SQL script exist.
- The README exists.
- Required API sections and SQL entities/roles are present.

The workflow is designed to run on every push and pull request. It checks the repository structure, required deliverables, API-plan sections, SQL entities, and role markers. After the workflow completes on GitHub, the successful run should be recorded below:

> **CI evidence:** The successful GitHub Actions screenshot will be added here after the first remote workflow run.

## Video presentation

The Part 1 video should explain the RaceDay scenario, the seven ERD entities and their cardinalities, the role-based API endpoint plan, the SQL constraints and seed data, and a live successful SSMS execution.

> **YouTube link:** Add the unlisted Part 1 video link here before submitting.

## Referencing

The RaceDay Part 1 planning decisions are based on the supplied assessment guide and are cited using Harvard Anglia style. See [`docs/RaceDay_References.md`](docs/RaceDay_References.md) for the full reference.

## Submission reminders

- Use the GitHub repository supplied for the assessment.
- Maintain at least 20 genuine commits that represent meaningful planning, design, documentation, or validation work.
- Push all work to GitHub rather than submitting a ZIP file.
- Add the green CI screenshot and unlisted YouTube link before submitting the repository link on ARC.