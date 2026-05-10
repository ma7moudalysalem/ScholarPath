# PB-018 — Tasks

**Owner**: @ma7moudalysalem • **Est**: 21 pts • **Iteration**: 4 (optional)

## Event infrastructure

- [ ] T-001 — Provision Azure Event Hub namespace + `domain-events` hub (INFRA)
- [ ] T-002 — MediatR notification handlers for ApplicationSubmitted / PaymentCaptured / BookingCompleted
- [ ] T-003 — Azure.Messaging.EventHubs client + publisher wrapper

## Live tile (PB-018-US-001)

- [ ] T-004 — Stream Analytics pass-through Event Hub → Power BI streaming dataset  @TasneemShaaban
- [ ] T-005 — Power BI streaming tile "Last 5 min" + 30-sec sparkline  @TasneemShaaban

## Anomaly detection (PB-018-US-002)

- [ ] T-006 — Stream Analytics baseline + 3σ detection query  @ma7moudalysalem
- [ ] T-007 — PagerDuty Incident API integration  @ma7moudalysalem
- [ ] T-008 — Runbook for operator response  @ma7moudalysalem

## Reverse ETL — churn risk (PB-018-US-003)

- [ ] T-009 — Power BI dataflow computes ChurnRiskScore from Gold  @ma7moudalysalem
- [ ] T-010 — Reverse ETL connector → `UserRiskFlags` OLTP table  @ma7moudalysalem
- [ ] T-011 — `UserRiskFlags` entity + migration  @ma7moudalysalem
- [ ] T-012 — Admin `UsersAdmin.tsx` shows `⚠ At risk` chip  @ma7moudalysalem

## QA

- [ ] T-013 — Unit: event publisher emits correct envelope
- [ ] T-014 — Integration: synthetic burst triggers anomaly alert
- [ ] T-015 — Manual: at-risk chip appears for flagged users after daily sync

## Done criteria

- Live tile updates within 5 seconds of an event.
- Anomaly alerts fire on a synthetic traffic spike in staging.
- At-risk chip rendered for ≥1 seeded at-risk student.
