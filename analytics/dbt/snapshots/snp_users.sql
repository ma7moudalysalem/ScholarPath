{% snapshot snp_users %}
    {{
        config(
            target_schema='snapshots',
            unique_key='user_id',
            strategy='check',
            check_cols=['active_role', 'account_status', 'email', 'country_of_residence']
        )
    }}

    -- Feeds dim_user. Captures every meaningful state change (role swap,
    -- account-status transition, email/country edit) and preserves history
    -- via dbt's built-in dbt_valid_from / dbt_valid_to columns.
    select
        user_id,
        email,
        first_name,
        last_name,
        country_of_residence,
        preferred_language,
        active_role,
        account_status,
        created_at
    from {{ ref('silver_users') }}
{% endsnapshot %}
