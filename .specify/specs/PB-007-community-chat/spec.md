# PB-007 — Community and Chat

**Owner**: @yousra-elnoby • **Priority**: High • **Iteration**: 4 • **Est**: 44 pts

## Problem statement

Authenticated users share knowledge via a threaded forum (categories, posts, replies, voting, flagging with 3+ auto-hide) and communicate 1:1 via real-time chat with online presence and block capability. Both surfaces are gated; content moderation runs in the admin queue (PB-011).

## User stories

| ID | Story | Size |
|----|-------|------|
| US-072 | Create a post | 3pt |
| US-073 | Reply to posts | 3pt |
| US-074 | Upvote/downvote content | 3pt |
| US-075 | No self-voting | 2pt |
| US-076 | Flag inappropriate content | 3pt |
| US-077 | Auto-hide after 3 valid distinct flags | 3pt |
| US-078 | Admin moderates flagged posts | 5pt |
| US-079 | 1:1 real-time chat | 4pt |
| US-080 | View chat history | 3pt |
| US-081 | Block users | 3pt |
| US-095 | Post validation feedback | 2pt |
| US-096 | Reply validation feedback | 2pt |
| US-097 | Flagging rules | 3pt |
| US-098 | Block user from new chats | 3pt |
| US-099 | Unblock user | 2pt |

## Functional requirements

FR-102 .. FR-112, FR-129 .. FR-134

## Acceptance criteria

1. **Post/reply CRUD** — categories configured by admin; posts up to 10,000 chars; markdown rendering; basic sanitization (DOMPurify).
2. **Voting** — 1 vote per user per post/reply; toggle or switch; self-vote returns 403.
3. **Flagging** — Each user can flag each post once; accumulates until 3+ distinct → post auto-hidden, routed to admin queue with `ModerationStatus=PendingAdmin`.
4. **Real-time chat** — SignalR hub `ChatHub`; typed events: `SendMessage`, `MessageReceived`, `TypingStart`, `TypingStop`, `OnlineStatus`.
5. **Presence** — Online set in Redis (feature-flagged to in-memory store in dev); auto-expire via SignalR disconnect hook.
6. **Block** — `BlockUserCommand` prevents blocked user from sending new messages; existing conversations read-only; blocker can unblock.
7. **History** — `GET /api/chat/conversations/{id}/messages?cursor=...` paginated by ascending timestamp.

## Non-goals

- Group chat / channels (v2)
- Message editing/deletion (v2)
- File attachments in chat (v2)
- Rich text in chat messages
