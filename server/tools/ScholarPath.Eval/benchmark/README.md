# Bilingual scholarship-eligibility questions

`eligibility-questions.jsonl` holds 1,320 eligibility questions, each in English and
in Arabic (2,640 lines), over 429 real open listings (DAAD, Erasmus Mundus and
government programmes) as of 27 October 2026.

Each question states a student's nationality, academic level, major and study
destination, and asks whether the student is eligible for one named listing,
considering those four criteria. The answer (`truth`) is computed by the
platform's eligibility rule on exactly those facts, the rule that agrees with
DAAD's published level and nationality criteria on every one of 39,300 compared
pairs (`real/external_check.py`). Each line carries the verdict and the outcome
of every criterion, so an answer can be checked claim by claim.

| category | items | what it tests |
|---|---|---|
| eligible | 360 | every criterion met |
| only_level | 120 | wrong academic level only |
| only_destination | 120 | destination not covered only |
| only_nationality | 120 | nationality not admitted only |
| nationality_is_destination | 120 | nationality not admitted, although it is one of the listing's destinations |
| only_field | 120 | major outside the accepted fields only |
| partial_field | 120 | major adjacent to an accepted field (partially eligible) |
| unknown_nationality | 120 | nationality not stated, listing restricts it (partially eligible) |
| multiple_fail | 120 | two or more criteria fail |

Erasmus Mundus programmes whose accepted fields could not be read from the title
are excluded, so every answer rests on facts the source states.

Regenerate:

    dotnet run --project server/tools/ScholarPath.Eval -- --mode questions --students 120 \
      --as-of 2026-10-27 --connection <Real8 database> --out benchmark/eligibility-questions.jsonl
