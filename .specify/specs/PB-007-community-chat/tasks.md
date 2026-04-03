# PB-007 — Tasks

**Owner**: @yousra-elnoby  •  **Est**: 44 pts  •  **Iteration**: 4

## Backend — Community
- [ ] T-001 — CRUD posts + replies + categories (FR-102..FR-103)
- [ ] T-002 — Vote toggle + self-vote block (FR-104..FR-105)
- [ ] T-003 — Flag + auto-hide at 3 distinct flags (FR-106..FR-107)
- [ ] T-004 — Broadcast new post/reply via `CommunityHub` (FR-108)
- [ ] T-005 — Sanitize post content server-side

## Backend — Chat
- [ ] T-006 — `ChatHub` with auth + presence (FR-109..FR-110)
- [ ] T-007 — `SendMessageCommand` + persistence (FR-111)
- [ ] T-008 — `BlockUserCommand` / `UnblockUserCommand` + enforcement (FR-112, FR-133..FR-134)
- [ ] T-009 — `GetMessagesQuery` with cursor paging
- [ ] T-010 — Integration tests for block enforcement

## Frontend
- [ ] T-011 — `Community.tsx` feed with category sidebar + sort
- [ ] T-012 — `CommunityThread.tsx` post + replies + vote UI
- [ ] T-013 — Flag menu + confirmation
- [ ] T-014 — `Chat.tsx` conversation list
- [ ] T-015 — `ChatWindow` with message bubbles + typing indicator
- [ ] T-016 — Block/unblock toggle in chat header
- [ ] T-017 — Presence dot in conversation list
- [ ] T-018 — SignalR typed client wrappers
- [ ] T-019 — Arabic copy review

## QA
- [ ] T-020 — E2E: post → reply → vote → flag (×3) → auto-hidden
- [ ] T-021 — E2E: chat between two users; block; sender fails; unblock

## Done criteria
Auto-hide rule tested; block enforcement tested; real-time messages <200ms in dev.
