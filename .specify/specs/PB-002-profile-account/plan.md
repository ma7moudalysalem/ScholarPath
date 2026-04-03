# PB-002 — Implementation Plan

## Architecture touchpoints

### Domain
- **Entities**: `UserProfile` (extended with role-specific JSON columns or split via TPT), `EducationEntry`, `ExpertiseTag`
- **Events**: `ProfileUpdatedEvent`, `ProfilePhotoChangedEvent`

### Application (`server/src/ScholarPath.Application/Profiles/`)
- **Commands**: `UpdateProfileCommand`, `UploadProfilePhotoCommand`, `ChangePasswordCommand`, `UpdateConsultantDetailsCommand`, `UpdateCompanyDetailsCommand`
- **Queries**: `GetMyProfileQuery`, `GetPublicProfileQuery` (for `/consultants/{id}` and `/companies/{id}` views)
- **Services**: `ProfileCompletenessCalculator` (pure function per role)

### Infrastructure
- `Services/BlobStorageService.cs` — handles profile photo upload; local filesystem in dev, Azure Blob in prod

### API (`server/src/ScholarPath.API/Controllers/ProfilesController.cs`)
- `GET /api/profiles/me`
- `PATCH /api/profiles/me`
- `POST /api/profiles/me/photo`
- `PATCH /api/profiles/me/password`
- `GET /api/profiles/consultants/{id}` (public within auth)
- `GET /api/profiles/companies/{id}` (public within auth)

### Frontend
- **Pages**: `pages/profile/{Profile, EditProfile, Security, ConsultantDetails, CompanyDetails, PublicProfile}.tsx`
- **Components**: `components/profile/{CompletenessRing, PhotoUploader, ExpertiseTagPicker}.tsx`
- **i18n**: `profile` namespace
- **Store**: extends `authStore` with profile cache or uses dedicated `profileStore`

### Tests
- Unit: `ProfileCompletenessCalculator` edge cases (empty, partial, full)
- Integration: CRUD on each role profile
- E2E: edit profile + see completeness meter update

## Dependencies

- **PB-001** (user identity + auth)

## Risks

1. Concurrent edits — use optimistic concurrency via `RowVersion` column
2. Photo upload size/type — enforce 5 MB, JPEG/PNG/WebP only, resize server-side
3. Password change with active SignalR connections — re-auth those connections
