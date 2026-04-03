# PB-003 — Tasks

**Owner**: @norra-mmhamed  •  **Est**: 40 pts  •  **Iteration**: 2

## Backend
- [ ] T-001 — Finalize `Scholarship` entity + configurations; confirm full-text index (FR-034 .. FR-036)
- [ ] T-002 — Implement `SearchScholarshipsQuery` with filters + pagination + sort (FR-035, FR-036)
- [ ] T-003 — Performance test: seed 100K, verify 500 ms p95 (see `docs/TESTING.md`)
- [ ] T-004 — Implement `CreateScholarshipCommand` with validation (deadline lead time FR-043)
- [ ] T-005 — Implement `UpdateScholarshipCommand` — block schema changes if active applications exist
- [ ] T-006 — Implement `ArchiveScholarshipCommand`
- [ ] T-007 — Implement `BookmarkToggleCommand` (FR-045)
- [ ] T-008 — Implement `CreateExternalListingCommand` (Admin-only) (FR-039, FR-053)
- [ ] T-009 — Implement `FeatureScholarshipCommand` with 12-item cap (FR-030)
- [ ] T-010 — `ScholarshipsController` wiring + `[Authorize]` per endpoint
- [ ] T-011 — Unit + integration tests (>=70% coverage)

## Frontend
- [ ] T-012 — Student `Scholarships.tsx` list page with filter drawer + bookmark toggle
- [ ] T-013 — Student `ScholarshipDetail.tsx` with apply CTA (branches In-App vs External)
- [ ] T-014 — Student `Bookmarks.tsx` — bookmarked scholarships grid
- [ ] T-015 — Company `Scholarships.tsx` — owned listings list with CRUD actions
- [ ] T-016 — Company `ScholarshipEditor.tsx` with form-field builder UI (react-hook-form + array fields)
- [ ] T-017 — Admin `ExternalListingEditor.tsx`
- [ ] T-018 — Admin `FeaturedScholarships.tsx` with drag-to-reorder
- [ ] T-019 — EligibilitySnapshot component (hooks into PB-008 if available)
- [ ] T-020 — Arabic copy review `scholarships` namespace

## QA
- [ ] T-021 — E2E: search, filter, bookmark, view detail
- [ ] T-022 — E2E: Company creates in-app listing → student applies → flow handed to PB-004

## Done criteria
500ms search p95 met, CRUD flows green, EN+AR parity.
