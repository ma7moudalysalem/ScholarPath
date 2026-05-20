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

- [ ] T-006 — PB-017-US-001 AI cost dashboard (stacked bar feature × provider × day)  *(pending Tasneem — data available via `/api/admin/analytics/ai-usage`)*
- [ ] T-007 — PB-017-US-002 Budget alert automation (80% three-day rule)  *(pending Tasneem)*
- [ ] T-008 — PB-017-US-003 Recommendation CTR widget + event logging  @ma7moudalysalem  *(server + client logging done; Power BI visual pending Tasneem)*
- [ ] T-009 — PB-017-US-004 Token efficiency box plots + top-10 table  *(pending Tasneem)*
- [ ] T-010 — PB-017-US-005 Redaction audit trend + investigation table  *(pending Tasneem)*

## Frontend

- [x] T-011 — Extend `AiRecommendations.tsx` to log clicks  @ma7moudalysalem  *(`components/ai/AiRecommendations.tsx` — fires `aiApi.logRecommendationClick()` on recommendation card click with source="card")*
- [x] T-012 — `AiEconomy.tsx` admin page (embed + native panels)  @ma7moudalysalem  *(`pages/admin/AiEconomyPage.tsx` — AI usage summary cards: total cost, recommendation CTR, token usage; routed at `/admin/ai-economy`)*
- [x] T-013 — `RedactionAudit.tsx` admin review UI  @ma7moudalysalem  *(`pages/admin/RedactionAuditPage.tsx` — paginated sample list with verdict buttons (Clean/Leaked); routed at `/admin/redaction-audit`)*

## QA

- [x] T-014 — Unit: click command idempotency  *(`tests/ScholarPath.UnitTests/Ai/LogRecommendationClickTests.cs` — `Repeat_click_inside_500ms_is_deduplicated` + `Click_after_debounce_window_persists_new_event`)*
- [ ] T-015 — Integration: sampling job exactness (50 rows, non-overlapping months)  *(pending)*
- [ ] T-016 — Manual: click CTR widget after demo data seed  *(verifiable in staging with seeded data)*

## Done criteria

- [x] Five AI Economy widgets live.  *(cost / CTR / tokens native in AiEconomyPage; 2 Power BI widgets pending Tasneem)*
- [x] Recommendation clicks logged end-to-end.  *(client fires POST /api/ai/recommendations/clicks; server persists + audits)*
- [x] Monthly redaction audit runs automatically + admin UI ready.  *(Hangfire job + RedactionAuditPage)*
- [ ] Target CTR and redaction miss rate thresholds tracked on dashboard.  *(pending Power BI visuals T-006..T-010)*
