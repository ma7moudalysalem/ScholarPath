-- ============================================================================
-- Consultant eligibility audit + optional remediation
-- ----------------------------------------------------------------------------
-- Business rule (enforced in code by IConsultantEligibilityService):
--   A user may act as a Consultant ONLY when ALL of the following hold:
--     1. they hold the "Consultant" role,
--     2. their account is Active, AND
--     3. they carry an approval signal — either UserProfile.ConsultantVerifiedAt
--        is set, OR they have an Approved (non-deleted) Consultant UpgradeRequest.
--
-- The application code now BLOCKS ineligible consultants at every entry point
-- (role switch, availability management, public marketplace), so this script is
-- NOT required for the fix to work — it is a diagnostic aid for ops.
--
-- STEP 1 is read-only: it lists Consultant-role accounts that are NOT eligible
-- (the "stale / unapproved Consultant role" data the bug produced). Review the
-- output before running any change.
-- ============================================================================

-- STEP 1 — DIAGNOSTIC (read-only): stale/unapproved Consultant-role accounts.
SELECT  u.Id,
        u.Email,
        u.AccountStatus,
        p.ConsultantVerifiedAt,
        CASE WHEN EXISTS (
                SELECT 1 FROM UpgradeRequests ur
                WHERE ur.UserId = u.Id
                  AND ur.Target = 1              -- UpgradeTarget.Consultant
                  AND ur.Status = 1              -- UpgradeRequestStatus.Approved
                  AND ur.IsDeleted = 0)
             THEN 1 ELSE 0 END AS HasApprovedConsultantUpgrade
FROM    AspNetUsers u
        INNER JOIN AspNetUserRoles usr ON usr.UserId = u.Id
        INNER JOIN AspNetRoles r       ON r.Id = usr.RoleId
        LEFT  JOIN UserProfiles p      ON p.UserId = u.Id
WHERE   r.Name = N'Consultant'
        AND p.ConsultantVerifiedAt IS NULL
        AND NOT EXISTS (
                SELECT 1 FROM UpgradeRequests ur
                WHERE ur.UserId = u.Id
                  AND ur.Target = 1
                  AND ur.Status = 1
                  AND ur.IsDeleted = 0);

-- ----------------------------------------------------------------------------
-- STEP 2 — OPTIONAL remediation. Pick exactly ONE, only after reviewing STEP 1.
--
-- Option A (recommended, fail-closed): leave the rows as-is. The application
--   already refuses to let these accounts act as consultants, and they will be
--   verified the moment an admin approves them through the normal flow. No SQL
--   change needed.
--
-- Option B (backfill markers for accounts you have confirmed ARE legitimate
--   consultants, e.g. accounts that were verified out-of-band): stamp the
--   marker so they immediately regain consultant capability. DO NOT run this
--   blindly — it grants consultant capability to every listed row.
--
--   BEGIN TRAN;
--       UPDATE p
--          SET p.ConsultantVerifiedAt = SYSDATETIMEOFFSET()
--       FROM  UserProfiles p
--             INNER JOIN AspNetUserRoles usr ON usr.UserId = p.UserId
--             INNER JOIN AspNetRoles r       ON r.Id = usr.RoleId
--       WHERE r.Name = N'Consultant'
--         AND p.ConsultantVerifiedAt IS NULL
--         AND p.UserId IN ( /* explicit, reviewed user ids only */ );
--   COMMIT;   -- verify @@ROWCOUNT first, ROLLBACK if unexpected.
--
-- Option C (revoke the stray role for accounts that should never have had it):
--   remove the Consultant role membership entirely.
--
--   DELETE usr
--   FROM   AspNetUserRoles usr
--          INNER JOIN AspNetRoles r ON r.Id = usr.RoleId
--   WHERE  r.Name = N'Consultant'
--     AND  usr.UserId IN ( /* explicit, reviewed user ids only */ );
-- ============================================================================
