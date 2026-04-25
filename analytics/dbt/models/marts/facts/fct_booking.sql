{{
    config(
        materialized='incremental',
        unique_key='booking_sk',
        on_schema_change='fail',
        incremental_strategy='merge',
        tags=['fact']
    )
}}

-- Grain: one row per consultant booking. Includes status history via a
-- flattened set of timestamps so both funnel and utilisation reports can
-- read from this one fact.

with src as (
    select *
    from {{ ref('silver_bookings') }}
    {% if is_incremental() %}
      where updated_at_utc > (select coalesce(max(updated_at_utc), cast('1900-01-01' as datetime2))
                               from {{ this }})
    {% endif %}
)

select
    {{ dbt_utils.generate_surrogate_key(['booking_id']) }}            as booking_sk,
    booking_id                                                        as booking_nk,
    stu.user_sk                                                       as student_sk,
    con.user_sk                                                       as consultant_sk,
    dd_booked.date_key                                                as booked_date_key,
    dd_scheduled.date_key                                             as scheduled_date_key,
    src.status,
    src.price_usd,
    datediff(minute, src.scheduled_start_at_utc, src.scheduled_end_at_utc) as scheduled_duration_min,
    case when src.status = 'Completed'        then 1 else 0 end       as is_completed,
    case when src.status in ('Cancelled', 'Rejected', 'Expired') then 1 else 0 end as is_lost,
    src.requested_at_utc,
    src.scheduled_start_at_utc,
    src.scheduled_end_at_utc,
    src.updated_at_utc
from src
left join {{ ref('dim_user') }} stu
  on stu.user_nk = src.student_id
 and src.requested_at_utc between stu.valid_from_utc and stu.valid_to_utc
left join {{ ref('dim_user') }} con
  on con.user_nk = src.consultant_id
 and src.requested_at_utc between con.valid_from_utc and con.valid_to_utc
left join {{ ref('dim_date') }} dd_booked
  on dd_booked.full_date = cast(src.requested_at_utc as date)
left join {{ ref('dim_date') }} dd_scheduled
  on dd_scheduled.full_date = cast(src.scheduled_start_at_utc as date)
