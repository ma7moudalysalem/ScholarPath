# analytics/dbt/

dbt project for the ScholarPath warehouse — Silver + Gold transformations
on top of the Bronze Parquet lake produced by `analytics/adf/cdc_to_bronze`.

## Layout

```
dbt/
├── dbt_project.yml              # project + paths + var declarations
├── profiles.yml.example         # copy to ~/.dbt/profiles.yml
├── packages.yml                 # dbt-utils + dbt_expectations
├── models/
│   ├── staging/                 # stg_* views over Bronze  (@yousra)
│   ├── silver/                  # silver_* clean tables    (@yousra)
│   └── marts/
│       ├── dims/                # 5 dimensions             (@mahmoud)
│       └── facts/               # 5 fact tables            (@mahmoud)
├── snapshots/                   # SCD Type 2 history       (@mahmoud)
├── seeds/                       # static lookups (ISO countries, etc.)
├── tests/                       # cross-model tests        (@yousra)
└── macros/                      # reusable SQL
```

## Star schema — Gold

```
                 ┌─────────────┐
                 │  dim_date   │
                 └─────┬───────┘
                       │
  ┌────────────┐   ┌───┴────────────┐   ┌────────────────┐
  │  dim_user  ├───┤ fct_application├───┤ dim_scholarship│
  └────┬───────┘   └────────────────┘   └────────┬───────┘
       │                                          │
       ├── fct_payment ───────────── dim_country   │
       ├── fct_booking                             │
       ├── fct_ai_interaction ─── dim_ai_feature   │
       └── fct_recommendation_click ───────────────┘
```

Join rule (SCD Type 2): `fct.valid_time between dim.valid_from_utc and dim.valid_to_utc`.

## Build order

```bash
dbt deps
dbt seed            # loads the ISO country reference
dbt snapshot        # refreshes user + scholarship SCD history
dbt run             # staging → silver → gold in dependency order
dbt test            # ~40 assertions including 5 RI joins
dbt docs generate && dbt docs serve
```

CI runs `dbt build` (= run + test) against an isolated `gold_ci` schema on
every PR that touches `analytics/dbt/**`. See `.github/workflows/analytics-ci.yml`.

## Conventions

- Staging models return *views* (cheap, thin). Silver returns *tables*. Marts
  return *tables* too, incremental where row count justifies it.
- Surrogate keys via `dbt_utils.generate_surrogate_key` so hashes stay stable
  across warehouse restores.
- Every fact has `{nk}_nk` column so you can still join to OLTP audit tools
  without re-mapping the surrogate.
- No SELECT * in marts. Facts are additive; any non-additive column gets a
  comment justifying it.
