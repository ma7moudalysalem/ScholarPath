# Power BI dataflows

Reusable M (Power Query) transformations that feed Power BI reports **and**
the reverse-ETL pipeline.

## Files

| File            | Purpose                                           | Consumer                               |
|-----------------|---------------------------------------------------|----------------------------------------|
| `churn-risk.pq` | Daily churn-risk score per active student        | `reverse_etl_user_risk_flags` ADF pipeline |

## Deploy

Dataflows can't (yet) be deployed from CLI. Manual promotion:

1. Open Power BI Service → Workspace: `ScholarPath Prod`
2. New → Dataflow → Add new entities → Blank query
3. Paste the contents of the `.pq` file as the "Advanced editor" expression
4. Map output to the workspace Data Lake (Gen2) container — the ADF
   reverse-ETL pipeline reads from:
   `gold/dataflows/churn_risk_snapshot/*.parquet`
5. Set refresh schedule → daily at 04:00 UTC
6. Enable the downstream ADF trigger `tr_reverse_etl_daily`

## Why a dataflow (not dbt)

The churn-risk rules are editable-by-analyst territory. Power Query's UI lets
Tasneem tweak thresholds without opening a PR; the logic itself stays in git
via this file so we don't lose history. dbt would be the right home if the
rules harden into a proper model later.

## Scoring inputs

See the inline comment block in `churn-risk.pq`. The five drivers are:

- days since last login (> 30 → +0.40)
- applications submitted in 30d (0 for students → +0.25)
- booking no-shows in 90d (≥ 2 → +0.15)
- recommendation clicks in 30d (0 AND no apps → +0.10)
- notification read-through rate (< 10% → +0.10)

Score is capped at 1.0, flagged at ≥ 0.65, and written alongside a
human-readable `Reason` string for the admin tooltip.
