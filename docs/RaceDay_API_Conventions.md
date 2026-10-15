# RaceDay API Conventions

These conventions support the endpoint plan in `RaceDay_API_Endpoint_Plan.md`.

- **Base path:** all endpoint paths start with `/api`.
- **JSON names:** request and response properties use camelCase.
- **Dates and times:** dates use ISO 8601 (`yyyy-MM-dd`); times use `HH:mm:ss`.
- **Roles:** exact role values are `Organiser` and `Participant`.
- **Authentication:** login returns an opaque bearer session token; send it as `Authorization: Bearer <token>`.
- **Session lifetime:** sessions expire after 12 hours and are revoked by logout. In-memory sessions end when the API process restarts.
- **Success responses:** reads and updates return `200 OK`; creates return `201 Created`; deletes and logout return `204 No Content`.
- **Errors:** validation returns `400 Bad Request`; unauthenticated requests return `401 Unauthorized`; wrong role or ownership returns `403 Forbidden`; missing resources return `404 Not Found`; duplicates and business-rule conflicts return `409 Conflict`.
- **Password handling:** plaintext passwords are accepted only for registration/login. The API stores an ASP.NET Core Identity password hash and never returns it.
- **Database:** SQL Server is the default EF Core provider. The in-memory provider is selected explicitly for automated tests and the Replit preview only.

The Part 2 implementation uses the endpoint routes and domain entities from the submitted Part 1 plan. Session state is kept outside the seven-table domain schema so no new database entity is introduced.
