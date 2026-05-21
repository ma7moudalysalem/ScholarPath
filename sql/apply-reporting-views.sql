-- ScholarPath PB-015 reporting views
-- Apply this once on Azure SQL to enable optional view-based analytics.
-- Safe to re-run (CREATE OR ALTER).

CREATE OR ALTER VIEW dbo.vw_funnel_daily AS
WITH registrations AS (
    SELECT CAST(CreatedAt AS date) AS ActivityDate,
           COUNT(*) AS Registrations,
           SUM(CASE WHEN IsOnboardingComplete = 1 THEN 1 ELSE 0 END) AS OnboardingCompleted
    FROM dbo.Users
    WHERE IsDeleted = 0
    GROUP BY CAST(CreatedAt AS date)
),
submitted AS (
    SELECT CAST(SubmittedAt AS date) AS ActivityDate,
           COUNT(*) AS ApplicationsSubmitted
    FROM dbo.Applications
    WHERE IsDeleted = 0 AND SubmittedAt IS NOT NULL
    GROUP BY CAST(SubmittedAt AS date)
),
accepted AS (
    SELECT CAST(COALESCE(DecisionAt, UpdatedAt, CreatedAt) AS date) AS ActivityDate,
           COUNT(*) AS ApplicationsAccepted
    FROM dbo.Applications
    WHERE IsDeleted = 0 AND Status = 'Accepted'
    GROUP BY CAST(COALESCE(DecisionAt, UpdatedAt, CreatedAt) AS date)
),
spine AS (
    SELECT ActivityDate FROM registrations
    UNION SELECT ActivityDate FROM submitted
    UNION SELECT ActivityDate FROM accepted
)
SELECT s.ActivityDate,
       COALESCE(r.Registrations, 0)           AS Registrations,
       COALESCE(r.OnboardingCompleted, 0)     AS OnboardingCompleted,
       COALESCE(sub.ApplicationsSubmitted, 0) AS ApplicationsSubmitted,
       COALESCE(acc.ApplicationsAccepted, 0)  AS ApplicationsAccepted
FROM spine s
LEFT JOIN registrations r ON r.ActivityDate = s.ActivityDate
LEFT JOIN submitted sub   ON sub.ActivityDate = s.ActivityDate
LEFT JOIN accepted acc    ON acc.ActivityDate = s.ActivityDate;
GO

CREATE OR ALTER VIEW dbo.vw_acceptance_rates AS
SELECT
    s.Id                                      AS ScholarshipId,
    s.TitleEn                                 AS ScholarshipTitleEn,
    s.TitleAr                                 AS ScholarshipTitleAr,
    COALESCE(c.NameEn, 'Uncategorized')       AS FieldEn,
    COALESCE(c.NameAr, N'غير مصنفة')          AS FieldAr,
    COALESCE(u.CountryOfResidence, 'Unknown') AS StudentCountry,
    COUNT(*)                                                       AS TotalApplications,
    SUM(CASE WHEN a.SubmittedAt IS NOT NULL THEN 1 ELSE 0 END)      AS SubmittedApplications,
    SUM(CASE WHEN a.Status = 'Accepted' THEN 1 ELSE 0 END)         AS AcceptedApplications,
    SUM(CASE WHEN a.Status = 'Rejected' THEN 1 ELSE 0 END)         AS RejectedApplications,
    CAST(100.0 * SUM(CASE WHEN a.Status = 'Accepted' THEN 1 ELSE 0 END)
         / NULLIF(SUM(CASE WHEN a.Status IN ('Accepted', 'Rejected') THEN 1 ELSE 0 END), 0)
         AS decimal(5, 2))                     AS AcceptanceRatePercent
FROM dbo.Applications a
JOIN dbo.Scholarships s   ON s.Id = a.ScholarshipId
LEFT JOIN dbo.Categories c ON c.Id = s.CategoryId
LEFT JOIN dbo.Users u      ON u.Id = a.StudentId
WHERE a.IsDeleted = 0
GROUP BY s.Id, s.TitleEn, s.TitleAr, c.NameEn, c.NameAr, u.CountryOfResidence;
GO

CREATE OR ALTER VIEW dbo.vw_finance_daily AS
WITH booking_payments AS (
    SELECT CAST(CapturedAt AS date)            AS ActivityDate,
           COUNT(*)                            AS CapturedCount,
           SUM(AmountCents) / 100.0            AS GrossUsd,
           SUM(ProfitShareAmountCents) / 100.0 AS ProfitShareUsd,
           SUM(PayeeAmountCents) / 100.0       AS PayeeNetUsd,
           SUM(RefundedAmountCents) / 100.0    AS RefundedUsd,
           SUM(CASE WHEN RefundedAmountCents > 0 THEN 1 ELSE 0 END) AS RefundCount
    FROM dbo.Payments
    WHERE IsDeleted = 0 AND CapturedAt IS NOT NULL
    GROUP BY CAST(CapturedAt AS date)
),
review_payments AS (
    SELECT CAST(CapturedAt AS date)            AS ActivityDate,
           COUNT(*)                            AS CapturedCount,
           SUM(AmountUsd)                      AS GrossUsd,
           SUM(ProfitShareAmountUsd)           AS ProfitShareUsd,
           SUM(PayeeAmountUsd)                 AS PayeeNetUsd,
           SUM(COALESCE(RefundedAmountUsd, 0)) AS RefundedUsd,
           SUM(CASE WHEN COALESCE(RefundedAmountUsd, 0) > 0 THEN 1 ELSE 0 END) AS RefundCount
    FROM dbo.CompanyReviewPayments
    WHERE CapturedAt IS NOT NULL
    GROUP BY CAST(CapturedAt AS date)
)
SELECT ActivityDate,
       CAST('ConsultantBooking' AS nvarchar(20)) AS RevenueStream,
       CapturedCount,
       CAST(GrossUsd AS decimal(14, 2))       AS GrossUsd,
       CAST(ProfitShareUsd AS decimal(14, 2)) AS ProfitShareUsd,
       CAST(PayeeNetUsd AS decimal(14, 2))    AS PayeeNetUsd,
       CAST(RefundedUsd AS decimal(14, 2))    AS RefundedUsd,
       RefundCount
FROM booking_payments
UNION ALL
SELECT ActivityDate,
       CAST('CompanyReview' AS nvarchar(20)) AS RevenueStream,
       CapturedCount,
       CAST(GrossUsd AS decimal(14, 2)),
       CAST(ProfitShareUsd AS decimal(14, 2)),
       CAST(PayeeNetUsd AS decimal(14, 2)),
       CAST(RefundedUsd AS decimal(14, 2)),
       RefundCount
FROM review_payments;
GO

CREATE OR ALTER VIEW dbo.vw_consultant_kpis AS
SELECT
    u.Id                           AS ConsultantId,
    u.FirstName + ' ' + u.LastName AS ConsultantName,
    u.Email                        AS ConsultantEmail,
    COALESCE(bk.TotalBookings, 0)     AS TotalBookings,
    COALESCE(bk.CompletedBookings, 0) AS CompletedBookings,
    COALESCE(bk.CancelledBookings, 0) AS CancelledBookings,
    COALESCE(bk.RejectedBookings, 0)  AS RejectedBookings,
    COALESCE(bk.ConsultantNoShows, 0) AS ConsultantNoShows,
    COALESCE(bk.StudentNoShows, 0)    AS StudentNoShows,
    CAST(COALESCE(bk.CompletedRevenueUsd, 0) AS decimal(12, 2)) AS CompletedRevenueUsd,
    COALESCE(rv.ReviewCount, 0)       AS ReviewCount,
    rv.AverageRating
FROM dbo.Users u
JOIN dbo.UserRoles ur ON ur.UserId = u.Id
JOIN dbo.Roles r      ON r.Id = ur.RoleId AND r.Name = 'Consultant'
LEFT JOIN (
    SELECT ConsultantId,
           COUNT(*)                                                AS TotalBookings,
           SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END)   AS CompletedBookings,
           SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END)   AS CancelledBookings,
           SUM(CASE WHEN Status = 'Rejected'  THEN 1 ELSE 0 END)   AS RejectedBookings,
           SUM(CASE WHEN IsNoShowConsultant = 1 THEN 1 ELSE 0 END) AS ConsultantNoShows,
           SUM(CASE WHEN IsNoShowStudent = 1 THEN 1 ELSE 0 END)    AS StudentNoShows,
           SUM(CASE WHEN Status = 'Completed' THEN PriceUsd ELSE 0 END) AS CompletedRevenueUsd
    FROM dbo.Bookings
    WHERE IsDeleted = 0
    GROUP BY ConsultantId
) bk ON bk.ConsultantId = u.Id
LEFT JOIN (
    SELECT ConsultantId,
           COUNT(*) AS ReviewCount,
           CAST(AVG(CAST(Rating AS decimal(4, 2))) AS decimal(4, 2)) AS AverageRating
    FROM dbo.ConsultantReviews
    WHERE IsDeleted = 0 AND IsHiddenByAdmin = 0
    GROUP BY ConsultantId
) rv ON rv.ConsultantId = u.Id
WHERE u.IsDeleted = 0;
GO

CREATE OR ALTER VIEW dbo.vw_student_journey AS
SELECT
    u.Id                           AS StudentId,
    u.FirstName + ' ' + u.LastName AS StudentName,
    u.Email                        AS StudentEmail,
    u.CountryOfResidence           AS StudentCountry,
    u.CreatedAt                    AS RegisteredAt,
    u.IsOnboardingComplete         AS OnboardingComplete,
    u.LastLoginAt                  AS LastLoginAt,
    COALESCE(ap.TotalApplications, 0)     AS TotalApplications,
    COALESCE(ap.SubmittedApplications, 0) AS SubmittedApplications,
    COALESCE(ap.AcceptedApplications, 0)  AS AcceptedApplications,
    ap.LastApplicationAt,
    COALESCE(bk.TotalBookings, 0)     AS TotalBookings,
    COALESCE(bk.CompletedBookings, 0) AS CompletedBookings,
    bk.LastBookingAt
FROM dbo.Users u
JOIN dbo.UserRoles ur ON ur.UserId = u.Id
JOIN dbo.Roles r      ON r.Id = ur.RoleId AND r.Name = 'Student'
LEFT JOIN (
    SELECT StudentId,
           COUNT(*)                                               AS TotalApplications,
           SUM(CASE WHEN SubmittedAt IS NOT NULL THEN 1 ELSE 0 END) AS SubmittedApplications,
           SUM(CASE WHEN Status = 'Accepted' THEN 1 ELSE 0 END)    AS AcceptedApplications,
           MAX(CreatedAt)                                          AS LastApplicationAt
    FROM dbo.Applications
    WHERE IsDeleted = 0
    GROUP BY StudentId
) ap ON ap.StudentId = u.Id
LEFT JOIN (
    SELECT StudentId,
           COUNT(*)                                              AS TotalBookings,
           SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedBookings,
           MAX(CreatedAt)                                        AS LastBookingAt
    FROM dbo.Bookings
    WHERE IsDeleted = 0
    GROUP BY StudentId
) bk ON bk.StudentId = u.Id
WHERE u.IsDeleted = 0;
GO

-- Verify
SELECT name, create_date FROM sys.views
WHERE name LIKE 'vw_%' AND schema_name(schema_id) = 'dbo'
ORDER BY name;
