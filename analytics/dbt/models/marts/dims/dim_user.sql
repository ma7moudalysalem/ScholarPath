{{
    config(
        materialized='table',
        unique_key='user_sk',
        tags=['dimension', 'scd2']
    )
}}

-- PB-016 FR-233 — SCD Type 2 on role changes. The snapshot (see
-- snapshots/snp_users.sql) captures every (user_id, active_role) version
-- along with valid_from / valid_to. This mart flattens the snapshot into
-- a dimension with a surrogate key per (user_id, version).

with versioned as (
    select
        user_id,
        email,
        first_name,
        last_name,
        country_of_residence,
        preferred_language,
        active_role,
        account_status,
        created_at,
        dbt_valid_from as valid_from_utc,
        coalesce(dbt_valid_to, cast('9999-12-31' as datetime2)) as valid_to_utc,
        case when dbt_valid_to is null then 1 else 0 end as is_current
    from {{ ref('snp_users') }}
)

select
    {{ dbt_utils.generate_surrogate_key(['user_id', 'valid_from_utc']) }} as user_sk,
    user_id                                                               as user_nk,
    email,
    first_name,
    last_name,
    first_name + ' ' + last_name                                          as full_name,
    country_of_residence,
    preferred_language,
    active_role,
    account_status,
    created_at,
    valid_from_utc,
    valid_to_utc,
    is_current
from versioned
