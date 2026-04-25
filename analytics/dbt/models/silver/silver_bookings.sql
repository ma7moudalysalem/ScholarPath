-- PB-016 T-003 stub — Silver consultant bookings (owner: @yousra-elnoby)
-- Replace with the real Silver model that cleans + de-dupes ConsultantBooking CDC rows.
{{ config(materialized='view') }}
select cast(null as bigint) as placeholder where 1 = 0
