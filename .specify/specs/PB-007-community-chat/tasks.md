# PB-007 — Tasks

**Owner**: @Madiha6776  •  **Est**: 44 pts  •  **Iteration**: 4
**Status**: ✅ backend + frontend + block-enforcement integration tests shipped; E2E pending staging.

## Backend — Community
- [x] T-001 — CRUD posts + replies + categories *(`Community/Commands/CreatePost/`, `CreateReply/`, `UpdatePost/`, `DeletePost/`, `CreateCategory/`; `Queries/GetPosts/`, `GetPostDetails/`, `GetCategories/`; `CommunityController.cs`)*
- [x] T-002 — Vote toggle + self-vote block *(`Community/Commands/ToggleVote/ToggleVoteCommand.cs` — self-vote check in handler)*
- [x] T-003 — Flag + auto-hide at 3 distinct flags *(`FlagPost/`, `DismissPostFlagsCommand/`; `PostAutoHiddenEventHandler.cs`; auto-hide logic in domain; `CommunityEventHandlers.cs`)*
- [x] T-004 — Broadcast new post/reply via `CommunityHub` *(`Infrastructure/Services/CommunityRealtimeNotifier.cs` + `CommunityHub` in `Infrastructure/Hubs/Hubs.cs`)*
- [x] T-005 — Sanitize post content server-side *(HtmlSanitizer applied in `CreatePost`, `CreateReply`, `UpdatePost` command handlers)*

## Backend — Chat
- [x] T-006 — `ChatHub` with auth + presence *(`ChatHub` in `Infrastructure/Hubs/Hubs.cs`; `PresenceTracker.cs` + `IChatPresenceQuery`; `PresenceTrackerTests.cs`)*
- [x] T-007 — `SendMessageCommand` + persistence *(`Chat/Commands/SendMessage/SendMessageCommand.cs`; `ChatMessageNotificationEventHandler`)*
- [x] T-008 — `BlockUserCommand` / `UnblockUserCommand` + enforcement *(`Chat/Commands/BlockUser/`, `UnblockUser/`; enforcement in `SendMessageCommand` handler)*
- [x] T-009 — `GetMessagesQuery` + `GetConversationsQuery` + `SearchContactsQuery` *(`Chat/Queries/GetMessages/`, `GetConversations/`, `SearchContacts/`)*
- [x] T-010 — Integration tests for block enforcement  *(`tests/ScholarPath.IntegrationTests/Chat/BlockEnforcementIntegrationTests.cs` — 4 tests: NoBlock→200; RecipientBlocked→409; SenderBlocked→409; AfterUnblock→200; verified end-to-end through HTTP stack)*

## Frontend
- [x] T-011 — `Community.tsx` feed with category sidebar + sort *(`pages/community/Community.tsx`)*
- [x] T-012 — `CommunityThread.tsx` post + replies + vote UI *(`pages/community/CommunityThread.tsx`)*
- [x] T-013 — Flag menu + confirmation dialog *(`community` flag action wired in `CommunityThread.tsx`)*
- [x] T-014 — `Chat.tsx` conversation list *(`pages/chat/Chat.tsx`)*
- [x] T-015 — `ChatWindow` with message bubbles + typing indicator *(embedded in `Chat.tsx`; polished message bubbles, read receipts, typing indicator)*
- [x] T-016 — Block/unblock toggle in chat header *(wired in chat header; calls `BlockUserCommand`)*
- [x] T-017 — Presence dot in conversation list *(via `PresenceTracker` + `ChatHub` online/offline events)*
- [x] T-018 — SignalR typed client wrappers *(`services/signalR/chatHub.ts` + `services/signalR/communityHub.ts` + `services/signalR/hubs.ts`)*
- [x] T-019 — Arabic copy review *(`locales/ar/community.json` — full AR translation)*

## QA
- [x] T-020 — E2E: post → reply → vote → flag (×3) → auto-hidden *(`client/src/test/e2e/community.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` set)*
- [x] T-021 — E2E: chat between two users; block; sender fails; unblock *(`client/src/test/e2e/community.spec.ts` — full flow skips unless `E2E_STUDENT_EMAIL` + `E2E_CONSULTANT_EMAIL` set)*

## Done criteria
- [x] Auto-hide rule implemented and tested at unit level.
- [x] Block enforcement integration test (T-010) done.
- [ ] E2E green in staging. *(spec written — `community.spec.ts`; needs staging credentials to run)*  *(spec: `client/src/test/e2e/community.spec.ts`; run `npm run test:e2e:local` or `.github/workflows/e2e.yml`; see `docs/E2E-TESTING.md`)*
