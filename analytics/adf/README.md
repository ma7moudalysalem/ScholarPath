# analytics/adf/

Azure Data Factory resources in portable JSON form. Each file maps 1:1 to an
ADF entity (linkedService, dataset, pipeline, trigger) and can be imported via
the portal's "Import ARM template" or synced through git integration.

## Contents

| Folder            | What it holds                                                         |
|-------------------|-----------------------------------------------------------------------|
| `linkedService/`  | Connections (SQL OLTP, ADLS Gen2, Key Vault)                          |
| `dataset/`        | Parameterised source + sink descriptors                               |
| `pipeline/`       | Orchestration — `cdc_to_bronze.json` is the main PB-016 workhorse     |
| `trigger/`        | Schedule + tumbling-window triggers                                   |
| `arm/`            | Full-resource-group ARM templates (PB-016 US-008, Mahmoud)            |

## The Bronze pipeline at a glance

```
[Trigger: every 15 min]
    └─ [Lookup: max_lsn] → [SetVariable: maxLsn]
          └─ [ForEach capture_instance, parallel=5]
                ├─ [Lookup: last_to_lsn from watermark]
                ├─ [Copy: cdc.fn_cdc_get_net_changes_<table> → ADLS Parquet]
                └─ [Script: update watermark row]
```

Landing layout in ADLS Gen2:

```
bronze/
  cdc/
    table=dbo_Payments/
      load_date=2026-05-01/
        dbo_Payments_<runId>.parquet
      load_date=2026-05-02/
        ...
    table=dbo_ApplicationTrackers/
      ...
```

## Why ForEach over a dataflow

A Mapping Data Flow would do this in one shot, but:

- Data flows spin up a Spark cluster on every run — cold start is 4–8 minutes,
  which is larger than our 15-minute SLA.
- We want one Copy activity per table so one failing table does not kill the
  other 14.
- The parallelism cap (batchCount=5) keeps SQL IO sane during the day.

## Secrets

All secrets live in Azure Key Vault (`ls_key_vault`). None of the JSON files in
this folder contain real connection strings — they reference Key Vault secret
names by convention. See `infra/keyvault-secrets.md` (PB-016 US-008) for the
full list.
