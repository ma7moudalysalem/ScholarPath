# analytics/

Home of the analytics layer (PB-015 through PB-018). See
`docs/ANALYTICS.md` for the architecture, `.specify/specs/PB-01[5-8]-*/`
for the module specs, and SRS section 8 for the full requirement set.

## Layout

```
analytics/
├── dbt/                     # dbt-core project (PB-016)
│   └── models/
│       ├── staging/         # Bronze -> typed + cleaned (stg_*)
│       ├── silver/          # de-duped, JSON flattened (silver_*)
│       └── marts/           # Gold star schema (fct_* + dim_*)
├── adf/                     # Azure Data Factory ARM templates (PB-016 US-173)
├── powerbi/                 # .pbix templates + DAX RLS role definitions (PB-015)
└── sql/                     # SQL views that DirectQuery dashboards read
                             # (PB-015, replaced by Gold once PB-016 ships)
```

## Ownership

| Path | Owner |
|---|---|
| `dbt/models/staging/` | @yousra-elnoby |
| `dbt/models/silver/` | @yousra-elnoby |
| `dbt/models/marts/` | @ma7moudalysalem |
| `dbt/tests/` | @yousra-elnoby |
| `adf/` | @ma7moudalysalem |
| `powerbi/` | @TasneemShaaban |
| `sql/` | @TasneemShaaban |

CODEOWNERS enforces this on every PR.

## Getting started

1. Read `docs/ANALYTICS.md` end to end.
2. Pick your spec folder (`.specify/specs/PB-015-analytics-foundation/` etc.).
3. Work through `tasks.md` in order, ticking `[x]` as you finish.
4. Every change references a US-xxx (US-160..US-181) and FR-xxx (FR-212..FR-270) in the commit.

## Status

This folder is **scaffolded but empty**. Content lands as each story is delivered:

| Folder | Filled by |
|---|---|
| `sql/` | PB-015 US-T001..T-006 (Tasneem, Iteration 2) |
| `powerbi/` | PB-015 US-T007..T-011 + PB-018 US-T004..T-005 (Tasneem) |
| `adf/` | PB-016 US-T002 + US-T008 (Mahmoud, Iteration 3) |
| `dbt/` | PB-016 US-T003..T-004 + US-T006..T-007 (Yosra + Mahmoud, Iteration 3) |
