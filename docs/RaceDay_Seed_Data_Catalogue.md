# RaceDay Seed Data Catalogue

The SQL script includes representative records so the design can be inspected in SSMS without first entering data manually.

| Data set | Purpose | Examples included |
|---|---|---|
| Users | Demonstrate both supported roles | Organisers and Participants |
| Events | Demonstrate event ownership and lifecycle | Published, draft, and completed events |
| Categories | Demonstrate multiple entry options | Running, walking, and cycling distances |
| Routes | Demonstrate optional course information | Route names, distances, elevation, and map links |
| EventWeather | Demonstrate forecast history | Forecast dates, conditions, temperature, wind, and rain probability |
| Enrolments | Demonstrate the participant/event bridge | Different participants and selected categories |
| Results | Demonstrate official timing data | Finish times, positions, and result statuses |

## Seed-data relationships

- Each seeded event belongs to an organiser.
- Each category belongs to its event.
- Each enrolment references an existing participant, event, and category.
- Each result references an existing enrolment.
- Route and weather records reference existing events.

## Verification queries

The final section of `docs/RaceDay_Database.sql` contains queries that can be used to demonstrate:

1. The tables created by the script.
2. The number of users in each role.
3. Categories offered by each event.
4. Participant enrolments and their selected categories.
5. Results joined to participants and events.

The seed data is deliberately realistic enough to demonstrate joins and constraints, but it is not production data.