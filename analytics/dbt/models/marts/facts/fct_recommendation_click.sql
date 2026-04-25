{{
    config(
        materialized='incremental',
        unique_key='click_sk',
        on_schema_change='fail',
        incremental_strategy='merge',
        tags=['fact']
    )
}}

-- Grain: one row per recommendation click event (PB-017 FR-249).
-- Joins to the AI interaction that generated the recommendation when
-- ai_interaction_id is present; NULL-safe otherwise.
--
-- CTR = count(fct_recommendation_click) / count(fct_ai_interaction WHERE feature='Recommendation')

with src as (
    select *
    from {{ ref('silver_recommendation_clicks') }}
    {% if is_incremental() %}
      where clicked_at_utc > (select coalesce(max(clicked_at_utc), cast('1900-01-01' as datetime2))
                               from {{ this }})
    {% endif %}
)

select
    {{ dbt_utils.generate_surrogate_key(['click_id']) }}          as click_sk,
    click_id                                                       as click_nk,
    usr.user_sk                                                    as user_sk,
    sch.scholarship_sk                                             as scholarship_sk,
    src.ai_interaction_id,                                         -- left natural, nullable
    dd.date_key                                                    as clicked_date_key,
    src.source_type,                                               -- card | list | modal
    src.clicked_at_utc
from src
left join {{ ref('dim_user') }} usr
  on usr.user_nk = src.user_id
 and src.clicked_at_utc between usr.valid_from_utc and usr.valid_to_utc
left join {{ ref('dim_scholarship') }} sch
  on sch.scholarship_nk = src.scholarship_id
 and src.clicked_at_utc between sch.valid_from_utc and sch.valid_to_utc
left join {{ ref('dim_date') }} dd
  on dd.full_date = cast(src.clicked_at_utc as date)
