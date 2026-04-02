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

## Commits

Conventional Commits:

- `feat(auth): add password reset flow (US-006)`
- `fix(bookings): prevent double-capture on webhook replay (FR-195)`
- `refactor(profiles): split completeness calc into its own service`
- `docs: update AUTH.md with SSO sequence diagram`
- `test(applications): cover reapply-after-withdrawal edge case`

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

| Role | Owner |
|------|-------|
| Team lead + architect + AI | @ma7moudalysalem |
| Consultant booking + profit share | @TasneemShaaban |
| Auth + profile + notifications | @Madiha6776 |
| Scholarships + applications + payments | @norra-mmhamed |
| Community + chat + resources + company review | @yousra-elnoby |

Sprint planning lives on the GitHub Projects board.
