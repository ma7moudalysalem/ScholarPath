-- ============================================================================
-- PB-016 US-001 / T-001 — Enable SQL Server CDC on 15 core OLTP tables.
--
-- Runs once per environment. Idempotent: guards with sys.change_tracking_tables
-- so re-running on an already-enabled database is a no-op.
--
-- Owner: @yousra-elnoby. This file is the reference script @ma7moudalysalem
-- uses from the ADF bootstrap activity in US-008; keep it in lock-step with
-- the ADF watermark table (see 03-bronze-watermark.sql).
--
-- Retention: 3 days (default 72h). The 15-minute ADF refresh lag + a 24h
-- re-run window on partition failure fits comfortably inside that retention.
-- ============================================================================

USE [ScholarPathDb];
GO

-- 1) Enable CDC on the database. Safe to re-run.
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = DB_NAME() AND is_cdc_enabled = 1)
BEGIN
    EXEC sys.sp_cdc_enable_db;
END
GO

-- 2) Enable CDC on each tracked table. Capture-role is 'cdc_role' so the
--    dbt + ADF service principals can SELECT from cdc.fn_cdc_get_net_changes_*
--    without needing db_owner.
DECLARE @tables TABLE (schema_name sysname, table_name sysname);
INSERT INTO @tables VALUES
    ('dbo', 'AspNetUsers'),          -- PB-001 identity
    ('dbo', 'UserProfiles'),         -- PB-002 profile
    ('dbo', 'UpgradeRequests'),      -- PB-001 onboarding funnel
    ('dbo', 'Scholarships'),         -- PB-003 catalog
    ('dbo', 'SavedScholarships'),    -- PB-003 bookmarks (CTR denominator)
    ('dbo', 'ApplicationTrackers'),  -- PB-004 applications (the big one)
    ('dbo', 'CompanyReviews'),       -- PB-005 ratings
    ('dbo', 'ConsultantBookings'),   -- PB-006 bookings
    ('dbo', 'Payments'),             -- PB-013 payments (revenue truth)
    ('dbo', 'Payouts'),              -- PB-013 consultant payouts
    ('dbo', 'ForumPosts'),           -- PB-007 community
    ('dbo', 'ChatMessages'),         -- PB-007 chat volume (no content leaves OLTP)
    ('dbo', 'AiInteractions'),       -- PB-008 AI usage (feeds PB-017)
    ('dbo', 'RecommendationClickEvents'), -- PB-017 CTR numerator
    ('dbo', 'Notifications');        -- PB-010 delivery outcomes

DECLARE @s sysname, @t sysname;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT schema_name, table_name FROM @tables;
OPEN cur;
FETCH NEXT FROM cur INTO @s, @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM cdc.change_tables ct
        INNER JOIN sys.tables st ON st.object_id = ct.source_object_id
        INNER JOIN sys.schemas sch ON sch.schema_id = st.schema_id
        WHERE sch.name = @s AND st.name = @t
    )
    BEGIN
        EXEC sys.sp_cdc_enable_table
            @source_schema = @s,
            @source_name   = @t,
            @role_name     = N'cdc_role',
            @supports_net_changes = 1;
    END
    FETCH NEXT FROM cur INTO @s, @t;
END
CLOSE cur;
DEALLOCATE cur;
GO

-- 3) Tighten retention to 3 days (from SQL default of 2880 minutes = 2 days; we want 4320).
EXEC sys.sp_cdc_change_job @job_type = N'cleanup', @retention = 4320;
GO
