# PB-009 — Resources Hub

**Owner**: @yousra-elnoby • **Priority**: High • **Iteration**: 4 • **Est**: 29 pts

## Problem statement

Curated guides, articles, videos, and checklists help Students prepare applications, write essays, and navigate visa processes. Authors are Consultants (with publishing permission), Companies (about their scholarships), or Admins. Content supports EN+AR, bookmarking, and progress tracking per chapter.

## User stories

| ID | Story | Size |
|----|-------|------|
| US-088 | Browse articles | 3pt |
| US-089 | Search/filter articles | 3pt |
| US-090 | Consultant publishes article | 4pt |
| US-091 | Company publishes article | 4pt |
| US-092 | Admin publishes article | 3pt |
| US-093 | Admin moderates visibility | 4pt |
| US-094 | Admin features articles | 2pt |
| US-100 | Validation feedback on publish (Consultant) | 2pt |
| US-101 | Validation feedback on publish (Company) | 2pt |
| US-102 | Validation feedback on publish (Admin) | 2pt |

## Functional requirements

FR-122 .. FR-128, FR-135 .. FR-137

## Acceptance criteria

1. **Resource model** — `Resource { title, slug, category, authorId, authorRole, contentMdEn, contentMdAr, chapters[], tags[], status: Draft|PendingReview|Published|Hidden, featured }`.
2. **Chapters** — Optional child rows with order + title + content for multi-part guides; reader tracks `ResourceProgress` per chapter.
3. **Publishing workflow** — Consultant/Company submit → `PendingReview` → Admin approves → `Published`. Admin can publish directly.
4. **Search** — Title/content FTS + category filter + tag filter + author filter + language (EN/AR).
5. **Bookmark** — Student `ResourceBookmark` row.
6. **Progress** — `ResourceProgress` with `ChaptersCompletedCount` updated via `POST /api/resources/{id}/chapters/{chapterId}/complete`.
7. **Featured** — Admin picks up to 6 for homepage hub.
8. **Validation** — Required fields on publish: title, category, EN+AR content, author bio snippet.

## Non-goals

- Rich media hosting (link externally)
- Commenting on articles (use community forum)
- Scheduled publishing (v2)
