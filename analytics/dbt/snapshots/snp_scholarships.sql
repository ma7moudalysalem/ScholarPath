{% snapshot snp_scholarships %}
    {{
        config(
            target_schema='snapshots',
            unique_key='scholarship_id',
            strategy='check',
            check_cols=['status', 'funding_type', 'funding_amount_usd', 'deadline', 'is_featured']
        )
    }}

    -- Feeds dim_scholarship. Tracks status (Draft → Open → Closed), funding
    -- shape, deadline drift, and feature-flag toggles so reports don't rely
    -- on the live OLTP row.
    select
        scholarship_id,
        title_en,
        title_ar,
        category_id,
        owner_company_id,
        funding_type,
        funding_amount_usd,
        target_level,
        status,
        deadline,
        is_featured
    from {{ ref('silver_scholarships') }}
{% endsnapshot %}
