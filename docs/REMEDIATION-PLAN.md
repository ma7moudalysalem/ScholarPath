# ScholarPath — Remediation & Clarity Plan

**Owner:** @ma7moudalysalem (team lead) · **Created:** 2026-06-02 · **Status:** living document

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
- **Tests**: 518 backend unit tests green (per `docs/reviews/calculations-audit-2026-05-21.md`).

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

### 🔴 P0 — Repo & documentation clarity (cheapest win, do first)

| # | Item | Where | Effort |
|---|---|---|---|
| P0-1 | **Branch sprawl**: ~40 `worktree-agent-*` branches + `.claude/worktrees/`, `backup/*`, `pr-24`. Prune worktrees, archive/delete stale branches. **Remote deletion is gated on the lead's explicit OK** (see Appendix A). | git | 1–2 h |
| P0-2 | **Orphan branches are superseded, not pending.** `main` independently implements PB-001/002/010 (auth incl. change-password, profile, notifications). Madiha's `feat/PB-001-*`, `feat/PB-002-profile`, `feat/PB-010-notifications` add nothing `main` lacks → **archive & close**, don't rescue. Backup refs already exist under `backup/*`. | git + `docs/reviews/madiha-orphan-branches-review.md` | 1 h |
| P0-3 | **`ROADMAP.md` + `HANDOFF.md` are stale (May).** They call Stripe/SSO/Hangfire/AI/40 pages "stub / awaiting wiring" — all now real. Add a "Status update 2026-06-02" banner pointing to this plan + `TECHNICAL-BRIEF.md`. **Important before the defense.** | `docs/ROADMAP.md`, `docs/HANDOFF.md` | 1 h |
| P0-4 | **`AUTH.md` JWT drift**: says "HMAC-SHA256 scratch key, migrate to RS256". Code/`appsettings.json:42` is **already RS256** with a Key-Vault-or-local key provider. Correct the doc. | `docs/AUTH.md:93` | 15 m |

### 🟠 P1 — Configuration & operations (needed for a "real" demo / deploy)

| # | Item | Where | Effort |
|---|---|---|---|
| P1-1 | **Azure OpenAI unconfigured** → `knowledge-base/rebuild` returns **503**, real AI chat/RAG unavailable. Default `Ai:Provider="Stub"`. Decide: (a) configure Azure OpenAI (needs lead's Azure access), or (b) set `Ai:Provider="Local"` for the demo and document it. Runbook: `docs/runbooks/azure-openai-setup.md`. | `appsettings.json:96`, `KnowledgeBaseIndexer.cs` | 1–3 h (a) / 15 m (b) |
| P1-2 | **Pre-prod secrets/services**: Key Vault URIs (JWT + field-encryption), Stripe **live** keys, SendGrid key, ACS connection string, Power BI service principal — all placeholders. Tracked for deploy phase. | `appsettings.json` | deploy phase |
| P1-3 | **Verify global authorization.** `AUTH.md` says a global `[Authorize]` fallback policy "will be added before production." Confirm every controller except `AuthController` is covered, or add the fallback policy in `Program.cs`. | `Program.cs` | 1 h |

### 🟢 P2 — Polish & consistency (the "feels finished" layer)

| # | Item | Where | Effort |
|---|---|---|---|
| P2-1 | **Admin dashboard greeting** uses the section title as the name instead of the admin's `firstName`. Make it consistent with the other three role dashboards. | `AdminDashboard.tsx:139` | 15 m |
| P2-2 | **i18n drift sweep**: confirm no key exists in EN but missing in AR (or vice-versa) across the 26 namespaces; flag any hardcoded English bypassing `t()`. | `client/src/locales/*` | 1–2 h |
| P2-3 | **Frontend test coverage**: currently smoke/E2E only. Add component tests for the critical flows (login, onboarding, checkout) toward the constitution's ≥70%. | `client/src/test/` | ongoing |

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
