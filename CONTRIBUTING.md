# Contributing to ScholarPath

Thanks for contributing! This project is spec-driven: every change traces back to the SRS (FR-xxx) and a user story (US-xxx). The constitution at `.specify/memory/constitution.md` is the source of truth.

## Ground rules

1. **One module at a time.** Work within your owner scope (see README). If you need a cross-module change, open an issue first.
2. **Spec → Code.** Read `.specify/specs/PB-xxx-.../spec.md` + `plan.md` + `tasks.md` before coding.
3. **EN + AR parity.** Untranslated user-facing strings are a hard block.
4. **Security-first.** Never commit `.env` or `secrets.json`. Never log passwords, tokens, or PII.

## Branches

- Format: `feat/PB-xxx-short-slug`, `fix/PB-xxx-bug-slug`, `chore/description`, `docs/description`
- One branch = one PR = one conceptual change.
- Rebase onto `main` before opening a PR. No merge commits on feature branches.
- **`main` is protected** — direct pushes from non-admins are blocked. Every change goes through a Pull Request. Your PR must pass:
  - CI green
  - At least one CODEOWNERS approval (GitHub auto-assigns based on the paths you touch)
  - All conversations resolved
- Force push and branch deletion on `main` are disabled for everyone except the team lead.

## Commits

Conventional Commits:

- `feat(auth): add password reset flow (US-006)`
- `fix(bookings): prevent double-capture on webhook replay (FR-195)`
- `refactor(profiles): split completeness calc into its own service`
- `docs: update AUTH.md with SSO sequence diagram`
- `test(applications): cover reapply-after-withdrawal edge case`
- `feat(analytics): executive dashboard + RLS (PB-015 US-160)`
- `feat(dbt): silver layer models for applications + payments (PB-016)`

Every commit message references the FR or US number when applicable.

## Pull request checklist

Paste this into your PR description:

```markdown
## What / Why
<1–2 sentences>

## SRS traceability
- FR-xxx, FR-yyy
- US-xxx

## Acceptance
- [ ] Tests added for new logic
- [ ] Coverage ≥70% in affected module
- [ ] EN + AR translations added
- [ ] No new warnings in `dotnet build`
- [ ] `npm run typecheck` + `npm run lint` green
- [ ] Manual smoke test against `/scalar/v1` or the UI
- [ ] `docs/` updated if public API changed
```

## Running tests locally

### Backend
```bash
cd server
dotnet test                         # unit + integration
dotnet test --filter Category=Unit  # unit only
dotnet format --verify-no-changes   # style check
```

### Frontend
```bash
cd client
npm run typecheck
npm run lint
npm run test              # Vitest unit
npm run test:e2e          # Playwright
npm run build             # production bundle check
```

## Code style

- Backend: `dotnet format` (EditorConfig enforced)
- Frontend: Prettier + ESLint (configured in the repo)
- Imports sorted; unused imports removed (`eslint --fix` handles)
- No `any` in TypeScript — use `unknown` and narrow, or import the generated type

## Bug reports / feature requests

- Open an issue with the `bug` or `enhancement` label
- Reference the affected `PB-xxx` module
- Include repro steps, expected vs actual, and logs

## Team

| Role | Owner | Modules |
|------|-------|---------|
| Team lead + architect + AI + Admin + Audit | @ma7moudalysalem | PB-008, PB-011, PB-012 |
| Analytics lead + Data warehouse + AI Economy + Realtime | @ma7moudalysalem | PB-016 (lead), PB-017, PB-018, INFRA |
| Auth + Profile + Notifications + Community + Resources | @Madiha6776 | PB-001, PB-002, PB-007, PB-009, PB-010 |
| Scholarships + Applications + Payments + Profit Share | @norra-mmhamed | PB-003, PB-004, PB-013, PB-014 |
| Consultant Booking + Power BI Analytics | @TasneemShaaban | PB-006, PB-015, parts of PB-016/PB-018 |
| Company Review + Data Engineering (moderate) | @yousra-elnoby | PB-005, parts of PB-016 (CDC, Silver dbt, DQ) |

## Analytics contributions (PB-015 .. PB-018)

- The `analytics/` top-level folder hosts Power BI assets, dbt projects, and ADF ARM templates.
- dbt models live in `analytics/dbt/models/{staging,silver,marts}/` — see `docs/ANALYTICS.md`.
- Power BI `.pbix` files stay binary in git; keep them small and export `.pbit` templates alongside when you change data contracts.
- Every analytics artifact carries an owner tag in the file header so CODEOWNERS picks up the review.
- Never commit real Power BI embed tokens, Azure SAS keys, or connection strings — they go in Key Vault and are injected via environment variables.

Sprint planning lives on the GitHub Projects board.
