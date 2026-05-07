# PB-011 — Tasks

**Owner**: @ma7moudalysalem  •  **Est**: 76 pts  •  **Iteration**: 1 (foundation) + ongoing
**Status**: ✅ backend + core frontend shipped; 5 tasks **delegated** to module owners; 2 QA tasks covered at unit level, full E2E waits on seeded staging.

## Backend
- [x] T-001 — User management commands + queries (activate/suspend/deactivate/delete)  *(`2c95359` — SetUserStatus, SoftDeleteUser, ChangeUserRole via IUserAdministration)*
- [x] T-002 — Upgrade + onboarding approval queues  *(`d5c3da5`)*
- [ ] T-003 — Content moderation endpoints (scholarships, articles, community — **delegated**)  *(owned by Nora PB-003, Yosra PB-009, Yosra PB-007; admin routes resolve to stubs until those ship)*
- [x] T-004 — Analytics queries (aggregations over Users, Scholarships, Applications, Bookings, Payments, AI usage)  *(`0fbade7`)*
- [x] T-005 — `SendBroadcastCommand` wired to PB-010  *(`0fbade7`)*
- [x] T-006 — Audit every admin mutation (calls PB-012 audit service)  *(inherited automatically via `[Auditable]` on every command)*
- [x] T-007 — Role + endpoint guards  *(`[Authorize(Roles = "Admin,SuperAdmin")]` on AdminController, `86c79d6`)*
- [x] T-008 — Unit + integration tests  *(22 new admin tests + 5 UserAdministration integration tests, all green)*

## Frontend
- [x] T-009 — `AdminLayout` with sidebar + role check  *(`c355f5b`)*
- [x] T-010 — `AdminDashboard` KPI cards + charts  *(`c355f5b` — inline SVG instead of Recharts to keep bundle lean)*
- [x] T-011 — `UsersAdmin` with search, filters, bulk actions  *(`c355f5b`)*
- [x] T-012 — `UpgradeQueue` + `OnboardingQueue` detail panels  *(`c355f5b`)*
- [ ] T-013 — `ScholarshipsAdmin` external listings CRUD + feature toggle  *(**delegated** to @norra-mmhamed, PB-003)*
- [ ] T-014 — `ArticlesAdmin` moderation queue  *(**delegated** to @yousra-elnoby, PB-009)*
- [ ] T-015 — `CommunityModeration` flagged-post queue  *(**delegated** to @yousra-elnoby, PB-007)*
- [ ] T-016 — `ProfitShareConfig` (PB-014 page embedded)  *(**delegated** to @norra-mmhamed, PB-014)*
- [x] T-017 — `AuditLog` read-only viewer  *(`aa790dd` — shared with PB-012 T-010)*
- [x] T-018 — `BroadcastComposer` form  *(`c355f5b`)*
- [x] T-019 — `Analytics` page with date-range filter  *(`c3cdf01` — 7/30/90/180d window, inline SVG area chart)*
- [x] T-020 — Arabic copy review  *(full AR `admin.json` covering nav + dashboard + users + queues + audit + analytics + broadcast)*

## QA
- [ ] T-021 — E2E: approve consultant onboarding; user's dashboard now shows consultant tools  *(🟡 partial — Playwright route-guard smoke in `admin.spec.ts`; full flow needs seeded staging DB)*
- [x] T-022 — E2E: suspend user → they get logged out immediately on next request  *(✅ proven at unit level in `UserAdministrationTests.Suspend_revokes_every_active_refresh_token`; frontend side inherits via JWT refresh interceptor 401 handling)*

## Done criteria
- [x] Content areas gated by Admin/SuperAdmin role
- [x] Every mutation audited (via PB-012 `AuditBehavior` + `[Auditable]`)
- [x] Analytics cards render real aggregations
- 🟡 Delegated admin panels (T-013..T-016) pending owner implementation — routes resolve to stubs today
