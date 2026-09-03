# RaceDay API Endpoint Plan

**System:** RaceDay Event Management System  
**Planned base URL:** `/api`  
**Document purpose:** Part 1 planning blueprint for the RESTful API to be developed in Part 2.

This endpoint plan follows the RaceDay roles and minimum functionality specified by The Independent Institute of Education (The IIE, 2026).

## Roles and access rules

| Role value | Meaning |
|---|---|
| `None` | Public endpoint; no login required |
| `Any` | Any authenticated RaceDay user |
| `Organiser` | Authenticated organiser only |
| `Participant` | Authenticated participant only |
| `Owner / Organiser` | The participant who owns the record or the organiser who owns the event |

Authentication is planned around a bearer access token returned by login. Passwords will be received only during registration/login and stored as a secure hash by the Part 2 API; the database seed values are clearly marked demo placeholders in the SQL script.

## Endpoint plan

### Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new RaceDay account and validates that the email address is not already registered. | None | `{ firstName, lastName, email, password, role, phoneNumber }` | `201 Created` with user profile; `400 Bad Request` for validation errors; `409 Conflict` for an existing email. |
| POST | `/api/auth/login` | Authenticates a registered user and returns an access token with the user ID and role. | None | `{ email, password }` | `200 OK` with `{ token, user }`; `400 Bad Request` for malformed input; `401 Unauthorized` for invalid credentials. |
| POST | `/api/auth/logout` | Invalidates the current session/token according to the Part 2 authentication implementation. | Any | No JSON body; bearer token required. | `204 No Content`; `401 Unauthorized` when the token is missing or invalid. |

### User profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the profile of the currently authenticated user without exposing the password hash. | Any | None | `200 OK` with `{ userId, firstName, lastName, email, role, phoneNumber }`; `401 Unauthorized`. |
| PUT | `/api/users/me` | Updates editable profile information for the currently authenticated user. The role cannot be changed through this endpoint. | Any | `{ firstName, lastName, phoneNumber }` | `200 OK` with the updated profile; `400 Bad Request`; `401 Unauthorized`. |

### Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Returns a searchable, filterable list of scheduled and completed events. | None | None; optional query parameters: `eventType`, `province`, `fromDate`, `toDate`, `status`, `search`. | `200 OK` with an event array; `400 Bad Request` for invalid filters. |
| GET | `/api/events/{eventId}` | Returns the complete public details for one event, including its organiser display name, categories, route summary, and latest weather forecast. | None | None | `200 OK` with the event detail; `404 Not Found` when the event does not exist. |
| GET | `/api/organiser/events` | Returns events created by the authenticated organiser. | Organiser | None; optional `status` query parameter. | `200 OK` with the organiser's event array; `401 Unauthorized`; `403 Forbidden`. |
| POST | `/api/events` | Creates a new road running, walking, or cycling event owned by the authenticated organiser. | Organiser | `{ eventName, description, eventDate, startTime, location, province, eventType, distanceKm, maxParticipants }` | `201 Created` with the new event; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`. |
| PUT | `/api/events/{eventId}` | Updates an event owned by the authenticated organiser. | Organiser | `{ eventName, description, eventDate, startTime, location, province, eventType, distanceKm, maxParticipants, status }` | `200 OK` with the updated event; `400 Bad Request`; `403 Forbidden` if not the owner; `404 Not Found`. |
| DELETE | `/api/events/{eventId}` | Deletes an event owned by the organiser when it has no confirmed enrolments, or marks it cancelled according to the Part 2 business rule. | Organiser | None | `204 No Content`; `403 Forbidden`; `404 Not Found`; `409 Conflict` when confirmed enrolments prevent deletion. |

### Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Returns all categories available for a specific event. | None | None | `200 OK` with category array; `404 Not Found` when the event does not exist. |
| POST | `/api/events/{eventId}/categories` | Adds a category to an event owned by the authenticated organiser. | Organiser | `{ categoryName, genderOption, minimumAge, maximumAge, entryFee, maxEntries }` | `201 Created`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict` for a duplicate category name. |
| PUT | `/api/categories/{categoryId}` | Updates category rules and pricing for an event owned by the authenticated organiser. | Organiser | `{ categoryName, genderOption, minimumAge, maximumAge, entryFee, maxEntries }` | `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict`. |
| DELETE | `/api/categories/{categoryId}` | Removes a category when it has no enrolments. | Organiser | None | `204 No Content`; `403 Forbidden`; `404 Not Found`; `409 Conflict` when enrolments exist. |

### Event enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/enrolments` | Enrols the participant in an event and records the selected category and emergency contact details. | Participant | `{ categoryId, emergencyContactName, emergencyContactPhone }` | `201 Created` with the enrolment; `400 Bad Request`; `401 Unauthorized`; `404 Not Found`; `409 Conflict` for a duplicate enrolment, closed event, or full category. |
| GET | `/api/enrolments/me` | Returns the authenticated participant's enrolments, with event, category, status, and result summary. | Participant | None; optional `status` query parameter. | `200 OK` with enrolment array; `401 Unauthorized`; `403 Forbidden` for an organiser account. |
| GET | `/api/enrolments/{enrolmentId}` | Returns one enrolment to its participant owner or the organiser responsible for the event. | Owner / Organiser | None | `200 OK` with enrolment details; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`. |
| GET | `/api/events/{eventId}/enrolments` | Returns all enrolments for an event owned by the authenticated organiser. | Organiser | None; optional `status` and `categoryId` query parameters. | `200 OK` with participant enrolment list; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`. |
| PUT | `/api/enrolments/{enrolmentId}` | Allows a participant to change their category or emergency contact before the event closes. | Participant | `{ categoryId, emergencyContactName, emergencyContactPhone }` | `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict` when changes are closed. |
| DELETE | `/api/enrolments/{enrolmentId}` | Withdraws the participant's own enrolment before the event deadline. | Participant | None | `204 No Content`; `403 Forbidden`; `404 Not Found`; `409 Conflict` after the withdrawal deadline. |

### Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/result` | Captures a participant's finish time, finishing position, pace, and result status for an enrolment. | Organiser | `{ finishPosition, finishTime, paceMinPerKm, resultStatus, notes }` | `201 Created`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if a result already exists. |
| PUT | `/api/results/{resultId}` | Corrects a result recorded for an event owned by the authenticated organiser. | Organiser | `{ finishPosition, finishTime, paceMinPerKm, resultStatus, notes }` | `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`. |
| GET | `/api/results/me` | Returns the authenticated participant's results and performance history across events. | Participant | None; optional `fromDate`, `toDate`, `eventType` filters. | `200 OK` with result history; `401 Unauthorized`; `403 Forbidden` for an organiser account. |
| GET | `/api/events/{eventId}/results` | Returns the results leaderboard for a completed event. | None | None; optional `categoryId` query parameter. | `200 OK` with ordered results; `404 Not Found`; `409 Conflict` if results have not been published. |
| GET | `/api/results/{resultId}` | Returns one published result with its event, category, finish position, and finish time. | Owner / Organiser | None | `200 OK`; `403 Forbidden` for an unrelated private result; `404 Not Found`. |

### Route information

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/route` | Returns the route description, distance, elevation, and map link for an event. | None | None | `200 OK` with route details; `404 Not Found` when no route has been published. |
| PUT | `/api/events/{eventId}/route` | Creates or replaces the route information for an event owned by the organiser. | Organiser | `{ routeName, distanceKm, routeDescription, mapUrl, elevationGainMeters }` | `200 OK`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`. |

### Weather information

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/weather` | Returns the available weather forecasts and race-day advisory for an event. | None | None; optional `forecastDate` query parameter. | `200 OK` with forecast array; `404 Not Found`. |
| POST | `/api/events/{eventId}/weather` | Adds a weather forecast or race-day advisory for an event managed by the organiser. | Organiser | `{ forecastDate, temperatureC, condition, windSpeedKph, rainProbabilityPercent, advisory }` | `201 Created`; `400 Bad Request`; `403 Forbidden`; `404 Not Found`; `409 Conflict` for a duplicate forecast date. |

## Planned response conventions

- Successful reads and updates return JSON with stable camelCase property names.
- Successful creation returns `201 Created` and a `Location` header where applicable.
- Successful deletion and logout return `204 No Content`.
- Validation errors return `400 Bad Request` with a field-level error list.
- Missing or invalid bearer tokens return `401 Unauthorized`.
- A logged-in user without permission returns `403 Forbidden`.
- A missing resource returns `404 Not Found`.
- Duplicate registration, duplicate enrolment, duplicate category names, and duplicate weather dates return `409 Conflict`.

## Relationship to the database design

- `Users.Role` enforces the two roles used by the API: `Organiser` and `Participant`.
- `Events.OrganiserID` restricts event management to the organiser who owns the event.
- `Enrolments` resolves the many-to-many relationship between participants and events, while also recording the selected category.
- `Results.EnrolmentID` is unique so one enrolment can have at most one official result.
- `Routes` supports one optional route per event.
- `EventWeather` supports multiple forecast snapshots for one event.