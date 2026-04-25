-- PB-016 T-003 stub — Silver scholarship target countries (owner: @yousra-elnoby)
-- Replace with the real Silver model that explodes Scholarship.TargetCountriesJson
-- into one row per (ScholarshipId, country_code).
{{ config(materialized='view') }}
select cast(null as nvarchar(2)) as jsonval where 1 = 0
