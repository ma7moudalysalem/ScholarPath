-- ============================================================================
-- PB-016 US-002 / T-002 — Parameterised CDC window query invoked by ADF.
--
-- ADF Copy activity sources from this as a SQL query with two parameters:
--   @from_lsn  : the last successful LSN stored in dbo.BronzeWatermark
--   @to_lsn    : sys.fn_cdc_get_max_lsn() captured at run start
--   @capture_instance : 'dbo_<table>' (ADF injects per ForEach iteration)
--
-- We use fn_cdc_get_net_changes_* (not fn_cdc_get_all_changes_*) so repeated
-- updates to a row inside the window collapse into one — Bronze stores the
-- latest value per primary key, cheaper for downstream Silver dedup.
-- ============================================================================

DECLARE @from_lsn binary(10) = CAST(0x000000000000000000 AS binary(10));  -- bound at runtime
DECLARE @to_lsn   binary(10) = sys.fn_cdc_get_max_lsn();                  -- bound at runtime

-- The CTE pattern lets ADF inject @capture_instance as a table-valued source
-- via dynamic SQL (ADF `Dataset` parameters). Keep the projected columns stable
-- so staging models don't shift beneath Yosra.
SELECT
    __$operation       AS cdc_operation,          -- 1=delete, 2=insert, 3=pre-update, 4=post-update
    __$update_mask     AS cdc_update_mask,
    __$seqval          AS cdc_seqval,
    sys.fn_cdc_map_lsn_to_time(__$start_lsn) AS cdc_commit_ts,
    *
FROM cdc.fn_cdc_get_net_changes_dbo_Payments(@from_lsn, @to_lsn, N'all with mask')
-- ADF dynamic SQL swaps the function name per table via string templating.
;
