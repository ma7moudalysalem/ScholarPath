# PB-015 — Analytics Foundation

**Owner**: @TasneemShaaban • **Priority**: High • **Iteration**: 2 • **Est**: 34 pts

## Problem statement

The platform needs decision-grade dashboards for the four roles — Admin, Finance, Consultant, Student — without adding load to the transactional database or introducing new data infrastructure yet. Power BI with DirectQuery (reading straight from SQL Server) gives the team a 1-2 sprint path to production dashboards, with Row-Level Security ensuring each role sees only their scope.

## User stories

US-160 .. US-165

## Functional requirements

FR-212 .. FR-225

## Acceptance criteria

Stories expanded in detail in the next section. Summary:
1. Five dashboards deployed (Executive, Student Success, Financial, Consultant, Student).
2. Power BI workspace `ScholarPath-Analytics` provisioned with embed-token auth.
3. Row-Level Security rules enforced via JWT `activeRole` claim → only one role's scope visible at a time.
4. Auto-refresh every four hours (DirectQuery is live but mashup caches refresh on that cadence).
5. All reports documented in `docs/ANALYTICS.md`.

## Non-goals

- Data warehouse / Gold layer (that is PB-016).
- Real-time streaming (that is PB-018).
- External-embed for non-admin visitors (v2).
