# Code Review — `PB-003_Scholarship-discovery`

**Reviewer**: @ma7moudalysalem · **Author**: @norra-mmhamed · **Date**: 2026-04-25
**Branch**: `PB-003_Scholarship-discovery` · **vs**: `main` · **PR**: not opened yet
**Scope**: PB-003 Scholarship Discovery — read-only slice (Get list + Get by id)
**Stats**: +395 / −11 across 15 files · 4 commits · 4 ahead / 20 behind main

---

## TL;DR

Thanks for getting the read path moving, Norra. The shape is right — `IRequest`/`IRequestHandler`, `AsNoTracking`, a `PaginatedList<T>` helper, two tests per query, integration tests using Testcontainers. That's a solid skeleton.

That said, this branch is **not mergeable as-is**. Out of T-001..T-011 you've delivered partial coverage of T-002 and the read half of T-003 only — the spec calls for `Search` (FT), `Create`, `Update`, `Archive`, `Bookmark`, `External`, and `Feature` commands plus Company/Admin authorization. None of the write commands exist, none of the filters in the spec are wired up (country, deadline range, level, tags, funded-only), the language is hardcoded `"en"`, the search is `LIKE %term%` not full-text, and the test packages have been added to the **production** csproj files. It also needs a rebase — it's 20 behind main and currently looks like it deletes 60+ analytics files.

**Verdict**: ❌ request changes. Fix the 5 blockers, knock out the missing commands, then re-request review.

---

## 🔴 Blockers (must-fix before merge)

### B1. Test-only packages added to all 4 production projects
[`server/Directory.Packages.props`](server/Directory.Packages.props) + the 4 csproj diffs

`MockQueryable.NSubstitute` and `NSubstitute` are now referenced from `ScholarPath.Domain`, `ScholarPath.Application`, `ScholarPath.API`, **and** `ScholarPath.Infrastructure`. These are test-stack packages — they have no business in production code. Domain in particular has a constitution rule "minimize external dependencies" (constitution §II + the comment in `ScholarPath.Domain.csproj`).

**Fix**: remove the 8 added `<PackageReference>` entries from the 4 production csproj files. Keep `MockQueryable.NSubstitute` in the test csproj files only (UnitTests already had it; you correctly added it to IntegrationTests).

### B2. Branch is 20 commits behind `main` — would obliterate analytics work on merge
`git diff main...origin/PB-003_Scholarship-discovery` shows **60+ deleted files**: all of `analytics/`, the PB-015..PB-018 specs, the Power BI dataflow, the runbooks, etc. None of those deletions are intentional — they're the by-product of the branch forking before that work landed. Merging this branch as-is via fast-forward / squash would silently delete it all.

**Fix**: rebase onto `origin/main` first.
```bash
git fetch origin
git checkout PB-003_Scholarship-discovery
git rebase origin/main
# resolve any conflicts in csproj files (re B1) and the seeder
git push --force-with-lease
```

### B3. Search is `LIKE %term%`, not Full-Text Search — spec FR-035 requires <500ms over 100K rows
[`server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipsQuery.cs:46-52`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipsQuery.cs:46)

```csharp
query = query.Where(s => s.TitleEn.Contains(term) || s.TitleAr.Contains(term) ||
                         s.DescriptionEn.Contains(term) || s.DescriptionAr.Contains(term));
```

`Contains` translates to `LIKE '%term%'`, which is **not sargable** — SQL Server scans the full table on every search and ignores all four `nvarchar` indexes. At 100K rows you will not hit 500ms. The spec acceptance criterion #1 explicitly says "Uses SQL Server Full-Text Search". There's also no FT catalog migration on the branch.

**Fix**: add a migration that creates an FT catalog and full-text indexes the 4 columns, then call `EF.Functions.Contains(s.TitleEn, term)` (FT predicate) or use the EF 10 raw FromSqlInterpolated path. T-003 is the perf test that proves this — until B3 is fixed, T-003 will fail.

### B4. Language is hardcoded `"en"` in the list query
[`GetScholarshipsQuery.cs:62`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipsQuery.cs:62)

```csharp
var lang = "en"; // Header
```

Constitution principle IV (Bilingual Parity) requires every shipped feature to render in EN+AR. The detail query (`GetScholarshipByIdQuery`) takes a `Language` param — the list query should too. Read it from the `Accept-Language` header (or pass an explicit `Language` query param like the detail handler does).

```csharp
public string? Language { get; init; } = "en";
// then inside the handler:
var lang = (request.Language ?? "en").ToLowerInvariant();
```

### B5. No write commands and no `[Authorize]` on the controller
[`server/src/ScholarPath.API/Controllers/ScholarshipController.cs:11-15`](server/src/ScholarPath.API/Controllers/ScholarshipController.cs:11)

The controller has zero auth attributes. The two GETs may be public, but the spec acceptance #5/#6/#7/#8 require `Create`, `Update`, `Archive`, `External`, and `Feature` — Company-scoped for the first three, Admin-scoped for the last two. None of these exist. Compare against `AdminController` and `AiController` on main — both have `[Authorize(Roles = "...")]` per action.

This is the bulk of the slice. T-004..T-009 are all open. At minimum you need:
- `CreateScholarshipCommand` (Company role, `Auditable(AuditAction.Create, "Scholarship")`, deadline >= 7d validator)
- `UpdateScholarshipCommand` (Company role, owner check, block schema change when `Applications.Any(IsActive)`)
- `ArchiveScholarshipCommand` (Company/Admin role, owner check)
- `BookmarkToggleCommand` (any authenticated user, FR-045)
- `CreateExternalListingCommand` (Admin role)
- `FeatureScholarshipCommand` (Admin role, 12-cap)

---

## 🟡 Important — architecture / convention

### I1. Filters from the spec are missing
[`GetScholarshipsQuery.cs:14-20`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipsQuery.cs:14)

The spec acceptance #2 lists: `country[]`, `deadline range`, `funding type`, `academic level`, `tags`, `category`, `funded-only flag`. The query record only has `CategoryId`, `FundingType`, `AcademicLevel` — and `AcademicLevel` is read but never used in the `Where` chain (line 38-40 only filters by `FundingType`). Add:
- `string[]? Countries` → check `s.TargetCountriesJson` contains any
- `DateTimeOffset? DeadlineFrom`/`DeadlineTo`
- `string[]? Tags` → check `s.TagsJson`
- `bool? FundedOnly` → `s.FundingType == FundingType.FullyFunded`
- Wire `AcademicLevel` into the where chain.

### I2. Route prefix is `api/v1/[controller]` — project convention is `api/<resource>`
[`ScholarshipController.cs:11`](server/src/ScholarPath.API/Controllers/ScholarshipController.cs:11)

```csharp
[Route("api/v1/[controller]")]   // → api/v1/scholarships
```

Every other controller on main uses `[Route("api/auth")]`, `[Route("api/admin")]`, `[Route("api/ai")]`. We don't ship API versioning yet. Either drop the `v1` and rename the controller class to `ScholarshipsController` (which is the file name, but the class is currently `ScholarshipsController : Controller` already — inconsistent with the file `ScholarshipController.cs`). Pick one.

Also: inherit `ControllerBase`, not `Controller`. We have no Razor views. Compare `AiController` on main.

### I3. `GetScholarshipsQuery` doesn't include Category and projects to a non-existent `OwnerCompanyName`/`Status`/`FundingType`/`TargetLevel` shape
[`GetScholarshipsQuery.cs:64-72`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipsQuery.cs:64)

The `Select` projects `CategoryName = s.Category.NameEn`. There's no `Include(s => s.Category)` and no explicit eager load — but because it's a projection inside `Select`, EF will JOIN automatically, so this works. **However**, the projection is also missing `OwnerCompanyName`, `Status`, `FundingType`, `TargetLevel`, `IsFeatured`, all of which are declared on `ScholarshipDto`. The list rendering on the client would show empty values for all of those. Either populate them or trim the DTO.

### I4. Sorting is not deterministic — pagination will jitter
[`GetScholarshipsQuery.cs:55-60`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipsQuery.cs:55)

```csharp
"newest" => query.OrderByDescending(s => s.CreatedAt),
_ => query.OrderByDescending(s => s.IsFeatured).ThenByDescending(s => s.CreatedAt)
```

Two scholarships created in the same millisecond will swap positions across `Skip`/`Take` page calls. Always tie-break on `Id`:
```csharp
"newest" => query.OrderByDescending(s => s.CreatedAt).ThenBy(s => s.Id),
```

### I5. `GetScholarshipByIdQuery` throws 409 when status != Open
[`GetScholarshipById.cs:34-38`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipById.cs:34)

A read query throwing `ConflictException` (409) for a closed scholarship is semantically wrong. A user clicking a stale link or browsing their bookmark should get the listing back with `Status = "Closed"` and the client decides whether to hide the apply button. Throwing 409 also breaks anyone who already applied and wants to see what they applied to.

If the requirement is "can't apply", that's a check on the `CreateApplicationCommand` (PB-004), not on the read. Remove this throw.

### I6. `Children.OrderBy(c => c.SortOrder).Select(...).ToList() ?? new List<...>()`
[`GetScholarshipById.cs:50-55`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipById.cs:50)

`ToList()` never returns null — the `??` is dead code. Remove it. Also, you eager-load `Children` and `Category` (line 28-29) for a request that only needs Children when the listing has any. Fine for now, but worth noting if/when `Children` rows balloon.

### I7. Test infrastructure errors
- [`ScholarshipApiTests.cs:73`](server/tests/ScholarPath.IntegrationTests/Scholarships/ScholarshipApiTests.cs:73) — the GUID `"0000000-0000-0000-0000-000000000000"` is **7 zeros** in the first segment, not 8 — `Guid.Parse` will throw at runtime and the test will fail before reaching the assert. The test on line 41 and line 73 also share the same all-zeros GUID; one expects 200, the other 409 — both call the same DB row.
- The factory has both `IAsyncLifetime.DisposeAsync()` (line 42) and `override async ValueTask DisposeAsync()` (line 31) — the explicit interface call returns `DisposeAsync().AsTask()` which would call the override, which calls `base.DisposeAsync()` first then nothing. The container is disposed twice. Use only the override.
- `GetScholarshipByIdHandlerTests` uses `UseInMemoryDatabase` — InMemory provider doesn't enforce indexes, FK cascades, or `HasQueryFilter`, so any test that depends on those would silently pass. For unit tests of EF queries, use `Sqlite` in-memory mode or move them to integration tests.

### I8. `GetScholarshipsQueryHandler.Handle` has a synchronous `Select` then awaits `CreateAsync` — the `CountAsync` happens twice
[`GetScholarshipsQuery.cs:64+74`](server/src/ScholarPath.Application/Scholarships/Queries/GetScholarshipsQuery.cs:64) + [`PaginatedList.cs:30`](server/src/ScholarPath.Application/Common/Models/PaginatedList.cs:30)

`PaginatedList<T>.CreateAsync` calls `source.CountAsync()` then `source.Skip(...).Take(...).ToListAsync()`. That's 2 round-trips. For 100K rows, the `COUNT(*)` is the slow one. Two options:
- Add an `EnsureProjection`/`OrderBy` before passing to `CreateAsync` so EF generates a single `SELECT … OFFSET … FETCH … COUNT() OVER()` window function.
- Cache the count for static filters (later optimization).

Not a blocker for the spec target since 100K isn't hit yet, but adds a free 30-50% latency on every search.

---

## 🟢 Nice-to-have / polish

### N1. File-level cleanup
- Trailing blank line + brace mis-indent at end of `GetScholarshipsQuery.cs` (line 79). `dotnet format` will fix.
- `using System; using System.Collections.Generic; using System.Text;` — three unused usings on most files. Remove.
- `using ScholarPath. Application.Common.Models;` in `GetScholarshipById.cs:13` has a space after `ScholarPath.` — it builds (C# allows whitespace in `using`) but is jarring.
- `PaginatedList.cs` declares the namespace on line 6 with a trailing semicolon (file-scoped) **and** then opens a nested non-namespaced block. Reformat to file-scoped style throughout.

### N2. Spelling typos in comments
- `GetScholarshipsQuery.cs:48` — "Sarch" → "Search"
- `GetScholarshipById.cs:38` — "currently" not "current"

### N3. `ScholarshipDto` has `OwnerCompanyName` but it's never set (see I3)
Either remove the property or populate it via `s.OwnerCompany!.UserProfile.OrgName` (the company display name).

### N4. `SavedScholarship` already has a unique constraint on `(UserId, ScholarshipId)`
[`EntityConfigurations.cs`](server/src/ScholarPath.Infrastructure/Persistence/Configurations/EntityConfigurations.cs) on main

So when you wire `BookmarkToggleCommand` (T-007) it can be a simple delete-if-exists / insert-otherwise. No extra index needed.

### N5. `_Module.md` was not added
[`server/src/ScholarPath.Application/Scholarships/_Module.md`](server/src/ScholarPath.Application/Scholarships/_Module.md)

The file exists in the listing but I don't see content changes in the diff — verify it documents the slice boundaries (queries, commands when added, events emitted) so the next person joining the slice has the map.

### N6. `db.Scholarships.AsNoTracking()` is the right call for `Get` and `GetById` — well done
Just noting this in the positive column too.

---

## ⚪ Spec gaps — missing tasks

| Task | Status |
|---|---|
| T-001 — finalize entity + FT index | ❌ FT index migration missing |
| T-002 — Search query | partial — no FT, no country/deadline-range/tags/funded filters, hardcoded lang |
| T-003 — 100K perf test | ❌ |
| T-004 — `CreateScholarshipCommand` | ❌ |
| T-005 — `UpdateScholarshipCommand` | ❌ |
| T-006 — `ArchiveScholarshipCommand` | ❌ |
| T-007 — `BookmarkToggleCommand` | ❌ |
| T-008 — `CreateExternalListingCommand` (Admin) | ❌ |
| T-009 — `FeatureScholarshipCommand` (12-cap) | ❌ |
| T-010 — Controller `[Authorize]` per endpoint | ❌ (controller has none) |
| T-011 — >=70% coverage | ❌ — 2 unit tests, 2 integration tests |
| T-012..T-018 — Frontend | ❌ |
| T-019 — `EligibilitySnapshot` (PB-008 hook) | ❌ |
| T-020 — Arabic copy | ❌ |
| T-021/T-022 — E2E | ❌ |

The frontend can land in a follow-up. The 6 missing commands cannot.

---

## ✅ Things I enjoyed reviewing

- **`AsNoTracking()` on both queries.** Read-only paths should never track — you got this right both times.
- **Bilingual projection in `GetScholarshipByIdQuery`.** The fallback chain `lang == "ar" ? entity.TitleAr ?? entity.TitleEn : entity.TitleEn ?? entity.TitleAr` is the right pattern when one side may be null.
- **Testcontainers for the integration test factory.** `MsSqlBuilder().Build()` is the correct setup — much better than InMemory for end-to-end coverage.
- **Pagination helper as a standalone class.** Small, generic, reusable across PB-004 and PB-007.
- **Query projection happens inside the IQueryable** (not after `ToList`), so EF will translate to SQL. Easy thing to get wrong.
- **`HasQueryFilter(s => !s.IsDeleted)`** is already wired on the entity — your queries inherit it automatically. Nice.
- **`ScholarshipDetailDto : ScholarshipDto`** record inheritance — clean way to share fields.

---

## Pre-merge checklist

- [ ] B1 — strip test packages from the 4 production csproj files
- [ ] B2 — `git rebase origin/main` and force-push
- [ ] B3 — FT catalog migration + `EF.Functions.Contains` (or document why deferred)
- [ ] B4 — language from header / query param, not hardcoded
- [ ] B5 — at minimum `Create` + `Update` + `Archive` + `Bookmark` commands, `[Authorize]` per endpoint
- [ ] I1 — wire the missing filters (country, deadline range, tags, funded-only, level)
- [ ] I2 — route prefix `api/scholarships`, inherit `ControllerBase`
- [ ] I3 — populate `OwnerCompanyName`/`Status`/`FundingType`/`TargetLevel` in the projection
- [ ] I4 — tie-break sort on `Id`
- [ ] I5 — drop the 409 throw on closed scholarships
- [ ] I7 — fix the malformed GUID in the integration test
- [ ] T-011 — get coverage above the 70% gate (constitution §V)
- [ ] CI green

I3, I4, I8, N1..N6 can land in a follow-up if time is tight. B1..B5 + I1 + I2 + I7 must ship in this PR before it can be merged.

You've got the right shape, Norra — the issues here are scope (most of the slice is missing) and the rebase, not architecture. Knock those out and we're in good shape.
