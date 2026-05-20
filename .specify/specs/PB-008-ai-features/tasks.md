# PB-008 — Tasks

**Owner**: @ma7moudalysalem  •  **Est**: 23 pts  •  **Iteration**: 4
**Status**: ✅ full stack shipped (backend + frontend + tests); QA E2E partial pending seeded staging.

## Backend
- [x] T-001 — `IAiService` interface (Application) + default impl  *(`16d9046` + `659fea0` — `LocalAiService` replaces the empty stub with real profile-match scoring)*
- [x] T-002 — `AiInteraction` entity + config  *(shipped with initial migration)*
- [x] T-003 — `GenerateRecommendationsCommand` (event-triggered) + cache  *(`db8adb9` — persists interaction row, hydrates scholarship titles, counts against cost budget)*
- [x] T-004 — `GetMyRecommendationsQuery` reading cache  *(`7d87794` — reads last successful AiInteraction within max-age, 204 on miss)*
- [x] T-005 — `CheckEligibilityCommand`  *(`db8adb9` — per-criterion yes/partial/no/unknown)*
- [x] T-006 — `AskChatbotCommand` with session context  *(`db8adb9` — sessionId roundtripped, scoped system prompt)*
- [x] T-007 — `OpenAiService` provider wiring via config  *(`7b3fa6b` — real gpt-4o-mini provider, swap via `Ai:Provider` config; graceful fallback to LocalAiService on network failure)*
- [x] T-008 — PII redaction helper  *(`db8adb9` — source-generated regex: email / credit card / phone, applied before persist or provider dispatch)*
- [x] T-009 — Daily cost limit per user  *(`db8adb9` — `AiCostGate` enforces rolling 24h budget, default $1 USD)*
- [x] T-010 — Unit + integration tests  *(16 tests: 8 PII redaction + validator + 5 AiCostGate integration, all green)*

## Frontend
- [x] T-011 — `AiRecommendations.tsx` widget on Student dashboard  *(`8a95758` — GET-first cached, auto-regen on miss, explicit refresh button POSTs)*
- [x] T-012 — `EligibilityChecker.tsx` modal on scholarship detail  *(`8a95758` — per-criterion icons, summary, disclaimer; enhanced: `ScholarshipDetail` now has a "Check Eligibility" deep-link → `/student/ai?tab=eligibility&sid=…` that pre-selects the scholarship and skips the manual search step — `161bbac`)*
- [x] T-013 — `Chatbot.tsx` slide-over  *(`8a95758` — turn-based UI, sessionId preserved across turns, cost/token hint per assistant reply)*
- [x] T-014 — `AiDisclaimer` reusable component on every AI output  *(`8a95758`)*
- [x] T-015 — `MatchScoreBadge` + eligibility breakdown  *(`8a95758` — tiered colors 0-100; breakdown embedded in eligibility modal)*
- [x] T-016 — Arabic copy review  *(full AR `ai.json` namespace)*

## QA
- [ ] T-017 — E2E: recommendations load (stub); eligibility returns per-criterion; chatbot Q/A round trip  *(🟡 partial — Playwright route-guard smoke in `ai.spec.ts`; full-flow E2E needs seeded staging env)*

## Done criteria
- [x] Stub mode passes all tests (52/52 green)
- [x] Real provider switchable via `Ai:Provider` config, no code change
- [x] Daily cost cap enforced (default $1/user/24h rolling)
- [x] PII never reaches a provider (redaction pass before dispatch)
