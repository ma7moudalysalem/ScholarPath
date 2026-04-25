# PB-017 — AI Economy Analytics

**Owner**: @ma7moudalysalem • **Priority**: High • **Iteration**: 3 • **Est**: 21 pts

## Problem statement

The AI features shipped in PB-008 introduce a new operational concern: spend, quality, and compliance must be observable. PB-017 turns every row already being written to `AiInteractions` into a FinOps + quality dashboard: who is consuming what, how much it costs, whether recommendations are actually clicked, how large prompts get, and whether PII redaction is holding up under random audit.

## User stories

US-174 .. US-178

## Functional requirements

FR-246 .. FR-258

## Acceptance criteria

1. **Cost breakdown** — daily, weekly, monthly USD split by feature (Recommendation / Eligibility / Chatbot) and provider (`LocalAiService` / `OpenAiService`).
2. **Budget alerting** — any user crossing 80% of their `DailyUserCostLimitUsd` for three consecutive days raises a red flag on the dashboard and emails the admins list.
3. **Recommendation CTR** — a new event `recommendation_clicked` is emitted from the frontend and persisted. Dashboard shows CTR overall and segmented by academic level + field. Target ≥ 15%.
4. **Token efficiency** — box-plots of prompt and completion token counts per (feature, provider). Table of the 10 most-expensive prompts with prompt text redacted.
5. **PII redaction audit** — monthly human-reviewed sample of 50 chat prompts. Dashboard tracks miss rate. Target < 1%.

## Non-goals

- Automatic prompt optimization / prompt registry (v2).
- Real-time cost controls (covered by `AiCostGate` at call time — dashboards are the observability layer).
- A/B testing of prompts (v2).
