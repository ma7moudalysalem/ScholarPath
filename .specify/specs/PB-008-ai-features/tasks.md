# PB-008 — Tasks

**Owner**: @ma7moudalysalem  •  **Est**: 23 pts  •  **Iteration**: 4

## Backend
- [ ] T-001 — `IAiService` interface (Application) + `StubAiService` impl (default)
- [ ] T-002 — `AiInteraction` entity + config
- [ ] T-003 — `GenerateRecommendationsCommand` (event-triggered) + cache
- [ ] T-004 — `GetMyRecommendationsQuery` reading cache
- [ ] T-005 — `CheckEligibilityCommand`
- [ ] T-006 — `AskChatbotCommand` with session context
- [ ] T-007 — (Optional) `OpenAiService` provider wiring via config
- [ ] T-008 — PII redaction helper
- [ ] T-009 — Daily cost limit per user (simple counter)
- [ ] T-010 — Unit + integration tests

## Frontend
- [ ] T-011 — `AiRecommendations.tsx` widget on Student dashboard
- [ ] T-012 — `EligibilityChecker.tsx` modal on scholarship detail
- [ ] T-013 — `Chatbot.tsx` slide-over with streaming (when real provider active)
- [ ] T-014 — `AiDisclaimer` reusable component on every AI output
- [ ] T-015 — `MatchScoreBadge` + `EligibilityBreakdown`
- [ ] T-016 — Arabic copy review

## QA
- [ ] T-017 — E2E: recommendations load (stub); eligibility returns per-criterion; chatbot Q/A round trip

## Done criteria
Stub mode passes all tests; real provider switchable via `Ai:Provider` config.
