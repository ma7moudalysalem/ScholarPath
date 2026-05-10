# PB-017 — Tasks

**Owner**: @ma7moudalysalem • **Est**: 21 pts • **Iteration**: 3

## Data model + backend

- [ ] T-001 — `RecommendationClickEvent` entity + migration + indexes  @ma7moudalysalem
- [ ] T-002 — `LogRecommendationClickCommand` + handler + `[Auditable]`  @ma7moudalysalem
- [ ] T-003 — `GetAiUsageSummaryQuery` for dashboard data  @ma7moudalysalem
- [ ] T-004 — Hangfire `RedactionAuditSamplingJob` (monthly, 50 random chat prompts)  @ma7moudalysalem
- [ ] T-005 — `AiRedactionAuditSample` entity + admin verdict commands  @ma7moudalysalem

## Dashboards (Power BI)

- [ ] T-006 — PB-017-US-001 AI cost dashboard (stacked bar feature × provider × day)
- [ ] T-007 — PB-017-US-002 Budget alert automation (80% three-day rule)
- [ ] T-008 — PB-017-US-003 Recommendation CTR widget + event logging  @ma7moudalysalem
- [ ] T-009 — PB-017-US-004 Token efficiency box plots + top-10 table
- [ ] T-010 — PB-017-US-005 Redaction audit trend + investigation table

## Frontend

- [ ] T-011 — Extend `AiRecommendations.tsx` to log clicks
- [ ] T-012 — `AiEconomy.tsx` admin page (embed + native panels)
- [ ] T-013 — `RedactionAudit.tsx` admin review UI

## QA

- [ ] T-014 — Unit: click command idempotency
- [ ] T-015 — Integration: sampling job exactness (50 rows, non-overlapping months)
- [ ] T-016 — Manual: click CTR widget after demo data seed

## Done criteria

- Five AI Economy widgets live.
- Recommendation clicks logged end-to-end.
- Monthly redaction audit runs automatically + admin UI ready.
- Target CTR and redaction miss rate thresholds tracked on dashboard.
