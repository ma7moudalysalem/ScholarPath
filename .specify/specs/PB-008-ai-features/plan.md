# PB-008 — Implementation Plan

## Architecture touchpoints

### Domain
- `AiInteraction` (session, feature, prompt, response, tokens, cost)
- Enums: `AiFeature { Recommendation, Eligibility, Chatbot }`, `AiProvider { Stub, OpenAi, AzureOpenAi }`

### Application (`server/src/ScholarPath.Application/AI/`)
- **Commands**: `AskChatbotCommand`, `GenerateRecommendationsCommand` (on event), `CheckEligibilityCommand`
- **Queries**: `GetMyRecommendationsQuery`, `GetChatHistoryQuery`
- **Services interface** (Application): `IAiService` — methods: `GenerateRecommendationsAsync`, `CheckEligibilityAsync`, `AskAsync`

### Infrastructure
- `Services/StubAiService.cs` — canned responses (default)
- `Services/OpenAiService.cs` — real provider (teammate adds)
- Caching: `IMemoryCache` for recommendations (1h per user)
- Listens to `ProfileUpdatedEvent` + `ScholarshipPublishedEvent` → invalidate cache

### API
- `GET  /api/ai/recommendations`
- `POST /api/ai/eligibility`
- `POST /api/ai/chat`
- `GET  /api/ai/chat/history`

### Frontend
- Student: `AiRecommendations.tsx` on dashboard, `EligibilityChecker.tsx` modal on scholarship detail, `Chatbot.tsx` slide-over
- Components: `MatchScoreBadge`, `EligibilityBreakdown`, `ChatMessage`, `AiDisclaimer`

### Tests
- Unit: `StubAiService` returns expected canned shapes; cache invalidation on events
- Integration: recommendations endpoint returns `MatchScore` + `Explanation`
- E2E: open chatbot, ask, receive stub response with disclaimer visible

## Dependencies
PB-001 (identity), PB-002 (profile data), PB-003 (scholarship data)

## Risks
1. Token cost explosion — enforce per-user daily limit; log cost per interaction
2. PII leakage to provider — redact email/phone from prompts
3. Hallucinated scholarship claims — always link to the actual listing
