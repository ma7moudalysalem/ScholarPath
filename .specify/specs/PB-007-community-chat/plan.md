# PB-007 — Implementation Plan

## Architecture touchpoints

### Domain
- `ForumCategory`, `ForumPost` (self-ref for replies), `ForumPostAttachment`, `ForumVote`, `ForumFlag`
- `ChatConversation`, `ChatMessage`, `UserBlock`
- Enums: `VoteType { Up, Down }`, `PostModerationStatus { Visible, Hidden, Removed }`

### Application
- Community commands: `CreatePostCommand`, `EditPostCommand`, `DeletePostCommand`, `VoteCommand`, `FlagCommand`, `UnflagCommand`
- Community queries: `GetPostsQuery` (by category, sort), `GetPostQuery` (with replies), `GetFlaggedPostsQuery` (admin)
- Chat commands: `SendMessageCommand`, `BlockUserCommand`, `UnblockUserCommand`, `MarkReadCommand`
- Chat queries: `GetConversationsQuery`, `GetMessagesQuery` (cursor paging), `GetPresenceQuery`
- Services: `AutoHideThresholdChecker` (triggered on FlagCommand)

### Infrastructure
- `Hubs/ChatHub.cs` — authenticated SignalR hub; enforces block rules
- `Hubs/CommunityHub.cs` — broadcasts post/reply live updates
- Presence tracking: `IPresenceService` with in-memory default, Redis adapter when enabled

### API
- `/api/forum/categories`, `/api/forum/posts`, `/api/forum/posts/{id}/vote`, `/api/forum/posts/{id}/flag`
- `/api/chat/conversations`, `/api/chat/messages`, `/api/chat/block`, `/api/chat/unblock`

### Frontend
- Pages: `Community.tsx` (feed), `CommunityThread.tsx`, `Chat.tsx` (conversation list + thread)
- Components: `PostEditor`, `ReplyThread`, `VoteButtons`, `FlagMenu`, `ChatWindow`, `OnlineDot`, `BlockToggle`
- SignalR clients via `src/services/signalR/`

### Tests
- Unit: auto-hide threshold logic; vote toggle; block enforcement
- Integration: post → 3 users flag → auto-hidden; chat sender blocked → send fails
- E2E: post → reply → upvote; chat between two test users

## Dependencies
PB-001, PB-011 (moderation)

## Risks
1. Self-hosted SignalR scaling — need Redis backplane for multi-instance
2. Flag abuse — rate-limit flag action per user
3. XSS in post content — enforce DOMPurify on render + server sanitization
