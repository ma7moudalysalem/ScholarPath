-- ============================================================================
-- PB-016 US-002 / T-002 — Watermark table for ADF Bronze ingestion.
--
-- Keeps the last successful LSN per (capture_instance) so the next ADF run
-- picks up exactly where we left off — no double-copy, no missed deltas.
-- UpdatedAtUtc is auditable.
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'analytics')
    EXEC('CREATE SCHEMA analytics');
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = 'analytics' AND t.name = 'BronzeWatermark'
)
BEGIN
    CREATE TABLE analytics.BronzeWatermark
    (
        CaptureInstance  nvarchar(128)   NOT NULL PRIMARY KEY,
        LastFromLsn      binary(10)      NOT NULL,
        LastToLsn        binary(10)      NOT NULL,
        RowsCopied       bigint          NOT NULL DEFAULT 0,
        UpdatedAtUtc     datetime2(3)    NOT NULL DEFAULT sysutcdatetime(),
        UpdatedByPipelineRunId uniqueidentifier NULL
    );
END
GO

-- Seed row for every capture instance that 01-enable-cdc.sql enabled.
-- Starting LSN = 0x00…00 means "replay from beginning" on first run.
MERGE analytics.BronzeWatermark AS target
USING (VALUES
    ('dbo_AspNetUsers'),
    ('dbo_UserProfiles'),
    ('dbo_UpgradeRequests'),
    ('dbo_Scholarships'),
    ('dbo_SavedScholarships'),
    ('dbo_ApplicationTrackers'),
    ('dbo_CompanyReviews'),
    ('dbo_ConsultantBookings'),
    ('dbo_Payments'),
    ('dbo_Payouts'),
    ('dbo_ForumPosts'),
    ('dbo_ChatMessages'),
    ('dbo_AiInteractions'),
    ('dbo_RecommendationClickEvents'),
    ('dbo_Notifications')
) AS source(CaptureInstance)
ON target.CaptureInstance = source.CaptureInstance
WHEN NOT MATCHED THEN
    INSERT (CaptureInstance, LastFromLsn, LastToLsn)
    VALUES (source.CaptureInstance, 0x00000000000000000000, 0x00000000000000000000);
GO
