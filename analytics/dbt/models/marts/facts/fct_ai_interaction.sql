{{
    config(
        materialized='incremental',
        unique_key='ai_interaction_sk',
        on_schema_change='fail',
        incremental_strategy='merge',
        tags=['fact']
    )
}}

-- Grain: one row per AI call. Drives the PB-017 economy dashboard:
--   - cost / feature / day
--   - success rate (error_message IS NULL)
--   - latency p50/p95 via (completed_at - started_at)
-- Errored or in-flight rows still land here with is_succeeded=0 and
-- latency_ms=NULL so the volume metric stays honest.

with src as (
    select *
    from {{ ref('silver_ai_interactions') }}
    {% if is_incremental() %}
      where started_at_utc > (select coalesce(max(started_at_utc), cast('1900-01-01' as datetime2))
                               from {{ this }})
    {% endif %}
)

select
    {{ dbt_utils.generate_surrogate_key(['ai_interaction_id']) }}   as ai_interaction_sk,
    ai_interaction_id                                                as ai_interaction_nk,
    usr.user_sk                                                      as user_sk,
    feat.ai_feature_sk                                               as ai_feature_sk,
    dd.date_key                                                      as started_date_key,
    src.provider,
    src.model_name,
    src.prompt_tokens,
    src.completion_tokens,
    src.cost_usd,
    datediff(millisecond, src.started_at_utc, src.completed_at_utc)  as latency_ms,
    case when src.error_message is null and src.completed_at_utc is not null
         then 1 else 0 end                                           as is_succeeded,
    src.started_at_utc,
    src.completed_at_utc
from src
left join {{ ref('dim_user') }} usr
  on usr.user_nk = src.user_id
 and src.started_at_utc between usr.valid_from_utc and usr.valid_to_utc
left join {{ ref('dim_ai_feature') }} feat
  on feat.feature_code = src.feature_code
left join {{ ref('dim_date') }} dd
  on dd.full_date = cast(src.started_at_utc as date)
