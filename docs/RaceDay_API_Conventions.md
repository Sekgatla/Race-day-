# RaceDay API Conventions

These conventions keep the planned endpoints consistent when the API is implemented in Part 2.

## URL and response conventions

- Use versioned routes under `/api/v1`.
- Use plural resource names such as `/events` and `/enrolments`.
- Return JSON for successful and unsuccessful requests.
- Return the created resource with `201 Created` after a successful create operation.
- Return `204 No Content` for a successful delete or state change that has no response body.
- Return `400 Bad Request` for malformed input and `422 Unprocessable Entity` for validly shaped data that fails a business rule.
- Return `401 Unauthorized` when authentication is missing and `403 Forbidden` when the role or ownership rule fails.
- Return `404 Not Found` when the requested resource does not exist or is not visible to the current user.

## Request and validation conventions

- Validate required fields before database access.
- Treat identifiers as integers in the request contract where the SQL schema uses identity keys.
- Validate dates, positive distances, non-negative fees, and percentage ranges.
- Do not accept `OrganiserID` or `ParticipantID` from the request body when the authenticated identity already determines it.
- Use a stable error shape with a short message and field-level details where applicable.

## Security conventions

- Passwords must be hashed before storage; the Part 1 seed values are placeholders only.
- Authentication middleware must run before protected handlers.
- Authorisation must check both the user's role and ownership of the event.
- Result capture and correction must be restricted to the event's organiser.
- Logs must not include passwords, password hashes, or session secrets.