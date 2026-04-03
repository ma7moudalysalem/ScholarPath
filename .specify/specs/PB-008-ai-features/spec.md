# PB-008 — AI Features

**Owner**: @ma7moudalysalem • **Priority**: High • **Iteration**: 4 • **Est**: 23 pts

## Problem statement

AI helps Students discover the right scholarships (personalized match scores + explanations), check eligibility against specific listings, and answer study-abroad questions via a chatbot. Every response carries a disclaimer. AI provider is abstracted behind `IAiService` — OpenAI or Azure OpenAI wiring is teammate choice.

## User stories

| ID | Story | Size |
|----|-------|------|
| US-082 | Personalized scholarship recommendations | 5pt |
| US-083 | Match score + explanation | 4pt |
| US-084 | Eligibility checker | 4pt |
| US-085 | Per-criterion eligibility feedback | 4pt |
| US-086 | Ask AI chatbot questions | 4pt |
| US-087 | AI disclaimer on each response | 2pt |

## Functional requirements

FR-113 .. FR-121

## Acceptance criteria

1. **Recommendations** — `GET /api/ai/recommendations` returns top-N scholarships ranked by match score with 1-sentence explanation each. Recomputed async on `ProfileUpdatedEvent`; cached per user for 1h.
2. **Match score** — 0–100 integer; computed via profile vs scholarship attributes (country, level, field, GPA, funding type match, tags overlap, deadline feasibility).
3. **Eligibility checker** — `POST /api/ai/eligibility` with `{scholarshipId}` returns per-criterion matches with `{criterion, studentValue, listingRequirement, match: "yes|partial|no|unknown"}`.
4. **Chatbot** — `POST /api/ai/chat` with message + session ID; maintains session context (last 10 turns); streaming response via SignalR.
5. **Disclaimer** — Every AI response includes `disclaimer: "AI-generated guidance. Verify with official sources before acting."`
6. **Stub mode** — If `Ai:Provider=stub` (default in dev), returns canned responses so module works without API keys.

## Non-goals

- AI-generated essay/cover-letter writing (maybe v2)
- AI-translated listings
- Multi-model routing (single provider in v1)
- RLHF / fine-tuning
