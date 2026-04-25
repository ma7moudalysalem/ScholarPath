# PB-016 — Data Warehouse

**Owner**: @ma7moudalysalem (lead) + @yousra-elnoby (moderate stories) • **Priority**: Medium • **Iteration**: 3 • **Est**: 55 pts

## Problem statement

DirectQuery on the OLTP database was a fine starting point in PB-015 but it does not scale: heavy analytics queries contend with live user transactions, historical snapshots are lost as rows update in place, and there is no path for richer work such as SCD-tracked dimensions or long-window aggregations. This Epic introduces a proper medallion (Bronze/Silver/Gold) pipeline built on Azure Data Lake Gen2 + dbt + Synapse Serverless, with SQL Server CDC as the source.

## User stories

US-166 .. US-173

## Functional requirements

FR-226 .. FR-245

## Acceptance criteria

Summary — detailed in the next section:
1. SQL Server CDC enabled on 15 core tables, 3-day retention.
2. Azure Data Factory pipeline lands CDC deltas into Bronze (daily-partitioned Parquet) every 15 minutes.
3. dbt staging + Silver models clean types, de-duplicate by business key, explode JSON. Test coverage ≥ 95%.
4. Gold star schema with five fact tables and five dimension tables (two SCD Type 2).
5. Every Power BI dashboard from PB-015 now reads from Gold via Synapse Serverless.
6. ≥20 data-quality assertions fail-stop the pipeline and alert on violation.
7. dbt docs site published with full lineage + column descriptions.
8. Entire infrastructure (ADF + dbt) versioned in git; every PR runs CI validation + a staging preview deploy.

## Non-goals

- Real-time / sub-minute analytics (that is PB-018).
- ML feature store (v2).
- Cross-region replication (deploy phase).
