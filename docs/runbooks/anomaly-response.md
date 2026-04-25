# Runbook — Anomaly alert response

**Scope**: alerts raised by the Stream Analytics job `sa-anomaly` and routed
through the PagerDuty bridge (PB-018 US-002).
**Audience**: on-call engineer (first responder), escalation: team lead.

## 1. Read the alert

Each PagerDuty incident includes these fields in `custom_details`:

| Field            | What it means                                  |
|------------------|------------------------------------------------|
| `event_count`    | events in the 1-minute window that triggered   |
| `baseline_mean`  | mean count over the previous 60 windows        |
| `baseline_stddev`| std-dev over the same window                   |
| `z_score`        | (event_count − mean) ÷ stddev                  |
| `window_end`     | UTC timestamp of the window that fired         |
| `component`      | event_type (e.g. `payment.captured`)           |

**Sanity check first.** A `z_score` between 3 and 4 on a low-volume event
(baseline_mean < 5) is usually noise — acknowledge and wait 5 minutes for a
follow-up window. A `z_score > 6` or `event_count > 10x baseline` is real.

## 2. Decide — incident or not?

```
Is baseline_mean < 2 AND event_count < 20?
  → Likely noise. Ack + monitor. Do not page.

Is event_type in { payment.captured, payout.created }?
  → Always treat as real. Go to step 3.

Is event_type in { ai.interaction.completed, recommendation.click }?
  → Likely benign traffic (launch, tweet, etc.) unless cost spike confirmed.
  → Check #3a first.

Otherwise:
  → Go to step 3.
```

### 3a. Cost spike check (AI only)

```sql
-- Run against Synapse Serverless (Gold)
SELECT SUM(cost_usd) AS cost_last_10m
FROM fct_ai_interaction
WHERE started_at_utc >= DATEADD(minute, -10, GETUTCDATE());
```

If `cost_last_10m > 5`, this is real — proceed to step 3. Otherwise ack.

## 3. Triage

1. Open the live Grafana board: **Ops → Live Firehose**.
2. Confirm the anomaly is still firing (last 1-minute bar ≥ 3σ).
3. Filter by `component` — is it one event type or multiple?
   - **One** → likely feature-specific bug / partner misconfig.
   - **Many** → infra-level (Event Hub backpressure, DB lag, DDoS).

## 4. Act

| Symptom                                     | First action                                            |
|---------------------------------------------|---------------------------------------------------------|
| `payment.captured` 10× normal               | Page finance lead. **Do not** disable Stripe webhook.  |
| `ai.interaction.completed` cost spike       | Trip the AI cost gate: set `AiCost:DailyBudgetUsd=0`.  |
| `login_failed` spike on single IP           | Add WAF block: `az network application-gateway ...`.   |
| Many event types simultaneously             | Scale out app service to 2× current; check DB CPU.     |
| False positive (confirmed via Grafana)      | Ack in PD. File an issue on the threshold.              |

## 5. Aftercare

- Every real incident gets a postmortem entry in `docs/incidents/`.
- If the alert was a false positive, log it in the runbook's "Known noise"
  table below and adjust the Stream Analytics baseline if it recurs.

## Known noise

| Date       | event_type              | Reason                      | Adjustment        |
|------------|-------------------------|-----------------------------|-------------------|
| _(none yet)_ |                       |                             |                   |
