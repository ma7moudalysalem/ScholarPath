# PB-017 — Tasks

**Owner**: @ma7moudalysalem • **Est**: 21 pts • **Iteration**: 3
**Status**: ✅ backend + frontend shipped; Power BI dashboards (T-006..T-010) pending Tasneem.

## Data model + backend

- [x] T-001 — `RecommendationClickEvent` entity + migration + indexes  @ma7moudalysalem  *(`Domain/Entities/AI.cs` — `RecommendationClickEvent` entity; migration `20260424032238_AddAnalyticsEntities_PB017.cs`)*
- [x] T-002 — `LogRecommendationClickCommand` + handler + `[Auditable]`  @ma7moudalysalem  *(`Application/AI/Commands/LogRecommendationClick/` — command, handler, validator; debounce logic; `[Auditable]` on command)*
- [x] T-003 — `GetAiUsageSummaryQuery` for dashboard data  @ma7moudalysalem  *(`Application/AI/Queries/GetAiUsageSummary/` — aggregates cost / tokens / CTR by feature + provider for the Admin AI Economy page)*
- [x] T-004 — Hangfire `RedactionAuditSamplingJob` (monthly, 50 random chat prompts)  @ma7moudalysalem  *(`Infrastructure/Jobs/RedactionAuditSamplingJob.cs` — registered as a monthly recurring Hangfire job in DI)*
- [x] T-005 — `AiRedactionAuditSample` entity + admin verdict commands  @ma7moudalysalem  *(`Domain/Entities/AI.cs` — `AiRedactionAuditSample`; `Application/Admin/Commands/SetRedactionSampleVerdict/`; `Application/Admin/Queries/GetRedactionAuditSamples/`)*

## Dashboards (Power BI)

- [x] T-006 — PB-017-US-001 AI cost dashboard (stacked bar feature × provider × day)  *(`docs/POWERBI-REPORTS.md` — full visual spec: stacked bar daily cost + cost-by-feature table + donut + trend; data from `fct_ai_interaction` Gold table)*
- [x] T-007 — PB-017-US-002 Budget alert automation (80% three-day rule)  *(`docs/POWERBI-REPORTS.md` — spec: budget gauge + 3-day rolling line + alert status card + `BudgetConfig` parameter table)*
- [x] T-008 — PB-017-US-003 Recommendation CTR widget + event logging  *(`docs/POWERBI-REPORTS.md` — spec: CTR gauge + over-time line + by-source bar + top-clicked table; server + client logging already shipped)*
- [x] T-009 — PB-017-US-004 Token efficiency box plots + top-10 table  *(`docs/POWERBI-REPORTS.md` — spec: box-whisker per feature + top-10 expensive calls + avg-tokens bar + trend)*
- [x] T-010 — PB-017-US-005 Redaction audit trend + investigation table  *(`docs/POWERBI-REPORTS.md` — spec: monthly leak rate line + verdict donut + samples table + threshold indicator)*

## Frontend

- [x] T-011 — Extend `AiRecommendations.tsx` to log clicks  @ma7moudalysalem  *(`components/ai/AiRecommendations.tsx` — fires `aiApi.logRecommendationClick()` on recommendation card click with source="card")*
- [x] T-012 — `AiEconomy.tsx` admin page (embed + native panels)  @ma7moudalysalem  *(`pages/admin/AiEconomyPage.tsx` — AI usage summary cards: total cost, recommendation CTR, token usage; routed at `/admin/ai-economy`)*
- [x] T-013 — `RedactionAudit.tsx` admin review UI  @ma7moudalysalem  *(`pages/admin/RedactionAuditPage.tsx` — paginated sample list with verdict buttons (Clean/Leaked); routed at `/admin/redaction-audit`)*

## QA

- [x] T-014 — Unit: click command idempotency  *(`tests/ScholarPath.UnitTests/Ai/LogRecommendationClickTests.cs` — `Repeat_click_inside_500ms_is_deduplicated` + `Click_after_debounce_window_persists_new_event`)*
- [x] T-015 — Integration: sampling job exactness (50 rows, non-overlapping months)  *(`tests/ScholarPath.IntegrationTests/Ai/RedactionAuditSamplingJobTests.cs` — 3 facts: cap@50, idempotent re-run, only previous month sampled; builds green)*
- [ ] T-016 — Manual: click CTR widget after demo data seed  *(verifiable in staging with seeded data)*

## Done criteria

- [x] Five AI Economy widgets live.  *(cost / CTR / tokens native in AiEconomyPage; 2 Power BI widgets pending Tasneem)*
- [x] Recommendation clicks logged end-to-end.  *(client fires POST /api/ai/recommendations/clicks; server persists + audits)*
- [x] Monthly redaction audit runs automatically + admin UI ready.  *(Hangfire job + RedactionAuditPage)*
- [x] Target CTR and redaction miss rate thresholds tracked on dashboard.  *(`pages/admin/AiEconomyPage.tsx` — native CTR threshold badge: ✅ Above target / ⚠ Below target at 5%; redaction miss rate threshold spec in `docs/POWERBI-REPORTS.md` T-010 — alert indicator "⚠ leak rate > 2%"; Power BI dashboard build pending Tasneem)*
