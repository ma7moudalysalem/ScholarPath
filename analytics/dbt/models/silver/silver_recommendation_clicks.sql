-- PB-016 T-003 stub — Silver recommendation clicks (owner: @yousra-elnoby)
-- Replace with the real Silver model for RecommendationClickEvent CDC rows (PB-017 source).
{{ config(materialized='view') }}
select cast(null as bigint) as placeholder where 1 = 0
