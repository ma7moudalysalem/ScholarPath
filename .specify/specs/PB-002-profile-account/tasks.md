# PB-002 — Tasks

**Owner**: @Madiha6776  •  **Est**: 15 pts  •  **Iteration**: 1

## Backend

- [ ] T-001 — Implement `GetMyProfileQueryHandler` returning role-specific DTO (FR-028 .. FR-031)
- [ ] T-002 — Implement `UpdateProfileCommandHandler` with FluentValidation per role
- [ ] T-003 — Implement `ProfileCompletenessCalculator` (unit-test heavy)
- [ ] T-004 — Implement `UploadProfilePhotoCommand` with size/type validation + blob storage
- [ ] T-005 — Implement `ChangePasswordCommand` — validate current password, update, revoke refresh tokens
- [ ] T-006 — Add `ProfilesController` with all 6 endpoints
- [ ] T-007 — Raise `ProfileUpdatedEvent` when relevant AI-input fields change (feeds PB-008)
- [ ] T-008 — Unit tests + integration tests (>=70% coverage)

## Frontend

- [ ] T-009 — Build `Profile.tsx` overview page (completeness ring + navigation to sub-sections)
- [ ] T-010 — Build `EditProfile.tsx` with react-hook-form + Zod schema per role
- [ ] T-011 — Build `Security.tsx` — change password + list active sessions (deferred)
- [ ] T-012 — Build `ConsultantDetails.tsx` — expertise tags, fee input, bio editor
- [ ] T-013 — Build `CompanyDetails.tsx` — org verification status + legal info
- [ ] T-014 — Build `PublicProfile.tsx` — consumed by `/consultants/{id}` and `/companies/{id}` routes
- [ ] T-015 — Photo upload component with drag-drop + crop preview
- [ ] T-016 — Arabic copy review for `profile` namespace

## QA

- [ ] T-017 — E2E: edit profile → completeness updates → save → reload shows saved state
- [ ] T-018 — E2E: change password → logout → login with new password

## Done criteria
- CI green, coverage ≥70%, EN+AR parity, completeness ring matches server calculation
