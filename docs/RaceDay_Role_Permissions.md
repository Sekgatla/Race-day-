# RaceDay Role and Permission Matrix

The role model is deliberately small for Part 1. Every authenticated user is either an Organiser or a Participant, and the API plan uses the same distinction as the database constraint.

| Capability | Organiser | Participant |
|---|:---:|:---:|
| Create an account | No | Yes |
| View or update own profile | Yes | Yes |
| Create an event | Yes | No |
| Edit or cancel an owned event | Yes | No |
| Browse published events | Yes | Yes |
| Create or edit categories for an owned event | Yes | No |
| View enrolments for an owned event | Yes | No |
| Enrol in an event | No | Yes |
| View own enrolments | No | Yes |
| Capture or correct results | Yes | No |
| View own results | No | Yes |
| Maintain an event route | Yes | No |
| Maintain event weather records | Yes | No |

## Ownership rules

- An organiser may manage only events where `Events.OrganiserID` matches the authenticated user.
- A participant may read or change only their own profile and enrolment records.
- Organiser result access is limited to participants enrolled in an event owned by that organiser.
- Published event information can be browsed without exposing organiser-only maintenance actions.

## Database alignment

The `Users.Role` check constraint is the database-level representation of this matrix. The API implementation in Part 2 must repeat the ownership checks because a valid role alone does not prove that a user owns a particular event.