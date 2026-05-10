# PB-016 — Implementation Plan

## Architecture (medallion)

```
SQL Server CDC → ADF → Data Lake (Bronze, Parquet)
                         → dbt staging → Silver
                         → dbt marts    → Gold (star schema)
                                          → Synapse Serverless views
                                          → Power BI (cutover from DirectQuery)
```

## Bronze layer
- Path: `datalake/bronze/<table>/yyyy=YYYY/mm=MM/dd=DD/part-*.parquet`
- Tables: all 15 CDC-enabled tables
- Retention: 90 days Bronze → colder Azure archive tier after.

## Silver layer
- Path: `datalake/silver/<table>/` (overwrite partition on each build)
- dbt models in `analytics/dbt/models/silver/`
- Rules: typed columns, de-duplicated by business key, JSON columns exploded into typed columns, soft-deletes honored.

## Gold layer — Star schema

Fact tables:
- `FactApplication` — grain: one snapshot per ApplicationTracker status change
- `FactPayment` — grain: one captured Payment
- `FactBooking` — grain: one ConsultantBooking lifecycle transition
- `FactAiInteraction` — grain: one AiInteraction row (feeds PB-017)
- `FactForumActivity` — grain: one post/vote/flag

Dim tables:
- `DimUser` (SCD Type 2 on role + status)
- `DimScholarship` (SCD Type 2 on status + funding amount)
- `DimDate` (conformed standard)
- `DimGeography` (country → region → continent hierarchy)
- `DimProfitShareConfig` (preserves historical rates for FactPayment reconciliation)

## Infrastructure
- Azure Data Lake Gen2 storage account + container `scholarpath-lake`
- Azure Data Factory pipeline `cdc-to-bronze` (triggered every 15 min)
- Azure Synapse Serverless pool (pay-per-query, no cluster idle cost)
- dbt project under `analytics/dbt/` using `dbt-sqlserver` adapter

## Orchestration
- ADF handles ingestion (Bronze).
- dbt Cloud or Airflow triggers `dbt build` hourly (Bronze → Silver → Gold + tests).
- Slack webhook + email alert on failure.

## Data quality
- dbt native tests: `unique`, `not_null`, `accepted_values`, `relationships`.
- Custom singular tests: no future deadlines, no negative payment amounts, idempotency keys uniqueness, CDC row-count vs source row-count drift.

## Pipeline as code
- ADF exported as ARM template in `analytics/adf/`.
- dbt project in `analytics/dbt/`.
- `.github/workflows/analytics.yml` validates both on PR, deploys to a staging Azure resource group on merge to a dedicated `analytics` branch.

## Dependencies
- PB-015 (dashboards already live; cutover is one of the tasks)
- INFRA (Azure + Data Lake provisioned)
- PB-017 schema changes (RecommendationClickEvent, etc.) must be CDC-enabled too
