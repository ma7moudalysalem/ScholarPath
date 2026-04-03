# PB-003 — Scholarship Discovery and Listing Management

**Owner**: @norra-mmhamed • **Priority**: Essential • **Iteration**: 2 • **Est**: 40 pts

## Problem statement

Students need to discover scholarships fast (500ms target over 100K listings). Companies create in-app listings with custom application forms + required docs. Admins create external-URL listings (students apply on external sites, self-tracked in-platform). Listings support bookmarking, AI match scoring (hooks into PB-008), and feature-flagging.

## User stories (from SRS)

| ID | Story | Size |
|----|-------|------|
| US-020 | Browse scholarship listings | 3pt |
| US-021 | Search by keyword | 3pt |
| US-022 | Filter by country/deadline/funding/level/tags | 5pt |
| US-023 | View scholarship details | 3pt |
| US-024 | Bookmark scholarships | 2pt |
| US-025 | Create in-app scholarship listing (Company) | 5pt |
| US-026 | Define application form fields (Company) | 5pt |
| US-027 | Define required documents (Company) | 4pt |
| US-028 | Edit/archive listings (Company) | 4pt |
| US-029 | Create external-URL listing (Admin) | 4pt |
| US-030 | Feature selected scholarships (Admin) | 2pt |

## Functional requirements

FR-034 .. FR-046

## Acceptance criteria

1. **Search** — `POST /api/scholarships/search` returns paginated results within 500ms for queries over a 100K-row index. Uses SQL Server Full-Text Search.
2. **Filters** — country[], deadline range, funding type, academic level, tags, category, funded-only flag.
3. **Sort** — relevance (default), deadline ascending, newest, recommended (AI-ranked; falls back to newest if PB-008 unavailable).
4. **Bookmark** — `POST /api/scholarships/{id}/bookmark` toggles `SavedScholarship`.
5. **Listing create (Company)** — form schema JSON + required docs list, deadline must be >= 7 days from today, status `Draft` / `Open` / `Archived` / `Under Review`.
6. **Listing edit** — Company edits own listings; all edits audit-logged; if the listing has active applications, schema changes are blocked (or prompt a major-version bump).
7. **External URL (Admin)** — separate entity flag `IsExternal=true`; students clicking `Apply` hit `/api/applications/external-intent` which creates a self-tracked `ApplicationTracker` and returns the external URL for client-side redirect.
8. **Feature flag** — admins can mark up to 12 featured scholarships shown on `/scholarships` hero carousel.
9. **i18n content** — Every listing has `TitleEn/TitleAr`, `DescriptionEn/DescriptionAr`, and eligibility fields translated.

## Non-goals

- AI-generated listing descriptions (v2)
- Paid listing boost for Companies (v2)
- Listing expiration auto-archival cron (done separately in Hangfire but owned by PB-012 Audit)
