# PB-009 — Implementation Plan

## Domain
- `Resource`, `ResourceChild` (chapters), `ResourceBookmark`, `ResourceProgress`, `ResourceProgressChild`
- Enums: `ResourceStatus { Draft, PendingReview, Published, Hidden, Removed }`, `ResourceCategory` (dynamic lookup), `ResourceType { Guide, Article, Checklist, VideoLink }`

## Application (`server/src/ScholarPath.Application/Resources/`)
- Commands: `CreateResourceCommand`, `UpdateResourceCommand`, `SubmitForReviewCommand`, `ApproveResourceCommand` (Admin), `RejectResourceCommand` (Admin), `FeatureResourceCommand`, `BookmarkToggleCommand`, `CompleteChapterCommand`
- Queries: `SearchResourcesQuery`, `GetResourceDetailQuery`, `GetMyBookmarksQuery`, `GetMyProgressQuery`, `GetPendingReviewResourcesQuery` (Admin), `GetFeaturedResourcesQuery`

## Infrastructure
- FTS on title + EN/AR content
- Markdown sanitization server-side

## API (`ResourcesController.cs`)
- Full CRUD + moderation endpoints (gated by role)

## Frontend
- Student: `Resources.tsx` hub (featured + categories), `ResourceDetail.tsx` (markdown renderer + chapter checklist), `MyBookmarks.tsx`
- Author: `ResourceEditor.tsx` with markdown editor + preview + chapter manager
- Admin: `ResourceModeration.tsx` review queue + `FeaturedResources.tsx`

## Tests
- Unit: status transitions
- Integration: full publish flow (submit → pending → approve → published)
- E2E: bookmark + track progress; admin features

## Dependencies
PB-001, PB-002, PB-011
