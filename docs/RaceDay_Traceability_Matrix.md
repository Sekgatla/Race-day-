# RaceDay Part 1 Traceability Matrix

This matrix shows how the main assessment requirements are represented in the repository.

| Requirement area | Design response | Evidence |
|---|---|---|
| Organiser event management | Organiser-owned events, categories, routes, weather, enrolments, and results | ERD, API endpoint plan, role matrix, SQL foreign keys |
| Participant registration | Participant accounts and event enrolments through the bridge entity | ERD, API endpoint plan, `Enrolments` table |
| Event categories | Categories belong to one event and are selected during enrolment | ERD, data dictionary, SQL constraints |
| Race results | Results reference enrolments and enforce one result per enrolment | ERD, API endpoint plan, unique result constraint |
| Route information | Optional one-to-one route information for an event | ERD, API endpoint plan, `Routes` table |
| Weather information | Multiple forecast records can be stored for an event | ERD, API endpoint plan, `EventWeather` table |
| Role-based access | `Organiser` and `Participant` are used consistently in the API plan and database | Role permissions matrix, SQL role check constraint |
| Data integrity | Primary keys, foreign keys, unique keys, check constraints, and defaults | SQL Server database script |
| Planning references | Assessment guide is cited in Harvard Anglia style | `docs/RaceDay_References.md` |
| Quality evidence | Automated checks and manual SSMS checks are defined | GitHub Actions workflow and validation plan |