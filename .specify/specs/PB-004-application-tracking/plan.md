# PB-004 — Implementation Plan

## Architecture touchpoints

### Domain
- **Entities**: `ApplicationTracker`, `ApplicationTrackerChild` (status history rows, attached documents, notes)
- **Enums**: `ApplicationStatus { Draft, Pending, UnderReview, Accepted, Rejected, Withdrawn, Intending, Applied, WaitingResult }`, `ApplicationMode { InApp, External }`
- **Events**: `ApplicationSubmittedEvent`, `ApplicationStatusChangedEvent`, `ApplicationWithdrawnEvent`

### Application (`server/src/ScholarPath.Application/Applications/`)
- **Commands**: `StartApplicationCommand`, `SaveDraftCommand`, `SubmitApplicationCommand`, `WithdrawApplicationCommand`, `UpdateExternalStatusCommand`, `AddExternalNoteCommand`, `ReviewApplicationCommand` (Company), `ChangeApplicationStatusCommand` (Company)
- **Queries**: `GetMyApplicationsQuery` (kanban), `GetApplicationDetailQuery`, `GetApplicationsForListingQuery` (Company)
- **Services**: `DuplicateApplicationGuard` (checks unique-active rule before persisting)

### Infrastructure
- `Persistence/Configurations/ApplicationTrackerConfiguration.cs` — unique filtered index, soft-delete
- `Persistence/Migrations/` — manual SQL for filtered unique index (EF Core 10 supports this natively in `HasIndex(...).HasFilter(...)`)

### API (`server/src/ScholarPath.API/Controllers/ApplicationsController.cs`)
- `GET  /api/applications/me` (kanban)
- `GET  /api/applications/{id}`
- `POST /api/applications/start/{scholarshipId}`
- `PATCH /api/applications/{id}` (save draft)
- `POST /api/applications/{id}/submit`
- `POST /api/applications/{id}/withdraw`
- `POST /api/applications/external-intent/{scholarshipId}` (creates self-tracked + returns URL)
- `PATCH /api/applications/{id}/external-status`
- `POST /api/applications/{id}/notes`
- `GET  /api/applications/for-listing/{scholarshipId}` (Company)
- `PATCH /api/applications/{id}/status` (Company)

### Frontend
- **Pages (Student)**: `pages/student/Applications.tsx` (kanban), `ApplicationDetail.tsx` (drawer), `ApplicationForm.tsx` (renders Company's custom form)
- **Pages (Company)**: `pages/company/ApplicationsReview.tsx`, `ApplicationReviewDetail.tsx`
- **Components**: `KanbanBoard`, `ApplicationCard`, `StatusTimeline`, `DocumentAttacher`, `FormFieldRenderer`
- **Real-time**: Subscribe to `NotificationHub` `ApplicationStatusChanged` → update kanban in place

### Tests
- Unit: `DuplicateApplicationGuard` (all transitions)
- Integration: start → draft → submit → company reviews → status change → student sees update
- E2E: withdraw → reapply flow

## Dependencies

- PB-001 (auth), PB-002 (profile docs), PB-003 (scholarships), PB-010 (notifications)

## Risks

1. Form schema evolution — if Company updates listing schema mid-apply, students' drafts may mismatch → surface a warning + allow re-sync
2. Document vault vs new upload — two paths for same file; dedupe by SHA256 hash
3. Race on single-active rule — rely on DB unique filtered index for authoritative constraint, not app-level check alone
