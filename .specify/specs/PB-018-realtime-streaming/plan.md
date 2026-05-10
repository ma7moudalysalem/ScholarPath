# PB-018 — Implementation Plan

## Event emission
- MediatR `INotificationHandler<T>` for three domain events:
  - `ApplicationSubmittedEvent`
  - `PaymentCapturedEvent`
  - `BookingCompletedEvent`
- Handler forwards a compact JSON envelope to Azure Event Hub via `Azure.Messaging.EventHubs`.
- Envelope: `{ type, occurredAt, userId, amount?, correlationId }` — no PII.

## Azure Event Hub
- Namespace `scholarpath-events` (Standard tier).
- Event Hub `domain-events` with 2 partitions.
- 7-day retention.

## Live tile
- Power BI streaming dataset with the same envelope schema.
- Stream Analytics pass-through job forwards Event Hub → Power BI streaming endpoint.
- Dashboard tile: "Last 5 minutes of activity" count + 30-sec rolling sparkline.

## Anomaly detection
- Second Stream Analytics job, query:
  ```sql
  WITH rolling_avg AS (
      SELECT EventType, AVG(count) OVER (WINDOW '7 days') AS baseline_mean,
             STDEV(count) OVER (WINDOW '7 days') AS baseline_std
      FROM Input
  )
  SELECT *
  FROM Input
  WHERE count > baseline_mean + 3 * baseline_std
  ```
- Triggers: HTTP POST to PagerDuty Incident API on each alert row.

## Reverse ETL — churn risk
- Power BI dataflow computes `ChurnRiskScore` per student from historical Gold data.
- Output written to a reverse-ETL connector (Census or hand-rolled dbt-python model) that writes into the OLTP `UserRiskFlags { UserId, Score, ComputedAt, Source }`.
- Daily sync.
- Admin UI: chip on user row `⚠ At risk (0.87)` linking to the student's journey dashboard.

## Data model additions
- `UserRiskFlags` table (OLTP) — written by reverse ETL, read by admin queries.

## API
- `GET /api/admin/users?riskFlag=at-risk` — filter already exists; just adds the derived column to the response DTO.

## Frontend
- `UsersAdmin.tsx` — new `AtRiskChip` component rendered when `user.riskFlag` is set.

## Tests
- Unit: event emitter publishes exactly one envelope per domain event.
- Integration (local): an `ApplicationSubmittedEvent` raised in-process lands in a local Kafka mock and gets surfaced to a stub Power BI receiver.
- Manual: synthetic traffic spike triggers anomaly alert.

## Dependencies
- PB-016 (Gold layer — source for churn score)
- PB-011 (Admin portal — hosts the at-risk chip)
- PB-010 (domain event infrastructure + Notification envelope pattern)
