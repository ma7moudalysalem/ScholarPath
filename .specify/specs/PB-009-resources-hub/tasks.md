# PB-009 — Tasks

**Owner**: @Madiha6776 → shipped by @ma7moudalysalem  •  **Est**: 29 pts  •  **Iteration**: 4
**Status**: ✅ backend + admin UI + student pages + author management shipped; E2E pending.

## Backend
- [x] T-001 — `Resource` + children entities + configs (+ FTS index)  *(`Domain/Entities/Resources.cs` — `Resource` + `ResourceChild` + `ResourceProgress` + `ResourceBookmark`; EF configs in `EntityConfigurations.cs`)*
- [x] T-002 — `CreateResourceCommand` + validator (EN+AR both required on publish)  *(`Application/Resources/Commands/CreateResource/` — author-only, creates as Draft; HtmlSanitizer on all text fields)*
- [x] T-003 — `SubmitForReviewCommand` + `ApproveResourceCommand` state machine  *(`SubmitResourceForReview`, `ApproveResource`, `RejectResource` commands; admin publishes/rejects)*
- [x] T-004 — `BookmarkToggleCommand` + `CompleteChapterCommand`  *(`ToggleResourceBookmark`, `CompleteResourceChapter` — both with `GetMyResourceBookmarks` + `GetMyResourceProgress` queries)*
- [x] T-005 — `SearchResourcesQuery` (title, content, category, tag, language)  *(`Application/Resources/Queries/SearchResources/` — paginated, filter by type/language/category/tag)*
- [x] T-006 — `FeatureResourceCommand` with 6-item cap  *(`FeatureResourceCommand` + `GetFeaturedResourcesQuery`; cap enforced in handler)*
- [x] T-007 — Unit + integration tests  *(`tests/ScholarPath.UnitTests/Resources/` — 5 test classes: Create, Feature, PublishWorkflow, StudentActions, SearchQuery; 504 tests green)*

## Frontend
- [x] T-008 — Student `Resources.tsx` hub — featured hero + category tiles  *(`pages/student/StudentResources.tsx` — paginated browse with search + type filter; wired to `GET /api/resources`)*
- [x] T-009 — `ResourceDetail.tsx` — markdown render, chapter checklist, bookmark toggle  *(`pages/student/ResourceDetail.tsx` — custom Markdown renderer, chapter list, bookmark button → `POST /api/resources/{id}/bookmark`, chapter "Done" button → `POST /api/resources/{id}/chapters/{chId}/complete`)*
- [x] T-010 — Author `ResourceEditor.tsx` — create/edit form with chapter support  *(`pages/author/ResourceEditor.tsx` — react-hook-form + zod; fields: type, title EN/AR, description EN/AR, markdown content EN/AR, external URL for VideoLink, chapters (dynamic list with useFieldArray), tags; submit draft or update; route-guarded to Consultant/Company/Admin)*
- [x] T-011 — Admin `ResourceModeration.tsx` review queue  *(`pages/admin/AdminArticles.tsx` — pending-review list with Approve / Reject (prompted) actions; wired to `GET /api/resources/pending-review` + approve/reject endpoints)*
- [x] T-012 — Author `MyResources.tsx` list + `FeaturedResources` in AdminArticles  *(`pages/author/MyResources.tsx` — author's own resources with status badges; edit/submit/view actions; nav link in Consultant sidebar)*
- [x] T-013 — Arabic copy + content-quality pass  *(`locales/en/resources.json` + `locales/ar/resources.json` — full EN+AR for browse, detail, moderation, author section; RTL-aware inputs)*

## QA
- [ ] T-014 — E2E: author creates → submits → admin approves → published and visible  *(needs seeded staging + Playwright flow)*

## Done criteria
- [x] Publishing workflow green; bookmark + progress persisted; EN+AR content both required on publish.
- [x] Author can create, edit, and submit resources from the consultant portal.
- [ ] E2E green in staging.
