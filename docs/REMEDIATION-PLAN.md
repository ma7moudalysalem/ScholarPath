# ScholarPath — Remediation & Clarity Plan

**Owner:** @ma7moudalysalem (team lead) · **Created:** 2026-06-02 · **Last updated:** 2026-07-03 · **Status:** living document

> ✅ **Status update 2026-06-29 — system is fully deployed and operational.**
> All P0 and P1 items are resolved. Azure resources are ScholarPath-branded.
> Production is live with all integrations configured (Azure OpenAI, ACS, Stripe,
> SendGrid, Blob Storage, SQL, SSO). See resolved items below.

This plan is the single source of truth for "what is actually left to do" on `main`.
It was produced by auditing the live code, not the older planning docs. Where the
older docs (`ROADMAP.md`, `HANDOFF.md`, `AUTH.md`) disagree with the code, **the
code wins** and the doc is flagged below as stale.

---

## 0. Reality snapshot (what is actually true on `main`, 2026-06-02)

`main` is **not** a scaffold — it is a mature, integrated product:

- **~60 real routes** across Student / Company / Consultant / Admin (no `<EmptyState owner/specPath>` stubs left in the router).
- **Auth is complete**: Login, Register, Refresh, Logout, Forgot/Reset, **Change-password**, SSO (Google/Microsoft), SwitchRole, SelectRole, Email-change — all real MediatR handlers.
- **Payments**: Stripe hold/capture/refund/payout, profit-share split, free-mode master switch, webhook idempotency.
- **AI**: Local + OpenAI + Azure providers behind `Ai:Provider`, per-user cost gate, RAG, PII redaction.
- **Cross-cutting**: audit log, GDPR export/delete, notifications + SignalR, community/chat, resources, analytics pages.
- **Tests**: 842 backend unit tests green (up from the 518 counted in `docs/reviews/calculations-audit-2026-05-21.md`, before the P0–P2 security remediation added coverage).

**Conclusion:** the remaining work is **clarity, configuration, and polish — not missing features.**

---

## 1. Verified clean — no action required

These were audited and found correct. Listed so we don't re-open settled questions.

| Area | Evidence |
|---|---|
| Booking financial math (cents, rounding, split re-sums to gross) | `calculations-audit-2026-05-21.md` §1 |
| No-show refund logic (manual + sweep, idempotent) | `calculations-audit-2026-05-21.md` §2 |
| **PartiallyRefunded earnings (audit §3.1)** — **now fixed** | `CancelBookingCommandHandler.cs:170-179`, `RefundPaymentCommand.cs:149-152`, `StripePayoutJob.cs:34` all recompute the split off the retained amount and include `PartiallyRefunded` in payout |
| **Refund analytics exclude PartiallyRefunded (audit §3.2)** — **now fixed** | `GetProfitShareAnalyticsQuery.cs:40` now includes `PartiallyRefunded` |
| Timezone handling, slot-overlap detection | `calculations-audit-2026-05-21.md` §4–5 |
| GDPR export/erasure | `docs/reviews/gdpr-audit.md` — all 5 findings fixed |
| Login welcome-by-name | **Done 2026-06-02** — `Login.tsx`, `SsoCallback.tsx`, `auth.json` (EN/AR) |

---

## 2. Open items — prioritized

### 🔴 P0 — Repo & documentation clarity

| # | Item | Status |
|---|---|---|
| P0-1 | **Branch sprawl** — `worktree-agent-*` + Madiha's orphan branches | ✅ **DONE 2026-06-29** — 6 stale remote branches deleted |
| P0-2 | **Orphan branches superseded** — `feat/PB-001-*`, `feat/PB-002-profile`, `feat/PB-010-notifications` | ✅ **DONE 2026-06-29** — all deleted |
| P0-3 | **`ROADMAP.md` + `HANDOFF.md` stale (May)** | ✅ **DONE 2026-06-02** — status banners added pointing to this plan |
| P0-4 | **`AUTH.md` JWT drift** — was described as HMAC, already RS256 in code | ✅ **DONE** — `docs/AUTH.md:93` already says RS256 |

### 🟠 P1 — Configuration & operations

| # | Item | Status |
|---|---|---|
| P1-1 | **Azure OpenAI** → knowledge-base/rebuild 503 | ✅ **RESOLVED 2026-06-02** — `ai-scholarpath-prod` (GlobalStandard gpt-4o-mini + text-embedding-3-small) wired in App Service; rebuild → 200, 849 docs indexed, chat live |
| P1-2 | **Pre-prod secrets** — KV, Stripe, SendGrid, ACS, SSO | ✅ **DONE 2026-06-29** — all secrets configured in Azure Key Vault + App Service + GitHub Actions |
| P1-3 | **Global authorization** — fallback policy check | ✅ **VERIFIED** — 26 controllers, each explicitly authorized: 21 with class-level `[Authorize]` (4 of them role-scoped `[Authorize(Roles="Admin,SuperAdmin")]`), 3 with class-level `[AllowAnonymous]` for webhook/public endpoints (`WebhooksController`, `MeetingRecordingWebhookController`, `StatusController`), and `AuthController` + `ScholarshipController` gating per action at the method level. `DiagnosticsController` got explicit class attributes in SEC-10; `AuthController` intentionally stays method-level. Pattern is correct; no fallback policy needed. |

### 🟢 P2 — Polish & consistency

| # | Item | Status |
|---|---|---|
| P2-1 | **Admin dashboard greeting** uses title not firstName | ✅ **DONE** — `AdminDashboard.tsx:141` already uses `firstName` with title fallback |
| P2-2 | **i18n drift sweep** | ✅ **VERIFIED 2026-06-29** — full audit found all keys in sync across EN/AR; no missing keys |
| P2-3 | **Frontend test coverage** | 🟡 ongoing — E2E smoke tests in place; component tests deferred post-defense |

### ✅ Copilot review follow-ups

| PR | Issue | Fix PR | Status |
|---|---|---|---|
| [#37](https://github.com/ma7moudalysalem/ScholarPath/pull/37) | FTS empty-term guard, RequiredDocs normalize, stable list key, localized aria-label | [#39](https://github.com/ma7moudalysalem/ScholarPath/pull/39) | ✅ Merged 2026-06-29 |
| [#34](https://github.com/ma7moudalysalem/ScholarPath/pull/34) | Blank entries in `studentCountries` eligibility check | [#40](https://github.com/ma7moudalysalem/ScholarPath/pull/40) | ✅ Merged 2026-06-29 |

---

## Appendix A — Branch-cleanup batch (GATED — do not run without the lead's OK)

> Destructive + touches the remote. Backups under `backup/*` already exist; create any missing ones first.

```bash
# 1. Remove stale agent worktrees (safe — they are throwaway)
git worktree list
git worktree prune
# for each leftover: git worktree remove --force <path>

# 2. Delete local agent branches (after worktrees are gone)
git branch | grep 'worktree-agent-' | xargs -r git branch -D

# 3. Archive then delete the superseded orphan remotes (CONFIRM FIRST)
#    These are fully superseded by main — keep backup/* refs as the archive.
#    git push origin --delete feat/PB-001-logout-command feat/PB-001-forgot-reset-password \
#      feat/PB-002-profile feat/PB-010-notifications test/PB-001-PB-002-unit-tests
```

## Appendix B — Suggested GitHub issues (one per open item)

`P0-1 … P2-3` above map 1:1 to issues. Labels: `chore`/`docs`/`config`/`polish` + priority + owner.
Run `/code-review` or open via `gh issue create` once the lead approves the list.
