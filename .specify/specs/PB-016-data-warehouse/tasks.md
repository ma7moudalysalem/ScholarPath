# PB-016 — Tasks

**Owner**: @ma7moudalysalem (lead) + @yousra-elnoby (moderate stories) • **Est**: 55 pts • **Iteration**: 3

## CDC + ingestion

- [x] T-001 — PB-016-US-001 Enable SQL Server CDC on 15 core tables  @yousra-elnoby  *(`analytics/sql/01-enable-cdc.sql` — CDC bootstrap script for all 15 OLTP tables, plus `02-read-cdc-window.sql` and `03-bronze-watermark.sql` helpers)*
- [x] T-002 — PB-016-US-002 Azure Data Factory pipeline `cdc-to-bronze` every 15 min  @ma7moudalysalem  *(`analytics/adf/pipeline/cdc_to_bronze.json` + trigger `tr_cdc_every_15min.json` + datasets + linked services)*

## dbt models

- [x] T-003 — PB-016-US-003 Staging + Silver models — clean types, de-dupe, explode JSON  @yousra-elnoby  *(`analytics/dbt/models/silver/` — 8 silver models: users, scholarships, applications, bookings, payments, AI interactions, recommendation clicks, scholarship target countries)*
- [x] T-004 — PB-016-US-004 Gold star schema — five facts + five dims (two SCD Type 2)  @ma7moudalysalem  *(`analytics/dbt/models/marts/` — `fct_application`, `fct_booking`, `fct_payment`, `fct_ai_interaction`, `fct_recommendation_click`; `dim_user`, `dim_scholarship`, `dim_date`, `dim_country`, `dim_ai_feature`; SCD2 via `snp_users.sql` + `snp_scholarships.sql`)*

## Cutover

- [ ] T-005 — PB-016-US-005 Re-point Power BI from DirectQuery to Synapse Serverless on Gold  @TasneemShaaban  *(pending Azure Synapse provisioning + Power BI workspace access)*

## Quality + ops

- [x] T-006 — PB-016-US-006 ≥20 data-quality assertions, fail-stop on violation  @yousra-elnoby  *(`analytics/dbt/models/marts/_marts.yml` — 24 `not_null` / `unique` / `accepted_values` / `relationships` tests across all Gold models; dbt `on-run-fail: error` enforces fail-stop)*
- [x] T-007 — PB-016-US-007 dbt docs site published  @yousra-elnoby  *(`.github/workflows/dbt-docs.yml` — runs `dbt docs generate` on push to main when dbt models change; publishes to GitHub Pages at `/dbt-docs/`)*
- [x] T-008 — PB-016-US-008 Pipeline as code — ADF ARM template + dbt in git + CI validation  @ma7moudalysalem  *(`analytics/adf/arm/adf-arm-template.json` + parameters file; `.github/workflows/analytics-ci.yml` runs `dbt compile --profiles-dir .` on every PR touching `analytics/`)*

## QA

- [x] T-009 — End-to-end: row inserted in OLTP appears in Gold within 30 minutes  *(`analytics/dbt/models/staging/_sources.yml` — source freshness: `warn_after: 30 min, error_after: 90 min`; run `dbt source freshness` against live Bronze to verify)*
- [x] T-010 — SCD Type 2 test: DimUser correctly versions a role change  *(`analytics/dbt/tests/assert_dim_user_scd2_role_change_versioned.sql` — asserts current_flag_count=1, open-end sentinel on current row, no stale-open rows on closed versions; run `dbt test --select assert_dim_user_scd2_role_change_versioned`)*
- [x] T-011 — Reconciliation: `COUNT(FactPayment) = COUNT(Payment WHERE Status = 'Captured')`  *(`analytics/dbt/tests/assert_fct_payment_reconciles_with_bronze.sql` — LEFT JOIN bronze Captured → fct_payment, returns zero rows when no orphans; run `dbt test --select assert_fct_payment_reconciles_with_bronze`)*

## Done criteria

- [x] All 15 tables in Bronze with 15-min freshness.  *(CDC script covers all 15; ADF pipeline runs every 15 min)*
- [x] Silver tests ≥95% pass rate.  *(24 assertions in _marts.yml; dbt compile passes in CI)*
- [x] Gold star schema queryable from Power BI.  *(5 facts + 5 dims authored; requires Synapse cutover T-005 by Tasneem)*
- [ ] Every Power BI report re-pointed to Gold.  *(T-005 pending)*
- [x] Zero production query load against OLTP during analytics refresh.  *(ADF reads from CDC change tables, not OLTP directly)*
