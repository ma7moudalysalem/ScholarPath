# PB-003 — Implementation Plan

## Architecture touchpoints

### Domain
- **Entities**: `Scholarship`, `ScholarshipChild` (for form fields + required docs as configurable child rows), `Category`, `SavedScholarship`
- **Enums**: `ScholarshipStatus { Draft, Open, Archived, UnderReview, Closed }`, `FundingType { FullyFunded, PartiallyFunded, TuitionOnly, StipendOnly }`, `AcademicLevel { HighSchool, Undergrad, Masters, PhD, PostDoc }`, `ListingMode { InApp, ExternalUrl }`
- **Events**: `ScholarshipPublishedEvent`, `ScholarshipArchivedEvent`

### Application (`server/src/ScholarPath.Application/Scholarships/`)
- **Commands**: `CreateScholarshipCommand`, `UpdateScholarshipCommand`, `ArchiveScholarshipCommand`, `BookmarkToggleCommand`, `FeatureScholarshipCommand` (Admin), `CreateExternalListingCommand` (Admin)
- **Queries**: `SearchScholarshipsQuery` (with filters), `GetScholarshipDetailQuery`, `GetMyBookmarksQuery`, `GetFeaturedScholarshipsQuery`, `GetMyListingsQuery` (Company)

### Infrastructure
- `Persistence/Configurations/ScholarshipConfiguration.cs` — full-text index on `TitleEn`, `TitleAr`, `DescriptionEn`, `DescriptionAr`; composite index on `(Status, Deadline, Country)`
- Optional: SQL Server Full-Text catalog setup in migration

### API (`server/src/ScholarPath.API/Controllers/ScholarshipsController.cs`)
- `POST /api/scholarships/search`
- `GET  /api/scholarships/{id}`
- `POST /api/scholarships` (Company/Admin)
- `PATCH /api/scholarships/{id}`
- `POST /api/scholarships/{id}/archive`
- `POST /api/scholarships/{id}/feature` (Admin)
- `POST /api/scholarships/{id}/bookmark`
- `GET  /api/scholarships/me/bookmarks`
- `GET  /api/scholarships/me/listings` (Company)
- `GET  /api/scholarships/featured`

### Frontend
- **Pages (Student)**: `pages/student/Scholarships.tsx` (list + filters), `ScholarshipDetail.tsx`, `Bookmarks.tsx`
- **Pages (Company)**: `pages/company/Scholarships.tsx` (list), `ScholarshipEditor.tsx` (create/edit with form-builder)
- **Pages (Admin)**: `pages/admin/Scholarships.tsx`, `ExternalListingEditor.tsx`, `FeaturedScholarships.tsx`
- **Components**: `components/scholarships/{FilterDrawer, ScholarshipCard, FormFieldBuilder, EligibilitySnapshot}.tsx`

### i18n
`scholarships` namespace

### Tests
- Integration: search with various filter combos + pagination, bookmark toggle, CRUD listing
- Performance: seed 100K scholarships in test DB, verify 500ms target

## Dependencies

- PB-001 (auth)
- PB-002 (profile to drive AI match score)
- PB-011 (admin for external URL + feature listing)
- PB-008 (AI match scoring — optional, degrades gracefully)
