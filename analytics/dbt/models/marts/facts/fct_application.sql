{{
    config(
        materialized='incremental',
        unique_key='application_event_sk',
        on_schema_change='fail',
        incremental_strategy='merge',
        tags=['fact']
    )
}}

-- Grain: one row per (application_id, status_change_at).
-- An application progresses Draft → Pending → UnderReview → Shortlisted →
-- Accepted/Rejected/Withdrawn. We derive each event from the CDC history
-- so the report can show funnel velocity without destructive updates.

with src as (
    select *
    from {{ ref('silver_application_events') }}
    {% if is_incremental() %}
      where status_change_at_utc > (select coalesce(max(status_change_at_utc), cast('1900-01-01' as datetime2))
                                     from {{ this }})
    {% endif %}
)

select
    {{ dbt_utils.generate_surrogate_key(['application_id', 'status_change_at_utc']) }} as application_event_sk,
    application_id                        as application_nk,
    usr.user_sk                           as student_sk,
    sch.scholarship_sk                    as scholarship_sk,
    dd.date_key                           as status_change_date_key,
    src.status_change_at_utc,
    src.previous_status,
    src.new_status,
    src.mode,                             -- InApp vs External
    datediff(day, src.submitted_at_utc, src.status_change_at_utc) as days_since_submit,
    src.is_terminal                       as is_terminal_status
from src
left join {{ ref('dim_user') }} usr
  on usr.user_nk = src.student_id
 and src.status_change_at_utc between usr.valid_from_utc and usr.valid_to_utc
left join {{ ref('dim_scholarship') }} sch
  on sch.scholarship_nk = src.scholarship_id
 and src.status_change_at_utc between sch.valid_from_utc and sch.valid_to_utc
left join {{ ref('dim_date') }} dd
  on dd.full_date = cast(src.status_change_at_utc as date)
