# PB-004 — In-App Application and External Tracking

**Owner**: @norra-mmhamed • **Priority**: Essential • **Iteration**: 2 • **Est**: 47 pts

## Problem statement

Students apply to scholarships either in-app (Company listings — answer custom form fields, upload docs) or externally (Admin-created listings — redirect to external URL, self-tracked in a personal tracker). Students see a kanban-style timeline of applications (planned, applied, pending, accepted, rejected). Single-active-application-per-scholarship rule prevents double-apply. Withdrawn applications can be reapplied if the scholarship is still open.

## User stories (from SRS)

| ID | Story | Size |
|----|-------|------|
| US-031 | Start an in-app application | 4pt |
| US-032 | Save application as draft | 3pt |
| US-033 | Upload documents from vault | 5pt |
| US-034 | Submit application | 3pt |
| US-035 | Track timeline + status | 4pt |
| US-036 | Withdraw application | 3pt |
| US-037 | Reapply after withdrawal | 3pt |
| US-038 | Initiate external application | 3pt |
| US-039 | Manually track external status | 4pt |
| US-040 | Add notes to external record | 2pt |
| US-041 | View submitted applications (Company) | 4pt |
| US-042 | Update application status (Company) | 4pt |
| US-043 | Prevent duplicate active applications | 3pt |
| US-044 | Lock final decisions read-only | 2pt |

## Functional requirements

FR-047 .. FR-062

## Acceptance criteria

1. **Kanban view** — Student sees all applications grouped by status (Draft / Pending / UnderReview / Accepted / Rejected / Withdrawn); drag-drop is read-only (status changes come from Company or system).
2. **Single-active rule** — Database enforces via unique filtered index on `(StudentId, ScholarshipId)` where `Status NOT IN (Withdrawn, Rejected, Accepted)`. Attempting a second active application returns 409 Conflict.
3. **Draft save** — Partial form state persisted as `Application.FormDataJson`; final submit validates required fields.
4. **Documents** — Students attach from their vault (`Documents/` table in profile) or upload new; max 20 MB per doc, PDF/JPEG/PNG.
5. **Withdrawal** — Allowed only while status in {Draft, Pending, UnderReview}; Accepted/Rejected are final and read-only.
6. **Reapply** — After withdrawal, if scholarship still open and no active application exists → allowed; otherwise 409.
7. **External tracking** — Self-tracked application has status choices `Intending / Applied / WaitingResult / Accepted / Rejected` and notes field; no Company-side review.
8. **Status history** — Every status change writes an `ApplicationStatusHistory` child row (who changed, when, from, to).

## Non-goals

- Bulk application submission (v2)
- Application templates ("apply with my default essay")
- Automatic document filling from profile fields
