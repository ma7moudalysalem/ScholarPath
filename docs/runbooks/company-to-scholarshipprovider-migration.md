# Runbook — Company → ScholarshipProvider rename (coordinated deploy)

This rename touches the application DB schema **and** the CDC/analytics pipeline. The
app migration (`20260702223703_RenameCompanyToScholarshipProvider`) is data-preserving,
but the CDC capture instances and the analytics warehouse reference the OLD physical
names and must be re-pointed **in the same maintenance window**, or reporting breaks.

## Order of operations (staging first, then prod)

1. **Enable maintenance mode** (PlatformSettings) — stops writes to the review tables.
2. **Pause the analytics pipeline** — disable the ADF trigger for `cdc_to_bronze` so no
   run fires against half-renamed tables.
3. **Apply the app migration.** It runs automatically on API startup (`MigrateAsync`), or
   apply manually: `dotnet ef database update --project src/ScholarPath.Infrastructure --startup-project src/ScholarPath.API`.
   It renames tables/columns/constraints in place (no data loss) and rewrites the role
   name (`Company`→`ScholarshipProvider`) in `Roles`, `Users.ActiveRole`,
   `Resources.AuthorRole`, plus the persisted enum values in `Payments.Type`,
   `UpgradeRequests.Target`, and `ScholarshipProviderReviewRequests.Status`, and the
   provider org-type value `Company`→`Corporation` in `UserProfiles.ScholarshipProviderType`.
4. **Re-point CDC.** SQL Server CDC does **not** follow a table rename. For each renamed
   table (`CompanyReviews`, `CompanyReviewPayments`, `CompanyReviewRequests`):
   - `sys.sp_cdc_disable_table` for the OLD capture instance (`dbo_CompanyReviews`, …).
   - `sys.sp_cdc_enable_table` for the NEW table (`dbo.ScholarshipProviderReviews`, …),
     naming the capture instance `dbo_ScholarshipProviderReviews`, etc.
   - Update `analytics/sql/01-enable-cdc.sql` (already renamed in this branch) is the
     source of truth for the new capture set.
   - Reset the bronze watermark rows in `analytics/sql/03-bronze-watermark.sql` for the
     renamed capture instances (they restart from the new instance's min LSN).
5. **Update the bronze landing** so `owner_company_id`→`owner_scholarship_provider_id`
   flows through (dbt sources/models/snapshots already renamed in this branch:
   `analytics/dbt/models/staging/_sources.yml`, `dim_scholarship.sql`, `snp_scholarships.sql`).
   Run `dbt run --full-refresh` on the affected models (a full refresh is required because
   the snapshot column changed name).
6. **Update Power BI.** The dataset's RLS roles and any table/column bindings that used
   `Company*`/`owner_company_id` must be re-pointed to the new names. The embed token
   already sends the plain active role, now `ScholarshipProvider` (see `PowerBiService.cs`);
   the Power BI RLS role must be named `ScholarshipProvider` (NOT `CompanyScope` — that
   mapping never existed in code; see the ANALYTICS-RLS doc fix).
7. **Re-enable the ADF trigger** and **disable maintenance mode**.
8. **Session note:** every currently-signed-in Scholarship Provider carries
   `active_role=Company` in their (≤15 min) access token; those requests 403 against the
   renamed `[Authorize(Roles="ScholarshipProvider")]` until their token refreshes. This is
   expected and self-heals within one refresh cycle. Optionally force-revoke provider
   refresh tokens post-migration so they re-login cleanly.

## Rollback
The migration `Down` reverses every rename and data update. If rolling back, also re-run
steps 4–6 against the OLD names (the git history of the analytics files has them).

## Verification checklist (staging)
- [ ] Migration applies with 0 errors on a **copy of prod data**; row counts on the 3
      renamed tables are unchanged before/after.
- [ ] A Scholarship Provider can log in, see their scholarships, and review applications.
- [ ] A `ScholarshipProviderReview` payment capture/refund still reconciles.
- [ ] CDC bronze picks up a new review row end-to-end.
- [ ] A Power BI report filtered by provider renders (RLS returns rows).
