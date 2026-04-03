# PB-005 — Company Review, Payment, Rating

**Owner**: @yousra-elnoby • **Priority**: High • **Iteration**: 3 • **Est**: 23 pts

## Problem statement

Companies charge a fee per application review (configurable per listing). Students pay upfront when submitting; Company earns on review completion (minus platform profit share from PB-014). Students can rate Companies post-decision; ratings drive Company profile trust score.

## User stories

| ID | Story | Size |
|----|-------|------|
| US-045 | Review student applications + documents (Company) | 5pt |
| US-046 | Manage review outcomes | 4pt |
| US-047 | Record + reverse review-related payments per refund rules | 5pt |
| US-048 | Configure Company review pricing (Admin) | 4pt |
| US-049 | Rate + review a Company (Student) | 3pt |
| US-050 | View received ratings (Company) | 2pt |

## Functional requirements

FR-063 .. FR-075, plus payment FRs from PB-013 relevant to Company side.

## Acceptance criteria

1. **Review fee** — Set per listing by Company (or platform default via Admin config from PB-014). Student sees fee at apply time; pays via Stripe PaymentIntent (PB-013).
2. **Escrow** — Funds held until Company completes review (status reaches Accepted or Rejected). Then captured and split per PB-014 profit share.
3. **Refund policy** — If Company doesn't review within 14 days after deadline → auto-refund 100% to student; if Student withdraws before submit → refund 100%; if Student withdraws after submit → refund 50% minus Stripe fee; if rejected → no refund.
4. **Company rating** — Student can rate 1–5 stars + optional comment after `ApplicationStatusChanged → Accepted|Rejected`. One rating per application.
5. **Aggregated score** — Company profile shows average rating + count; recalculated on each new rating.

## Non-goals

- Company-to-student ratings (unidirectional v1)
- Disputes/appeals flow (v2)
