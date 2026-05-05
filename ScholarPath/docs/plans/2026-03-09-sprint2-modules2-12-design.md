# Sprint 2 — Modules 2–12 Design Document

**Date:** 2026-03-09
**Implementation Order:** Full sequential (Module 2 → 3 → 4 → ... → 12)
**Approach:** Each module completed (backend + frontend) before moving to next

## Modules

| # | Module | Backend Tasks | Frontend Tasks | Effort |
|---|--------|--------------|----------------|--------|
| 2 | Scholarship Search & Listing | 3 | 3 | Large |
| 3 | Scholarship Details & Apply | 2 | 2 | Medium |
| 4 | Student Dashboard & Tracking | 3 | 3 | Large |
| 5 | Resources Center | 2 | 2 | Medium |
| 6 | Success Stories | 1 | 2 | Medium |
| 7 | Mentorship & Advisor Sessions | 3 | 3 | XLarge |
| 8 | Calendar & Deadlines | 1 | 1 | Medium |
| 9 | Notifications | 2 | 2 | Large |
| 10 | Admin CMS & Moderation | 4 | 5 | XLarge |
| 11 | Community Module | 3 | 4 | XLarge |
| 12 | AI Assistance (Phase 2) | 1 | 2 | Large |

## Architecture Decisions

- **Backend:** Clean Architecture with existing patterns (controllers → services → repositories)
- **Frontend:** React 19 + MUI + Zustand + TanStack Query + i18n (EN/AR)
- **Database:** SQL Server with EF Core, migrations per module batch
- **Caching:** Redis/in-memory via existing ICachingService
- **Background Jobs:** Hangfire for async processing
- **Real-time:** SignalR for notifications (Module 9)
- **Payments:** Stripe Checkout for mentorship bookings (Module 7)
- **AI:** Anthropic API for document analysis (Module 12)

## Key Patterns to Follow

- Entities extend BaseEntity/AuditableEntity, implement ISoftDeletable where needed
- FluentValidation for all request DTOs
- AutoMapper for entity↔DTO mapping
- Pagination via existing PaginatedResponse<T> pattern
- API versioning: /api/v1/...
- i18n: bilingual fields (Title/TitleAr) on all user-facing entities
- Frontend: lazy-loaded pages, TanStack Query for server state, Zustand for client state
