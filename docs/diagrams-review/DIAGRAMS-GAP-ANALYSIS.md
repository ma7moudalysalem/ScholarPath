# ScholarPath Diagrams — Gap Analysis vs Code

**Date:** 2026-05-24
**Scope:** Verifying four diagram reports against the live code
**Sources of truth (code):**
- `server/src/ScholarPath.Domain/Entities/*.cs` (19 files, 48+ persistent classes)
- `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs`
- `server/src/ScholarPath.Infrastructure/Persistence/Configurations/*.cs`
- `server/src/ScholarPath.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` (114 `modelBuilder.Entity(...)` blocks)
- `server/src/ScholarPath.Application/Common/Interfaces/*.cs` (ports)
- `server/src/ScholarPath.Infrastructure/Services/*.cs` (adapters)
- `server/src/ScholarPath.Infrastructure/DependencyInjection.cs`

**Documents reviewed:**
1. `9016f119-01eerdreport.docx` — Enhanced Entity-Relationship Diagram (EERD)
2. `35e41cc8-02relationalmappingreport.docx` — Relational Mapping
3. `c4ddeaea-03classdiagramreport_1_1.docx` — Class Diagrams (UML)
4. `476d50a2-04componentdiagramreport.docx` — Component Diagram (UML)

**Severity legend:** `[CRITICAL]` = factually wrong, will mislead a grader; `[MAJOR]` = missing significant element; `[MINOR]` = labelling / scoping issue; `[OK]` = matches code.

---

## Headline summary

| Report | Verdict | Critical | Major | Minor |
|---|---|---|---|---|
| 01 — EERD | Largely accurate; 6 entities missing from sub-area diagrams | 1 | 5 | 3 |
| 02 — Relational Mapping | The most accurate of the four; 1 table missing, several columns under-listed | 0 | 3 | 4 |
| 03 — Class Diagram | Inheritance claim for `ApplicationUser` is wrong; ports/adapters list incomplete | 2 | 4 | 3 |
| 04 — Component Diagram | Several real adapters & ports missing; alt providers (OpenAI-direct) not shown | 0 | 5 | 4 |

Codebase reality:
- **Persistent entity classes in `Domain/Entities`:** 48 (`grep "class .* : (BaseEntity|AuditableEntity)"` + `ApplicationUser` + `ApplicationRole`).
- **EF model entities in snapshot:** 60 (the 48 domain classes + 5 standard ASP.NET Identity child tables + `Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>` etc., plus 2 owned/configured types as `modelBuilder.Entity` blocks for related shadow types).
- **Application port interfaces:** 27 distinct `I*` interfaces in `Application/Common/Interfaces` + 2 in `Domain/Interfaces` (`ICurrentUserService`, `IDateTimeService`).
- **Infrastructure adapters:** 40+ classes in `Infrastructure/Services` (real + stub + key-vault providers).

---

## Report 01 — EERD (`01eerdreport.docx`)

### What the report claims
- "Roughly 55 persistent entities" reverse-engineered from the EF model snapshot.
- Six sub-area diagrams + a consolidated diagram.
- The "Enhanced" part is the USER over­lapping specialization (Student/Company/Consultant/Admin), all materialised on a single `Users` table + `UserRoles` join + a single `UserProfiles` table with NULL-able role-specific columns.

### What's correct
- `[OK]` USER specialization is single-table — confirmed by `Domain/Entities/Identity.cs:11` (`ApplicationUser : IdentityUser<Guid>`) and `UserProfile` carrying all role-specific fields (`Gpa`, `OrganizationLegalName`, `SessionFeeUsd`, etc.).
- `[OK]` UserRoles is the `IdentityUserRole<Guid>` join table (renamed via `b.ToTable("UserRoles")` in `ApplicationDbContext.cs:151`).
- `[OK]` "Loose reference" convention is real — confirmed at `ApplicationDbContext.cs:170-180`: every cascade FK pointing at `ApplicationUser` is rewritten to `Restrict` to avoid SQL Server error 1785 (multiple-cascade-paths), and high-volume tables (`Payment`, `Notification`, `AuditLog`, `ForumVote`, `ChatMessage`, …) deliberately have no `b.HasOne(...).HasForeignKey(...)` for their `UserId` column.
- `[OK]` The relationships drawn (1:1 USER↔USER_PROFILE, 1:N USER→REFRESH_TOKEN cascade, M:N USER↔ROLE, 1:1 BOOKING↔CONSULTANT_REVIEW etc.) all match the EF configuration.
- `[OK]` Cross-area "paid review" dashed link matches `CompanyReviewRequest` linking Student + Company + Scholarship + Payment.

### Gaps and inaccuracies

| # | Sev | Finding |
|---|---|---|
| E1 | `[CRITICAL]` | **Entity count "~55" is under-stated.** The `ApplicationDbContextModelSnapshot.cs` has 60 `modelBuilder.Entity` blocks (55 ScholarPath domain + 5 standard Identity tables: `IdentityUserClaim`, `IdentityUserLogin`, `IdentityUserToken`, `IdentityRoleClaim`, `IdentityUserRole`). A more honest count is "**48 first-class domain entities** + 7 Identity tables (`ApplicationUser`, `ApplicationRole`, plus 5 standard junctions) = 55 EF-managed entities" or, counting all persistent rows, **60**. Either is defensible; "55" without qualification is wrong. |
| E2 | `[MAJOR]` | **Notation legend contradicts itself.** §1 says "all entities here are *strong*" but §4 still labels `EDUCATION_ENTRY` and `UPGRADE_REQUEST_FILE` weak entities with HAS_EDU / HAS_FILE diamonds. This is internally inconsistent — pick one (these are physically strong rows with their own surrogate `Id`, total participation only). |
| E3 | `[MAJOR]` | **Six entities are completely missing from the per-area diagrams** (although some appear in the textual descriptions and §3 context box). Missing entities: <br>• `ExpertiseTag` (lookup table, §4) — exists at `Identity.cs:201`. <br>• `UpgradeRequestLink` (§4) — exists at `UpgradeRequests.cs:35`. <br>• `SavedScholarship` (§5) — exists at `Scholarships.cs:84`. <br>• `StripeWebhookEvent`, `ProfitShareConfig`, `FinancialConfigRule` (§6) — exist at `Payments.cs:61/73/90`. <br>• `ForumPostAttachment` (§7) — exists at `Community.cs:82`. <br>• `UserBlock` (§7) — exists at `Chat.cs:32`. <br>• `SuccessStory` (§9) — exists at `CrossCutting.cs:36`. <br>• `CompanyReviewPayment` (§5) — exists at `Ratings.cs:24` (legacy). |
| E4 | `[MAJOR]` | `[CompanyReviewPayment]` is **a real EF entity in the live model** (`ApplicationDbContext.cs:62`, `Ratings.cs:24`, has its own config and unique indexes on `StripePaymentIntentId` and `IdempotencyKey`). It is functionally legacy/duplicated by `CompanyReviewRequest` + `Payment`, but it's still in the schema. Not mentioned anywhere in the EERD. |
| E5 | `[MAJOR]` | `[ApplicationTrackerChild]` is in §5 (as `APPLICATION_CHILD`) but `[ScholarshipChild]` (the EAV row for scholarship requirements/benefits) is shown only as a sibling in §5 — the relationship `SCHOLARSHIP 1:N SCHOLARSHIP_CHILD HAS_DETAIL` is correct, but the text in §5 says the link is "loose" (`ScholarshipId(soft)`). Looking at code (`Scholarships.cs:73-82`): there is **no `[ForeignKey]` attribute, no Fluent API configuration linking `ScholarshipChild.ScholarshipId → Scholarship.Id`** in `EntityConfigurations.cs:287-298`. So the EERD's "loose" claim is consistent with code, but the diagram in §5 still draws a solid diamond — a dashed one (per the report's own §1 convention) would be more accurate. |
| E6 | `[MINOR]` | §4 "USER 1:N LOGIN_ATTEMPT (loose reference)" is correct (`LoginAttempt.UserId` is a `Guid?` with no FK config — confirmed at `Identity.cs:74` and `EntityConfigurations.cs:161-171`). Good. |
| E7 | `[MINOR]` | §9 says "AUDIT_LOG uses a polymorphic (TargetType, TargetId) reference". Confirmed at `CrossCutting.cs:10-11`. The diagram correctly shows `Action` and `TargetType` but does not annotate `(polymorphic)` on the diagram itself — only in the text. Minor labelling polish. |
| E8 | `[MINOR]` | The consolidated diagram (Fig. 9) is unreadably dense in the source `.docx` (text bleeds across rows). This is a layout / rendering issue. |

---

## Report 02 — Relational Mapping (`02relationalmappingreport.docx`)

### What the report claims
- Applies Elmasri & Navathe ER→Relational mapping rules (steps 1-7).
- Lists every relation with its primary key, foreign keys, and delete rule.
- Documents deliberate denormalisations (JSON columns for multivalued attributes).

### What's correct
- `[OK]` Every relation name and primary key matches the EF entity / table name.
- `[OK]` Filtered unique indexes are accurate:
  - `UX_Applications_Student_Scholarship_Active` (FR-057) — `EntityConfigurations.cs:340-349`.
  - `UX_CompanyReviewRequests_Student_Scholarship_Active` — `EntityConfigurations.cs:431-440`.
  - `UX_Bookings_Consultant_Slot_Active` — `EntityConfigurations.cs:507-510`.
  - `UX_ProfitShareConfig_ActivePerType` — `EntityConfigurations.cs:587-590`.
  - `UX_FinancialConfigRule_ActivePerType` — `EntityConfigurations.cs:608-611`.
- `[OK]` Delete-rule columns (Cascade/Restrict/SetNull) are right for every relation I spot-checked.
- `[OK]` Encrypted columns: `UserProfile.Biography` and `ApplicationTracker.PersonalNotes` are marked `(enc)`. Confirmed at `EntityConfigurations.cs:50-57`.
- `[OK]` Money-in-cents convention (`PAYMENTS.AmountCents`, `PAYOUTS.AmountCents`) matches `Payments.cs:12-16`.
- `[OK]` "Loose reference" columns (no FK constraint) are correctly listed.
- `[OK]` `USERS` correctly has the ASP.NET Identity columns (`NormalizedEmail`, `PasswordHash`, `SecurityStamp`, `LockoutEnd`, `AccessFailedCount`).

### Gaps and inaccuracies

| # | Sev | Finding |
|---|---|---|
| R1 | `[MAJOR]` | **`COMPANY_REVIEW_PAYMENTS` table is missing.** This entity exists in `Ratings.cs:24-37` and is registered in `ApplicationDbContext.cs:62` as `DbSet<CompanyReviewPayment>`. The configuration block at `EntityConfigurations.cs:394-410` defines unique indexes on `StripePaymentIntentId` and `IdempotencyKey`. The relational mapping report skips it entirely — this is the same gap as EERD §5. Either delete the entity from the code (it looks legacy, superseded by `CompanyReviewRequest` + `Payment`) or document it. |
| R2 | `[MAJOR]` | **`USERS` audit columns are over-stated.** The `(audit)` shorthand expands to `(CreatedAt, CreatedByUserId, UpdatedAt, UpdatedByUserId, RowVersion)`. But `ApplicationUser` (`Identity.cs:11-48`) only declares `CreatedAt`, `UpdatedAt`, `RowVersion` — it does **not** inherit `AuditableEntity`. There are no `CreatedByUserId` / `UpdatedByUserId` columns on the `Users` table. The report should call USERS out as carrying a stripped-down audit triple, not the full quintet. |
| R3 | `[MAJOR]` | **`USER_PROFILES` column list is incomplete.** The report lists ~15 columns; the actual entity has **~50 columns** (`Identity.cs:82-185`). Missing from the report (high-signal columns the SRS depends on): `BiographyAr`, `LinkedInUrl`, `WebsiteUrl`, `Timezone`, `CurrentInstitution`, `PreferredFieldsJson`, `OrganizationRegistrationNumber`, `OrganizationWebsite`, `OrganizationVerifiedAt`, `OrganizationEmail`, `OrganizationCountry`, `OrganizationTaxNumber`, `CompanyType`, `CompanyDescription`, `ContactPersonFullName`, `ContactPersonPosition`, `ContactPhoneNumber`, `TaxNotApplicableReason`, `IsLegallyRegistered`, `LegalRegistrationNotApplicableReason`, `LastOnboardingRejectionReason`, `LastOnboardingRejectedAt`, `ProfessionalTitle`, `HighestDegree`, `FieldOfExpertise`, `YearsOfExperience`, `PortfolioUrl`, `ConsultantVerifiedAt`, `BookingIntakeSuspendedAt`, `StripeConnectOnboardedAt`. Add a note like "(extended onboarding fields elided — see `Identity.cs`)" or list them. |
| R4 | `[MAJOR]` | **`BOOKINGS` is missing four fields** that the live `ConsultantBooking` (`Bookings.cs:30-92`) carries: `RecordingStartedAt`, `NoShowMarkedAt`, `StudentJoinedAt`, `ConsultantJoinedAt` (FR-217 meeting-attendance tracking). |
| R5 | `[MINOR]` | **`COMPANY_REVIEW_REQUESTS.Status` enum is under-stated.** The filter expression in the report only lists the four active statuses (Draft/Submitted/Pending/UnderReview) — correct for the filtered unique index. But the textual relation also lists `CancelReason, RejectReason` and lifecycle timestamps; the actual `CompanyReviewRequestStatus` enum (`Enums.cs:118-131`) has **eleven** values including `Failed`, `CancelledByStudent`, `RejectedByCompany`. The report says the status enum's terminal set is "Cancelled/Rejected/Expired/Completed/Closed" but misses `Failed`, `CancelledByStudent`, `RejectedByCompany`. |
| R6 | `[MINOR]` | **`KNOWLEDGE_DOCUMENTS` is missing one column.** The report lists `(Id, SourceType, SourceId(soft, polymorphic), SourceKey, TitleEn, TitleAr, ContentEn, ContentAr, ContentHash, Embedding, EmbeddingDimensions, EmbeddingModel, IndexedAt, MetadataJson, (audit))`. Actual code (`Knowledge.cs:16-51`) confirms all of those, **plus** a computed `IsEmbedded` getter (ignored in EF mapping at `EntityConfigurations.cs:819`). Not a column — fine to omit, but worth noting. |
| R7 | `[MINOR]` | **Index `IX_UserProfiles_CompanyLowRatingFlagged` is undocumented.** A filtered index `WHERE [CompanyLowRatingFlaggedAt] IS NOT NULL` exists at `EntityConfigurations.cs:130-132`. The report mentions the column but not the index. |
| R8 | `[MINOR]` | The "Exporting the authoritative SQL" command at the end has typos that look like OCR artefacts (`nigrations` instead of `migrations`, `--proj ect` instead of `--project`). Same OCR-style artefacts appear throughout the report (e.g. `M:N` rendered as `h:N`, `0` substituted for capital `O` — `F0RUM`, `B00KING`, etc.). This is a rendering issue in the source DOCX, not a content error. |

---

## Report 03 — Class Diagrams (`03classdiagramreport_1_1.docx`)

### What the report claims
- Six per-area UML class diagrams + a base-types diagram + a "ports & adapters" architecture diagram.
- All persistent classes inherit `AuditableEntity` (which itself inherits `BaseEntity`).
- Lists ports and their production / dev-stub adapters.

### What's correct
- `[OK]` `BaseEntity` / `AuditableEntity` / `ISoftDeletable` definitions match `Common/BaseEntity.cs`.
- `[OK]` Domain-event plumbing on `BaseEntity` matches the code (`RaiseDomainEvent`, `ClearDomainEvents`).
- `[OK]` `UserProfile` 1↔0..1 `ApplicationUser` matches the unique index `IX_UserProfiles_UserId` (`EntityConfigurations.cs:137`).
- `[OK]` `EducationEntry` composition into `UserProfile` matches `Identity.cs:184`.
- `[OK]` `ApplicationTracker.IsActive` / `IsReadOnly` are real computed properties (`Applications.cs:56-69`) and EF ignores them (`EntityConfigurations.cs:330`).
- `[OK]` `Scholarship → ApplicationTracker` 1..* `← CompanyReview` 0..1 matches the schema.
- `[OK]` `ConsultantBooking` ↔ `ConsultantAvailability`, `Payment`, `ConsultantReview`, `SessionRecording` cardinalities match.
- `[OK]` `ForumPost` self-reference for threaded replies matches `Community.cs:22, 46-47`.
- `[OK]` `AiInteraction → KnowledgeDocument` "citesRAG" 0..1 — the relationship is not formally on `AiInteraction` (no `KnowledgeDocumentId` column), but the RAG retriever does query `KnowledgeDocuments` per interaction (`KnowledgeRetriever.cs`). This is a conceptual association — defensible.

### Gaps and inaccuracies

| # | Sev | Finding |
|---|---|---|
| C1 | `[CRITICAL]` | **Fig. 2 header says "Identity & Profile (all classes : `AuditableEntity`)" — this is wrong for `ApplicationUser`.** `ApplicationUser` (`Identity.cs:11`) inherits **`IdentityUser<Guid>`, `ISoftDeletable`** — not `AuditableEntity`. It manually carries `CreatedAt`, `UpdatedAt`, `RowVersion` (no `CreatedByUserId` / `UpdatedByUserId`), and re-implements its own `DomainEvents` collection on lines 44-47. The class diagram should either: (a) draw `ApplicationUser : IdentityUser<Guid>, ISoftDeletable` with stereotype `«identity»`, or (b) qualify the header to "all classes *except `ApplicationUser`* inherit AuditableEntity". |
| C2 | `[CRITICAL]` | **Fig. 6 "Ports & Adapters" table is missing several real ports.** The actual Application interfaces inventory is 27 (in `Application/Common/Interfaces/`) plus 2 in `Domain/Interfaces/`. The diagram and the table on p. 6 show 10 ports. **Missing ports/adapters that exist in code:** <br>• `IBlobStorageService` → `FileStorageService` (file storage for the document vault FR-216) — the report mistakenly labels the slot `(file storage)`. <br>• `IPasswordHasher` → `IdentityPasswordHasher` <br>• `ITokenService` → `TokenService` <br>• `ISsoService` → `SsoService` / `StubSsoService` <br>• `IEmailVerificationService` → `EmailVerificationService` <br>• `IEmailChangeService` → `EmailChangeService` <br>• `IAuditService` → `AuditService` <br>• `IUserAdministration` → `UserAdministration` <br>• `IAdminReadService` → `AdminReadService` <br>• `IConsultantReadService` → `ConsultantReadService` <br>• `IChatContactReadService` → `ChatContactReadService` <br>• `IChatRealtimeNotifier`, `ICommunityRealtimeNotifier`, `IChatPresenceQuery`, `IPresenceTracker` (SignalR adapters) <br>• `IKnowledgeRetriever`, `IKnowledgeBaseIndexer`, `IDatasetProvider` (RAG plumbing) <br>• `IFieldEncryptionKeyProvider`, `IJwtKeyProvider` (key-rotation adapters — Key Vault vs local) <br>• `ICurrentUserService`, `IDateTimeService` (Domain-layer interfaces) <br>• Job interfaces: `IBookingReminderJob`, `IMeetingNoShowSweepJob`, `IScholarshipAutoCloseJob`, `ISessionExpiryJob`, `ICompletionJob` |
| C3 | `[MAJOR]` | **OpenAI-direct adapter is missing from Fig. 6.** Code has three AI adapters (`DependencyInjection.cs:269-290`): `AzureOpenAiService` (Azure), `OpenAiService` (OpenAI direct), `LocalAiService` (offline). The diagram shows only Azure + Local. Same for embeddings: `OpenAiEmbeddingService` is missing. |
| C4 | `[MAJOR]` | **Most stub adapters are mis-labelled or missing.** The code has these dev stubs (selected at startup via configuration in `DependencyInjection.cs`): `StubEmailService`, `StubSsoService`, `StubStripeService`, `StubPowerBiService`, `StubEventPublisher`, `NoOpFileScanService` (note: not `Stub*FileScanService`). The diagram shows `«dev stub»` only on `StubMeetingService`, `LocalAiService`, `LocalEmbeddingService`. Add the rest, or note "stubs omitted for legibility". |
| C5 | `[MAJOR]` | **Class diagram does not show entities for several major schema areas** that are documented in the EERD: <br>• `RefreshToken`, `PasswordResetToken`, `LoginAttempt` (Identity & Profile, Fig. 2) <br>• `ScholarshipChild`, `SavedScholarship`, `Category` is shown but `ApplicationTrackerChild` is not (Fig. 3) <br>• `Payout`, `StripeWebhookEvent`, `ProfitShareConfig`, `FinancialConfigRule` (Fig. 4 only shows `Payout` as a single box without attributes) <br>• `ForumCategory`, `ForumTag`, `ForumPostTag`, `ForumVote`, `ForumFlag`, `ForumBookmark`, `ForumPostAttachment`, `UserBlock` (Fig. 5) <br>• `Notification` is present but `NotificationPreference` is not <br>• `Resource` is present but `ResourceChild`, `ResourceBookmark`, `ResourceProgress`, `ResourceProgressChild` are not <br>• `KnowledgeDocument` is shown, but `RecommendationClickEvent`, `AiRedactionAuditSample` are not <br>• `AuditLog`, `UserDataRequest`, `SuccessStory`, `UserRiskFlag`, `PlatformSetting` are absent from the entire report |
| C6 | `[MAJOR]` | **`UpgradeRequest` Fig. 2 missing children.** `UpgradeRequest` is shown but its composed `UpgradeRequestFile` and `UpgradeRequestLink` (`UpgradeRequests.cs:25, 35`) are not drawn — this is a "weak entity composes" relationship documented elsewhere. |
| C7 | `[MINOR]` | **`ApplicationUser` operations:** the diagram lists `+FullName() : string`. In code (`Identity.cs:41`) this is a property, not a method: `public string FullName => $"{FirstName} {LastName}".Trim();`. Labelling polish — a derived property in UML is conventionally underlined with a `/`. |
| C8 | `[MINOR]` | **`CompanyReviewRequest` rating** — Fig. 3 says `CompanyReviewRequest` has `-Status, -ReviewFeeUsdSnapshot`. Both correct. But it implies the `CompanyReview` is the rating side. In code (`CompanyReviewRequest.cs:20-75`) the request is the engagement, the review is the rating; the request itself has no rating. The labels are consistent with the cardinalities (`receives 1 CompanyReviewRequest` from Scholarship side). |
| C9 | `[MINOR]` | **OCR artefacts.** Several class names render as e.g. `CompanyReviev`, `ApplicationUser`, `Identity & Profile` rendered with stray characters. Same source issue as the relational-mapping doc — `DOCX` rendering, not code. |

---

## Report 04 — Component Diagram (`04componentdiagramreport.docx`)

### What the report claims
- Sommerville-style component diagram of the runtime.
- Clean Architecture monolith: API → Application → Domain; Infrastructure → Application/Domain.
- React 19 SPA client on Azure Static Web Apps; Azure App Service for the API.
- External integrations: Stripe, Azure OpenAI, Azure Communication Services, Azure Blob Storage, Azure SQL, Azure Key Vault, Azure Event Hub → Power BI, SMTP, ClamAV.

### What's correct
- `[OK]` Clean-Architecture dependency direction (`API → Application → Domain`, `Infrastructure → Application/Domain`) matches the project references in `*.csproj` and `ScholarPath.slnx`.
- `[OK]` SignalR Hubs exist: `ChatHub`, `NotificationHub`, `CommunityHub` (`Hubs/Hubs.cs:14, 60, 77`) — mounted at `Program.cs:350-352`.
- `[OK]` Stripe-webhook receiver: `WebhooksController.cs` is an API controller (verified in `Controllers/`).
- `[OK]` Azure App Service deployment target matches `Dockerfile.api` and `docker-compose.yml`.
- `[OK]` "Reverse-ETL (UserRiskFlags)" arrow from Power BI to Azure SQL matches the `UserRiskFlag` entity (`CrossCutting.cs:71-91`) and the comment on lines 58-70.
- `[OK]` Production / dev adapter swap via `DependencyInjection.cs` is real and configured per `Ai__Provider`, `Storage:Provider`, `Email:Provider`, `Stripe:SecretKey`, `Acs:ConnectionString`, `PowerBi:WorkspaceId`, `EventHubs:ConnectionString`, `FileScanning:Enabled`.

### Gaps and inaccuracies

| # | Sev | Finding |
|---|---|---|
| D1 | `[MAJOR]` | **Hangfire Jobs is shown as a first-class component, but Hangfire is feature-flagged off by default.** `Program.cs:196-221` shows Hangfire only registers when `Hangfire:Enabled` is `true` in config; otherwise jobs don't run. The diagram should annotate `«optional»` or "Hangfire (when `Hangfire:Enabled=true`)" to reflect this. Job classes (`Infrastructure/Jobs/*.cs`: `BookingReminderJob`, `ScholarshipAutoCloseJob`, `MeetingNoShowSweepJob`, `SessionExpiryJob`, `CompletionJob`, `StripePayoutJob`, `DataExportJob`, `DataDeleteJob`, `RedactionAuditSamplingJob`, `CompanyReviewTimeoutRefundJob`, `IntegrityCheckJob`, `NotificationDispatcherJob`, `DeadlineReminderJob`) exist regardless, but only run inside the Hangfire server. |
| D2 | `[MAJOR]` | **Three ports / adapters are omitted from the integrations table:** <br>• `IPasswordHasher → IdentityPasswordHasher` (no external system; cited as completeness gap). <br>• `ITokenService → TokenService` (signs JWTs via `IJwtKeyProvider`). <br>• `ISsoService → SsoService` (external: Google + Microsoft OAuth) — **a substantial external integration that's entirely absent from the diagram**. `DependencyInjection.cs:104-141` shows the real `SsoService` is wired by default and goes out to Google + Microsoft. |
| D3 | `[MAJOR]` | **`IFieldEncryptionService → AesGcmFieldEncryptionService → Azure Key Vault` is mis-attributed.** The actual chain is `IFieldEncryptionService → AesGcmFieldEncryptionService → IFieldEncryptionKeyProvider → (KeyVaultFieldEncryptionKeyProvider OR LocalFieldEncryptionKeyProvider)`. The Key Vault dependency is *via* a separate port (`IFieldEncryptionKeyProvider`), not directly. Same pattern for JWT signing: `IJwtKeyProvider → KeyVaultJwtKeyProvider`. The diagram lumps these together. |
| D4 | `[MAJOR]` | **OpenAI-direct provider is missing.** The DI selects between **three** AI providers (`DependencyInjection.cs:237-290`): `AzureOpenAi`, `OpenAi`, or `Stub`. The component diagram and the table show only the Azure provider. Same for embeddings (`AzureOpenAiEmbeddingService` and `OpenAiEmbeddingService` exist, only the Azure one is shown). |
| D5 | `[MAJOR]` | **The `Recommend()` operation is shown on the wrong port.** Fig. 6 shows `IAiService` with `Recommend()`, `CheckEligibility()`, `Ask()`. Confirmed in `IAiService.cs` — fine. **But** the report's narrative implies recommendations come from Azure OpenAI; in the live code, the recommendation pipeline is driven by `LocalAiService` (deterministic offline scoring on metadata + RAG retrieval) and the chat / Q&A is what may delegate to Azure OpenAI. The diagram should note this so a reader doesn't assume the OpenAI bill scales with recommendation volume. |
| D6 | `[MINOR]` | **"SMTP mail" arrow is one-directional.** Code shows two adapters: `MailKitEmailService` (real SMTP) or `StubEmailService` (dev log-only). The diagram correctly shows SMTP — labelling could be tightened to "SMTP / dev stub" or annotated with the config switch (`Email:Provider`). |
| D7 | `[MINOR]` | **Power BI integration arrow direction is incomplete.** The diagram shows "Power BI ← reverse-ETL → UserRiskFlags". In code (`PowerBiService.cs`), the API also reads from Power BI (for embed tokens / report URLs) — so the arrow should be **bidirectional**: API → Power BI (embed tokens, `IPowerBiService`) and Power BI → SQL (reverse-ETL upsert into `UserRiskFlags`). |
| D8 | `[MINOR]` | **Identity / ASP.NET Identity is implicit but not drawn.** The whole authentication stack (`AddIdentity<ApplicationUser, ApplicationRole>`, `AddJwtBearer`) is a first-class infrastructure component. Worth a dedicated "ASP.NET Identity" box inside the API node. |
| D9 | `[MINOR]` | **No persistent caching shown.** The diagram doesn't mention `IMemoryCache` (`DependencyInjection.cs:333`), which is in use. A Redis swap is mentioned in code comments but not configured. Either add `MemoryCache` to the API component or note the planned Redis swap. |

---

## Concrete fixes (in priority order)

If you only have time to fix the highest-impact items before submission, in this order:

1. **C1 (class diagram):** Re-draw `ApplicationUser` to inherit `IdentityUser<Guid>` and implement `ISoftDeletable` (not `AuditableEntity`). Either change the Fig. 2 header or add a `«identity»` stereotype to the box.
2. **R1 / E4 (relational + EERD):** Either delete `CompanyReviewPayment` from the code (it looks superseded by `CompanyReviewRequest`+`Payment`) **or** add it to both reports as a documented legacy table.
3. **E3 / C5 (EERD + class diagram):** Add the missing entities to the per-area diagrams (`SavedScholarship`, `UserBlock`, `ForumPostAttachment`, `StripeWebhookEvent`, `ProfitShareConfig`, `FinancialConfigRule`, `SuccessStory`, `ResourceChild`, `ResourceBookmark`, `ResourceProgress`, `ResourceProgressChild`, `NotificationPreference`, `RecommendationClickEvent`, `AiRedactionAuditSample`, `AuditLog`, `UserDataRequest`, `UserRiskFlag`, `PlatformSetting`).
4. **R2 (relational):** Fix the `(audit)` shorthand for `USERS` — it only carries `(CreatedAt, UpdatedAt, RowVersion)`, not the full five-column audit set.
5. **R3 / R4 (relational):** Extend `USER_PROFILES` and `BOOKINGS` column lists, or add an "elided" footnote.
6. **D2 / D4 (component):** Add `ISsoService` (Google + Microsoft OAuth — a real external integration) and the OpenAI-direct AI provider to the component diagram.
7. **D3 (component):** Split the field-encryption / JWT key-vault dependency through their key-provider ports (`IFieldEncryptionKeyProvider`, `IJwtKeyProvider`).
8. **C2 (class diagram):** Add at least the high-traffic ports missing from the ports & adapters diagram (`IBlobStorageService`, `ITokenService`, `ISsoService`, `IPasswordHasher`, `IAuditService`, the three realtime notifiers).
9. **E1 (EERD):** Re-state the entity count as "48 first-class domain entities + 7 Identity tables = 55 EF entities (60 if you include the 5 standard Identity child tables)".
10. **D1 (component):** Annotate Hangfire Jobs as `«optional / feature-flagged»`.

---

## Cross-cutting observations (apply to all four reports)

- **OCR-style character corruption in the `.docx` files** (`O` → `0`, `M` → `h`, `F0RUM`, `B00KING`, `nigrations`, `proj ect`). This is consistent across all four reports — likely a font-embedding or PDF→DOCX issue. Re-export from the original LaTeX / authoring tool with a Unicode-safe font.
- **None of the four reports references the `IsReadOnly` filtered query behaviour or the global query filters for soft-delete** (`b.HasQueryFilter(... => !x.IsDeleted)` is on ~15 entities). The class diagram could call these out as a `«invariant»` note on each soft-deletable class — they materially affect what a query returns.
- **The relational mapping report and the EERD are tightly aligned with each other** (good — they were clearly produced as a pair). The class diagram is the most out of step with the schema (substantially fewer entities drawn).

— end of report —
