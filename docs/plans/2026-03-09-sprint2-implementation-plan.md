# Sprint 2 — Modules 2–12 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the full Sprint 2 feature set: scholarship search/details/tracking, resources, success stories, mentorship, calendar, notifications, admin CMS, community, and AI assistance.

**Architecture:** Clean Architecture (Domain → Application → Infrastructure → API). Controllers inject services/DbContext directly (not CQRS). Frontend uses React 19 + MUI + Zustand + TanStack Query with bilingual i18n.

**Tech Stack:** .NET 10 / EF Core / SQL Server / Redis / Hangfire / SignalR / Stripe / React 19 / TypeScript 5.7 / MUI 6 / Zustand / TanStack Query / i18next

---

## Key Conventions Reference

**Backend patterns:**
- Controllers extend `BaseController`, route: `api/v{version:apiVersion}/[controller]`
- Use `_dbContext` directly for queries, `UserManager<ApplicationUser>` for user ops
- FluentValidation for all request DTOs, registered via `DependencyInjection.cs`
- Entities: `BaseEntity` (Id, CreatedAt, UpdatedAt), `AuditableEntity` (+CreatedBy, UpdatedBy), `ISoftDeletable`
- Return helpers: `OkResult()`, `BadRequestResult()`, `NotFoundResult()`, `UnauthorizedResult()`, `ForbiddenResult()`, `ConflictResult()`

**Frontend patterns:**
- Pages in `src/pages/`, components in `src/components/`, services in `src/services/`
- Services use `api` from `@/services/api` (axios with JWT interceptors)
- Zustand for client state, TanStack Query for server state
- All pages lazy-loaded in `App.tsx`, translations in `en.json`/`ar.json`
- Types/interfaces in `src/types/index.ts`

---

## Module 2: Scholarship Search & Listing

### Task 2.1: Enhance Scholarship Entity & Add DB Indexes

**Files:**
- Modify: `server/src/ScholarPath.Domain/Entities/Scholarship.cs`
- Modify: `server/src/ScholarPath.Domain/Enums/` — add new enums
- Create: `server/src/ScholarPath.Infrastructure/Persistence/Configurations/ScholarshipConfiguration.cs` (or modify existing)
- Migration: new migration after entity changes

**Steps:**

1. Add missing fields to `Scholarship.cs`:
   - `ProviderName` (string), `ProviderNameAr` (string?), `Tags` (string? JSON array)
   - `ViewCount` (int, default 0)
   - `OverviewHtml` (string?), `HowToApplyHtml` (string?)
   - `DocumentsChecklist` (string? JSON array)

2. Add `ScholarshipSortBy` enum to `server/src/ScholarPath.Domain/Enums/`:
   ```csharp
   public enum ScholarshipSortBy { Relevance = 0, DeadlineSoonest = 1, Newest = 2, HighestFunding = 3 }
   ```

3. Add `ScholarshipStatus` enum:
   ```csharp
   public enum ScholarshipStatus { Draft = 0, Published = 1, Archived = 2 }
   ```

4. Add `Status` property to Scholarship entity (ScholarshipStatus, default Published).

5. Create/update EF configuration with indexes:
   - Full-text index on Title + ProviderName
   - Compound index: (Country, DegreeLevel)
   - Compound index: (FieldOfStudy, Deadline)
   - Index on Deadline

6. Create migration: `dotnet ef migrations add EnhanceScholarshipEntity`

7. Commit: `feat(m2): enhance scholarship entity with provider, tags, and indexes`

---

### Task 2.2: Scholarship Search & Filter Endpoint

**Files:**
- Create: `server/src/ScholarPath.Application/Scholarships/DTOs/ScholarshipSearchRequest.cs`
- Create: `server/src/ScholarPath.Application/Scholarships/DTOs/ScholarshipListItemDto.cs`
- Create: `server/src/ScholarPath.Application/Scholarships/Validators/ScholarshipSearchRequestValidator.cs`
- Create: `server/src/ScholarPath.API/Controllers/ScholarshipsController.cs`

**Steps:**

1. Create `ScholarshipSearchRequest` DTO:
   ```csharp
   public class ScholarshipSearchRequest {
       public string? Search { get; set; }
       public string? Country { get; set; }
       public DegreeLevel? DegreeLevel { get; set; }
       public string? FieldOfStudy { get; set; }
       public ScholarshipFundingType? FundingType { get; set; }
       public DateTime? DeadlineFrom { get; set; }
       public DateTime? DeadlineTo { get; set; }
       public int Page { get; set; } = 1;
       public int PageSize { get; set; } = 20;
       public ScholarshipSortBy SortBy { get; set; } = ScholarshipSortBy.Relevance;
       public bool IncludeExpired { get; set; } = false;
   }
   ```

2. Create `ScholarshipListItemDto`:
   ```csharp
   public class ScholarshipListItemDto {
       public Guid Id { get; set; }
       public string Title { get; set; }
       public string? TitleAr { get; set; }
       public string? ProviderName { get; set; }
       public string? Country { get; set; }
       public DegreeLevel DegreeLevel { get; set; }
       public ScholarshipFundingType FundingType { get; set; }
       public decimal? AwardAmount { get; set; }
       public string? Currency { get; set; }
       public DateTime? Deadline { get; set; }
       public int? DeadlineCountdownDays { get; set; }
       public bool IsExpiringSoon { get; set; }
       public bool IsSaved { get; set; }
       public string? ImageUrl { get; set; }
       public DateTime CreatedAt { get; set; }
   }
   ```

3. Create FluentValidation validator:
   - PageSize: 1–50
   - Page: >= 1
   - DeadlineFrom <= DeadlineTo when both provided
   - SortBy: valid enum value

4. Create `ScholarshipsController` with `GET /api/v1/scholarships`:
   - Query `_dbContext.Scholarships` with filters
   - Exclude expired by default (Deadline < today && !IncludeExpired)
   - Exclude non-Published status
   - Apply search: `.Where(s => s.Title.Contains(search) || s.ProviderName.Contains(search))`
   - Apply all filters (Country, DegreeLevel, FieldOfStudy, FundingType, deadline range)
   - Sort by enum: DeadlineSoonest → OrderBy Deadline, Newest → OrderByDescending CreatedAt, HighestFunding → OrderByDescending AwardAmount, Relevance → default order
   - Paginate with Skip/Take
   - Compute DeadlineCountdownDays and IsExpiringSoon (deadline within 7 days)
   - For authenticated users: left-join SavedScholarships to set IsSaved
   - Return `PaginatedResponse<ScholarshipListItemDto>` with totalCount, page, pageSize, totalPages

5. Register validator in DI.

6. Commit: `feat(m2): add scholarship search endpoint with filters and pagination`

---

### Task 2.3: Profile-Based Recommendations Endpoint

**Files:**
- Create: `server/src/ScholarPath.Application/Scholarships/DTOs/RecommendedScholarshipDto.cs`
- Modify: `server/src/ScholarPath.API/Controllers/ScholarshipsController.cs`

**Steps:**

1. Create `RecommendedScholarshipDto` extending `ScholarshipListItemDto`:
   - Add `int Score { get; set; }` and `string[] MatchReasons { get; set; }`

2. Add `GET /api/v1/scholarships/recommended` to ScholarshipsController (Authorize):
   - Load user's profile (UserProfile)
   - If profile incomplete (no DegreeLevel or FieldOfStudy on profile): return `{ items: [], profileIncomplete: true }`
   - Load active, non-expired scholarships
   - Score each: degreeLevel match +3, fieldOfStudy match +3, country match +2, tag overlap +1 each
   - Build matchReasons array per scholarship
   - Return top 10 sorted by score desc
   - Cache per userId (5 min TTL)

3. Commit: `feat(m2): add profile-based scholarship recommendations`

---

### Task 2.4: Save/Unsave Scholarship Endpoints

**Files:**
- Modify: `server/src/ScholarPath.API/Controllers/ScholarshipsController.cs`

**Steps:**

1. Add `POST /api/v1/scholarships/{id}/save` (Authorize):
   - Check scholarship exists (404 if not)
   - Check if already saved (idempotent — return 200 if already saved)
   - Create SavedScholarship entry

2. Add `DELETE /api/v1/scholarships/{id}/save` (Authorize):
   - Remove if exists, return 200 either way (idempotent)

3. Add `GET /api/v1/saved-scholarships` (Authorize):
   - Paginated list of saved scholarships for current user
   - Return ScholarshipListItemDto with isSaved=true

4. Commit: `feat(m2): add save/unsave scholarship endpoints`

---

### Task 2.5: Scholarships List Page (Frontend)

**Files:**
- Rewrite: `client/src/pages/scholarships/ScholarshipList.tsx`
- Create: `client/src/components/scholarships/ScholarshipCard.tsx`
- Create: `client/src/components/scholarships/ScholarshipFilters.tsx`
- Create: `client/src/components/scholarships/RecommendedCarousel.tsx`
- Modify: `client/src/services/scholarshipService.ts` — add new API calls
- Modify: `client/src/types/index.ts` — add new types
- Modify: `client/src/i18n/locales/en.json` and `ar.json`

**Steps:**

1. Update `types/index.ts`:
   - Add `ScholarshipSortBy` enum
   - Update `ScholarshipFilters` with all new params (deadlineFrom, deadlineTo, sortBy, includeExpired)
   - Add `RecommendedScholarshipDto` interface with score and matchReasons
   - Add `RecommendedResponse` interface with profileIncomplete flag

2. Update `scholarshipService.ts`:
   - Update `getScholarships()` to pass all filter params
   - Add `getRecommendedScholarships(): Promise<RecommendedResponse>`
   - Add `getSavedScholarships(page, pageSize): Promise<PaginatedResponse<ScholarshipDto>>`

3. Create `ScholarshipCard.tsx`:
   - MUI Card: title, provider, country, deadline with countdown, funding type badge, bookmark icon, "View Details" button
   - "Expiring Soon" chip if deadline within 7 days
   - Bookmark click: call save/unsave with optimistic UI + toast
   - If not authenticated: bookmark click opens auth modal

4. Create `ScholarshipFilters.tsx`:
   - Degree Level multi-select, Country searchable dropdown, Field of Study multi-select
   - Funding Type checkbox group, Deadline Range date pickers
   - Sticky sidebar on desktop, MUI Drawer on mobile
   - Filter count badges, "Clear All" button
   - All filter state synced to URL search params

5. Create `RecommendedCarousel.tsx`:
   - Horizontal scroll of ScholarshipCards
   - "Why recommended?" tooltip showing matchReasons
   - Profile incomplete banner with CTA
   - localStorage toggle to show/hide
   - Skeleton loader (3 cards)

6. Rewrite `ScholarshipList.tsx`:
   - RecommendedCarousel at top (auth only)
   - ScholarshipFilters sidebar + results grid
   - Sort dropdown, pagination controls
   - URL query string sync for all filters
   - Skeleton cards (6) loading state
   - Empty state with "Reset Filters" CTA
   - "Include expired" toggle
   - "Saved Only" filter pill
   - TanStack Query with filter dependencies as query keys

7. Add all i18n keys to en.json and ar.json.

8. Commit: `feat(m2): implement scholarships list page with search, filters, and recommendations`

---

## Module 3: Scholarship Details & Apply

### Task 3.1: Scholarship Detail Endpoint

**Files:**
- Modify: `server/src/ScholarPath.API/Controllers/ScholarshipsController.cs`
- Create: `server/src/ScholarPath.Application/Scholarships/DTOs/ScholarshipDetailDto.cs`

**Steps:**

1. Create `ScholarshipDetailDto` with all fields:
   - All base fields + OverviewHtml, EligibilityDescription, RequiredDocuments, HowToApplyHtml, DocumentsChecklist, OfficialLink
   - IsSaved, IsTracked (for auth users), DeadlineCountdownDays
   - ProviderName, Category info, ViewCount

2. Add `GET /api/v1/scholarships/{id}`:
   - Return full scholarship detail
   - 404 if not found
   - For auth users: check saved/tracked status
   - Increment ViewCount fire-and-forget
   - Compute DeadlineCountdownDays

3. Commit: `feat(m3): add scholarship detail endpoint with view tracking`

---

### Task 3.2: Application Tracking Entity & Endpoint

**Files:**
- Create: `server/src/ScholarPath.Domain/Entities/ApplicationTracker.cs`
- Create: `server/src/ScholarPath.Domain/Enums/ApplicationStatus.cs`
- Modify: `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `server/src/ScholarPath.Application/Applications/DTOs/TrackApplicationRequest.cs`
- Modify: `server/src/ScholarPath.API/Controllers/` — create ApplicationsController

**Steps:**

1. Create `ApplicationStatus` enum:
   ```csharp
   public enum ApplicationStatus { Planned = 0, Applied = 1, Pending = 2, Accepted = 3, Rejected = 4 }
   ```

2. Create `ApplicationTracker` entity:
   ```csharp
   public class ApplicationTracker : AuditableEntity, ISoftDeletable {
       public Guid UserId { get; set; }
       public Guid ScholarshipId { get; set; }
       public ApplicationStatus Status { get; set; } = ApplicationStatus.Planned;
       public string? Notes { get; set; }
       public string? ChecklistJson { get; set; } // JSON array of {text, isChecked}
       public string? RemindersJson { get; set; } // JSON array of reminder presets
       public bool RemindersPaused { get; set; }
       // ISoftDeletable
       public bool IsDeleted { get; set; }
       public DateTime? DeletedAt { get; set; }
       public string? DeletedBy { get; set; }
       // Navigation
       public ApplicationUser User { get; set; } = null!;
       public Scholarship Scholarship { get; set; } = null!;
   }
   ```

3. Add DbSet to ApplicationDbContext: `public DbSet<ApplicationTracker> ApplicationTrackers => Set<ApplicationTracker>();`

4. Create EF configuration with unique index on (UserId, ScholarshipId).

5. Create `POST /api/v1/applications/track` endpoint:
   - Upsert: if already tracked, return existing with `alreadyExisted: true`
   - Default status to Planned
   - Return tracking record

6. Create migration: `dotnet ef migrations add AddApplicationTracker`

7. Commit: `feat(m3): add application tracking entity and endpoint`

---

### Task 3.3: Scholarship Details Page (Frontend)

**Files:**
- Rewrite: `client/src/pages/scholarships/ScholarshipDetail.tsx`
- Create: `client/src/components/scholarships/TrackingModal.tsx`
- Modify: `client/src/services/scholarshipService.ts`
- Create: `client/src/services/applicationService.ts`
- Modify: `client/src/types/index.ts`

**Steps:**

1. Add types: `ScholarshipDetailDto`, `ApplicationStatus` enum, `TrackApplicationRequest`, `ApplicationTrackerDto`.

2. Add service methods:
   - `applicationService.trackApplication(req)` — POST /applications/track
   - scholarshipService.getScholarshipById already exists

3. Rewrite `ScholarshipDetail.tsx`:
   - Header: title, provider, country flag, save bookmark
   - Key info card: deadline + countdown, award amount, degree level, funding type badge
   - Tabbed sections: Overview | Eligibility | Requirements | How to Apply | Documents
   - "Apply Now" button → external link (noopener noreferrer)
   - "Add to Tracking" button → opens TrackingModal
   - Share button (copy link)
   - Breadcrumb: Home > Scholarships > {title}
   - Sticky bottom bar on mobile: Save | Apply | Track
   - Skeleton loader, 404 page
   - "Report an issue" link

4. Create `TrackingModal.tsx`:
   - Status dropdown (Planned / Applied)
   - Notes textarea (optional)
   - Already tracked message with link to dashboard
   - Success toast with "View in Dashboard" link
   - Auth guard: opens login modal if not authenticated

5. i18n keys for all text.

6. Commit: `feat(m3): implement scholarship details page with tracking modal`

---

## Module 4: Student Dashboard & Application Tracking

### Task 4.1: Dashboard Summary Endpoint

**Files:**
- Create: `server/src/ScholarPath.API/Controllers/DashboardController.cs`
- Create: `server/src/ScholarPath.Application/Dashboard/DTOs/DashboardSummaryDto.cs`

**Steps:**

1. Create `DashboardSummaryDto`:
   ```csharp
   public class DashboardSummaryDto {
       public Dictionary<string, int> StatusCounts { get; set; } // Saved, Planned, Applied, Pending, Accepted, Rejected
       public List<UpcomingDeadlineDto> DeadlinesSoon { get; set; }
       public List<string> RecommendedActions { get; set; }
   }
   ```

2. Create `DashboardController` with `GET /api/v1/dashboard/summary` (Authorize):
   - Count saved scholarships
   - Count tracked applications by status
   - Get tracked apps with deadline in next 14 days
   - Build recommended actions: profile completion %, empty tracker, no reminders set
   - Cache per userId (2 min TTL)

3. Commit: `feat(m4): add dashboard summary endpoint`

---

### Task 4.2: Application Tracker CRUD Endpoints

**Files:**
- Create: `server/src/ScholarPath.API/Controllers/ApplicationsController.cs` (expand from Task 3.2)
- Create: `server/src/ScholarPath.Application/Applications/DTOs/` — multiple DTOs
- Create: `server/src/ScholarPath.Application/Applications/Validators/`

**Steps:**

1. Add endpoints to `ApplicationsController`:
   - `GET /api/v1/applications` — paginated list, filter by status, sort by deadline/updatedAt
   - `PUT /api/v1/applications/{id}/status` — update status (validate enum)
   - `PUT /api/v1/applications/{id}/notes` — update notes (max 2000 chars)
   - `PUT /api/v1/applications/{id}/checklist` — replace checklist items
   - `DELETE /api/v1/applications/{id}` — soft delete
   - All endpoints: ownership check (403 if not owner)

2. Add validators for each update request.

3. Commit: `feat(m4): add application tracker CRUD endpoints`

---

### Task 4.3: Deadline Reminder Scheduling

**Files:**
- Create: `server/src/ScholarPath.Application/Applications/DTOs/ReminderSettingsDto.cs`
- Modify: `server/src/ScholarPath.API/Controllers/ApplicationsController.cs`
- Create: `server/src/ScholarPath.Infrastructure/Jobs/DeadlineReminderJob.cs`

**Steps:**

1. Add `PUT /api/v1/applications/{id}/reminders`:
   - Body: `{ presets: [1,3,7,14,30], channels: { inApp: true, email: false } }`
   - Store in ApplicationTracker.RemindersJson
   - Schedule Hangfire recurring jobs based on deadline - preset days
   - Dedup existing jobs for same app + preset

2. Create `DeadlineReminderJob`:
   - Query due reminders
   - Create notification via notification service (built in Module 9)
   - Send email if channel enabled
   - Retry with exponential backoff

3. Commit: `feat(m4): add deadline reminder scheduling with Hangfire`

---

### Task 4.4: Dashboard Overview Page (Frontend)

**Files:**
- Rewrite: `client/src/pages/Dashboard.tsx`
- Create: `client/src/components/dashboard/StatusTiles.tsx`
- Create: `client/src/components/dashboard/DeadlinesWidget.tsx`
- Create: `client/src/components/dashboard/ActionsWidget.tsx`
- Create: `client/src/services/dashboardService.ts`

**Steps:**

1. Create `dashboardService.ts`:
   - `getSummary(): Promise<DashboardSummaryDto>`

2. Create dashboard components:
   - StatusTiles: 6 colored tiles with count + click filters to tracker
   - DeadlinesWidget: upcoming 14-day deadlines with countdown badges
   - ActionsWidget: recommended actions checklist

3. Rewrite `Dashboard.tsx`:
   - Status tiles row
   - DeadlinesWidget + ActionsWidget side by side
   - Empty state: onboarding CTA cards
   - Skeleton loaders
   - Auto-refresh on status changes

4. i18n keys.

5. Commit: `feat(m4): implement dashboard overview page`

---

### Task 4.5: Application Tracker — Kanban + List View (Frontend)

**Files:**
- Create: `client/src/pages/dashboard/Tracker.tsx`
- Create: `client/src/components/tracker/KanbanBoard.tsx`
- Create: `client/src/components/tracker/TrackerCard.tsx`
- Create: `client/src/components/tracker/CardDetailDrawer.tsx`
- Modify: `client/src/App.tsx` — add route

**Steps:**

1. Add route `/dashboard/tracker` to App.tsx.

2. Create `KanbanBoard.tsx`:
   - Columns: Planned | Applied | Pending | Accepted | Rejected
   - Drag-and-drop (use `@hello-pangea/dnd` or similar)
   - Mobile: list view with status dropdown per card

3. Create `TrackerCard.tsx`:
   - Title, provider, deadline countdown, reminder indicator, last updated
   - Quick actions on hover: Open Details | Set Reminder | Mark Applied
   - Overdue: red border + badge

4. Create `CardDetailDrawer.tsx`:
   - MUI Drawer with notes editor, checklist, reminder presets
   - Notes: textarea with character counter (2000 max)
   - Checklist: add/remove/check items with strikethrough
   - Reminders: preset multi-select + channel toggles
   - Links to scholarship details and apply button

5. Filter bar: filter by status, sort by deadline/updated.

6. Commit: `feat(m4): implement application tracker with kanban and detail drawer`

---

### Task 4.6: Reminder Settings UI (Frontend)

**Files:**
- Integrated into `CardDetailDrawer.tsx` from Task 4.5

**Steps:**

1. Add reminder section to drawer:
   - Preset checkboxes: 30d | 14d | 7d | 3d | 1d
   - Channel toggles: In-app | Email
   - Pause toggle
   - Timezone label
   - Disable if no deadline
   - Auto-save on change with debounce

2. Commit: `feat(m4): add reminder settings UI in tracker drawer`

---

## Module 5: Resources Center

### Task 5.1: Enhance Resource Entity & Endpoints

**Files:**
- Modify: `server/src/ScholarPath.Domain/Entities/Resource.cs`
- Create: `server/src/ScholarPath.Domain/Enums/ResourceTopic.cs`
- Create: `server/src/ScholarPath.Domain/Entities/ResourceBookmark.cs`
- Create: `server/src/ScholarPath.Domain/Entities/ResourceProgress.cs`
- Modify: `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs`
- Create: `server/src/ScholarPath.API/Controllers/ResourcesController.cs`
- Create: `server/src/ScholarPath.Application/Resources/DTOs/`

**Steps:**

1. Add `ResourceTopic` enum: CV, PersonalStatement, InterviewPrep, CommonMistakes, ScholarshipTips, VisaProcess.

2. Enhance `Resource.cs`:
   - Add: Topic (ResourceTopic), ContentHtml (string), ReadingTimeMinutes (int), DifficultyLevel (string), Excerpt (string), Status (ResourceStatus: Draft/Published), ViewCount (int), AttachmentsJson (string? JSON)

3. Create `ResourceBookmark` entity (UserId, ResourceId, unique constraint).

4. Create `ResourceProgress` entity (UserId, ResourceId, CompletedItemIds JSON string).

5. Add DbSets.

6. Create `ResourcesController`:
   - `GET /api/v1/resources` — paginated, filters: topic, search, sort (popularity/newest)
   - `GET /api/v1/resources/{id}` — full article with content
   - `POST /api/v1/resources/{id}/bookmark` — idempotent
   - `DELETE /api/v1/resources/{id}/bookmark`
   - `GET /api/v1/resources/bookmarked` — user's bookmarked resources
   - `PUT /api/v1/resources/{id}/progress` — save checklist progress
   - `GET /api/v1/resources/{id}/progress` — get checklist progress
   - isBookmarked flag on list/detail responses for auth users

7. Create migration.

8. Commit: `feat(m5): add resources center backend with bookmarks and progress tracking`

---

### Task 5.2: Resources Center Pages (Frontend)

**Files:**
- Create: `client/src/pages/resources/ResourceList.tsx`
- Create: `client/src/pages/resources/ResourceDetail.tsx`
- Create: `client/src/components/resources/ResourceCard.tsx`
- Create: `client/src/components/resources/TableOfContents.tsx`
- Create: `client/src/components/resources/InteractiveChecklist.tsx`
- Create: `client/src/services/resourceService.ts`
- Modify: `client/src/App.tsx` — add routes
- Modify: `client/src/types/index.ts`

**Steps:**

1. Add types: ResourceTopic, ResourceDto, ResourceDetailDto, ResourceFilters.

2. Create `resourceService.ts` with all API calls.

3. Create `ResourceList.tsx`:
   - Search bar with debounce
   - Topic filter pills
   - Sort toggle: Most Popular | Newest
   - ResourceCard grid with skeleton + empty state
   - Pagination

4. Create `ResourceDetail.tsx`:
   - Sticky ToC (sidebar desktop, collapsible mobile)
   - Semantic H1/H2/H3 rendering
   - Callout/tip boxes
   - InteractiveChecklist component
   - Download buttons for attachments
   - Bookmark button, last updated date
   - Related resources at bottom

5. Add routes: `/resources`, `/resources/:id`

6. i18n keys.

7. Commit: `feat(m5): implement resources center list and detail pages`

---

## Module 6: Success Stories

### Task 6.1: Enhance SuccessStory Entity & Endpoints

**Files:**
- Modify: `server/src/ScholarPath.Domain/Entities/SuccessStory.cs`
- Create: `server/src/ScholarPath.Domain/Enums/StoryStatus.cs`
- Create: `server/src/ScholarPath.API/Controllers/SuccessStoriesController.cs`
- Create: `server/src/ScholarPath.Application/SuccessStories/DTOs/`
- Modify: `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs`

**Steps:**

1. Add `StoryStatus` enum: Pending, Approved, Rejected.

2. Enhance `SuccessStory.cs`:
   - Add: ScholarshipName, Country, FieldOfStudy, DegreeLevel, Year (int)
   - Add: BackgroundSection, PreparationSection, ChallengesSection, WhatWorkedSection, TipsSection
   - Add: ShortTip (excerpt), Status (StoryStatus), IsAnonymous (bool)
   - Add: RejectionReason, ReviewedById, ReviewedAt, ViewCount
   - Add: LinksJson (string? JSON — LinkedIn, Portfolio, GitHub)
   - Remove old: IsApproved, ApprovedAt, ApprovedBy (replace with Status + ReviewedAt/By)

3. Create `SuccessStoriesController`:
   - `GET /api/v1/success-stories` — public, only Approved, filters: country, fieldOfStudy, degreeLevel, year, sort: mostViewed/newest
   - `GET /api/v1/success-stories/{id}` — full story, increment viewCount
   - `POST /api/v1/success-stories` — auth, submit (defaults to Pending), validate sections, rate limit 3 pending max
   - `GET /api/v1/success-stories/my-submissions` — auth, own stories with status
   - Admin endpoints (in AdminController or separate):
     - `GET /api/v1/admin/success-stories?status=` — admin queue
     - `PUT /api/v1/admin/success-stories/{id}/approve`
     - `PUT /api/v1/admin/success-stories/{id}/reject` — with reason
   - Audit log: adminId, decision, timestamp, reason

4. Create migration.

5. Commit: `feat(m6): add success stories backend with moderation`

---

### Task 6.2: Success Stories Pages (Frontend)

**Files:**
- Create: `client/src/pages/stories/SuccessStoriesList.tsx`
- Create: `client/src/pages/stories/SuccessStoryDetail.tsx`
- Create: `client/src/pages/stories/SubmitStory.tsx`
- Create: `client/src/pages/stories/MySubmissions.tsx`
- Create: `client/src/components/stories/StoryCard.tsx`
- Create: `client/src/services/storyService.ts`
- Modify: `client/src/App.tsx`

**Steps:**

1. Add types and service layer.

2. Create list page with filters (country, field, degree, year), sort, cards.

3. Create detail page with structured sections, author info (or anonymous), share button, related stories.

4. Create submit form:
   - Multi-section: Background, Preparation, Challenges, What Worked, Tips
   - Character counters with min/max
   - Optional links (max 3)
   - Anonymous toggle
   - Draft autosave (localStorage)
   - Submit → Pending confirmation screen

5. Create My Submissions page with status badges.

6. Add routes: `/success-stories`, `/success-stories/:id`, `/success-stories/submit`, `/success-stories/my-submissions`

7. i18n keys.

8. Commit: `feat(m6): implement success stories pages with submission form`

---

## Module 7: Mentorship & Advisor Sessions

### Task 7.1: Advisor & Booking Entities

**Files:**
- Create: `server/src/ScholarPath.Domain/Entities/AdvisorProfile.cs`
- Create: `server/src/ScholarPath.Domain/Entities/AdvisorAvailability.cs`
- Create: `server/src/ScholarPath.Domain/Entities/Booking.cs`
- Create: `server/src/ScholarPath.Domain/Entities/Payment.cs`
- Create: `server/src/ScholarPath.Domain/Enums/BookingStatus.cs`
- Create: `server/src/ScholarPath.Domain/Enums/SessionType.cs`
- Modify: `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs`

**Steps:**

1. Create enums:
   ```csharp
   public enum BookingStatus { Hold = 0, Confirmed = 1, Completed = 2, Cancelled = 3, NoShow = 4 }
   public enum SessionType { OneOnOne = 0, Group = 1 }
   ```

2. Create `AdvisorProfile`:
   - UserId, Bio, Education (string), Experience (string), ExpertiseTags (string JSON)
   - Languages (string JSON), SessionTypes (string JSON), Prices (string JSON per type)
   - Rating (decimal), RatingCount (int), IsApproved (bool)
   - CancellationPolicyCutoffHours (int, default 24)

3. Create `AdvisorAvailability`:
   - AdvisorProfileId, SlotStartUtc (DateTime), SlotEndUtc (DateTime), IsBooked (bool)

4. Create `Booking`:
   - StudentId, AdvisorProfileId, AvailabilitySlotId, SessionType
   - Status (BookingStatus), HoldExpiresAt (DateTime?)
   - Price (decimal), Currency (string)
   - PreSessionNotes (string?), MeetingLink (string?)
   - CancellationReason (string?), RefundAmount (decimal?)
   - PaymentIntentId (string? — Stripe)

5. Create `Payment`:
   - BookingId, StripeSessionId, StripePaymentIntentId, Amount, Currency, Status, PaidAt

6. Add all DbSets. Create migration.

7. Commit: `feat(m7): add advisor, booking, and payment entities`

---

### Task 7.2: Advisors Directory & Booking Endpoints

**Files:**
- Create: `server/src/ScholarPath.API/Controllers/AdvisorsController.cs`
- Create: `server/src/ScholarPath.API/Controllers/BookingsController.cs`
- Create: `server/src/ScholarPath.API/Controllers/PaymentsController.cs`
- Create: `server/src/ScholarPath.Application/Advisors/DTOs/`
- Create: `server/src/ScholarPath.Application/Bookings/DTOs/`
- Create: `server/src/ScholarPath.Infrastructure/Services/StripeService.cs`

**Steps:**

1. `AdvisorsController`:
   - `GET /api/v1/advisors` — paginated, filters: expertiseTags, language, priceMin/Max, sessionType, sort: price/rating/nextAvailable
   - `GET /api/v1/advisors/{id}` — full profile
   - `GET /api/v1/advisors/{id}/availability?from=&to=` — available slots in UTC

2. `BookingsController`:
   - `POST /api/v1/bookings` — create with Hold status, lock slot for 10 min
   - `POST /api/v1/bookings/{id}/add-notes` — pre-session notes
   - `GET /api/v1/bookings/my` — student's booking history
   - `PUT /api/v1/bookings/{id}/cancel` — policy check, refund if within policy
   - `PUT /api/v1/bookings/{id}/reschedule` — validate new slot, update

3. `PaymentsController`:
   - `POST /api/v1/payments/checkout-session` — create Stripe Checkout Session
   - `POST /api/v1/payments/webhook` — handle Stripe events (payment_intent.succeeded/failed)

4. Create `StripeService` for Stripe API integration.

5. Background job: release expired holds every minute.

6. Commit: `feat(m7): add advisor directory, booking flow, and Stripe integration`

---

### Task 7.3: Advisor & Booking Pages (Frontend)

**Files:**
- Create: `client/src/pages/advisors/AdvisorsList.tsx`
- Create: `client/src/pages/advisors/AdvisorProfile.tsx`
- Create: `client/src/pages/advisors/BookingFlow.tsx`
- Create: `client/src/pages/dashboard/MyBookings.tsx`
- Create: `client/src/components/advisors/AdvisorCard.tsx`
- Create: `client/src/components/advisors/AvailabilityCalendar.tsx`
- Create: `client/src/components/advisors/BookingSummary.tsx`
- Create: `client/src/services/advisorService.ts`
- Create: `client/src/services/bookingService.ts`
- Modify: `client/src/App.tsx`

**Steps:**

1. Add types and services.

2. Create `AdvisorsList.tsx`:
   - Filter panel: expertise, language, price range, session type
   - Advisor cards with photo, rating, tags, next available, price
   - Sort, pagination

3. Create `AdvisorProfile.tsx`:
   - Profile header, bio, education, experience
   - AvailabilityCalendar: weekly view, slots in user timezone
   - Session type selector, "Book Now" CTA

4. Create `BookingFlow.tsx` (multi-step):
   - Step 1: Booking summary (slot, type, price, policy), pre-session notes, 10-min hold timer
   - Step 2: Stripe Checkout redirect (skip for free sessions)
   - Success: confirmation details, Add to Calendar (ICS download)
   - Failure: retry button

5. Create `MyBookings.tsx`:
   - List with status badges
   - Cancel/reschedule actions with policy display

6. Add routes: `/advisors`, `/advisors/:id`, `/advisors/:id/book`, `/dashboard/bookings`

7. i18n keys.

8. Commit: `feat(m7): implement advisor directory, booking flow, and my bookings pages`

---

## Module 8: Calendar & Deadlines

### Task 8.1: Unified Calendar Endpoint

**Files:**
- Create: `server/src/ScholarPath.API/Controllers/CalendarController.cs`
- Create: `server/src/ScholarPath.Application/Calendar/DTOs/CalendarEventDto.cs`

**Steps:**

1. Create `CalendarEventDto`:
   ```csharp
   public class CalendarEventDto {
       public Guid Id { get; set; }
       public string Type { get; set; } // ScholarshipDeadline, AdvisorSession, PersonalReminder
       public string Title { get; set; }
       public DateTime DateUtc { get; set; }
       public DateTime? EndDateUtc { get; set; }
       public int? CountdownDays { get; set; }
       public Guid? ReferenceId { get; set; } // scholarshipId or bookingId
       public string? AdvisorName { get; set; }
       public string? MeetingLink { get; set; }
       public bool? IsSaved { get; set; }
       public bool? IsTracked { get; set; }
   }
   ```

2. Create `CalendarController` with `GET /api/v1/calendar?from=&to=` (Authorize):
   - Validate from < to, max range 3 months
   - Collect events from:
     - Tracked scholarship deadlines (ApplicationTracker → Scholarship.Deadline)
     - Saved scholarship deadlines
     - Bookings (Confirmed)
   - Return merged list sorted by date
   - Cache per userId + range (2 min TTL)

3. Commit: `feat(m8): add unified calendar endpoint`

---

### Task 8.2: Calendar Page (Frontend)

**Files:**
- Create: `client/src/pages/Calendar.tsx`
- Create: `client/src/components/calendar/CalendarGrid.tsx`
- Create: `client/src/components/calendar/EventModal.tsx`
- Create: `client/src/services/calendarService.ts`
- Modify: `client/src/App.tsx`

**Steps:**

1. Install a React calendar library (e.g., `react-big-calendar` or build custom MUI grid).

2. Create `CalendarGrid.tsx`:
   - Month | Week | Day view toggles
   - Color-coded events: Blue (deadline), Green (session), Orange (reminder)
   - Legend bar
   - Filter toggles per event type
   - Previous/Next/Today navigation
   - Range-based API fetching on period change

3. Create `EventModal.tsx`:
   - Event details: title, date/time, countdown/duration
   - Links to related item (scholarship/booking)
   - Reminder settings
   - "Add to external calendar" links

4. Mobile: day list view instead of grid.

5. Add route: `/calendar`

6. i18n keys.

7. Commit: `feat(m8): implement calendar page with month/week/day views`

---

## Module 9: Notifications

### Task 9.1: Notifications System Backend

**Files:**
- Modify: `server/src/ScholarPath.Domain/Entities/Notification.cs`
- Modify: `server/src/ScholarPath.Domain/Enums/NotificationType.cs`
- Create: `server/src/ScholarPath.Domain/Entities/NotificationPreference.cs`
- Create: `server/src/ScholarPath.Infrastructure/Services/NotificationService.cs`
- Create: `server/src/ScholarPath.API/Controllers/NotificationsController.cs`
- Create: `server/src/ScholarPath.API/Hubs/NotificationHub.cs`

**Steps:**

1. Update `NotificationType` enum:
   ```csharp
   public enum NotificationType {
       System = 0, UpgradeStatus = 1, ScholarshipAlert = 2, CommunityMention = 3,
       Message = 4, SessionReminder = 5, DeadlineReminder = 6,
       BookingConfirmed = 7, BookingCancelled = 8,
       StoryApproved = 9, StoryRejected = 10, CommunityReply = 11
   }
   ```

2. Add `DeepLinkUrl` (string?) to Notification entity.

3. Create `NotificationPreference` entity:
   - UserId (unique), DeadlinesInApp, DeadlinesEmail, BookingsInApp, BookingsEmail, CommunityInApp, CommunityEmail, SystemInApp, SystemEmail, DailySummary (bool)

4. Create `NotificationService`:
   - `CreateNotification(userId, type, title, body, deepLinkUrl, referenceId)` — internal service
   - Check user preferences before sending
   - Enforce daily cap per category (5/day)
   - Queue emails
   - Push SignalR for real-time badge update

5. Create/update `NotificationsController`:
   - `GET /api/v1/notifications` — paginated, filter: all/unread
   - `PUT /api/v1/notifications/{id}/read`
   - `PUT /api/v1/notifications/read-all`
   - `GET /api/v1/notifications/unread-count`
   - `PUT /api/v1/profile/notification-preferences`
   - `GET /api/v1/profile/notification-preferences`

6. Set up `NotificationHub` (SignalR) for real-time unread count pushes.

7. Create migration.

8. Commit: `feat(m9): add notifications system with preferences and SignalR`

---

### Task 9.2: Notifications UI (Frontend)

**Files:**
- Rewrite: `client/src/pages/Notifications.tsx`
- Create: `client/src/components/notifications/NotificationBell.tsx`
- Create: `client/src/components/notifications/NotificationDropdown.tsx`
- Create: `client/src/pages/settings/NotificationSettings.tsx`
- Modify: `client/src/services/notificationService.ts`
- Modify: `client/src/components/Layout/AuthenticatedLayout.tsx` — add bell to header

**Steps:**

1. Create `NotificationBell.tsx`:
   - Bell icon with unread count badge (9+ cap)
   - Click opens dropdown (last 5) with "View All" link
   - Polling every 30s for unread count (or SignalR)

2. Add bell to authenticated layout header.

3. Rewrite `Notifications.tsx`:
   - Filter tabs: All | Unread
   - Notification items: icon per type, title, body, time ago, read indicator
   - Click: mark as read + navigate to deep link
   - "Mark all as read" button
   - Infinite scroll
   - Empty state

4. Create `NotificationSettings.tsx`:
   - Category toggles with per-category channel toggles
   - Daily summary toggle
   - Email disabled tooltip if unverified
   - Auto-save with debounce

5. Add route: `/settings/notifications`

6. i18n keys.

7. Commit: `feat(m9): implement notification bell, list, and preferences UI`

---

## Module 10: Admin CMS & Moderation

### Task 10.1: Admin Scholarship CRUD + Bulk Import Endpoints

**Files:**
- Create: `server/src/ScholarPath.API/Controllers/Admin/AdminScholarshipsController.cs`
- Create: `server/src/ScholarPath.Application/Admin/DTOs/`
- Create: `server/src/ScholarPath.Infrastructure/Jobs/ScholarshipImportJob.cs`

**Steps:**

1. Create `AdminScholarshipsController` (Authorize Roles=Admin):
   - `GET /api/v1/admin/scholarships` — paginated with status/country/degreeLevel filters + search
   - `POST /api/v1/admin/scholarships` — create
   - `PUT /api/v1/admin/scholarships/{id}` — update
   - `DELETE /api/v1/admin/scholarships/{id}` — soft delete (set Archived)
   - `PUT /api/v1/admin/scholarships/bulk` — bulk publish/archive
   - `POST /api/v1/admin/scholarships/import` — upload CSV/XLSX, return jobId
   - `GET /api/v1/admin/scholarships/import/{jobId}` — poll job status
   - Audit log on all changes

2. Create `ScholarshipImportJob`:
   - Parse CSV/XLSX (use CsvHelper or EPPlus)
   - Row-level validation
   - Partial import: valid rows imported, invalid skipped
   - Result: createdCount, updatedCount, skippedCount, errors
   - Generate downloadable error report CSV
   - File size limit: 10MB, row limit: 5000

3. Commit: `feat(m10): add admin scholarship CRUD and bulk import endpoints`

---

### Task 10.2: Admin Resources, Users, and Moderation Endpoints

**Files:**
- Create: `server/src/ScholarPath.API/Controllers/Admin/AdminResourcesController.cs`
- Create: `server/src/ScholarPath.API/Controllers/Admin/AdminUsersController.cs`
- Modify: `server/src/ScholarPath.API/Controllers/Admin/` — existing admin controller

**Steps:**

1. `AdminResourcesController` (Admin):
   - Full CRUD for resources with Draft/Published status
   - Preview endpoint

2. `AdminUsersController` (Admin):
   - `GET /api/v1/admin/users` — paginated, filter by role/status, search
   - `PUT /api/v1/admin/users/{id}/suspend` — invalidate sessions
   - `PUT /api/v1/admin/users/{id}/activate`
   - `GET /api/v1/admin/users/{id}` — detail with activity summary
   - Audit log on all actions
   - Suspended users get 403 on auth requests

3. Success Stories admin endpoints (if not done in Module 6).

4. Community reports admin endpoints (preview for Module 11).

5. Commit: `feat(m10): add admin resources, users, and moderation endpoints`

---

### Task 10.3: Admin Frontend — Dashboard, Scholarships, Import

**Files:**
- Create: `client/src/pages/admin/AdminDashboard.tsx`
- Create: `client/src/pages/admin/AdminScholarships.tsx`
- Create: `client/src/pages/admin/ScholarshipForm.tsx`
- Create: `client/src/pages/admin/ScholarshipImport.tsx`
- Create: `client/src/components/admin/AdminLayout.tsx`
- Create: `client/src/services/admin/adminScholarshipService.ts`
- Modify: `client/src/App.tsx`

**Steps:**

1. Create `AdminLayout.tsx`:
   - Sidebar: Dashboard | Scholarships | Resources | Stories | Reports | Users
   - Separate from student layout

2. Create `AdminDashboard.tsx`:
   - Summary tiles: total scholarships, pending stories, open reports, total users
   - Quick links to queues

3. Create `AdminScholarships.tsx`:
   - Data table with search, status filter, bulk select
   - Bulk actions: Publish | Archive
   - Row actions: Edit | Archive | Preview
   - Pagination with page size selector

4. Create `ScholarshipForm.tsx`:
   - Create/edit form with all fields + validation
   - Status selector, preview mode

5. Create `ScholarshipImport.tsx`:
   - Upload (drag-drop), processing with polling, results summary, error table, download report

6. Add admin routes with role guard.

7. Commit: `feat(m10): implement admin dashboard, scholarship management, and import UI`

---

### Task 10.4: Admin Frontend — Moderation Queues, Users

**Files:**
- Create: `client/src/pages/admin/AdminStories.tsx`
- Create: `client/src/pages/admin/AdminCommunityReports.tsx`
- Create: `client/src/pages/admin/AdminUsers.tsx`
- Create: `client/src/services/admin/adminUserService.ts`
- Create: `client/src/services/admin/adminStoryService.ts`

**Steps:**

1. Create `AdminStories.tsx`:
   - Queue: filter by Pending/Approved/Rejected
   - Side panel preview with full story
   - Approve/Reject actions with confirmation

2. Create `AdminCommunityReports.tsx`:
   - Reports list with target type and reason
   - Preview reported content
   - Actions: Resolve | Hide | Warn | Suspend

3. Create `AdminUsers.tsx`:
   - Table with search, role/status filters
   - Suspend/Activate actions with confirmation modals
   - User detail panel

4. Commit: `feat(m10): implement admin moderation queues and user management`

---

## Module 11: Community Module

### Task 11.1: Enhance Community Entities

**Files:**
- Modify: `server/src/ScholarPath.Domain/Entities/Post.cs`
- Modify: `server/src/ScholarPath.Domain/Entities/Group.cs`
- Modify: `server/src/ScholarPath.Domain/Entities/Comment.cs`
- Create: `server/src/ScholarPath.Domain/Entities/PostReaction.cs`
- Create: `server/src/ScholarPath.Domain/Entities/SavedPost.cs`
- Create: `server/src/ScholarPath.Domain/Entities/CommunityReport.cs`
- Create: `server/src/ScholarPath.Domain/Entities/GroupJoinRequest.cs`
- Create: `server/src/ScholarPath.Domain/Enums/ReportReason.cs`
- Create: `server/src/ScholarPath.Domain/Enums/ReportStatus.cs`
- Modify: `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs`

**Steps:**

1. Enhance `Post.cs`:
   - Add: Title (string), Tags (string? JSON), AttachmentsJson (string?), ReactionCount (int), CommentCount (int)

2. Enhance `Group.cs`:
   - Add: Category (string?), Rules (string?), PinnedPostIds (string? JSON)

3. Enhance `Comment.cs`:
   - Add: soft delete fields (ISoftDeletable)

4. Create `PostReaction` (UserId, PostId, ReactionType: Like/Helpful, unique constraint).

5. Create `SavedPost` (UserId, PostId, unique constraint).

6. Create `CommunityReport`:
   - ReporterId, TargetType (Post/Comment), TargetId, Reason (ReportReason enum), Notes, Status (ReportStatus: Open/Resolved/Dismissed), ResolvedById, ResolvedAt

7. Create `GroupJoinRequest` (UserId, GroupId, Status: Pending/Approved/Rejected).

8. Add all DbSets. Create migration.

9. Commit: `feat(m11): enhance community entities with reactions, saves, reports`

---

### Task 11.2: Community Feed, Groups, Posts, Comments Endpoints

**Files:**
- Create: `server/src/ScholarPath.API/Controllers/CommunityController.cs`
- Create: `server/src/ScholarPath.Application/Community/DTOs/`

**Steps:**

1. Feed endpoints:
   - `GET /api/v1/community/feed?tab=forYou|latest|myGroups` — paginated
   - forYou: joined groups + trending, latest: all public + joined newest, myGroups: joined only
   - Unauthenticated: public posts read-only

2. Groups endpoints:
   - `GET /api/v1/community/groups` — paginated with category, visibility filters
   - `GET /api/v1/community/groups/{id}` — detail with rules, pinned posts, memberCount
   - `POST /api/v1/community/groups/{id}/join` — public: join, private: request
   - `DELETE /api/v1/community/groups/{id}/leave`
   - `GET /api/v1/community/groups/{id}/posts` — group posts

3. Posts CRUD:
   - `POST /api/v1/community/groups/{id}/posts` — validate: title 10-120, body 50-5000, max 5 tags, max 3 attachments
   - `PUT /api/v1/community/posts/{id}` — edit own (or admin/mod)
   - `DELETE /api/v1/community/posts/{id}` — soft delete own
   - `GET /api/v1/community/posts/{id}` — with first page of comments

4. Comments:
   - `POST /api/v1/community/posts/{id}/comments`
   - `PUT /api/v1/community/comments/{id}`
   - `DELETE /api/v1/community/comments/{id}`
   - `GET /api/v1/community/posts/{id}/comments` — paginated

5. Reactions & saves:
   - `POST /api/v1/community/posts/{id}/reactions` — toggle (idempotent)
   - `POST /api/v1/community/posts/{id}/save` / `DELETE`
   - `GET /api/v1/community/saved`

6. Reports:
   - `POST /api/v1/community/reports`
   - Admin: `GET /api/v1/admin/community/reports`, resolve, hide post/comment

7. Ownership checks, rate limiting (10 posts/hr, 30 comments/hr).

8. Notifications on new comments and @mentions.

9. Commit: `feat(m11): add community feed, groups, posts, comments, and moderation endpoints`

---

### Task 11.3: Community Frontend — Feed, Groups, Posts

**Files:**
- Rewrite: `client/src/pages/community/Community.tsx`
- Rewrite: `client/src/pages/community/GroupDetail.tsx`
- Create: `client/src/pages/community/PostDetail.tsx`
- Create: `client/src/pages/community/GroupsDirectory.tsx`
- Create: `client/src/pages/community/SavedPosts.tsx`
- Create: `client/src/components/community/PostCard.tsx`
- Create: `client/src/components/community/PostComposer.tsx`
- Create: `client/src/components/community/CommentSection.tsx`
- Create: `client/src/components/community/ReportModal.tsx`
- Create: `client/src/components/community/GroupCard.tsx`
- Create: `client/src/services/communityService.ts`
- Modify: `client/src/App.tsx`

**Steps:**

1. Add all types and service layer.

2. Rewrite `Community.tsx` (feed):
   - Tabs: For You | Latest | My Groups
   - PostCard grid with infinite scroll
   - Filter bar, "Create Post" FAB
   - Unauthenticated: disabled interactions with tooltip

3. Create `GroupsDirectory.tsx`:
   - Category filter + search
   - Group cards with join/leave actions

4. Rewrite `GroupDetail.tsx`:
   - Header, rules panel, pinned posts, posts list

5. Create `PostComposer.tsx`:
   - Modal: group selector, title, body (rich text), tags, attachments
   - Guidelines panel, preview mode, draft autosave

6. Create `PostDetail.tsx`:
   - Full post, reactions, save, comments with lazy loading
   - Edit/delete own content, report others

7. Create `ReportModal.tsx`:
   - Reason dropdown, notes, submit toast

8. Create `SavedPosts.tsx` at `/community/saved`.

9. Add all routes.

10. i18n keys.

11. Commit: `feat(m11): implement community feed, groups, post composer, and moderation UI`

---

## Module 12: AI Assistance (Phase 2)

### Task 12.1: AI Document Analysis Backend

**Files:**
- Create: `server/src/ScholarPath.Domain/Entities/AiAnalysisJob.cs`
- Create: `server/src/ScholarPath.Domain/Enums/AnalysisType.cs`
- Create: `server/src/ScholarPath.Domain/Enums/AiJobStatus.cs`
- Create: `server/src/ScholarPath.API/Controllers/AiController.cs`
- Create: `server/src/ScholarPath.Infrastructure/Services/AiAnalysisService.cs`
- Create: `server/src/ScholarPath.Infrastructure/Jobs/AiAnalysisJob.cs`
- Modify: `server/src/ScholarPath.Infrastructure/Persistence/ApplicationDbContext.cs`

**Steps:**

1. Create enums:
   ```csharp
   public enum AnalysisType { ImproveClarity = 0, GrammarCheck = 1, TailorToScholarship = 2, FullReview = 3 }
   public enum AiJobStatus { Queued = 0, Processing = 1, Completed = 2, Failed = 3 }
   ```

2. Create `AiAnalysisJob` entity:
   - UserId, AnalysisType, ScholarshipId (optional)
   - Status (AiJobStatus), FileName, FileContentType, FileSize
   - ResultJson (structured output: strengths[], issues[], suggestions[], sampleRewrites[])
   - ErrorMessage, RetryCount, CreatedAt, CompletedAt
   - Auto-delete after 7 days (background job)

3. Create `AiController`:
   - `POST /api/v1/ai/analyze-document` — multipart upload, validate (PDF/DOCX, 5MB max), create job, return jobId
   - `GET /api/v1/ai/jobs/{id}` — poll status + result
   - `DELETE /api/v1/ai/jobs/{id}` — delete job + file
   - `GET /api/v1/ai/match-explanation?scholarshipId=` — AI-generated match explanation
   - Rate limit: 5 jobs/day/user

4. Create `AiAnalysisService`:
   - Extract text from PDF/DOCX
   - Call Anthropic Claude API with structured prompt
   - Parse structured response
   - Timeout 60s, max 2 retries

5. Create Hangfire background job for processing.

6. Create migration. Add DbSet.

7. Commit: `feat(m12): add AI document analysis backend with async job system`

---

### Task 12.2: AI Assistance Frontend

**Files:**
- Create: `client/src/pages/dashboard/AiAssistance.tsx`
- Create: `client/src/components/ai/AnalysisResults.tsx`
- Create: `client/src/components/ai/MatchExplanationDrawer.tsx`
- Create: `client/src/services/aiService.ts`
- Modify: `client/src/App.tsx`
- Modify: `client/src/components/scholarships/RecommendedCarousel.tsx` — add "Why recommended?" drawer

**Steps:**

1. Create `aiService.ts`:
   - `submitAnalysis(file, type, scholarshipId?): Promise<{jobId}>`
   - `getJobStatus(jobId): Promise<AiJobDto>`
   - `deleteJob(jobId): Promise<void>`
   - `getMatchExplanation(scholarshipId): Promise<MatchExplanationDto>`

2. Create `AiAssistance.tsx` at `/dashboard/ai`:
   - Privacy notice banner (checkbox acknowledgment)
   - Upload zone (drag-drop, PDF/DOCX, 5MB)
   - Analysis type selector
   - Scholarship search dropdown (for "Tailor to Scholarship")
   - Submit button
   - Job status: Queued → Processing (progress animation) → Complete/Failed
   - Polling every 3s during Processing
   - AnalysisResults component: Strengths (green), Issues (red), Suggestions (blue), Sample Rewrites (collapsible)
   - Delete analysis button with confirmation
   - AI disclaimer text
   - Feedback buttons (Helpful/Not Helpful)
   - Job history list

3. Create `MatchExplanationDrawer.tsx`:
   - "Why recommended?" button on recommendation cards
   - Drawer with AI explanation: matching factors, strengths, gaps
   - Skeleton while loading
   - Fallback to static matchReasons
   - Feedback thumbs

4. Add route: `/dashboard/ai`

5. i18n keys.

6. Commit: `feat(m12): implement AI document analysis UI and match explanation drawer`

---

## Final Tasks

### Task F.1: Database Migration & Seed Data

**Steps:**

1. Consolidate all entity changes into a clean migration (or keep per-module migrations).
2. Update seed data if needed.
3. Test migration up/down.
4. Commit: `chore: consolidate sprint 2 database migrations`

---

### Task F.2: Integration Testing

**Steps:**

1. Add integration tests for key endpoints per module.
2. Test auth flows, pagination, filtering.
3. Commit: `test: add sprint 2 integration tests`

---

### Task F.3: i18n Completion

**Steps:**

1. Ensure all new UI text has en.json and ar.json entries.
2. Verify RTL rendering for all new pages.
3. Commit: `chore: complete sprint 2 i18n translations`

---

## Dependency Graph

```
Module 2 (Scholarship Search) ──→ Module 3 (Details & Apply)
                                      │
                                      ▼
                               Module 4 (Dashboard & Tracking)
                                      │
                               ┌──────┼──────┐
                               ▼      ▼      ▼
                            Mod 5  Mod 8   Mod 9
                          (Resources)(Calendar)(Notifications)
                               │
                               ▼
                            Mod 6 (Stories)
                               │
                               ▼
                            Mod 7 (Mentorship)
                               │
                               ▼
                            Mod 10 (Admin)
                               │
                               ▼
                            Mod 11 (Community)
                               │
                               ▼
                            Mod 12 (AI)
```

Module 9 (Notifications) is a cross-cutting concern used by Modules 4, 6, 7, 8, 11. It should be implemented early or its service interface stubbed.
