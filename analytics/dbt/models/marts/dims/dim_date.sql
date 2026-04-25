{{
    config(
        materialized='table',
        unique_key='date_key'
    )
}}

-- Generated calendar table. Re-materialised once — no dependency on source
-- data. Range is wide enough (2020-01-01 through 2035-12-31) to cover all
-- foreseeable ScholarPath reporting without a refresh.

with date_spine as (
    {{ dbt_utils.date_spine(
        datepart='day',
        start_date="cast('2020-01-01' as date)",
        end_date="cast('2036-01-01' as date)"
    ) }}
)

select
    cast(replace(convert(varchar(10), date_day, 23), '-', '') as int) as date_key,     -- 20260425
    cast(date_day as date)                                            as full_date,
    datepart(year,        date_day) as year_number,
    datepart(quarter,     date_day) as quarter_number,
    datepart(month,       date_day) as month_number,
    datename(month,       date_day) as month_name,
    datepart(week,        date_day) as week_number,
    datepart(day,         date_day) as day_of_month,
    datepart(weekday,     date_day) as day_of_week,
    datename(weekday,     date_day) as day_name,
    case when datepart(weekday, date_day) in (1, 7) then 1 else 0 end as is_weekend,
    datepart(dayofyear,   date_day) as day_of_year,
    -- Semester fields make cohort / intake analytics easier
    case
        when datepart(month, date_day) between 9 and 12 then 'Fall'
        when datepart(month, date_day) between 1 and 4  then 'Spring'
        else 'Summer'
    end as academic_semester
from date_spine
