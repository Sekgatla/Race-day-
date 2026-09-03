# RaceDay Event Management System

RaceDay is a planned event-management system for South African running, walking, and cycling events.

## Run & Operate

- `pnpm --filter @workspace/api-server run dev` — run the API server (port 5000)
- `pnpm run typecheck` — full typecheck across all packages
- `pnpm run build` — typecheck + build all packages
- `pnpm --filter @workspace/api-spec run codegen` — regenerate API hooks and Zod schemas from the OpenAPI spec
- `pnpm --filter @workspace/db run push` — push DB schema changes (dev only)
- Required env: `DATABASE_URL` — Postgres connection string

## Stack

- pnpm workspaces, Node.js 24, TypeScript 5.9
- API: Express 5
- DB: PostgreSQL + Drizzle ORM
- Validation: Zod (`zod/v4`), `drizzle-zod`
- API codegen: Orval (from OpenAPI spec)
- Build: esbuild (CJS bundle)

## Where things live

- `docs/RaceDay_ERD.dot` and `docs/RaceDay_ERD.pdf` — Part 1 ERD source and export
- `docs/RaceDay_API_Endpoint_Plan.md` — Part 1 REST endpoint blueprint
- `docs/RaceDay_Database.sql` — SQL Server schema, constraints, and seed data
- `docs/RaceDay_Data_Dictionary.md` — database field meanings and integrity rules
- `docs/RaceDay_Role_Permissions.md` — role and ownership matrix
- `docs/RaceDay_Validation_Plan.md` — automated and SSMS validation plan
- `.github/workflows/part1-ci.yml` — required-file and content validation

## Architecture decisions

- `Enrolments` resolves the many-to-many relationship between participants and events and stores the selected category.
- `Results` references an enrolment and has a unique enrolment key, allowing at most one official result per entry.
- `Routes` models one optional route per event, while `EventWeather` supports multiple forecast dates per event.
- `Users.Role` is constrained to `Organiser` or `Participant` so API authorization and the database design share one role model.

## Product

Part 1 plans the data model and API for organiser event management, participant enrolment, race results, route information, and weather information. Parts 2 and 3 will implement the planned API and MVC application.

## User preferences

- Complete Part 1 only; do not build the API or MVC application in this stage.
- Keep the repository evidence-focused: the README, ERD, API plan, SQL script, references, and validation artefacts must agree on entities and roles.

## Gotchas

- The SQL deliverable targets SQL Server/SSMS and intentionally uses `GO`, `IDENTITY`, and SQL Server filtered indexes.
- The CI screenshot and unlisted video link must be added by the student after the GitHub workflow and video are available.

## Pointers

- See the `pnpm-workspace` skill for workspace structure, TypeScript setup, and package details
