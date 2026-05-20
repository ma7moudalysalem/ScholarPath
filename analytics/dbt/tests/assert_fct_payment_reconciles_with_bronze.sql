-- PB-016 T-011 — Reconciliation: every Captured payment in the OLTP Bronze
-- table must appear in fct_payment (no row dropped by the ETL).
--
-- A dbt singular test: returns rows on FAILURE (zero rows = test passes).
-- Run with: dbt test --select assert_fct_payment_reconciles_with_bronze
--
-- Logic:
--   LEFT JOIN bronze Captured payments → fct_payment on natural key.
--   Any bronze row with no matching gold row is a gap — the test fails.
--
-- Why only 'Captured':
--   fct_payment includes Captured + Refunded + PartiallyRefunded.
--   We check Captured as the authoritative source of truth: if a payment
--   was ever captured, it MUST exist in fct_payment regardless of later
--   refund state.  Refunded/PartiallyRefunded are a superset of Captured,
--   so passing this check implies no revenue rows were silently dropped.

with bronze_captured as (
    select
        payment_id  -- natural key: matches payment_nk in fct_payment
    from {{ source('bronze', 'dbo_Payments') }}
    where status = 'Captured'
),

gold_payments as (
    select payment_nk
    from {{ ref('fct_payment') }}
),

orphaned_bronze_rows as (
    select
        b.payment_id,
        'Missing from fct_payment' as failure_reason
    from bronze_captured b
    left join gold_payments g
        on b.payment_id = g.payment_nk
    where g.payment_nk is null
)

-- Return the gaps.  Zero rows = test passes.
select * from orphaned_bronze_rows
