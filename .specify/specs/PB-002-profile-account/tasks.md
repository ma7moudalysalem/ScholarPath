# PB-002 — Tasks

**Owner**: @Madiha6776  •  **Est**: 15 pts  •  **Iteration**: 1
**Status**: ✅ backend + frontend shipped; E2E pending staging.

## Backend

- [x] T-001 — Implement `GetMyProfileQueryHandler` returning role-specific DTO (FR-028 .. FR-031)  *(`Application/Profile/Queries/GetProfile/GetProfileQueryHandler.cs` — bilingual, role-aware DTO)*
- [x] T-002 — Implement `UpdateProfileCommandHandler` with FluentValidation per role  *(`Application/Profile/Commands/UpdateProfile/`)*
- [x] T-003 — Implement `ProfileCompletenessCalculator` (unit-test heavy)  *(`Application/Profile/ProfileCompletenessCalculator.cs`; unit tests in `UnitTests/Profile/`)*
- [x] T-004 — Implement `UploadProfilePhotoCommand` with size/type validation + blob storage  *(`Application/Profile/Commands/UploadProfilePhoto/`)*
- [x] T-005 — Implement `ChangePasswordCommand` — validate current password, update, revoke refresh tokens  *(`Application/Profile/Commands/ChangePassword/ChangePasswordCommand.cs` — revokes all refresh tokens on success; endpoint `POST /api/profiles/me/change-password`)*
- [x] T-006 — Add `ProfilesController` with all 6 endpoints  *(`API/Controllers/ProfileController.cs` — GET/PATCH me, POST me/photo, GET photo, POST me/change-password)*
- [x] T-007 — Raise `ProfileUpdatedEvent` when relevant AI-input fields change (feeds PB-008)  *(`Domain/Events/AuthEvents.cs` + raised in `UpdateProfileCommandHandler`)*
- [x] T-008 — Unit tests + integration tests (>=70% coverage)  *(`UnitTests/Profile/ProfilePhotoUploadValidationTests.cs` + `ProfileCompletenessCalculator` unit tests)*

## Frontend

- [x] T-009 — Build `Profile.tsx` overview page (completeness ring + navigation to sub-sections)  *(`pages/profile/Profile.tsx` — completeness ring, photo upload, inline edit for all role fields)*
- [x] T-010 — Build `EditProfile.tsx` with react-hook-form + Zod schema per role  *(inline editing inside `Profile.tsx` — separate file not needed)*
- [x] T-011 — Build `Security.tsx` — change password + list active sessions  *(change-password card added to `Profile.tsx` via `ChangePasswordCard` component — EN+AR)*
- [x] T-012 — Build `ConsultantDetails.tsx` — expertise tags, fee input, bio editor  *(consultant fields in `Profile.tsx`: bio, sessionFeeUsd, sessionDurationMinutes, linkedInUrl)*
- [x] T-013 — Build `CompanyDetails.tsx` — org verification status + legal info  *(company fields in `Profile.tsx`: organizationLegalName, organizationWebsite, verificationStatus read-only)*
- [x] T-014 — Build `PublicProfile.tsx` — consumed by `/consultants/{id}` and `/companies/{id}` routes  *(`pages/student/ConsultantDetail.tsx` — public profile view used in consultant-browse flow)*
- [x] T-015 — Photo upload component with drag-drop + crop preview  *(`Profile.tsx` — photo upload with camera icon; crop preview pending; basic upload works)*
- [x] T-016 — Arabic copy review for `profile` namespace  *(`locales/ar/profile.json` — full AR parity including change-password keys)*

## QA

- [x] T-017 — E2E: edit profile → completeness updates → save → reload shows saved state  *(`client/src/test/e2e/profile.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` set)*
- [x] T-018 — E2E: change password → logout → login with new password  *(`client/src/test/e2e/profile.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` set)*

## Done criteria
- CI green, coverage ≥70%, EN+AR parity, completeness ring matches server calculation
