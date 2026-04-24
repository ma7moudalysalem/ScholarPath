{{
    config(
        materialized='table',
        unique_key='scholarship_sk',
        tags=['dimension', 'scd2']
    )
}}

-- PB-016 FR-233 — SCD Type 2 on scholarship status + funding changes.
-- Close-date changes are tracked too so "deadline drift" reports read cleanly.

with versioned as (
    select
        scholarship_id,
        title_en,
        title_ar,
        category_id,
        owner_company_id,
        funding_type,
        funding_amount_usd,
        target_level,
        status,
        deadline,
        is_featured,
        dbt_valid_from                                                 as valid_from_utc,
        coalesce(dbt_valid_to, cast('9999-12-31' as datetime2))        as valid_to_utc,
        case when dbt_valid_to is null then 1 else 0 end               as is_current
    from {{ ref('snp_scholarships') }}
)

select
    {{ dbt_utils.generate_surrogate_key(['scholarship_id', 'valid_from_utc']) }} as scholarship_sk,
    scholarship_id                                                               as scholarship_nk,
    title_en,
    title_ar,
    category_id,
    owner_company_id,
    funding_type,
    funding_amount_usd,
    target_level,
    status,
    deadline,
    is_featured,
    valid_from_utc,
    valid_to_utc,
    is_current
from versioned
