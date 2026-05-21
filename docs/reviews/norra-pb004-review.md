# Code Review — `PB-004-application-tracking`

**Reviewer**: @ma7moudalysalem · **Author**: @norra-mmhamed · **Date**: 2026-04-25
**Branch**: `PB-004-application-tracking` · **vs**: `main` · **PR**: not opened yet
**Scope**: PB-004 In-App Application + External Tracking — `CreateApplicationCommand` slice only
**Stats**: +292 / 0 across 8 files · 3 commits · 3 ahead / 20 behind main

---

## TL;DR

This is the smallest of the two — one command, one validator, one DTO, one controller, two tests. The shape is right (record command, FluentValidation, MediatR, AutoMapper, `ICurrentUserService` + `IDateTimeService` injected — exactly the abstractions we want), and the active-application check is in the right place. Good fundamentals.

But this is **3% of the spec** (1 of 22 tasks: ~T-002 partially). None of the workflow that defines PB-004 is here: no draft save, no submit-with-validation, no withdraw, no reapply, no Company `ChangeStatus`, no external-tracking commands, no state machine, no domain event, no auth on the read side. The single command also has 4 functional bugs that would surface the first time a real student double-applies. And like PB-003, the branch is 20 behind `main` and currently looks like it deletes 60+ analytics files.

**Verdict**: ❌ request changes. Fix B1..B5, finish at least the state-machine + withdraw + reapply commands, then re-request review.

---

## 🔴 Blockers (must-fix before merge)

### B1. Single-active-application check is not race-safe and not enforced by the DB
[`server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommandHandler.cs:31-40`](server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommandHandler.cs:31)

Spec acceptance #2 (FR-057): "Database enforces via unique filtered index on `(StudentId, ScholarshipId)` where `Status NOT IN (Withdrawn, Rejected, Accepted)`. Attempting a second active application returns 409 Conflict."

Current code does a `Status != Rejected && Status != Withdrawn` `AnyAsync` check then a separate `Add` + `SaveChanges`. Two concurrent requests from the same student can both pass the check before either inserts → **two active rows survive**. The check also misses `Status != Accepted`, so a student could re-apply on top of an already-accepted application.

**Fix two ways and ship both**:

1. Tighten the predicate to use `IsActive` semantics matching the entity:
```csharp
var hasActiveApp = await db.Applications
    .AnyAsync(a => a.StudentId == userId
                && a.ScholarshipId == request.ScholarshipId
                && a.Status != ApplicationStatus.Rejected
                && a.Status != ApplicationStatus.Withdrawn
                && a.Status != ApplicationStatus.Accepted, ct);
```

2. Add a migration with the unique filtered index — this is the spec, and it's the only durable fix for the race:
```sql
CREATE UNIQUE INDEX UX_Applications_Student_Scholarship_Active
    ON Applications (StudentId, ScholarshipId)
    WHERE Status NOT IN ('Withdrawn', 'Rejected', 'Accepted');
```
Then catch `DbUpdateException` (SQL error 2601 / 2627) inside the handler and translate it to `ConflictException` so the API still returns 409 instead of 500.

### B2. State machine missing — handler creates rows directly in `Pending` and skips `Draft`
[`CreateApplicationCommandHandler.cs:42-51`](server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommandHandler.cs:42)

Spec acceptance #1: state machine is `Draft → Pending/UnderReview → Accepted/Rejected/Withdrawn`. The handler always sets `Status = ApplicationStatus.Pending` and stamps `SubmittedAt = clock.UtcNow` — there is no draft entry path, no `SubmitApplicationCommand`, and no validation that the form schema's required fields are filled (FR-050).

This is T-002 + T-003 + T-004 + T-005 + T-006 + T-007 + T-008 from the spec. At minimum the slice needs:
- `StartApplicationCommand` → creates `Status = Draft`, no `SubmittedAt`
- `SaveDraftCommand` → updates `FormDataJson`, status stays `Draft`
- `SubmitApplicationCommand` → validates `FormDataJson` against `Scholarship.ApplicationFormSchemaJson`, validates `AttachedDocumentsJson` against `RequiredDocumentsJson`, transitions `Draft → Pending`, stamps `SubmittedAt`, raises `ApplicationStatusChangedEvent`
- `WithdrawApplicationCommand` → only allowed in `{Draft, Pending, UnderReview}`, transitions to `Withdrawn`
- `ChangeApplicationStatusCommand` (Company role) → enforces only legal transitions: `Pending → UnderReview → {Accepted, Rejected}`. Illegal transitions throw a `DomainException` (see B5).

A static `ApplicationStateMachine.IsTransitionAllowed(from, to, role)` helper makes the rules grep-able.

### B3. External tracking is conflated with in-app — Mode is mis-derived
[`CreateApplicationCommandHandler.cs:50`](server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommandHandler.cs:50)

```csharp
Mode = scholarship.Mode == ListingMode.InApp ? ApplicationMode.InApp : ApplicationMode.External
```

For an external listing this command would create a row with `Status = Pending` and `SubmittedAt = now`, marking it as if the student had actually applied. Spec FR-053..FR-056: external-listing application is **self-tracked**, has its own status enum (`Intending / Applied / WaitingResult / Accepted / Rejected`), no Company review, and starts via `ExternalIntentCommand` not this one.

**Fix**: at the top of the handler, throw if `scholarship.Mode == ListingMode.ExternalUrl`:
```csharp
if (scholarship.Mode == ListingMode.ExternalUrl)
    throw new ConflictException("External listings must use ExternalIntentCommand.");
```
Or split the read path completely and never let `CreateApplicationCommand` see external listings.

### B4. Branch is 20 behind `main` — would obliterate analytics work on merge
`git diff main...origin/PB-004-application-tracking` shows the analytics layer (`PB-015..PB-018`, `analytics/` dir, ADF + dbt + Stream Analytics, ~60 files) as deletions. Not destructive intent — it's the by-product of an old fork point — but if anyone fast-forwards this it deletes my work.

The branch also drops `RecommendationClickEvent`, `AiRedactionAuditSample`, `UserRiskFlag` from the domain and removes their EF configurations + the `RedactionAuditSamplingJob` Hangfire registration in `Program.cs`. Visible in `IApplicationDbContext.cs`, `AI.cs`, `CrossCutting.cs`, `EntityConfigurations.cs`, and `Program.cs`. Same root cause.

**Fix**: rebase onto `origin/main`.
```bash
git fetch origin
git checkout PB-004-application-tracking
git rebase origin/main
git push --force-with-lease
```

### B5. `[Authorize(Roles = "student")]` is lowercase — won't match the seeded role
[`server/src/ScholarPath.API/Controllers/ApplicationsController.cs:9`](server/src/ScholarPath.API/Controllers/ApplicationsController.cs:9)

```csharp
[Authorize(Roles = "student")]
```

`DbSeeder` seeds `"Student"` (capital S). `RoleManager.NormalizedName` is set to `"STUDENT"` but ASP.NET Core role checks are **case-sensitive on `Roles =`** — every authenticated student request to `POST /api/applications` will return 403.

**Fix**: `[Authorize(Roles = "Student")]`. Ditto on the controller — there's no class-level `[ApiController]`, no `[Route(...)]`, and the controller inherits `Controller` instead of `ControllerBase`. Compare `AuthController` on main:
```csharp
[ApiController]
[Route("api/applications")]
[Produces("application/json")]
[Authorize(Roles = "Student")]
public sealed class ApplicationsController(IMediator mediator) : ControllerBase { ... }
```

---

## 🟡 Important — architecture / convention

### I1. Missing `[Auditable]` on the command
[`CreateApplicationCommand.cs:9`](server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommand.cs:9)

Constitution principle requires every state-mutating command to be auditable. The pipeline (`AuditBehavior`) picks the attribute up automatically — zero boilerplate. Pattern from `LogRecommendationClickCommand` on main:

```csharp
[Auditable(AuditAction.Create, "Application",
    SummaryTemplate = "Application started ({Id})",
    SkipOnNull = true)]
public sealed record CreateApplicationCommand(Guid ScholarshipId, string? PersonalNotes)
    : IRequest<ApplicationDto>;
```

Apply this to the future `SubmitApplicationCommand`, `WithdrawApplicationCommand`, `ChangeApplicationStatusCommand` too.

### I2. No domain event raised on status change
Spec T-010: "Raise `ApplicationStatusChangedEvent` → consumed by PB-010 (Notifications) + PB-008 (AI re-rank)". Today nothing is published. PB-010 needs this. Define under `Domain/Events/`:
```csharp
public sealed record ApplicationStatusChangedEvent(
    Guid ApplicationId, Guid StudentId, Guid ScholarshipId,
    ApplicationStatus From, ApplicationStatus To, DateTimeOffset OccurredAt) : INotification;
```
Publish it from every status-mutating handler after `SaveChangesAsync`.

### I3. `ApplicationDto` is missing the fields the kanban will need
[`ApplicationDto.cs`](server/src/ScholarPath.Application/Applications/DTOs/ApplicationDto.cs)

Current fields: `Id, ScholarshipId, Status, SubmittedAt, PersonalNotes`. The kanban (spec acceptance #1) needs at least: `ScholarshipTitleEn/Ar`, `Mode`, `Deadline`, `IsActive`, `IsReadOnly`, `WithdrawnAt`, `DecisionAt`. Without these the client can't render the cards. Add a `ScholarshipSummary` sub-record on the DTO, populated from the projection.

### I4. AutoMapper profile is not in the diff
The handler calls `mapper.Map<ApplicationDto>(entity)` (line 54) but no `Profile` is registered for `ApplicationTracker → ApplicationDto`. AutoMapper 14 will throw at runtime ("missing type map") on the very first call. Either:
- Add a `MappingProfile` under `Application/Applications/Mapping/ApplicationProfile.cs`
- Or drop AutoMapper for this slice and project explicitly:
  ```csharp
  return new ApplicationDto(entity.Id, entity.ScholarshipId, entity.Status.ToString(),
      entity.SubmittedAt!.Value, entity.PersonalNotes);
  ```
The integration test `Should_Create_Application_Successfully` will catch this once it actually runs end-to-end (currently it pulls the handler directly, bypassing DI registration — see I7).

### I5. Spec messages are Arabic-only
[`CreateApplicationCommandHandler.cs:34, 40`](server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommandHandler.cs:34) and [`CreateApplicationCommandValidator.cs:13`](server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommandValidator.cs:13)

`"عذراً، هذه المنحة غير متاحة..."`, `"لديك طلب تقديم نشط بالفعل..."`, `"الملاحظات يجب ألا تتجاوز 4000 حرف."` — Arabic-only error strings. Constitution principle IV is bilingual parity. The pattern on main is to throw an exception with a stable error code and let the i18n bundle on the client render the message. At minimum, switch to English in the throw and let the API surface the Arabic via `Accept-Language` later:
```csharp
throw new ConflictException("Scholarship is not open for applications.");
throw new ConflictException("An active application already exists for this scholarship.");
```

### I6. `ConflictException` for business-rule violation is fine, but `InvalidOperationException` will leak as 500
There is no `InvalidOperationException` in this branch yet — but the moment you add the state machine (B2) it'll be tempting to throw that for "illegal transition". Don't. Add a `Domain/Exceptions/ApplicationDomainException.cs` and throw that. The existing exception-handler middleware on main maps it to 422. Pattern matches Tasneem's review note I5.

### I7. Integration test does not actually exercise the controller
[`CreateApplicationCommandHandler.cs:30-50`](server/tests/ScholarPath.IntegrationTests/Applications/CreateApplicationCommandHandler.cs:30) (yes, the test file is named after the production class — see N1)

The test resolves `CreateApplicationCommandHandler` directly from DI and calls `Handle(...)` — it never goes through the controller, so `[Authorize(Roles = ...)]`, `[ApiController]` model binding, and the MediatR pipeline (validation, audit, logging behaviors) are all skipped. The duplicate test also doesn't seed a logged-in user — `currentUser.UserId` will be null and the handler will throw `ForbiddenAccessException` before reaching the conflict check, so the test would assert `ConflictException` but actually catch `ForbiddenAccessException`. Test passes for the wrong reason.

Use `factory.CreateClient()` and `_client.PostAsJsonAsync("/api/applications", ...)` like the PB-006 booking tests do. Stub the auth via a test handler that injects a known `Student` claim.

### I8. Hardcoded student GUID `"00000000-0000-0000-0000-000000000000"` in the conflict test
[`CreateApplicationCommandHandler.cs:73`](server/tests/ScholarPath.IntegrationTests/Applications/CreateApplicationCommandHandler.cs:73)

`Guid.Parse("00000000-0000-0000-0000-000000000000")` is `Guid.Empty`. The handler's `currentUser.UserId` is also unset (null), so the `userId` ends up being whatever `ForbiddenAccessException` short-circuits on — not `Guid.Empty`. This is the wrong-reason-pass case from I7.

### I9. `ApplicationsController` action `Create` returns `ActionResult<ApplicationDto>` but never uses 201/Location
A `POST` that creates a resource should return `CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto)` with a 201. Today it returns 200 with the DTO and no `Location` header. Minor but worth fixing now while the controller is small.

---

## 🟢 Nice-to-have / polish

### N1. Test file name matches the production class — confusing
[`server/tests/ScholarPath.IntegrationTests/Applications/CreateApplicationCommandHandler.cs`](server/tests/ScholarPath.IntegrationTests/Applications/CreateApplicationCommandHandler.cs)

The file containing `CreateApplicationTests` is named `CreateApplicationCommandHandler.cs` — same as the production handler. Rename to `CreateApplicationCommandHandlerTests.cs` so a global search for the production class lands you on the production file.

### N2. `using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;`
Same file, line 13 — orphaned using, never used. Remove.

### N3. AutoMapper is the wrong tool here
Mapping a 5-property record can be done by hand — AutoMapper's overhead (reflection cache, Profile registration, runtime errors when types drift) buys nothing. The PB-006 slice on main uses explicit mapping for the same reason.

### N4. `CreateApplicationCommand` record uses non-standard formatting
[`CreateApplicationCommand.cs:9-10`](server/src/ScholarPath.Application/Applications/Commands/CreateApplication/CreateApplicationCommand.cs:9)

```csharp
public record CreateApplicationCommand
(Guid ScholarshipId, string? PersonalNotes) : IRequest<ApplicationDto>;
```

The parameter list on a new line is jarring. Standard:
```csharp
public sealed record CreateApplicationCommand(Guid ScholarshipId, string? PersonalNotes)
    : IRequest<ApplicationDto>;
```

### N5. Two unused usings each in 4 files
`System;`, `System.Collections.Generic;`, `System.Text;` — present in `CreateApplicationCommand.cs`, `CreateApplicationCommandValidator.cs`, `ApplicationDto.cs`, `CreateApplicationCommandHandler.cs`. `dotnet format` removes them.

### N6. `_Module.md` is empty / placeholder
[`server/src/ScholarPath.Application/Applications/_Module.md`](server/src/ScholarPath.Application/Applications/_Module.md)

Document the slice: which commands exist, which events are published, which DB constraints back the rules. Mirrors `Bookings/_Module.md` from PB-006.

### N7. `clock.UtcNow` is being used — good
You injected `IDateTimeService` instead of calling `DateTimeOffset.UtcNow` directly. That's the right call — it makes the handler unit-testable. Keep doing that.

---

## ⚪ Spec gaps — missing tasks

| Task | Status |
|---|---|
| T-001 — `ApplicationTracker` entity + unique filtered index migration | partial — entity exists, **filtered index migration missing** |
| T-002 — `StartApplicationCommand` (with dup check + load schema) | partial — current command is closer to "Submit" |
| T-003 — `SaveDraftCommand` | ❌ |
| T-004 — `SubmitApplicationCommand` (validate required fields + docs) | ❌ |
| T-005 — `WithdrawApplicationCommand` (allowed-states check) | ❌ |
| T-006 — `ReapplyCommand` | ❌ |
| T-007 — `ChangeApplicationStatusCommand` (Company, state machine) | ❌ |
| T-008 — Lock final `Accepted/Rejected` read-only | ❌ — `IsReadOnly` exists on entity but never set |
| T-009 — External flow: `ExternalIntent` + `UpdateExternalStatus` + `AddExternalNote` | ❌ |
| T-010 — `ApplicationStatusChangedEvent` | ❌ |
| T-011 — >=70% coverage | ❌ — 1 unit test (validator), 2 partial integration tests |
| T-012..T-018 — Frontend kanban / forms / external tracker / Company review | ❌ |
| T-019 — `NotificationHub` subscription | ❌ |
| T-020 — Arabic copy review | ❌ |
| T-021/T-022 — E2E flows | ❌ |

The Company-side `ChangeApplicationStatusCommand` and the external commands are critical — without them, students can't get a status update and the external listings created in PB-003 lead to a dead end.

---

## ✅ Things I enjoyed reviewing

- **`ICurrentUserService` + `IDateTimeService` injected via primary-constructor syntax** — exactly the abstractions we want, exactly the syntax we want.
- **`AsNoTracking()` on the scholarship lookup** — this is a read-only validation pass, no need to track.
- **Throwing `NotFoundException` and `ConflictException`** instead of returning sentinels — matches the middleware mapping.
- **`FluentValidation` validator for the input shape** with both empty-id and max-length checks.
- **MediatR record command** — concise, immutable, correct.
- **Testcontainers for the integration test fixture** — way better than InMemory for catching real schema/index issues. Setup will pay off once the test actually drives through the controller (see I7).
- **Active-application check is in the right layer** (handler, not controller, not validator) — the architectural intent is correct, the implementation just needs B1's tightening + DB-level guarantee.

---

## Pre-merge checklist

- [ ] B1 — tighten predicate (add `!= Accepted`) and add the unique filtered-index migration
- [ ] B2 — at minimum: `StartApplicationCommand` (Draft) + `SubmitApplicationCommand` + `WithdrawApplicationCommand` + state machine helper
- [ ] B3 — guard against `ListingMode.ExternalUrl` in this command
- [ ] B4 — `git rebase origin/main` and force-push (ensures no analytics deletions)
- [ ] B5 — `[Authorize(Roles = "Student")]` (capital S), `[ApiController]`, `[Route]`, `ControllerBase`
- [ ] I1 — `[Auditable]` on every state-mutating command
- [ ] I2 — `ApplicationStatusChangedEvent` published from every transition
- [ ] I3 — DTO carries the fields the kanban needs
- [ ] I4 — AutoMapper profile registered, or drop AutoMapper for this slice
- [ ] I5 — error strings in English, i18n on the client
- [ ] I7 — integration test goes through `_client`, not the bare handler
- [ ] T-009 — at least the read of `ExternalIntentCommand` + `UpdateExternalStatusCommand` shipped
- [ ] T-011 — cover the state machine + each command (>=70% gate)
- [ ] CI green

I9, N1..N7 are polish — handle in the same PR if quick, otherwise a follow-up.

The bones are right, Norra. The active-app check, the abstractions, the testcontainers setup — none of those are wrong, they're just incomplete and racy. Get the state machine + withdraw + the unique index in place and this becomes a real slice. PB-003 and PB-004 should probably ship as a single coordinated feature; the order of operations is "scholarship exists → student starts an application → student submits → company reviews", and right now both branches stop at step 1.
