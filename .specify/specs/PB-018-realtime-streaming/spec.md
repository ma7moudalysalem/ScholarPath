# PB-018 — Real-time Streaming

**Owner**: @ma7moudalysalem • **Priority**: Low (optional) • **Iteration**: 4 • **Est**: 21 pts

## Problem statement

Hourly or fifteen-minute refreshes are fine for most of the analytics story. A few moments benefit from sub-second freshness: launch-day activity tiles, fraud anomaly detection, and acting on insights the BI layer produced (reverse ETL). This Epic adds an event stream alongside the existing medallion pipeline.

## User stories

US-179 .. US-181

## Functional requirements

FR-259 .. FR-270

## Acceptance criteria

1. **Live activity tile** — `ApplicationSubmitted`, `PaymentCaptured`, `BookingCompleted` domain events flow into Azure Event Hub; a Power BI streaming dataset tile shows them within 5 seconds.
2. **Anomaly detection** — an Azure Stream Analytics query flags any 5-minute window where event volume is 3σ above the trailing 7-day baseline. Flags open a PagerDuty incident automatically.
3. **Reverse ETL — churn risk** — a daily job pushes an "at risk" student segment from Power BI back into a `UserRiskFlags` table; admin portal lists a warning chip on matching users in `/admin/users`.

## Non-goals

- Cross-event correlation (future streaming ML layer).
- WebSocket live push to user-facing screens (PB-007 SignalR is a different concern).
- Cost-optimised eventing tier (start on standard, review in deploy phase).
