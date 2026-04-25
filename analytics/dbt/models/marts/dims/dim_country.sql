{{
    config(
        materialized='table',
        unique_key='country_sk'
    )
}}

-- ISO 3166-1 alpha-2 country reference. Built from the union of every
-- country code we have in user profiles + scholarship target lists, with
-- human-readable names and region/sub-region for map visuals.

with observed as (
    select distinct country_of_residence as code from {{ ref('silver_users') }} where country_of_residence is not null
    union
    select distinct jsonval               as code from {{ ref('silver_scholarship_target_countries') }}
),
enriched as (
    select
        o.code,
        iso.country_name,
        iso.region,
        iso.sub_region
    from observed o
    left join {{ ref('seed_iso_countries') }} iso on iso.alpha2 = o.code
)

select
    {{ dbt_utils.generate_surrogate_key(['code']) }} as country_sk,
    code                                             as country_code,
    country_name,
    region,
    sub_region
from enriched
where code is not null
