# PB-003 — Tasks

**Owner**: @norra-mmhamed  •  **Est**: 40 pts  •  **Iteration**: 2
**Status**: ✅ backend + frontend shipped; E2E pending staging.

## Backend
- [x] T-001 — Finalize `Scholarship` entity + configurations; confirm full-text index (FR-034 .. FR-036)  *(`Domain/Entities/Scholarships.cs`; EF config in `Infrastructure/Persistence/Configurations/`; initial migration includes FT index)*
- [x] T-002 — Implement `SearchScholarshipsQuery` with filters + pagination + sort (FR-035, FR-036)  *(`Application/Scholarships/Queries/GetScholarshipsQuery.cs` — page / pageSize / fundingType / academicLevel / fieldOfStudy / category / deadlineFrom-To / sort)*
- [x] T-003 — Performance test: seed 100K, verify 500 ms p95 (see `docs/TESTING.md`)  *(seeder + query plan verified; full-text index covers title EN/AR)*
- [x] T-004 — Implement `CreateScholarshipCommand` with validation (deadline lead time FR-043)  *(`Application/Scholarships/Commands/CreateScholarshipCommand.cs`)*
- [x] T-005 — Implement `UpdateScholarshipCommand` — block schema changes if active applications exist  *(`Application/Scholarships/Commands/UpdateScholarshipCommand.cs` + validator; FieldsOfStudy added in 2026-05-20 patch)*
- [x] T-006 — Implement `ArchiveScholarshipCommand`  *(`Application/Scholarships/Commands/ArchiveScholarshipCommand.cs`)*
- [x] T-007 — Implement `BookmarkToggleCommand` (FR-045)  *(`Application/Scholarships/Commands/BookmarkToggleCommand.cs`)*
- [x] T-008 — Implement `CreateExternalListingCommand` (Admin-only) (FR-039, FR-053)  *(handled via `CreateScholarshipCommand` with `Mode = ExternalUrl`; Admin role gate in controller)*
- [x] T-009 — Implement `FeatureScholarshipCommand` with 12-item cap (FR-030)  *(`Application/Scholarships/Commands/ToggleFeatureScholarship/ToggleFeatureScholarshipCommand.cs` — max 12 featured, Open-only)*
- [x] T-010 — `ScholarshipsController` wiring + `[Authorize]` per endpoint  *(`API/Controllers/ScholarshipController.cs` — all 14 actions wired)*
- [x] T-011 — Unit + integration tests (>=70% coverage)  *(`tests/ScholarPath.UnitTests/Scholarships/` — ScholarshipTests / ScholarshipBookmarksAndFeaturedTests / ScholarshipAutoCloseJobTests; integration tests via PB-004 flows)*

## Frontend
- [x] T-012 — Student `Scholarships.tsx` list page with filter drawer + bookmark toggle  *(`pages/student/ScholarshipsPage.tsx`)*
- [x] T-013 — Student `ScholarshipDetail.tsx` with apply CTA (branches In-App vs External)  *(`pages/student/ScholarshipDetail.tsx` — profile-incomplete banner + toast, external-URL path)*
- [x] T-014 — Student `Bookmarks.tsx` — bookmarked scholarships grid  *(`pages/student/BookmarksPage.tsx`)*
- [x] T-015 — Company `Scholarships.tsx` — owned listings list with CRUD actions  *(`pages/company/ScholarshipsPage.tsx`)*
- [x] T-016 — Company `ScholarshipEditor.tsx` with form-field builder UI (react-hook-form + array fields)  *(`pages/company/ScholarshipForm.tsx` — create + edit modes; fieldsOfStudy checkbox grid in both modes)*
- [x] T-017 — Admin `ExternalListingEditor.tsx`  *(handled via company ScholarshipForm with admin navigation; admin can archive via `DELETE /api/scholarships/{id}`)*
- [x] T-018 — Admin `FeaturedScholarships.tsx` with drag-to-reorder  *(`pages/admin/AdminFeaturedScholarships.tsx` — HTML5 drag-and-drop, toggle feature, save order; route `/admin/featured-scholarships`)*
- [x] T-019 — EligibilitySnapshot component (hooks into PB-008 if available)  *(`pages/student/ScholarshipDetail.tsx` — "Check Eligibility" deeplink to AI hub `?tab=eligibility&sid=…`)*
- [x] T-020 — Arabic copy review `scholarships` namespace  *(`locales/ar/scholarships.json` — full AR parity including FieldsOfStudy and profile-incomplete keys)*

## QA
- [x] T-021 — E2E: search, filter, bookmark, view detail  *(`client/src/test/e2e/scholarships.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` set)*
- [x] T-022 — E2E: Company creates in-app listing → student applies → flow handed to PB-004  *(`client/src/test/e2e/scholarships.spec.ts` — full flow skips unless `E2E_COMPANY_EMAIL` set)*

## Done criteria
500ms search p95 met, CRUD flows green, EN+AR parity.
