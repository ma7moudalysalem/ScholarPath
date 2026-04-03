# PB-004 — Tasks

**Owner**: @norra-mmhamed  •  **Est**: 47 pts  •  **Iteration**: 2

## Backend
- [ ] T-001 — Confirm `ApplicationTracker` entity + unique filtered index migration (FR-057)
- [ ] T-002 — `StartApplicationCommand` (checks dup, loads scholarship form schema) (FR-047)
- [ ] T-003 — `SaveDraftCommand` + validation (FR-048)
- [ ] T-004 — `SubmitApplicationCommand` (validates required fields + docs) (FR-047, FR-050)
- [ ] T-005 — `WithdrawApplicationCommand` (allowed-states check) (FR-058)
- [ ] T-006 — `ReapplyCommand` or allow `StartApplicationCommand` to re-enter after withdrawal (FR-059)
- [ ] T-007 — `ChangeApplicationStatusCommand` (Company role; transitions via state machine) (FR-052)
- [ ] T-008 — Lock final `Accepted/Rejected` as read-only via domain check (FR-060)
- [ ] T-009 — External flow: `ExternalIntentCommand` + `UpdateExternalStatusCommand` + `AddExternalNoteCommand` (FR-053 .. FR-056)
- [ ] T-010 — Raise `ApplicationStatusChangedEvent` → consumed by PB-010 (Notifications) + PB-008 (AI re-rank if needed)
- [ ] T-011 — Unit + integration tests (>=70% coverage)

## Frontend
- [ ] T-012 — Student `Applications.tsx` kanban (shadcn `data-table` or custom columns)
- [ ] T-013 — `ApplicationDetail` side drawer with timeline
- [ ] T-014 — `ApplicationForm.tsx` — renders Company's dynamic schema (build `FormFieldRenderer`)
- [ ] T-015 — Document vault picker + upload flow
- [ ] T-016 — Withdraw + Reapply UI with confirmation modals
- [ ] T-017 — External tracker page — manual status update + notes
- [ ] T-018 — Company `ApplicationsReview.tsx` list + detail panel
- [ ] T-019 — Subscribe to `NotificationHub` → auto-refresh kanban on status change
- [ ] T-020 — Arabic copy review `applications` namespace

## QA
- [ ] T-021 — E2E: start → draft → submit → Company review → accept → student sees accepted → try withdraw (blocked)
- [ ] T-022 — E2E: withdraw (allowed state) → reapply → submit

## Done criteria
All 22 tasks, CI green, no duplicate-active bug, real-time updates working.
