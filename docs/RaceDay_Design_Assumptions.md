# RaceDay Part 1 Design Assumptions

These assumptions keep the Part 1 design focused and provide a clear boundary for the later implementation stages.

## Scope assumptions

1. RaceDay supports road running, walking, and cycling events in South Africa.
2. Part 1 documents the system plan and relational database only; it does not implement authentication, an API, or an MVC user interface.
3. An event has one organiser, but an organiser may own many events.
4. A participant may enrol in many events, and an event may have many participants.
5. Each event offers one or more categories, and each enrolment selects one category.
6. An event may have one route record and multiple weather records for different forecast dates.
7. An enrolment can have zero or one official result. This is represented by a unique `Results.EnrolmentID`.

## Data and workflow assumptions

- Email is the account identifier and must be unique.
- Organisers are responsible for event maintenance, entry oversight, and result capture.
- Participants can see their own records but cannot change official results.
- Cancelled or completed events remain in the database so enrolment and result history is preserved.
- Monetary amounts use fixed precision rather than floating-point storage.
- The SQL script targets Microsoft SQL Server and is intended to be executed in SSMS.

## Future-stage assumptions

- Password hashing and session management will be implemented in Part 2.
- API authentication will provide the current user identity to ownership checks.
- Payment processing, live timing, notifications, and external weather APIs are outside Part 1.
- The MVC interface in Part 3 will consume the planned API rather than bypassing the database design.