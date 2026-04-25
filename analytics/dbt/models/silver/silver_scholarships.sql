-- PB-016 T-003 stub — Silver scholarships (owner: @yousra-elnoby)
-- Replace with the real Silver model that cleans Scholarship CDC rows
-- and prepares them for dim_scholarship (SCD Type 2 in Gold).
{{ config(materialized='view') }}
select cast(null as bigint) as placeholder where 1 = 0
