## What / Why

<!-- 1–2 sentences. What does this PR do, and why? -->

## SRS traceability

- FRs: FR-xxx, FR-yyy
- User stories: US-xxx
- Epic: PB-xxx

## Checklist

- [ ] Tests added/updated for new logic
- [ ] Module coverage stays ≥70%
- [ ] EN + AR translations added for any user-facing strings
- [ ] `dotnet build` (server) — 0 warnings, 0 errors
- [ ] `npm run typecheck` + `npm run lint` + `npm run test` (client) — all green
- [ ] Manual smoke test via `/scalar/v1` (API) or dev UI
- [ ] Docs updated (`docs/*.md`) if public contract changed
- [ ] No secrets / tokens / PII in code or logs

## Screenshots / demo (UI changes only)

<!-- Drag in before/after screenshots. For interactions, record a <10s clip. -->

## Deployment notes

<!-- Migrations? Feature flags? Environment variables? Config changes? -->
