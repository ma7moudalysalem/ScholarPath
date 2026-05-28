-- ─────────────────────────────────────────────────────────────────────────────
-- Seed the 3 payments-related platform settings introduced by the free / paid
-- refactor (master switch + two allow-free toggles).
--
-- These rows are also added by DbSeeder.SeedPlatformSettingsAsync at API start
-- (idempotent on missing keys), so this script is mostly a belt-and-braces
-- option for prod environments where you don't want to rely on the seeder
-- running. Safe to execute multiple times — each block is gated on the key
-- not already existing in PlatformSettings.
--
-- No schema migration is required by the free / paid work: every column the
-- handlers touch was already nullable, and no new columns were added.
-- ─────────────────────────────────────────────────────────────────────────────

-- 1) Master payments switch. When OFF the platform runs fully free —
--    every scholarship fee + consultant fee is silently forced to 0, the
--    Apply Now / Booking flows always take the free path, fee inputs hide
--    in the client, billing dashboards show a banner.
IF NOT EXISTS (SELECT 1 FROM [PlatformSettings] WHERE [Key] = N'payments.enabled')
BEGIN
    INSERT INTO [PlatformSettings]
        ([Id], [Key], [Value], [ValueType], [DescriptionEn], [DescriptionAr],
         [Category], [CreatedAt])
    VALUES (
        NEWID(),
        N'payments.enabled',
        N'true',
        N'Boolean',
        N'Master toggle. When off, the platform runs fully free — all fees become 0, Stripe is bypassed everywhere.',
        N'المفتاح الرئيسي. عند إيقافه تعمل المنصة مجاناً بالكامل — جميع الرسوم تصبح صفر، ولا يُستدعى Stripe.',
        N'Payments',
        SYSDATETIMEOFFSET()
    );
END;

-- 2) Per-feature allow-free for scholarships. Used only when payments.enabled
--    is on. When OFF a Company must set a Review Service Fee > 0.
IF NOT EXISTS (SELECT 1 FROM [PlatformSettings] WHERE [Key] = N'payments.allowFreeScholarships')
BEGIN
    INSERT INTO [PlatformSettings]
        ([Id], [Key], [Value], [ValueType], [DescriptionEn], [DescriptionAr],
         [Category], [CreatedAt])
    VALUES (
        NEWID(),
        N'payments.allowFreeScholarships',
        N'true',
        N'Boolean',
        N'Allow companies to mark in-app scholarships as free (review fee = 0).',
        N'السماح للشركات بجعل المنح داخل المنصة مجانية (رسوم المراجعة = 0).',
        N'Payments',
        SYSDATETIMEOFFSET()
    );
END;

-- 3) Per-feature allow-free for consultant sessions. Same pattern.
IF NOT EXISTS (SELECT 1 FROM [PlatformSettings] WHERE [Key] = N'payments.allowFreeConsultantSessions')
BEGIN
    INSERT INTO [PlatformSettings]
        ([Id], [Key], [Value], [ValueType], [DescriptionEn], [DescriptionAr],
         [Category], [CreatedAt])
    VALUES (
        NEWID(),
        N'payments.allowFreeConsultantSessions',
        N'true',
        N'Boolean',
        N'Allow consultants to offer free sessions (session fee = 0).',
        N'السماح للمستشارين بتقديم جلسات مجانية (رسوم الجلسة = 0).',
        N'Payments',
        SYSDATETIMEOFFSET()
    );
END;

-- ─── Verification ────────────────────────────────────────────────────────────
-- Uncomment to confirm the 3 rows are present after running:
-- SELECT [Key], [Value], [Category]
--   FROM [PlatformSettings]
--  WHERE [Key] IN (
--      N'payments.enabled',
--      N'payments.allowFreeScholarships',
--      N'payments.allowFreeConsultantSessions')
--  ORDER BY [Key];
