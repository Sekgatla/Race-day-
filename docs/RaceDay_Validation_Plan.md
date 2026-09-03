# RaceDay Part 1 Validation Plan

## Automated validation

The GitHub Actions workflow checks the minimum repository contract:

- Required documentation folders and files exist.
- The ERD source and exported PDF are present.
- The API plan contains the required resource sections.
- The SQL script contains the core tables and both supported roles.
- `README.md` is available at the repository root.

The same checks can be run locally from the repository root:

```bash
test -d docs
test -d .github/workflows
test -f docs/RaceDay_ERD.dot
test -f docs/RaceDay_ERD.pdf
test -f docs/RaceDay_API_Endpoint_Plan.md
test -f docs/RaceDay_Database.sql
test -f README.md
```

## SSMS validation

After executing `docs/RaceDay_Database.sql` in SSMS:

1. Confirm all seven tables are listed under `RaceDayDb`.
2. Confirm primary keys and foreign keys are present.
3. Confirm the role check constraint accepts `Organiser` and `Participant`.
4. Confirm duplicate emails are rejected.
5. Confirm an enrolment cannot reference a different event's category.
6. Confirm one enrolment cannot receive two results.
7. Run the verification queries at the end of the script and capture the result grids.

## Evidence to retain

- Green GitHub Actions run screenshot.
- SSMS screenshot showing successful execution and verification output.
- ERD screenshot or exported PDF.
- Unlisted presentation-video link in the README.