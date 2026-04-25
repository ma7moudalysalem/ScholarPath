# PB-017 — Implementation Plan

## Data model changes

New entity:
- `RecommendationClickEvent { Id, UserId, ScholarshipId, AiInteractionId (FK nullable), ClickedAt, Source (card/list/modal) }`
  - Indexed on `(UserId, ClickedAt)` and `(ScholarshipId, ClickedAt)`.
  - CDC-enabled (folds into `FactAiInteraction` as a join in PB-016 Gold).

Existing `AiInteractions` table already has everything else we need: `UserId`, `Feature`, `Provider`, `PromptTokens`, `CompletionTokens`, `CostUsd`, `StartedAt`, `CompletedAt`, `ErrorMessage`.

## Application
- New query: `LogRecommendationClickCommand(ScholarshipId, AiInteractionId?)`
  - Called by the frontend when a user clicks a recommendation card.
  - `[Auditable(AuditAction.Create, "RecommendationClick")]`.
- New query: `GetAiUsageSummaryQuery(from?, to?)` — admin read for the dashboards that do not have direct Power BI access.

## Infrastructure
- Audit sample pipeline (monthly): a Hangfire job selects 50 random chat prompts from the previous month, inserts them into `AiRedactionAuditSample` table for a human reviewer.
- Reviewer UI: admin page with 50 rows, each with the redacted prompt + four radio buttons (`clean`, `missed_email`, `missed_phone`, `missed_card`).

## Dashboards
Five Power BI widgets, all on the existing `AdminAnalytics` page or a new `/admin/analytics/ai-economy`:

1. Cost stacked-bar — feature × provider × day.
2. Budget-at-risk users — table sorted by `sum(CostUsd) / DailyUserCostLimitUsd` descending, red-flagged rows.
3. CTR funnel — `recommended → shown → clicked` counts + rates.
4. Token efficiency — box plots + top-10 prompts table.
5. Redaction audit — monthly miss rate trend + investigation table for flagged samples.

## API
- `POST /api/ai/recommendations/{scholarshipId}/click` — 204 on success
- `GET /api/admin/ai/usage-summary` — JSON for the dashboards (also backs Power BI when not using DirectQuery)
- `GET /api/admin/ai/redaction-audit` — paginated, Admin only
- `POST /api/admin/ai/redaction-audit/{sampleId}/verdict` — admin submits verdict

## Frontend
- Extend `AiRecommendations.tsx` to call `LogRecommendationClickCommand` on card click.
- New page `client/src/pages/admin/AiEconomy.tsx`.
- New page `client/src/pages/admin/RedactionAudit.tsx`.

## Tests
- Unit: `LogRecommendationClickCommand` handler idempotency (double-click same card within 500ms is one event).
- Integration: sample job produces exactly 50 rows, no duplicates.
- E2E: admin can mark a sample and the dashboard updates.

## Dependencies
- PB-008 (AI features — source of AiInteractions data)
- PB-011 (Admin portal — hosts the new pages)
- PB-016 (Gold layer — FactAiInteraction joins RecommendationClickEvent)
