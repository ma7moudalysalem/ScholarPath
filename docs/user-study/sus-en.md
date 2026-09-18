# System Usability Scale

Ten items, answered by the participant alone, immediately after the last task
and before the debrief. Do not explain the items; if asked, say *"answer with
whatever it means to you."* An item may be skipped.

Brooke, J. (1996). *SUS: A 'quick and dirty' usability scale.* In P. W. Jordan
et al. (Eds.), Usability Evaluation in Industry (pp. 189–194). Taylor & Francis.

---

For each statement, mark one box.
**1 = strongly disagree … 5 = strongly agree**

| # | Statement | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|---|
| 1 | I think that I would like to use this system frequently. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 2 | I found the system unnecessarily complex. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 3 | I thought the system was easy to use. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 4 | I think that I would need the support of a technical person to be able to use this system. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 5 | I found the various functions in this system were well integrated. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 6 | I thought there was too much inconsistency in this system. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 7 | I would imagine that most people would learn to use this system very quickly. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 8 | I found the system very cumbersome to use. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 9 | I felt very confident using the system. | ☐ | ☐ | ☐ | ☐ | ☐ |
| 10 | I needed to learn a lot of things before I could get going with this system. | ☐ | ☐ | ☐ | ☐ | ☐ |

---

## Scoring

Do not score in front of the participant. `analyze.py` does this; the rule is
recorded here so the computation can be checked by hand.

- Odd-numbered items: subtract 1 from the response.
- Even-numbered items: subtract the response from 5.
- Add the ten adjusted values and multiply by 2.5.

The result is a number from 0 to 100. **It is not a percentage**, and it must
not be written as one.

Acceptability ranges (Bangor, Kortum & Miller, 2008):

| Score | Reading |
|---|---|
| below 50 | not acceptable |
| 50 – 70 | marginal |
| above 70 | acceptable |
