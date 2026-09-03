# RaceDay ERD Notes

The ERD contains seven entities selected to represent the core race-day workflow without duplicating data.

## Entity purpose

1. **Users** stores both organisers and participants.
2. **Events** stores the race-day event and its organiser.
3. **Categories** stores the entry options offered by an event.
4. **Enrolments** resolves the many-to-many relationship between participants and events.
5. **Results** stores the official outcome for an enrolment.
6. **Routes** stores optional course information for an event.
7. **EventWeather** stores one or more forecast records for an event.

## Cardinality decisions

- Users to Events: one organiser can manage many events; each event has one organiser.
- Events to Categories: one event can offer many categories; each category belongs to one event.
- Users to Enrolments: one participant can create many enrolments; each enrolment belongs to one participant.
- Events to Enrolments: one event can have many enrolments; each enrolment belongs to one event.
- Categories to Enrolments: one category can be selected by many enrolments; each enrolment selects one category.
- Enrolments to Results: one enrolment has zero or one result; a result belongs to one enrolment.
- Events to Routes: an event has zero or one route; a route belongs to one event.
- Events to EventWeather: an event can have many weather records; each weather record belongs to one event.

The editable source is stored in `docs/RaceDay_ERD.dot`, while `docs/RaceDay_ERD.pdf` is the submission-ready export.