# PB-009 — Tasks

**Owner**: @yousra-elnoby  •  **Est**: 29 pts  •  **Iteration**: 4

## Backend
- [ ] T-001 — `Resource` + children entities + configs (+ FTS index)
- [ ] T-002 — `CreateResourceCommand` + validator (EN+AR both required on publish)
- [ ] T-003 — `SubmitForReviewCommand` + `ApproveResourceCommand` state machine
- [ ] T-004 — `BookmarkToggleCommand` + `CompleteChapterCommand`
- [ ] T-005 — `SearchResourcesQuery` (title, content, category, tag, language)
- [ ] T-006 — `FeatureResourceCommand` with 6-item cap
- [ ] T-007 — Unit + integration tests

## Frontend
- [ ] T-008 — Student `Resources.tsx` hub — featured hero + category tiles
- [ ] T-009 — `ResourceDetail.tsx` — markdown render, chapter checklist, bookmark toggle
- [ ] T-010 — Author `ResourceEditor.tsx` — markdown editor with preview
- [ ] T-011 — Admin `ResourceModeration.tsx` review queue
- [ ] T-012 — Admin `FeaturedResources.tsx`
- [ ] T-013 — Arabic copy + content-quality pass

## QA
- [ ] T-014 — E2E: author creates → submits → admin approves → published and visible

## Done criteria
Publishing workflow green; bookmark + progress persisted; EN+AR content both required on publish.
