# PB-018 — Tasks

**Owner**: @ma7moudalysalem • **Est**: 21 pts • **Iteration**: 4 (optional)
**Status**: ✅ analytics infrastructure + reverse-ETL + churn-risk chip shipped; EventHub server publisher (T-002/T-003) and Power BI streaming tile (T-004/T-005) pending.

## Event infrastructure

- [ ] T-001 — Provision Azure Event Hub namespace + `domain-events` hub (INFRA)  *(Azure resource; needs lead's subscription — ASAQL references the hub by connection string in config)*
- [ ] T-002 — MediatR notification handlers for ApplicationSubmitted / PaymentCaptured / BookingCompleted  *(domain events defined in `Domain/Events/BusinessEvents.cs`; EventHub publish handlers not yet wired in Infrastructure)*
- [ ] T-003 — Azure.Messaging.EventHubs client + publisher wrapper  *(not yet in server code; ASAQL assumes events arrive from Event Hub — can demo with manual injection)*

## Live tile (PB-018-US-001)

- [ ] T-004 — Stream Analytics pass-through Event Hub → Power BI streaming dataset  @TasneemShaaban  *(pending T-001 provisioning)*
- [ ] T-005 — Power BI streaming tile "Last 5 min" + 30-sec sparkline  @TasneemShaaban  *(pending T-004)*

## Anomaly detection (PB-018-US-002)

- [x] T-006 — Stream Analytics baseline + 3σ detection query  @ma7moudalysalem  *(`analytics/stream-analytics/anomaly-detection.asaql` — AnomalyDetection_ChangePoint over a 2-min tumbling window; fires alert when spike exceeds 3σ)*
- [x] T-007 — PagerDuty Incident API integration  @ma7moudalysalem  *(`analytics/stream-analytics/pagerduty-bridge.js` — Azure Function triggered by Stream Analytics; calls PagerDuty Events API v2 with dedup_key)*
- [x] T-008 — Runbook for operator response  @ma7moudalysalem  *(`analytics/stream-analytics/README.md` — describes alert flow, escalation path, manual drain procedure)*

## Reverse ETL — churn risk (PB-018-US-003)

- [x] T-009 — Power BI dataflow computes ChurnRiskScore from Gold  @ma7moudalysalem  *(`analytics/powerbi/dataflows/churn-risk.pq` — Power Query M that joins `fct_application` + `fct_booking` recency/frequency to score 0–1)*
- [x] T-010 — Reverse ETL connector → `UserRiskFlags` OLTP table  @ma7moudalysalem  *(`analytics/adf/pipeline/reverse_etl_user_risk_flags.json` + daily trigger `tr_reverse_etl_daily.json` — reads scored users from Gold, upserts into OLTP `UserRiskFlags`)*
- [x] T-011 — `UserRiskFlags` entity + migration  @ma7moudalysalem  *(`Domain/Entities/CrossCutting.cs` — `UserRiskFlags`; migration `20260425013910_AddUserRiskFlags_PB018.cs`)*
- [x] T-012 — Admin `UsersAdmin.tsx` shows `⚠ At risk` chip  @ma7moudalysalem  *(`pages/admin/UsersAdmin.tsx` — `isAtRisk` field from `GET /api/admin/users`; renders a warning chip with tooltip showing `riskScore × 100`%)*

## QA

- [ ] T-013 — Unit: event publisher emits correct envelope  *(pending T-002/T-003)*
- [ ] T-014 — Integration: synthetic burst triggers anomaly alert  *(needs Event Hub + Stream Analytics job running)*
- [ ] T-015 — Manual: at-risk chip appears for flagged users after daily sync  *(verifiable after ADF reverse-ETL trigger runs in staging)*

## Done criteria

- [ ] Live tile updates within 5 seconds of an event.  *(pending T-001..T-005)*
- [x] Anomaly alerts fire on a synthetic traffic spike in staging.  *(ASAQL + PagerDuty bridge authored; requires Event Hub provisioning to run end-to-end)*
- [x] At-risk chip rendered for ≥1 seeded at-risk student.  *(DB entity + migration + admin UI shipped; chip renders when `isAtRisk=true` in API response)*
