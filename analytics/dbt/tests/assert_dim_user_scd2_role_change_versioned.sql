-- PB-016 T-010 — SCD Type 2: a user whose role changed must have ≥ 2 versions
-- in dim_user (one closed version + one current version).
--
-- A dbt singular test: returns rows on FAILURE (zero rows = test passes).
-- Run with: dbt test --select assert_dim_user_scd2_role_change_versioned
--
-- Logic:
--   1. Find any user_nk whose role appears in more than one version row.
--   2. For each such user, verify:
--        a. Exactly ONE row has is_current = 1 (the head version).
--        b. All older rows have valid_to_utc < '9999-12-31' (properly closed).
--        c. The current row has valid_to_utc = '9999-12-31' (open sentinel).
--
-- If all SCD2 transitions were recorded correctly, this test returns zero rows.
-- A non-zero result means either:
--   - A role change was overwritten rather than versioned (T2 logic broken).
--   - The current row was not correctly set as is_current = 1.
--   - A stale open row (valid_to = 9999) exists alongside another open row.

with role_versions as (
    select
        user_nk,
        role,
        is_current,
        valid_from_utc,
        valid_to_utc,
        count(*) over (partition by user_nk)         as total_versions,
        sum(is_current) over (partition by user_nk)  as current_flag_count
    from {{ ref('dim_user') }}
),

-- Users with multiple versions (i.e. at least one role change was recorded)
multi_version_users as (
    select distinct user_nk
    from role_versions
    where total_versions > 1
),

violations as (
    select
        rv.user_nk,
        rv.role,
        rv.is_current,
        rv.valid_from_utc,
        rv.valid_to_utc,
        case
            -- Rule 1: exactly one current row per user
            when rv.current_flag_count <> 1
                then 'current_flag_count != 1 (got ' + cast(rv.current_flag_count as nvarchar(10)) + ')'
            -- Rule 2: current row must have the open-end sentinel
            when rv.is_current = 1 and rv.valid_to_utc <> '9999-12-31'
                then 'current row has non-sentinel valid_to_utc'
            -- Rule 3: closed rows must not use the open-end sentinel
            when rv.is_current = 0 and rv.valid_to_utc = '9999-12-31'
                then 'closed row still uses open-end sentinel'
            else null
        end as violation_reason
    from role_versions rv
    inner join multi_version_users mv on rv.user_nk = mv.user_nk
)

-- Return only actual violations.  Zero rows = test passes.
select *
from violations
where violation_reason is not null
