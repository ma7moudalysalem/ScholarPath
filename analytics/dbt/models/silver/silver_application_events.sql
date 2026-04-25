-- PB-016 T-003 stub — Silver application events (owner: @yousra-elnoby)
-- Replace with the real Silver model that explodes ApplicationTracker status changes.
{{ config(materialized='view') }}
select cast(null as bigint) as placeholder where 1 = 0
