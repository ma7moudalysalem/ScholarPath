# PB-002 — Profile and Account Management

**Owner**: @Madiha6776 • **Priority**: Essential • **Iteration**: 1 • **Est**: 15 pts

## Problem statement

Every authenticated user needs a profile page — for Students it drives AI recommendations and eligibility checks; for Companies and Consultants it's the public-facing trust signal for applicants and bookers. Users should see a completeness meter, update password/security, and (for Consultants) manage expertise/credentials/session fee.

## User stories (from SRS)

| ID | Story | Size |
|----|-------|------|
| US-015 | As a Student, I can edit my profile so that recommendations are accurate. | 3pt |
| US-016 | As a Company, I can manage my organization profile so students can trust listings. | 3pt |
| US-017 | As a Consultant, I can manage expertise, credentials, and session fee. | 4pt |
| US-018 | As a User, I can view my profile completeness so I know what's missing. | 2pt |
| US-019 | As a User, I can update password and security settings. | 3pt |

## Functional requirements

FR-028 .. FR-033, FR-177 (audit)

## Acceptance criteria

1. `GET /api/profiles/me` returns role-specific profile (Student / Company / Consultant).
2. `PATCH /api/profiles/me` validates and persists partial updates; emits `ProfileUpdatedEvent` for AI recalc trigger (PB-008).
3. Profile completeness is recomputed server-side on every update; returns percentage in `GetMe` response.
4. Password change requires current password; invalidates all active refresh tokens (FR-025).
5. Consultant profile fields: expertise tags, credentials, bio, hourly fee (USD), languages, availability preview link.
6. Company profile fields: legal name, website, country, description, verification status badge.
7. Student profile fields: academic level, field of study, nationality, GPA, languages, preferred countries, funding type preference.
8. Uploading a profile photo hits `/api/profiles/me/photo` → `IBlobStorageService` → returns CDN URL.

## Non-goals

- Multi-currency fees (USD only in v1)
- Public consultant bio translation (consultant writes in their language)
- Historical profile version browsing
