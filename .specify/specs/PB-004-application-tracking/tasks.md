# PB-004 — Tasks

**Owner**: @norra-mmhamed  •  **Est**: 47 pts  •  **Iteration**: 2
**Status**: ✅ backend + frontend + unit + integration tests shipped; E2E pending staging.

## Backend
- [x] T-001 — `ApplicationTracker` entity + `ApplicationStateMachine` + unique filtered index migration *(ApplicationStateMachine.cs; entity in Domain + EF config)*
- [x] T-002 — `StartApplicationCommand` (dup-check, loads form schema, idempotent resume) *(`Applications/Commands/StartApplication/StartApplicationHandler.cs`)*
- [x] T-003 — `SaveApplicationDraftCommand` + validation *(`Applications/Commands/SaveApplicationDraft/`)*
- [x] T-004 — `SubmitApplicationCommand` (required-fields + docs check) *(`Applications/Commands/SubmitApplication/`)*
- [x] T-005 — `WithdrawApplicationCommand` (allowed-states check) *(`Applications/Commands/WithdrawApplication/` — validator + handler)*
- [x] T-006 — Reapply handled by idempotent `StartApplicationCommand` (re-enters after withdrawal)
- [x] T-007 — `ReviewApplicationCommand` (Company role; state-machine transitions) *(`Applications/Commands/ReviewApplication/`)*
- [x] T-008 — `ApplicationStateMachine` locks Accepted/Rejected as read-only via domain check *(`Applications/Common/ApplicationStateMachine.cs`)*
- [x] T-009 — External flow: `ExternalIntentCommand` + `UpdateExternalStatusCommand` *(`Commands/ExternalIntent/` + `Commands/UpdateExternalStatus/`)*
- [x] T-010 — `ApplicationStatusHistoryEventHandler` (status-change events consumed) *(`Applications/EventHandlers/ApplicationStatusHistoryEventHandler.cs`)*
- [x] T-011 — Unit + integration tests *(`UnitTests/Applications/` — CreateApplicationCommandHandlerTests + ExternalIntentCommandHandlerTests; `IntegrationTests/Applications/` — ScholarshipApplicationsIntegrationTests)*

## Frontend
- [x] T-012 — Student `Applications.tsx` — kanban board with drag-and-drop columns *(`pages/student/Applications.tsx`)*
- [x] T-013 — `StudentApplicationDetail.tsx` — full detail view with editable Draft form, timeline, documents, personal notes, status badge, and withdraw button *(`pages/student/StudentApplicationDetail.tsx`)*
- [x] T-014 — `DraftApplicationForm` (dynamic schema renderer with `FormFieldRenderer`) — embedded in `StudentApplicationDetail.tsx`
- [x] T-015 — Document vault picker + per-slot upload flow — embedded in `DraftApplicationForm`
- [x] T-016 — Withdraw UI with `ConfirmDialog` — Withdraw button shown for non-terminal statuses in `StudentApplicationDetail.tsx`
- [x] T-017 — External tracker: `AddExternalApplicationModal` + manual status update via kanban *(`components/application/AddExternalApplicationModal.tsx`)*
- [x] T-018 — Company `ApplicationsReview.tsx` list + detail panel with accept/reject decisions *(`pages/company/ApplicationsReview.tsx`)*
- [x] T-019 — Subscribe to `NotificationHub` → real-time kanban invalidation via `useNotificationHub` (PB-018)
- [x] T-020 — Arabic copy review — `locales/ar/applications.json` (full AR translation)

## QA
- [x] T-021 — E2E: start → draft → submit → Company review → accept → student sees accepted → try withdraw (blocked) *(`client/src/test/e2e/applications.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` + `E2E_COMPANY_EMAIL` set)*
- [x] T-022 — E2E: withdraw (allowed state) → reapply → submit *(`client/src/test/e2e/applications.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` set)*

## Done criteria
- [x] All backend commands, state machine, and tests shipped; CI green.
- [ ] E2E green in staging. *(spec written — `applications.spec.ts`; needs staging credentials to run)*
