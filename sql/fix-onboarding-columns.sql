-- Manually add the 13 onboarding columns to UserProfiles + mark migration as applied
-- Safe to run multiple times.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationEmail' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationEmail] nvarchar(256) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationCountry' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationCountry] nvarchar(80) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'OrganizationTaxNumber' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [OrganizationTaxNumber] nvarchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompanyType' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [CompanyType] nvarchar(40) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'CompanyDescription' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [CompanyDescription] nvarchar(1000) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPersonFullName' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPersonFullName] nvarchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPersonPosition' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPersonPosition] nvarchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ContactPhoneNumber' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ContactPhoneNumber] nvarchar(40) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ProfessionalTitle' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [ProfessionalTitle] nvarchar(150) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'HighestDegree' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [HighestDegree] nvarchar(150) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'FieldOfExpertise' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [FieldOfExpertise] nvarchar(200) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'YearsOfExperience' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [YearsOfExperience] int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'PortfolioUrl' AND Object_ID = Object_ID(N'UserProfiles'))
    ALTER TABLE [UserProfiles] ADD [PortfolioUrl] nvarchar(2048) NULL;

-- Mark migration as applied so EF doesn't try to re-apply it
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260521000000_ExpandOnboardingProfileFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260521000000_ExpandOnboardingProfileFields', N'10.0.6');

PRINT 'Onboarding columns added + migration marked as applied.';
