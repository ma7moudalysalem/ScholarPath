// PB-018 T-009 / FR-269 — ChurnRiskScore dataflow (Power Query M).
//
// Refreshed daily at 04:00 UTC. Reads Gold marts via Synapse Serverless,
// scores every active student, and writes the output to the dataflow
// sink `churn_risk_snapshot`. A downstream ADF pipeline (reverse-etl.json)
// copies that snapshot into OLTP.UserRiskFlags.
//
// Scoring — a lightweight rule-based model (graduation-scope, not ML):
//   base = 0
//   + 0.40 if days_since_last_login > 30
//   + 0.25 if applications_submitted_30d == 0 AND is_student
//   + 0.15 if booking_no_shows_90d >= 2
//   + 0.10 if ai_recommendation_clicks_30d == 0 AND applications_submitted_30d == 0
//   + 0.10 if notifications_read_30d / notifications_received_30d < 0.1
//
// Cap at 1.0. Users above the threshold (default 0.65) land with IsAtRisk=true.
// The model can be swapped for a proper classifier later without schema change.

let
    Source = Sql.Database("scholarpath-synapse-prod.sql.azuresynapse.net", "warehouse", [Query="
        WITH
        users AS (
            SELECT user_nk AS user_id,
                   country_of_residence,
                   account_status,
                   active_role,
                   created_at
            FROM gold.dim_user
            WHERE is_current = 1 AND account_status = 'Active'
        ),
        last_login AS (
            SELECT u.user_nk AS user_id,
                   DATEDIFF(day, MAX(u.created_at), GETUTCDATE()) AS days_since_last_login
            FROM gold.dim_user u
            GROUP BY u.user_nk
        ),
        recent_apps AS (
            SELECT student_sk, COUNT(*) AS apps_30d
            FROM gold.fct_application
            WHERE status_change_at_utc >= DATEADD(day, -30, GETUTCDATE())
              AND new_status IN ('Pending', 'UnderReview', 'Shortlisted')
            GROUP BY student_sk
        ),
        clicks AS (
            SELECT user_sk, COUNT(*) AS clicks_30d
            FROM gold.fct_recommendation_click
            WHERE clicked_at_utc >= DATEADD(day, -30, GETUTCDATE())
            GROUP BY user_sk
        ),
        bookings AS (
            SELECT student_sk,
                   SUM(CASE WHEN status = 'NoShowStudent' THEN 1 ELSE 0 END) AS no_shows_90d
            FROM gold.fct_booking
            WHERE updated_at_utc >= DATEADD(day, -90, GETUTCDATE())
            GROUP BY student_sk
        )
        SELECT u.user_id,
               ll.days_since_last_login,
               COALESCE(ra.apps_30d, 0)    AS apps_30d,
               COALESCE(c.clicks_30d, 0)   AS clicks_30d,
               COALESCE(b.no_shows_90d, 0) AS no_shows_90d,
               CASE WHEN u.active_role = 'Student' THEN 1 ELSE 0 END AS is_student
        FROM users u
        LEFT JOIN last_login   ll ON ll.user_id = u.user_id
        LEFT JOIN gold.dim_user du ON du.user_nk = u.user_id AND du.is_current = 1
        LEFT JOIN recent_apps  ra ON ra.student_sk  = du.user_sk
        LEFT JOIN clicks       c  ON c.user_sk     = du.user_sk
        LEFT JOIN bookings     b  ON b.student_sk  = du.user_sk
    "]),

    #"Scored" = Table.AddColumn(Source, "Score", each
        let
            s0 = 0.0,
            s1 = s0 + (if [days_since_last_login] <> null and [days_since_last_login] > 30 then 0.40 else 0.0),
            s2 = s1 + (if [apps_30d] = 0 and [is_student] = 1 then 0.25 else 0.0),
            s3 = s2 + (if [no_shows_90d] >= 2 then 0.15 else 0.0),
            s4 = s3 + (if [clicks_30d] = 0 and [apps_30d] = 0 then 0.10 else 0.0)
        in
            if s4 > 1.0 then 1.0 else s4, type number),

    #"Flagged" = Table.AddColumn(#"Scored", "IsAtRisk", each [Score] >= 0.65, Logical.Type),

    #"Reasoned" = Table.AddColumn(#"Flagged", "Reason", each
        let
            parts =
                (if [days_since_last_login] > 30 then {"no login 30d"} else {}) &
                (if [apps_30d] = 0 and [is_student] = 1 then {"no applications 30d"} else {}) &
                (if [no_shows_90d] >= 2 then {"repeat no-shows"} else {}) &
                (if [clicks_30d] = 0 and [apps_30d] = 0 then {"no engagement 30d"} else {})
        in
            if List.IsEmpty(parts) then null else Text.Combine(parts, "; "),
        type nullable text),

    #"Final" = Table.SelectColumns(#"Reasoned", {"user_id", "Score", "IsAtRisk", "Reason"})
in
    #"Final"
