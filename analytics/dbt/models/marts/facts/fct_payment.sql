{{
    config(
        materialized='incremental',
        unique_key='payment_sk',
        on_schema_change='fail',
        incremental_strategy='merge',
        tags=['fact']
    )
}}

-- Grain: one row per captured Stripe payment intent. We don't include Pending
-- or Failed rows here — PB-015 revenue KPIs read only from captured state.
-- Refunded payments contribute a negative amount via refund_amount_cents so
-- gross vs net revenue works without a second fact.

with src as (
    select *
    from {{ ref('silver_payments') }}
    where status in ('Captured', 'Refunded', 'PartiallyRefunded')
    {% if is_incremental() %}
      and captured_at_utc > (select coalesce(max(captured_at_utc), cast('1900-01-01' as datetime2))
                              from {{ this }})
    {% endif %}
)

select
    {{ dbt_utils.generate_surrogate_key(['payment_id']) }}       as payment_sk,
    payment_id                                                    as payment_nk,
    payer.user_sk                                                 as payer_sk,
    dd.date_key                                                   as captured_date_key,
    src.payment_type,
    src.amount_cents,
    src.refund_amount_cents,
    src.amount_cents - coalesce(src.refund_amount_cents, 0)       as net_amount_cents,
    src.profit_share_amount_cents,
    src.currency,
    src.stripe_charge_id,
    src.captured_at_utc
from src
left join {{ ref('dim_user') }} payer
  on payer.user_nk = src.payer_user_id
 and src.captured_at_utc between payer.valid_from_utc and payer.valid_to_utc
left join {{ ref('dim_date') }} dd
  on dd.full_date = cast(src.captured_at_utc as date)
