# ScholarPath user study — instrument

This folder holds everything needed to run the usability study described in
Section 8.9 of the paper, and nothing else. **It contains no results**, because
the study has not been run.

| File | What it is |
|---|---|
| `protocol.md` | How the session is run, start to finish. Read this first. |
| `consent-en.md`, `consent-ar.md` | Participant information sheet and consent form. |
| `screening.md`, `screening-ar.md` | Six questions that decide whether someone is eligible to take part. |
| `tasks-en.md`, `tasks-ar.md` | The six task scenarios, with success criteria. |
| `sus-en.md`, `sus-ar.md` | The System Usability Scale, ten items. |
| `debrief.md`, `debrief-ar.md` | The open questions asked after the tasks. |
| `responses.template.csv` | The data sheet. One row per participant. |
| `analyze.py` | Computes every reported figure from that sheet. |

## Why the analysis script exists before the data

`analyze.py` is written and fixed **before** the first session, so that the way
the numbers are computed cannot be adjusted after seeing them. Running it now
against the empty template prints the exact table that will be filled in later.

```bash
python docs/user-study/analyze.py docs/user-study/responses.template.csv
```

## A note on who may be a participant

The people who built ScholarPath cannot be participants in it. A usability
result from an author measures the author's familiarity with their own system,
not the system, and reporting one as evidence would misrepresent the study to
the reader.

The authors do have one legitimate role: a **pilot**, run before recruitment
begins, whose only purpose is to find ambiguous task wording, broken test
accounts, and timing problems in the script. Pilot data is recorded in a
separate file, is never pooled with participant data, and is never reported.
`analyze.py` refuses to read a file whose `role` column says `author`.
