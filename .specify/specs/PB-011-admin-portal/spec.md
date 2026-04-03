# PB-011 — Admin Portal and Oversight

**Owner**: @ma7moudalysalem • **Priority**: Essential • **Iteration**: 1 • **Est**: 76 pts

## Problem statement

Admins need a central portal to approve Company/Consultant onboarding, activate/suspend accounts, moderate community content and articles, create external scholarship listings, feature content, send broadcasts, review flagged items, and see analytics. This module is cross-cutting — it reads into every other module.

## User stories

US-118 .. US-138

## Functional requirements

FR-153 .. FR-176

## Acceptance criteria

1. **User management** — Search/filter users by role, status; view profile summary; activate/suspend/deactivate/delete; approve/reject upgrade requests; audit every action.
2. **Content management** — Scholarships (create external, feature up to 12), articles (review queue + publish + feature), community (flagged queue + restore/remove/suspend user).
3. **Analytics dashboard** — Users, scholarships, applications, bookings, payments, AI usage, content metrics. Charts via Recharts or similar.
4. **Profit-share config** — See PB-014.
5. **Broadcast announcements** — All users or filtered.
6. **Audit** — Every admin action produces `AuditLog` row with before/after JSON.
7. **Role guard** — Every endpoint gated by `[Authorize(Roles = "Admin")]`.

## Non-goals

- Admin-editable email templates (v2)
- Fine-grained permission sub-roles (single Admin role in v1)
