-- PB-016 T-003 stub — Silver AI interactions (owner: @yousra-elnoby)
-- Replace with the real Silver model that cleans + de-dupes Bronze AI interactions.
-- Until then this returns zero rows so dim/fact compilation downstream succeeds.
{{ config(materialized='view') }}
select cast(null as bigint) as placeholder where 1 = 0
