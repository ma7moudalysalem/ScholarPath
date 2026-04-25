-- PB-016 T-003 stub — Silver payments (owner: @yousra-elnoby)
-- Replace with the real Silver model: cents → decimal USD, status normalisation,
-- and de-dupe by (StripePaymentIntentId, latest CDC seqval).
{{ config(materialized='view') }}
select cast(null as bigint) as placeholder where 1 = 0
