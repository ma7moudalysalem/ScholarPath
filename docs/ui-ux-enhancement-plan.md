# ScholarPath — UI/UX Enhancement Plan (Session Handoff)

> **Purpose.** This is a self-contained brief for a session dedicated to making
> ScholarPath's UI/UX consistent, polished, accessible, and production-grade.
> Execute it **phase by phase**, in order. Each phase has concrete tasks and a
> Definition of Done. Do not skip the QA gates.

---

## 0. Project context — read this first

**Stack**
- **Client:** React 19, Vite 8 (rolldown), TypeScript, Tailwind CSS **v4**,
  `react-i18next` (English + Arabic, Arabic is **RTL**), `zustand` (with
  `persist`), `@tanstack/react-query`, `@radix-ui/react-dialog`, SignalR, `zod`.
- **Server:** .NET 10 API (not in scope for UI work, but don't break its contracts).
- Repo: `client/` (SPA) and `server/` (API).

**Where things live**
- Design tokens: **`client/src/theme/globals.css`** — the single source of truth.
- Tailwind v4: configured by the Vite plugin in `client/vite.config.ts` — there is
  **no `tailwind.config.js`**; tokens live in `globals.css`.
- Pages: **53 files** under `client/src/pages/` (`admin`, `auth`, `chat`,
  `community`, `company`, `consultant`, `notifications`, `profile`, `public`,
  `student`).
- Components: `client/src/components/{ai,application,chat,common,community,company,layout}`.
- Routes: `client/src/routes/router.tsx`.
- Locales: `client/src/locales/{en,ar}/*.json` — 24 namespaces.

**Build & verify (run from `client/`)**
- Type-check: `npx tsc -p tsconfig.app.json --noEmit`
- Lint: `npm run lint`  (this is `eslint . --max-warnings 0` — zero warnings allowed)
- Build: `npm run build`
- All three MUST be green before every commit.

**Branch & deploy**
- Work on the `integration` branch.
- Publish: `git checkout main && git merge --ff-only integration && git push origin main && git checkout integration`
- Deploy: GitHub Actions → `Actions → Deploy → Run workflow → production`
  (or `gh workflow run deploy.yml --ref main -f environment=production`).
- **Commit rule (CLAUDE.md):** never add a `Co-Authored-By: Claude` trailer.

**Live URLs**
- Client: `https://polite-pebble-0c7c9b00f.7.azurestaticapps.net`
- API: `https://app-scholarpath-prod-api.azurewebsites.net`

**Hard constraints**
- Every screen must work in **English + Arabic (RTL)** and **light + dark** mode.
- Do not change API request/response shapes or break existing flows.
- This is a graduation project — keep changes reviewable and the build always green.

---

## 1. The headline problem — design-system fragmentation

Three different colour systems currently coexist. This is the #1 thing to fix.

| System | Where | Status |
|--------|-------|--------|
| **Design tokens** (`bg-bg-elevated`, `text-text-primary`, `border-border-subtle`, `brand-500`, `text-on-brand`) | Most pages — scholarships, AI, community, admin, applications | ✅ Correct — the target |
| **Hardcoded hex** (`#1d1d1f`, `#f5f5f7`, `#2563eb`, `#4b5563`, `#e5e7eb`…) | The whole **consultant/bookings module** (8 files, see §12) | ❌ No dark mode, off-brand |
| **`slate-*` / `primary-*`** | `client/src/pages/company/Dashboard.tsx` | ❌ Inconsistent palette |

**Goal:** every page uses the one token system from `globals.css`. No raw hex, no
stray `slate-*`, in any component or page.

---

## 2. Phase 1 — Lock the design system

1. Read `client/src/theme/globals.css`. Inventory **every** token: colours
   (background, text, border, brand), spacing, radius, shadow, typography.
2. Confirm every token has both a **light** and a **dark** value.
3. Add the **semantic** tokens if missing — `success`, `warning`, `danger`,
   `info` (some components referenced undefined `warning-500` / `danger-500`).
   Either add them, or standardise on `emerald-*` / `amber-*` / `rose-*` / `sky-*`
   and document that choice.
4. Define a typography scale (display / h1–h4 / body / caption) and a spacing
   rhythm; document both.
5. Update `docs/DESIGN.md` with the final, canonical token + scale reference.

**Definition of Done:** one documented palette + scale; a developer never needs a
raw hex value again.

---

## 3. Phase 2 — Component library

1. Audit `client/src/components/common/`.
2. Ensure these primitives exist — tokenised, dark-mode-correct, RTL-aware,
   accessible (visible `focus-visible` ring, correct ARIA):
   `Button` (variants primary / secondary / ghost / danger × sizes sm / md / lg),
   `Input`, `Textarea`, `Select`, `Card`, `Badge`/`Pill`, `Dialog`/`Modal`,
   `Skeleton`, `EmptyState`, `Spinner`, `Tabs`, `Table`, `Pagination`, `Avatar`,
   `Tooltip`, `DropdownMenu`. (Toasts already use `sonner` — standardise usage.)
3. **Fix `EmptyState.tsx`** — it renders developer metadata (`owner`, `module` →
   e.g. "Owner: @yousra-elnoby") visible to end users. Remove that from the
   rendered output; the Company Dashboard's empty-reviews state currently leaks it.
4. Replace ad-hoc markup across pages with these primitives.

**Definition of Done:** a documented primitive set; pages compose primitives
instead of bespoke `<div>`/`<button>` markup.

---

## 4. Phase 3 — Page-by-page conversion (largest effort)

Convert every page to the token system. Priority order:

1. **Consultant/bookings module** — the 8 hardcoded-hex files in §12. Replace
   every `#hex` with a token; verify light + dark.
2. **`company/Dashboard.tsx`** — replace `slate-*` / `primary-*` with tokens.
3. **Full sweep** — `grep -rE "#[0-9a-fA-F]{6}" client/src` and
   `grep -rn "slate-" client/src` until both return nothing meaningful.

For **each page**, audit and fix: token usage · dark mode · RTL · responsive
(360/768/1024/1440) · loading + empty + error states · accessibility.

Track progress in a checklist table (page × the 6 criteria).

**Definition of Done:** zero raw hex / `slate-*`; every page passes the 6-point audit.

---

## 5. Phase 4 — RTL & i18n polish

1. Replace physical CSS props with **logical** ones everywhere:
   `ml-`/`mr-` → `ms-`/`me-`, `pl-`/`pr-` → `ps-`/`pe-`, `left-`/`right-` →
   `start-`/`end-`, `text-left`/`text-right` → `text-start`/`text-end`.
2. Mirror directional icons (arrows, chevrons) using `i18n.dir() === "rtl"`.
3. Verify **EN/AR key parity** across all 24 locale namespaces (flatten both
   JSON trees and diff — no missing keys either way).
4. Locale-aware number / date / currency formatting.
5. Walk every page in Arabic — check alignment, no clipped or overflowing text.

**Definition of Done:** every page is visually correct in RTL; locale parity is clean.

---

## 6. Phase 5 — Accessibility (target WCAG AA)

- Keyboard: every interactive element reachable and operable; visible
  `focus-visible` rings; logical tab order; modals trap focus and close on `Esc`.
- Semantics: landmark regions, correct heading hierarchy, ARIA labels on
  icon-only buttons, every form control labelled.
- Contrast: AA (4.5:1 for text) in **both** light and dark.
- Honour `prefers-reduced-motion`.
- Do a screen-reader pass on the core flows.

**Definition of Done:** keyboard-only and screen-reader users can complete every
core flow; contrast passes AA.

---

## 7. Phase 6 — Responsive

- Audit at 360 / 768 / 1024 / 1440 px.
- Touch targets ≥ 44 px; working mobile navigation (drawer); data tables collapse
  to cards on small screens; no horizontal scrolling anywhere.

**Definition of Done:** every page is usable and clean on a phone.

---

## 8. Phase 7 — Polish & micro-interactions

- Consistent hover / active / focus transitions.
- Skeleton loaders for every async region (replace bare spinners where a skeleton
  fits the layout).
- Optimistic UI for mutations where it improves feel.
- Consistent toast usage, friendly empty states, smooth page transitions,
  consistent `lucide-react` iconography and spacing rhythm.

---

## 9. Phase 8 — Performance

- Analyse the `npm run build` output; review chunk sizes.
- Verify route-level code-splitting (routes are already `lazy`).
- Optimise images (profile photos, hero art); set a sensible font-loading strategy.
- Remove dead code and unused locale keys.
- Run Lighthouse — target ≥ 90 for Performance / Accessibility / Best Practices.

---

## 10. QA gates & acceptance

- Before **every** commit: `tsc` + `npm run lint` + `npm run build` all green.
- **Verification matrix:** each page × {English, Arabic} × {light, dark} ×
  {mobile, desktop}.
- Cross-browser: Chrome, Firefox, Safari, Edge.
- No functional regressions in: auth + Google/Microsoft SSO, scholarship
  browse/detail/apply, AI (recommendations / eligibility / chat), consultant
  booking + Stripe payment, community, admin.

---

## 11. Execution discipline

- Small, coherent commits — one phase or one module per commit.
- Verify (tsc + lint + build) before each commit; **no `Co-Authored-By: Claude`** trailer.
- After each phase: publish to `main` and deploy via the Deploy workflow, then
  smoke-test the live site.
- Never break a working flow to make something prettier — behaviour first.

---

## 12. Known specific issues to fix

**Hardcoded-hex files (Phase 3, priority 1):**
- `client/src/pages/consultant/ConsultantAvailability.tsx`
- `client/src/pages/consultant/ConsultantBookingDetails.tsx`
- `client/src/pages/consultant/ConsultantBookings.tsx`
- `client/src/pages/student/BookingCheckout.tsx`
- `client/src/pages/student/ConsultantDetail.tsx`
- `client/src/pages/student/ConsultantsBrowse.tsx`
- `client/src/pages/student/StudentBookingDetails.tsx`
- `client/src/pages/student/StudentBookings.tsx`

**Other:**
- `client/src/pages/company/Dashboard.tsx` — `slate-*` / `primary-*` → tokens.
- `client/src/components/common/EmptyState.tsx` — stop rendering `owner`/`module`
  developer metadata to end users.

**Out of UI scope but worth a follow-up (server-side cleanup):**
- Dead stub classes never registered in DI — `StubAiService`,
  `StubBlobStorageService`, `StubNotificationDispatcher`, `StubAuditService` in
  `server/src/ScholarPath.Infrastructure/Services/StubServices.cs` — safe to delete.
- `StubPasswordHasher` is misnamed — it delegates to the real ASP.NET Identity
  hasher; consider renaming for clarity.
- Antivirus upload scanning runs as `NoOpFileScanService` unless a ClamAV daemon
  is provisioned (`FileScanning:Enabled`).

---

## 13. Suggested sequencing

1. Phase 1 (design system) + Phase 2 (`EmptyState` fix + primitives) — foundation.
2. Phase 3 — consultant/bookings module, then `company/Dashboard`, then the sweep.
3. Phases 4–6 (RTL/i18n, a11y, responsive) — can interleave per page.
4. Phase 7 (polish) + Phase 8 (performance).
5. Phase 10 QA throughout; final full matrix pass before the last deploy.

Deliver incrementally — each phase shippable on its own.
