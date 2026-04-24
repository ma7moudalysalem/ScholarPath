{{
    config(
        materialized='table',
        unique_key='ai_feature_sk'
    )
}}

-- Tiny conformed dimension used by fct_ai_interaction + fct_recommendation_click.
-- Source of truth: ScholarPath.Domain/Enums/Enums.cs :: AiFeature.
-- When a new feature is added to the enum, add the row here so reports don't
-- break silently on an unknown key.

with features as (
    select * from (
        values
            (0, 'Recommendation', 'Student-facing scholarship recommender'),
            (1, 'Eligibility',    'Rule-based eligibility explainer'),
            (2, 'Chatbot',        'Guided assistant — chat turns')
    ) as v(feature_code, feature_name, feature_description)
)
select
    feature_code as ai_feature_sk,
    feature_code,
    feature_name,
    feature_description
from features
