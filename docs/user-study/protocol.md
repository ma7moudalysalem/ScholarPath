# Protocol

## Purpose

To find out whether a student who has never seen ScholarPath can complete the
six things the platform exists to let them do, how long each takes, where they
get stuck, and how usable they judge it afterwards.

The study measures usefulness and usability. It does **not** measure whether
using ScholarPath leads to more scholarship offers; that would require following
participants across a full application cycle and is out of scope.

## Participants

- **Target:** 12–15 participants. Task-based usability testing surfaces the
  majority of recurring problems in this range, and the marginal problem found
  per additional participant falls sharply beyond it (Nielsen & Landauer, 1993).
- **Who:** final-year undergraduates and graduate students who have looked for a
  scholarship in the last two years, or intend to in the next one.
- **Who may not:** anyone who contributed to ScholarPath's design, code,
  documentation, or testing. See the note in `README.md`.
- **Recruitment:** university mailing lists and student groups. No incentive is
  offered, so that participation is not a reason to be generous in the ratings.

## Before the session

1. The participant receives `consent-en.md` or `consent-ar.md` at least 24 hours
   in advance and returns a signed copy. No session starts without one.
2. `screening.md` is completed. An ineligible respondent is told so and thanked.
3. A clean test account is created and its credentials are written on the
   session sheet. Accounts are not reused between participants, so one
   participant's saved searches and applications never appear to the next.
4. The catalog is reset to the same seeded state for every participant, so task
   difficulty does not drift as the data changes.

## The session

Approximately 45 minutes, one participant at a time, screen shared or in person.

| Stage | Minutes | What happens |
|---|---|---|
| Welcome | 5 | Consent confirmed verbally. The participant is told they may stop at any time and that the system, not they, is being tested. |
| Warm-up | 3 | The participant signs in and looks around with no task. Not timed. |
| Tasks | 25 | The six tasks in `tasks-en.md`, in the given order. |
| SUS | 5 | The ten items in `sus-en.md`, completed by the participant alone. |
| Debrief | 7 | The open questions in `debrief.md`. |

### Running a task

- Read the scenario aloud exactly as written. Do not paraphrase it — a
  paraphrase is a hint.
- Start the timer when the participant begins, stop it when the success
  criterion is met or when they give up.
- Say nothing while they work. If they ask a question, answer *"What would you
  do if I weren't here?"* Record that the question was asked.
- Cap each task at 5 minutes. A task that reaches the cap is recorded as not
  completed, with the time recorded as 300 seconds.
- Record the outcome as `completed`, `completed_with_help`, or `failed`, and
  write one line on where the difficulty was.

### Think-aloud

Ask the participant to say what they are looking for as they go. Think-aloud
inflates task times, so times are comparable **between participants** but should
not be reported as how long the task takes in ordinary use. This is stated
wherever the times are reported.

## After the session

1. Enter the row into `responses.csv` (copy `responses.template.csv` first).
2. Store the recording, if any, under the participant code only.
3. Delete the test account.

## Analysis

Run `analyze.py`. It reports, for each task, the completion rate and the median
time; overall, the mean SUS score with its standard deviation and the
distribution of scores across the interpretation bands of Bangor, Kortum &
Miller (2008).

No figure is computed any other way, and the script is not edited after the
first session. If a change to it becomes necessary, the change and the reason
are recorded in this file and the original result is reported alongside.

## Ethics and data

- Participation is voluntary and may be withdrawn at any point, including after
  the session, without giving a reason.
- Responses are pseudonymous: `responses.csv` carries a participant code, never
  a name or an email address.
- The linking sheet between code and person is kept separately and destroyed
  once analysis is complete.
- No scholarship application made during a session is real, and no document
  uploaded during a session is retained.
