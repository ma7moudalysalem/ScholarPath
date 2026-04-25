-- PB-016 T-003 stub — Silver users (owner: @yousra-elnoby)
-- Replace with the real Silver model that joins AspNetUsers + UserProfiles
-- and flattens role + status. Source for dim_user (SCD2) and dim_country.
{{ config(materialized='view') }}
select cast(null as bigint) as placeholder where 1 = 0
