# RaceDay — PROG6212 Portfolio of Evidence

This repository keeps the two submitted parts clearly separated.

## Part 1 — System planning

Part 1 planning evidence remains in [`docs/`](docs/): the ERD, API endpoint plan, and SQL database script.

- [RaceDay ERD](docs/RaceDay_ERD.pdf)
- [API endpoint plan](docs/RaceDay_API_Endpoint_Plan.md)
- [SQL database script](docs/RaceDay_Database.sql)

## Part 2 — RESTful API

The complete Part 2 project is in the [`Part 2/` folder](Part%202/README.md). Its detailed README covers setup, API endpoints, authentication, tests, and CI.

## GitHub Actions

[Part 1 documentation check](.github/workflows/part1-ci.yml) and [Part 2 API build and tests](.github/workflows/part2-ci.yml) run from the repository-root `.github/workflows/` directory, as required by GitHub. The Part 2 workflow builds and tests `Part 2/RaceDay.sln`.

## Repository layout

```text
Race-day-/
├── README.md                         # Submission map
├── docs/                             # Part 1 planning evidence
├── Part 2/                           # Complete Part 2 project
│   ├── README.md
│   ├── RaceDay.sln
│   ├── RaceDay.Api/
│   ├── RaceDay.Contracts/
│   ├── RaceDay.Api.Tests/
│   └── docs/
└── .github/workflows/               # Part 1 and Part 2 CI workflows
```
