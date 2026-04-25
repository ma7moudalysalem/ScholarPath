# PB-016 — Tasks

**Owner**: @ma7moudalysalem (lead) + @yousra-elnoby (moderate stories) • **Est**: 55 pts • **Iteration**: 3

## CDC + ingestion

- [ ] T-001 — PB-016-US-001 Enable SQL Server CDC on 15 core tables  @yousra-elnoby
- [ ] T-002 — PB-016-US-002 Azure Data Factory pipeline `cdc-to-bronze` every 15 min  @ma7moudalysalem

## dbt models

- [ ] T-003 — PB-016-US-003 Staging + Silver models — clean types, de-dupe, explode JSON  @yousra-elnoby
- [ ] T-004 — PB-016-US-004 Gold star schema — five facts + five dims (two SCD Type 2)  @ma7moudalysalem

## Cutover

- [ ] T-005 — PB-016-US-005 Re-point Power BI from DirectQuery to Synapse Serverless on Gold  @TasneemShaaban

## Quality + ops

- [ ] T-006 — PB-016-US-006 ≥20 data-quality assertions, fail-stop on violation  @yousra-elnoby
- [ ] T-007 — PB-016-US-007 dbt docs site published  @yousra-elnoby
- [ ] T-008 — PB-016-US-008 Pipeline as code — ADF ARM template + dbt in git + CI validation  @ma7moudalysalem

## QA

- [ ] T-009 — End-to-end: row inserted in OLTP appears in Gold within 30 minutes
- [ ] T-010 — SCD Type 2 test: DimUser correctly versions a role change
- [ ] T-011 — Reconciliation: `COUNT(FactPayment) = COUNT(Payment WHERE Status = 'Captured')`

## Done criteria

- All 15 tables in Bronze with 15-min freshness.
- Silver tests ≥95% pass rate.
- Gold star schema queryable from Power BI.
- Every Power BI report re-pointed to Gold.
- Zero production query load against OLTP during analytics refresh.
