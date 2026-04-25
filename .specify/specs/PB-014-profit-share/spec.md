# PB-014 — Portal Profit Share

**Owner**: @norra-mmhamed • **Priority**: High • **Iteration**: 3 • **Est**: 22 pts

## Problem statement

Platform retains a configurable percentage of each Consultant booking and Company review fee. Configuration is Admin-controlled via the Admin Portal (PB-011). Profit share is calculated + recorded on every capture; included in Consultant earnings and Company earnings views; appears in analytics. Transparent to payees (shown as line item in receipts).

## User stories

US-153 .. US-159

## Functional requirements

FR-201 .. FR-211

## Acceptance criteria

1. **Config** — `ProfitShareConfig { paymentType, percentage, effectiveFrom, effectiveTo?, setByAdminId, createdAt }`. Only one active per `paymentType` at any time.
2. **Default** — 10% Consultant bookings, 15% Company reviews (overridable).
3. **Calculation** — On `CapturePaymentIntentCommand`, load active config → `profitShareAmount = round(amount * percentage)`, `payeeAmount = amount - profitShareAmount`.
4. **Stored** — Written to `Payment.ProfitShareAmount` + `Payment.PayeeAmount` snapshot (not re-derived; config can change later).
5. **Admin UI** — CRUD history for ProfitShareConfig with effective dates; changes audit-logged (PB-012).
6. **Analytics** — Aggregate profit share per day/month exposed to admin dashboard.
7. **Receipts** — Line item "Platform fee (X%)" on every receipt.

## Non-goals

- Tiered profit-share by volume (v2)
- Per-user profit-share (single rate per payment type)
