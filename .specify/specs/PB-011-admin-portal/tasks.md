# PB-011 — Tasks

**Owner**: @ma7moudalysalem  •  **Est**: 76 pts  •  **Iteration**: 1 (foundation) + ongoing

## Backend
- [ ] T-001 — User management commands + queries (activate/suspend/deactivate/delete)
- [ ] T-002 — Upgrade + onboarding approval queues
- [ ] T-003 — Content moderation endpoints (scholarships, articles, community — delegated)
- [ ] T-004 — Analytics queries (aggregations over Users, Scholarships, Applications, Bookings, Payments, AI usage)
- [ ] T-005 — `SendBroadcastCommand` wired to PB-010
- [ ] T-006 — Audit every admin mutation (calls PB-012 audit service)
- [ ] T-007 — Role + endpoint guards
- [ ] T-008 — Unit + integration tests

## Frontend
- [ ] T-009 — `AdminLayout` with sidebar + role check
- [ ] T-010 — `AdminDashboard` KPI cards + Recharts charts
- [ ] T-011 — `UsersAdmin` with search, filters, bulk actions
- [ ] T-012 — `UpgradeQueue` + `OnboardingQueue` detail panels
- [ ] T-013 — `ScholarshipsAdmin` external listings CRUD + feature toggle
- [ ] T-014 — `ArticlesAdmin` moderation queue
- [ ] T-015 — `CommunityModeration` flagged-post queue
- [ ] T-016 — `ProfitShareConfig` (PB-014 page embedded)
- [ ] T-017 — `AuditLog` read-only viewer
- [ ] T-018 — `BroadcastComposer` form
- [ ] T-019 — `Analytics` page with date-range filter
- [ ] T-020 — Arabic copy review

## QA
- [ ] T-021 — E2E: approve consultant onboarding; user's dashboard now shows consultant tools
- [ ] T-022 — E2E: suspend user → they get logged out immediately on next request

## Done criteria
All 14 content areas gated by Admin role; every mutation audited; analytics cards render real data.
