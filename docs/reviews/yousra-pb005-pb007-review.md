# Code Review — `feat/yosra-pb005-pb007`

**Reviewer**: @ma7moudalysalem · **Author**: @yousra-elnoby · **Date**: 2026-04-25
**Branch**: `feat/yosra-pb005-pb007` · **vs**: `main` · **PR**: #15 (DRAFT)
**Scope**: PB-005 Company Reviews + PB-007 Community & Chat (full vertical slice; also touches PB-004 company-side application review)
**Stats**: +4,650 / −28 across 97 files · 2 commits · 20 commits behind `main`

---

## TL;DR

Huge surface area landed here — Company Reviews payment/refund flow, the Community module (posts/replies/votes/flags/auto-hide), 1:1 Chat with SignalR, and the company-side Application review. Volume is genuinely impressive for one PR. The folder structure is correct, validators exist, EN copy is in place, and three test classes cover the most important pieces.

That said, **the branch does not currently compile** (`dotnet build` produces 12 errors in the Application project), and there are a handful of money-logic and security blockers under the layer-violation surface. Most issues are mechanical; the architecture is sound once the layering and event base-class problems are fixed.

**Verdict**: 🚫 **request-changes**. The build must be green and the money/webhook bugs must be fixed before this can be re-reviewed. Once those land, the rest is mostly polish.

---

## 🔴 Blockers (must-fix before merge)

### B1. The branch does not compile — 12 errors in `ScholarPath.Application`

I checked out the branch into a worktree and ran `dotnet build`. Output:

```
Build FAILED. 12 Error(s)
```

The errors fall into 5 buckets, each is a quick fix:

#### (a) Application layer references Infrastructure (Clean Architecture violation + CS0234)
[`server/src/ScholarPath.Application/Chat/Commands/SendMessage/SendMessageCommand.cs:8`](server/src/ScholarPath.Application/Chat/Commands/SendMessage/SendMessageCommand.cs:8)
[`server/src/ScholarPath.Application/Community/EventHandlers/CommunityEventHandlers.cs:6`](server/src/ScholarPath.Application/Community/EventHandlers/CommunityEventHandlers.cs:6)

```csharp
using ScholarPath.Infrastructure.Hubs;          // ← forbidden
using Microsoft.AspNetCore.SignalR;              // ← also forbidden in Application
```

`ScholarPath.Application.csproj` only references `ScholarPath.Domain` (correct — Constitution principle II). The Application layer cannot know about Infrastructure or about ASP.NET-specific assemblies like SignalR. The **right fix** is to introduce abstractions in Application and implement them in Infrastructure:

```csharp
// Application/Common/Interfaces/IChatRealtimeNotifier.cs
public interface IChatRealtimeNotifier
{
    Task NotifyNewMessageAsync(Guid conversationId, Guid messageId, Guid senderId, string body, DateTimeOffset sentAt, CancellationToken ct);
}

public interface ICommunityRealtimeNotifier
{
    Task NotifyNewPostAsync(Guid postId, string categorySlug, CancellationToken ct);
    Task NotifyNewReplyAsync(Guid replyId, Guid parentPostId, CancellationToken ct);
}
```

Then Infrastructure implements them with the actual `IHubContext<ChatHub>` / `IHubContext<CommunityHub>` and registers in DI. The handler now depends only on the abstraction.

#### (b) Domain events don't implement `INotification` (CS0311 ×3)
[`CommunityEventHandlers.cs:10`](server/src/ScholarPath.Application/Community/EventHandlers/CommunityEventHandlers.cs:10)
[`CompanyReviews/EventHandlers/ApplicationStatusChangedEventHandler.cs:9`](server/src/ScholarPath.Application/CompanyReviews/EventHandlers/ApplicationStatusChangedEventHandler.cs:9)

`Domain/Common/BaseEntity.cs` defines:

```csharp
public abstract record DomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
```

It needs to implement MediatR's `INotification` for handlers to bind. Either:

```csharp
// Domain/Common/BaseEntity.cs — Domain owning a MediatR type is acceptable per project convention
public abstract record DomainEvent : MediatR.INotification { ... }
```

Or wrap `DomainEvent` with a MediatR notification in Infrastructure dispatch. The first is what Tasneem's `BookingConfirmedEvent` etc. follow — check those.

#### (c) `AuditAction` not in scope (CS0103 ×2)
[`WithdrawApplicationCommand.cs:6`](server/src/ScholarPath.Application/Applications/Commands/WithdrawApplication/WithdrawApplicationCommand.cs:6)
[`SubmitCompanyRatingCommand.cs:6`](server/src/ScholarPath.Application/CompanyReviews/Commands/SubmitCompanyRating/SubmitCompanyRatingCommand.cs:6)

Both files need `using ScholarPath.Domain.Enums;` (where `AuditAction` lives). Compare against [`server/src/ScholarPath.Application/Admin/Commands/SetUserStatus/SetUserStatusCommand.cs`](server/src/ScholarPath.Application/Admin/Commands/SetUserStatus/SetUserStatusCommand.cs) — the working version has both usings.

#### (d) `Paginated<>` does not exist (CS0246) and return-type mismatch (CS0738)
[`Community/Queries/GetPosts/GetPostsQuery.cs:18`](server/src/ScholarPath.Application/Community/Queries/GetPosts/GetPostsQuery.cs:18)

```csharp
public sealed record GetPostsQuery(...) : IRequest<PagedResult<ForumPostDto>>;       // declared
public async Task<Paginated<ForumPostDto>> Handle(...)                                // implemented (typo)
```

`Paginated` is undefined. Replace with `PagedResult<ForumPostDto>` (which is the type already defined in [`Applications/DTOs/ApplicationDTOs.cs`](server/src/ScholarPath.Application/Applications/DTOs/ApplicationDTOs.cs)).

#### (e) `global.json` reverts the SDK from .NET 10 to .NET 9
[`server/global.json`](server/global.json)

```diff
- "version": "10.0.201",
+ "version": "9.0.102",
```

The constitution pins .NET 10. This change will fail CI. Drop this file from the diff.

---

### B2. Stripe webhook is parsed by regex — bypasses Stripe.net signature library
[`server/src/ScholarPath.Application/Payments/Commands/ProcessStripeWebhook/ProcessStripeWebhookCommand.cs:62`](server/src/ScholarPath.Application/Payments/Commands/ProcessStripeWebhook/ProcessStripeWebhookCommand.cs:62)

```csharp
private string? ExtractIntentId(string json)
{
    // Simple extraction for stub/demo purposes
    var match = System.Text.RegularExpressions.Regex.Match(json, "\"id\":\\s*\"(pi_[^\"]+)\"");
    return match.Success ? match.Groups[1].Value : null;
}
```

Two problems:
1. **Picks the wrong `id`**. Stripe webhooks have `"id": "evt_..."` at the root and the PaymentIntent `"id": "pi_..."` nested under `data.object`. Regex matches the **first** `pi_*` but if the order is different or the payload contains a charge with an associated `pi_*` first, it will silently update the wrong record.
2. **Bypasses the Stripe.net `EventUtility.ConstructEvent`** that we already invoke in `WebhooksController` (`stripeService.ParseWebhook`). The handler should accept the parsed `Event` (or a strongly-typed DTO with `PaymentIntentId`, `ChargeId`, `Amount`, etc.) — not the raw JSON.

**Fix**: extend `StripeWebhookParseResult` so `ParseWebhook` extracts the typed sub-object once, then pass it to the handler:

```csharp
public sealed record StripeWebhookParseResult(
    string EventId,
    string EventType,
    string? PaymentIntentId,
    string? ChargeId,
    long? AmountCents,
    string DataJson);
```

### B3. Webhook handler never persists `StripeWebhookEvent` — idempotency window is wide open
[`ProcessStripeWebhookCommand.cs:18`](server/src/ScholarPath.Application/Payments/Commands/ProcessStripeWebhook/ProcessStripeWebhookCommand.cs:18)

The `StripeWebhookEvent` entity exists in [`Domain/Entities/Payments.cs`](server/src/ScholarPath.Domain/Entities/Payments.cs) precisely so we can dedupe by `StripeEventId`. The handler never reads or writes that table. If Stripe retries the same event (which it does — they retry up to 3 days), we re-process and the refund / capture path runs again.

**Fix**: at the top of `Handle`:

```csharp
if (await db.StripeWebhookEvents.AnyAsync(e => e.StripeEventId == request.EventId, ct))
    return true; // already processed, idempotent ack

db.StripeWebhookEvents.Add(new StripeWebhookEvent
{
    StripeEventId = request.EventId,
    EventType = request.EventType,
    RawPayload = request.DataJson,
});
// ... later, after success:
webhookEvent.ProcessedAt = DateTimeOffset.UtcNow;
webhookEvent.IsProcessed = true;
```

This is the same pattern Tasneem flagged she's missing in PB-006 — let's get it right here once and reuse.

### B4. `RefundCompanyReviewCommand` partial-refund path silently no-ops on Stripe failure
[`server/src/ScholarPath.Application/CompanyReviews/Commands/RefundCompanyReview/RefundCompanyReviewCommand.cs:73`](server/src/ScholarPath.Application/CompanyReviews/Commands/RefundCompanyReview/RefundCompanyReviewCommand.cs:73)

```csharp
if (stripeResult.Status == "succeeded")
{
    payment.Status = PaymentStatus.PartiallyRefunded;
    // ... updates fields ...
}
// else: nothing — no exception, no logging, no payment state change
```

If Stripe returns `requires_action` / `processing` / anything other than `"succeeded"`, the handler silently returns `true` and `await db.SaveChangesAsync` writes nothing useful. The student is told the refund was processed; the payment is still `Held`. This is the same shape of bug as Tasneem's B1.

**Fix**: throw a domain exception (or set `PaymentStatus.Failed` + `FailureReason`) and let the caller / job retry. Add a unit test that simulates `stripeResult.Status = "requires_action"` and asserts the payment record was not updated to `PartiallyRefunded`.

### B5. Idempotency keys are random GUIDs on every Stripe call — defeats the purpose
[`CaptureCompanyReviewPaymentCommand.cs:31`](server/src/ScholarPath.Application/CompanyReviews/Commands/CaptureCompanyReviewPayment/CaptureCompanyReviewPaymentCommand.cs:31)
[`RefundCompanyReviewCommand.cs:42`](server/src/ScholarPath.Application/CompanyReviews/Commands/RefundCompanyReview/RefundCompanyReviewCommand.cs:42)
[`RefundCompanyReviewCommand.cs:67`](server/src/ScholarPath.Application/CompanyReviews/Commands/RefundCompanyReview/RefundCompanyReviewCommand.cs:67)
[`CompanyReviewTimeoutRefundJob.cs:51`](server/src/ScholarPath.Infrastructure/Jobs/CompanyReviewTimeoutRefundJob.cs:51)

```csharp
await stripeService.CapturePaymentIntentAsync(payment.StripePaymentIntentId, null, Guid.NewGuid().ToString("N"), ct);
```

A new GUID every call means Stripe treats every retry as a brand-new request. If MediatR retries (transient SQL error / network blip) we'll capture the same PaymentIntent twice and the second call will throw — but the user-facing behaviour is unpredictable. Use a deterministic key derived from the payment + operation:

```csharp
var idemKey = $"company-review-capture:{payment.Id:N}";
var idemKey = $"company-review-refund:{payment.Id:N}:{(request.IsFullRefund ? "full" : "partial")}";
var idemKey = $"company-review-timeout-refund:{payment.Id:N}";
```

The Stripe.net library passes this through to Stripe's `Idempotency-Key` header. Tasneem's PB-006 review already calls this pattern out as one of the things she got right.

### B6. `ConfigureReviewFeeCommand` has no validator — negative or zero fees accepted
[`server/src/ScholarPath.Application/Scholarships/Commands/ConfigureReviewFee/ConfigureReviewFeeCommandValidator.cs`](server/src/ScholarPath.Application/Scholarships/Commands/ConfigureReviewFee/ConfigureReviewFeeCommandValidator.cs)

The file exists and registers a validator class but **the body is empty** — no `RuleFor(...)` lines. The controller accepts any decimal, including negative numbers. A negative fee then gets stored on the Scholarship and used to compute `AmountCents` in the eventual PaymentIntent — Stripe will reject the call but the listing is now in a corrupted state.

**Fix**:
```csharp
RuleFor(v => v.ReviewFeeUsd)
    .GreaterThanOrEqualTo(0m)
    .LessThanOrEqualTo(500m)
    .WithMessage("Review fee must be between $0 and $500.");
RuleFor(v => v.ScholarshipId).NotEmpty();
```

### B7. `SendMessageCommand` argument mismatch — chat is broken end-to-end
[`server/src/ScholarPath.API/Controllers/ChatController.cs:39`](server/src/ScholarPath.API/Controllers/ChatController.cs:39)

The frontend `chatApi.sendMessage` and the controller `SendMessageRequest` both use `RecipientId`:

```typescript
async sendMessage(req: { recipientId: string; body: string }): Promise<string>
```

```csharp
public record SendMessageRequest(Guid RecipientId, string Body);
// ... in controller:
var command = new SendMessageCommand(request.RecipientId, request.Body);
```

But `SendMessageCommand` is defined as:

```csharp
public sealed record SendMessageCommand(Guid ConversationId, string Body) : IRequest<Guid>;
```

So `RecipientId` is silently passed as `ConversationId`. Inside the handler, `db.Conversations.FirstOrDefaultAsync(c => c.Id == request.ConversationId)` will return `null` and throw `NotFoundException`. **No message will ever be sent through this controller path.**

The handler logic is fine — it expects a known conversation. The controller / frontend should:
1. Either look up (or create) a conversation between the two participants, then send the conversation id to `SendMessageCommand`.
2. Or change the command to `(Guid RecipientId, string Body)` and have it find/create the conversation internally.

Option 2 matches the intent of `T-007` and is what the spec implies.

### B8. The 3 commands listed below are missing `[Auditable(...)]`
Constitution principle: every state-mutating command goes through `AuditBehavior`. The PR has `[Auditable]` on `SubmitCompanyRatingCommand`, `WithdrawApplicationCommand`, `ReviewApplicationCommand`, `ConfigureReviewFeeCommand` (good!), but the following are missing it:

- [`CaptureCompanyReviewPaymentCommand`](server/src/ScholarPath.Application/CompanyReviews/Commands/CaptureCompanyReviewPayment/CaptureCompanyReviewPaymentCommand.cs:9) — money moves; needs audit
- [`RefundCompanyReviewCommand`](server/src/ScholarPath.Application/CompanyReviews/Commands/RefundCompanyReview/RefundCompanyReviewCommand.cs:9) — money moves; needs audit
- [`UpdateExternalStatusCommand`](server/src/ScholarPath.Application/Applications/Commands/UpdateExternalStatus/UpdateExternalStatusCommand.cs:5) — student-mutates application state
- All 7 Community commands ([`CreatePost`](server/src/ScholarPath.Application/Community/Commands/CreatePost/CreatePostCommand.cs), [`UpdatePost`](server/src/ScholarPath.Application/Community/Commands/UpdatePost/UpdatePostCommand.cs), [`DeletePost`](server/src/ScholarPath.Application/Community/Commands/DeletePost/DeletePostCommand.cs), [`CreateReply`](server/src/ScholarPath.Application/Community/Commands/CreateReply/CreateReplyCommand.cs), [`FlagPost`](server/src/ScholarPath.Application/Community/Commands/FlagPost/FlagPostCommand.cs), [`ToggleVote`](server/src/ScholarPath.Application/Community/Commands/ToggleVote/ToggleVoteCommand.cs), [`CreateCategory`](server/src/ScholarPath.Application/Community/Commands/CreateCategory/CreateCategoryCommand.cs))
- All 3 Chat commands ([`SendMessage`](server/src/ScholarPath.Application/Chat/Commands/SendMessage/SendMessageCommand.cs), [`BlockUser`](server/src/ScholarPath.Application/Chat/Commands/BlockUser/BlockUserCommand.cs), [`UnblockUser`](server/src/ScholarPath.Application/Chat/Commands/UnblockUser/UnblockUserCommand.cs))

The audit log is what we use to chase moderation appeals, payment disputes, and account-status complaints — these absolutely need to be there.

### B9. Arabic translation files are NOT loaded
[`client/src/lib/i18n.ts:23`](client/src/lib/i18n.ts:23)

The `applications.json` and `company.json` AR files exist (good — translation work was done) but are not imported nor registered in the `resources.ar` block:

```typescript
import enApplications from "@/locales/en/applications.json";
import enCompany from "@/locales/en/company.json";
// ❌ no `import arApplications` / `import arCompany`

resources: {
  en: { ..., applications: enApplications, company: enCompany },
  ar: { ..., /* applications and company missing */ },
}
```

Result: every AR user sees the EN text for these screens. Constitution principle IV: "An English-only page or an untranslated label is a shipping blocker." Add the imports and entries.

### B10. `WebhooksController` is `[AllowAnonymous]` but doesn't reject if `WebhookSecret` is missing
[`server/src/ScholarPath.API/Controllers/WebhooksController.cs:31`](server/src/ScholarPath.API/Controllers/WebhooksController.cs:31)

```csharp
var parsed = stripeService.ParseWebhook(json, signature, stripeOptions.Value.WebhookSecret!);
```

If `WebhookSecret` is null/empty in config, the `!` suppresses the warning and Stripe.net will throw — but the catch block returns `BadRequest()` with no log line that distinguishes "we are misconfigured" from "Stripe sent us a bad signature". A misconfigured production deployment will accept zero webhooks and look identical to a normal Stripe outage on the dashboard.

**Fix**: at startup or controller construction, throw if `WebhookSecret` is empty in non-Development environments. Log the parse exception with `EventId`/source IP for visibility.

---

## 🟡 Important — architecture / convention

### I1. `ApplicationStatusChangedEventHandler` triggers payment capture on *Rejected* too
[`CompanyReviews/EventHandlers/ApplicationStatusChangedEventHandler.cs:14`](server/src/ScholarPath.Application/CompanyReviews/EventHandlers/ApplicationStatusChangedEventHandler.cs:14)

```csharp
if (notification.NewStatus is ApplicationStatus.Accepted or ApplicationStatus.Rejected)
{
    var captureCommand = new CaptureCompanyReviewPaymentCommand(notification.ApplicationId);
    await sender.Send(captureCommand, ct);
}
```

The spec acceptance criterion #3 says: **"if rejected → no refund"**, but it also implies the company *earns the fee on review completion*. So capturing on `Rejected` is intentional — but the spec criterion #2 **"Funds held until Company completes review"** plus FR-075 are ambiguous on whether a Rejected outcome captures or not. Confirm with Mahmoud / spec, then either:
- Keep capture on both (current behaviour) and document it as policy, or
- Capture only on Accepted, refund 100% on Rejected.

If the answer is "capture on both", current code is right but there's still a subtler bug: if the company *never* reviews and 14 days pass after the deadline, the timeout job refunds 100% (correct per criterion #3). But if the company reviews on day 15, this handler then tries to capture an already-cancelled PaymentIntent, which Stripe will reject. There's no guard for "payment already refunded".

Add at the top of `CaptureCompanyReviewPaymentCommandHandler.Handle`:
```csharp
if (payment.Status != PaymentStatus.Held) return false; // already captured/refunded
```
You already have the `Status == PaymentStatus.Held` filter in the WHERE clause — that handles it. Just confirm the handler returns `false` cleanly without throwing when the payment is in a terminal state.

### I2. `CompanyReviewTimeoutRefundJob` swallows errors per-application but shares one DbContext for the batch
[`server/src/ScholarPath.Infrastructure/Jobs/CompanyReviewTimeoutRefundJob.cs:42`](server/src/ScholarPath.Infrastructure/Jobs/CompanyReviewTimeoutRefundJob.cs:42)

The job updates `payment` records inside a `foreach` loop and only calls `SaveChangesAsync` once at the end (line 80). If one Stripe call throws (caught by the inner try), the partial mutations on EARLIER payments still persist. If a later one throws *outside* the inner try (e.g., DB exception), all updates are lost.

**Fix**: per-app `SaveChangesAsync` inside the loop, and use `IServiceScopeFactory` so each app gets its own DbContext + Stripe service if Hangfire ever runs them in parallel.

Also: the job constructor takes `ApplicationDbContext` directly, not the abstraction (`IApplicationDbContext`). Switch to the interface for consistency with handlers.

### I3. `CompanyReviewPricingService.CalculateFeesAsync` returns the input as `TotalFeeUsd`
[`server/src/ScholarPath.Application/CompanyReviews/Services/CompanyReviewPricingService.cs:19`](server/src/ScholarPath.Application/CompanyReviews/Services/CompanyReviewPricingService.cs:19)

```csharp
return (baseReviewFeeUsd, platformFeeUsd, companyPayoutUsd);
```

`TotalFeeUsd == baseReviewFeeUsd`. Either rename the field to `BaseFeeUsd` for clarity or include any platform-side surcharge in the total. Today the student is charged exactly the base fee and the platform takes its cut from the company's portion — that's the intended model, but the field name is misleading.

Also: the service is registered nowhere in DI (no `AddScoped<ICompanyReviewPricingService, CompanyReviewPricingService>()` in `ConfigureServices.cs`). It's also not actually called from any command in this PR. The pricing call presumably belongs in `SubmitApplicationCommand` (PB-004) when creating the PaymentIntent — that handler isn't in this slice. Document this in `_Module.md` so Norra picks it up.

### I4. `ProcessStripeWebhookCommandValidator.cs` is an empty class
[`server/src/ScholarPath.Application/Payments/EventHandlers/ProcessStripeWebhookCommandValidator.cs`](server/src/ScholarPath.Application/Payments/EventHandlers/ProcessStripeWebhookCommandValidator.cs)

The file exists, is named `*Validator`, doesn't extend `AbstractValidator<T>`, and contains a TODO comment in lieu of a body. Worse: it lives in `Payments/EventHandlers/` while the command lives in `Payments/Commands/ProcessStripeWebhook/`. Either:
- Move it next to the command and make it a real `AbstractValidator<ProcessStripeWebhookCommand>` that asserts `EventId` non-empty and `EventType` is in a known whitelist, or
- Delete it.

### I5. Folder-name / namespace mismatch: `Payments/EventHandlers/`
[`server/src/ScholarPath.Application/Payments/EventHandlers/ProcessStripeWebhookCommandValidator.cs:5`](server/src/ScholarPath.Application/Payments/EventHandlers/ProcessStripeWebhookCommandValidator.cs:5)

Namespace inside the file: `ScholarPath.Application.Payments.EventHandlers`. Folder layout matches, but the file is the validator for the `ProcessStripeWebhookCommand` — should be in the command's folder.

### I6. `SubmitCompanyRatingCommand` doesn't trust the `CompanyId` from the request
[`SubmitCompanyRatingCommandHandler.cs:30`](server/src/ScholarPath.Application/CompanyReviews/Commands/SubmitCompanyRating/SubmitCompanyRatingCommandHandler.cs:30)

The student sends `(ApplicationId, CompanyId, Rating, Comment)` and the handler stores the rating against `CompanyId` from the request. A malicious student could submit a rating for `App = mine, Company = Apple` to defame any company — there's no check that the application's scholarship.OwnerCompanyId matches the supplied `CompanyId`.

**Fix**: drop `CompanyId` from the command and resolve it server-side:

```csharp
var application = await db.Applications
    .Include(a => a.Scholarship)
    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
    ?? throw new NotFoundException(...);

var companyId = application.Scholarship?.OwnerCompanyId
    ?? throw new ConflictException("Application has no associated company.");
```

### I7. Domain events are raised but never dispatched
[`Applications/Commands/ReviewApplication/ReviewApplicationCommandHandler.cs:42`](server/src/ScholarPath.Application/Applications/Commands/ReviewApplication/ReviewApplicationCommandHandler.cs:42)

```csharp
application.RaiseDomainEvent(new ApplicationStatusChangedEvent(...));
await db.SaveChangesAsync(ct);
```

`RaiseDomainEvent` adds the event to the entity's `_domainEvents` list, but nothing in the project's `SaveChangesAsync` interceptor picks them up and publishes through MediatR. As a result, `ApplicationStatusChangedEventHandler` (which the spec says triggers payment capture) **is never invoked**, even after fixing build error B1(b). The whole capture-on-decision flow is non-functional.

Either:
- Add a `DomainEventDispatcher` interceptor in `Infrastructure/Persistence/` that runs after `SaveChangesAsync`, walks the `ChangeTracker`, drains `DomainEvents`, and `mediator.Publish`es each one. (See `docs/ARCHITECTURE.md` if it documents this pattern; otherwise propose the design.)
- Or have the handler explicitly `await sender.Send(captureCommand, ...)` directly after `SaveChangesAsync` — less clean but works for now.

The `ISender sender` constructor parameter is already injected into `ReviewApplicationCommandHandler` and `WithdrawApplicationCommandHandler` but never used — looks like the intent was the second approach.

### I8. `WithdrawApplicationCommandHandler` always refunds 50% — spec says 100% pre-submit
[`WithdrawApplicationCommandHandler.cs:39`](server/src/ScholarPath.Application/Applications/Commands/WithdrawApplication/WithdrawApplicationCommandHandler.cs:39)

```csharp
// Refund policy: withdraws after submit -> refund 50%. Since it's held, it's considered "after submit".
var refundCommand = new RefundCompanyReviewCommand(application.Id, IsFullRefund: false);
```

Spec acceptance criterion #3: *"if Student withdraws **before submit** → refund 100%; if Student withdraws after submit → refund 50%"*. The handler decides "before/after" by whether `payment.Status == Held`, but a `Held` payment is created *at* submit time. So the 100% case (withdraw a draft) currently has `Held = true` only if a payment was held — which won't have happened in Draft state. The result is correct for the post-submit case but the comment is misleading.

A clearer fix:
```csharp
var isFullRefund = application.Status == ApplicationStatus.Draft 
                || application.Status == ApplicationStatus.Pending; // not yet under review
var refundCommand = new RefundCompanyReviewCommand(application.Id, IsFullRefund: isFullRefund);
```

Add a unit test for both paths.

### I9. `ChatHub` broadcasts presence to **all** users — N² fan-out
[`server/src/ScholarPath.Infrastructure/Hubs/Hubs.cs:18`](server/src/ScholarPath.Infrastructure/Hubs/Hubs.cs:18)

```csharp
await Clients.All.SendAsync("UserOnline", Context.UserIdentifier);
```

Every connect/disconnect goes to every other user — including users who have no chat history with the connecting user. With 1k concurrent users that's 1M messages on each connect. Track per-conversation presence: only notify the other participant of conversations involving this user.

Spec criterion #5 also calls for **Redis-backed** presence with auto-expire — current implementation has none of that. Track as a follow-up if scope is tight.

### I10. `ChatHub.OnDisconnectedAsync` — `IsArchivedForParticipant*` reset is wrong
[`SendMessageCommand.cs:67`](server/src/ScholarPath.Application/Chat/Commands/SendMessage/SendMessageCommand.cs:67)

```csharp
conversation.IsArchivedForParticipantOne = false;
conversation.IsArchivedForParticipantTwo = false;
```

Sending one message un-archives the conversation for **both** participants. If User B archived a conversation and User A sends them a message, only User B's archive flag should flip back. Use the sender vs. recipient check to flip only the recipient's flag, then keep the sender's whatever it was.

### I11. `BlockUserCommand` uses `UserIdToBlock`; controller/frontend send `UserId`
[`server/src/ScholarPath.API/Controllers/ChatController.cs:53`](server/src/ScholarPath.API/Controllers/ChatController.cs:53)

```csharp
public record BlockUserRequest(Guid UserId, string? Reason);
// ...
var command = new BlockUserCommand(request.UserId, request.Reason);
```

Versus the command:
```csharp
public sealed record BlockUserCommand(Guid UserIdToBlock, string? Reason) : IRequest<bool>;
```

Positional record matching means it works (both are `Guid`), but the frontend payload uses `userId` and the command property is `UserIdToBlock` — readers will be confused. Pick one: rename the command field to `UserId` (matches FE) or rename the request field to `UserIdToBlock`.

### I12. `FlagPostCommand` doesn't notify admins when threshold reached
[`FlagPostCommand.cs:61`](server/src/ScholarPath.Application/Community/Commands/FlagPost/FlagPostCommand.cs:61)

```csharp
post.IsAutoHidden = true;
post.AutoHiddenAt = DateTimeOffset.UtcNow;
post.RaiseDomainEvent(new PostAutoHiddenEvent(post.Id, distinctValidFlags));
```

The event is raised but (per I7) never dispatched. Even if dispatched, no handler exists in `CompanyReviews/EventHandlers/` or anywhere else for `PostAutoHiddenEvent`. Spec criterion #3 says: *"routed to admin queue with `ModerationStatus=PendingAdmin`"*. Add a handler that:
1. Sets `post.ModerationStatus = PendingAdmin`
2. Sends a `NotificationType` notification to all admins (or queues an `AdminTaskItem` if such a thing exists)

### I13. `GetPostsQuery` search uses `Contains` — SQL Server does case-sensitive match by default
[`Community/Queries/GetPosts/GetPostsQuery.cs:36`](server/src/ScholarPath.Application/Community/Queries/GetPosts/GetPostsQuery.cs:36)

```csharp
query = query.Where(p => p.Title!.Contains(request.SearchQuery) || p.BodyMarkdown.Contains(request.SearchQuery));
```

Depending on the column collation this may or may not be case-insensitive. Use `EF.Functions.Like` with `'%searchterm%'` and a case-insensitive collation, or normalise both sides to lower. Currently a search for `python` may not find a post titled `Python`.

---

## 🟢 Nice-to-have / polish

### N1. `CompanyReviewPricingService` doesn't round consistently with cents math
[`CompanyReviewPricingService.cs:23`](server/src/ScholarPath.Application/CompanyReviews/Services/CompanyReviewPricingService.cs:23)

`Math.Round(baseReviewFeeUsd * platformPercentage, 2)` is decimal-USD rounding. The Stripe call eventually multiplies by 100 to get cents — possible 1¢ drift across many payments. Either move pricing math to cents end-to-end (long), or document that the residual stays with the company.

### N2. `RefundCompanyReviewCommand.Handle` calculates `refundAmountCents` incorrectly
[`RefundCompanyReviewCommand.cs:62`](server/src/ScholarPath.Application/CompanyReviews/Commands/RefundCompanyReview/RefundCompanyReviewCommand.cs:62)

```csharp
long refundAmountCents = (long)(payment.AmountUsd * 50m);   // ← typo: should be * 50 / 1 = $0.50 * cents...
var captureAmountCents = (long)(payment.AmountUsd * 100m) - refundAmountCents;
```

For `AmountUsd = 100`, `refundAmountCents = 5000` (✅ $50.00 — correct), `captureAmountCents = 10000 - 5000 = 5000` (✅ $50.00). It actually works because of the `* 50m` vs `* 100m` ratio, but it's reading `100 USD * 50 = 5000 cents` which is misleading. Use a helper:
```csharp
long totalCents = payment.AmountUsd.ToCents();
long captureCents = totalCents / 2;     // capture half
```

### N3. `CompanyReviewTimeoutRefundJob` has a dangling `// Mark application as expired` comment
[`CompanyReviewTimeoutRefundJob.cs:67-72`](server/src/ScholarPath.Infrastructure/Jobs/CompanyReviewTimeoutRefundJob.cs:67)

The TODO-style comment block ("Let's just leave the status... or maybe we just leave the status...") needs a decision. Recommend: set `application.Status = ApplicationStatus.Rejected` with a system-generated `DecisionReason = "Auto-rejected: review window expired"` and dispatch `ApplicationStatusChangedEvent` so downstream handlers run.

### N4. `UpdatePostCommand` allows changing `Title` to null silently
[`UpdatePostCommand.cs:38`](server/src/ScholarPath.Application/Community/Commands/UpdatePost/UpdatePostCommand.cs:38)

If `request.Title == null` for a root post, the validator throws (good), but if `request.Title == ""`, the validator passes (`MaximumLength` allows empty) and `IsNullOrWhiteSpace` then throws a manual `ValidationException`. Add `.NotEmpty().When(v => v.PostId == ... root)` to keep validation in one place.

### N5. `DeletePostCommand` is missing a validator file
[`server/src/ScholarPath.Application/Community/Commands/DeletePost/`](server/src/ScholarPath.Application/Community/Commands/DeletePost/)

Trivial — `RuleFor(v => v.PostId).NotEmpty();`. Pattern consistency across the slice.

### N6. `SendMessageCommand.Handle` includes `conversation.Messages` which is unbounded
[`SendMessageCommand.cs:34`](server/src/ScholarPath.Application/Chat/Commands/SendMessage/SendMessageCommand.cs:34)

```csharp
var conversation = await db.Conversations
    .Include(c => c.Messages)
    .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
```

Loading every message in the conversation just to send a new one is an O(N) query. Drop the `.Include` — you don't read the existing messages.

### N7. `GetMessagesQuery` cursor-paging key is wrong
[`GetMessagesQuery.cs:31`](server/src/ScholarPath.Application/Chat/Queries/GetMessages/GetMessagesQuery.cs:31)

The query takes `Before: DateTimeOffset?` — but multiple messages can share `SentAt` to the millisecond. Use `(SentAt, Id)` as the composite cursor for stability:
```csharp
.Where(m => m.SentAt < before.Value || (m.SentAt == before.Value && m.Id < beforeId))
```

Or accept the rare duplicate and document.

Spec criterion #7 specifies `cursor=...` — exact format isn't defined, so pick one and document in `_Module.md`.

### N8. Frontend `Chat.tsx` handler uses `framer-motion` but project standardises on `motion/react`
[`client/src/pages/chat/Chat.tsx`](client/src/pages/chat/Chat.tsx) imports — actually the file uses lucide and date-fns only. But [`client/src/components/application/KanbanBoard.tsx:2`](client/src/components/application/KanbanBoard.tsx:2) imports `from "framer-motion"`. Constitution pins **Motion 12.38** from `"motion/react"`. Find/replace.

### N9. `NotificationContent` 3-arg call won't compile in handlers (now blocked by B1, but listed for tracking)
[`SubmitCompanyRatingCommandHandler.cs:55`](server/src/ScholarPath.Application/CompanyReviews/Commands/SubmitCompanyRating/SubmitCompanyRatingCommandHandler.cs:55)

```csharp
new NotificationContent("New Rating", $"You received a {request.Rating}-star rating.", null)
```

`NotificationContent` ctor takes `(TitleEn, TitleAr, BodyEn, BodyAr, MetadataJson?)` — 4 required strings. Once B1 is fixed and tests run, every site will need both EN and AR strings. Plan for 8+ such call sites in the slice.

### N10. `CommunityController` requests live under `[Authorize]` but `GetPosts`/`GetCategories`/`GetPostDetails` are `[AllowAnonymous]`
[`server/src/ScholarPath.API/Controllers/CommunityController.cs:21`](server/src/ScholarPath.API/Controllers/CommunityController.cs:21)

Constitution principle I: "The Home Page is the only public route. Every other page, API endpoint, asset, and socket connection requires an authenticated session." The community read endpoints must require auth. Either:
- Remove `[AllowAnonymous]` and let the class-level `[Authorize]` apply.
- Or amend the constitution if you want SEO-friendly forum reads (separate decision).

Same with [`CompanyReviewsController.cs:18`](server/src/ScholarPath.API/Controllers/CompanyReviewsController.cs:18) — `GetCompanyRatings` is `[AllowAnonymous]`.

### N11. `ChatController.SendMessage` returns `SendMessageResult` (now an `IActionResult`) — should be `Created`
[`server/src/ScholarPath.API/Controllers/ChatController.cs:39`](server/src/ScholarPath.API/Controllers/ChatController.cs:39)

`Ok(messageId)` → `201 Created` with `Location` header pointing at `GET /api/chat/conversations/{id}/messages?...` is more RESTy.

### N12. `CompanyReviewsController` route uses `~/` to break out of class prefix
[`CompanyReviewsController.cs:18`](server/src/ScholarPath.API/Controllers/CompanyReviewsController.cs:18)

```csharp
[HttpGet("~/api/companies/{companyId:guid}/reviews")]
```

Same anti-pattern Tasneem flagged on `BookingsController`. Either move `GetCompanyRatings` to a `CompaniesController`, or move `SubmitCompanyRating` to `/api/companies/{id}/reviews`.

### N13. Localisation key `t("community.ask_question", "Ask a Question")` uses fallback string in code
[`client/src/pages/community/Community.tsx:54`](client/src/pages/community/Community.tsx:54)

Fallback strings inside `t()` calls hide missing-key errors. Add proper `community.json` locales for both EN and AR (the file doesn't exist yet — see spec gap below).

### N14. `_Module.md` for `CompanyReviews/`, `Community/`, `Chat/` is the template — no module-specific notes
[`server/src/ScholarPath.Application/CompanyReviews/_Module.md`](server/src/ScholarPath.Application/CompanyReviews/_Module.md)

Quick block describing pricing flow + idempotency keys + refund matrix + auto-hide threshold would help the rest of the team.

---

## ⚪ Spec gaps — missing tasks

| Spec | Task | Status |
|---|---|---|
| PB-005 | T-001 `CompanyReviewPayment` entity + config | ✅ entity exists in `Ratings.cs` (pre-existing); no `EntityTypeConfiguration` added in this PR |
| PB-005 | T-002 `SubmitCompanyRatingCommand` | ✅ done |
| PB-005 | T-003 `CaptureCompanyReviewPaymentCommand` | ✅ done (but B1+I7 break the trigger) |
| PB-005 | T-004 `RefundCompanyReviewCommand` | ⚠️ partial — silent failure on Stripe non-success (B4) |
| PB-005 | T-005 `CompanyReviewTimeoutRefundJob` | ⚠️ exists but not registered with Hangfire and uses concrete DbContext |
| PB-005 | T-006 Stripe webhook branch | ⚠️ exists but uses regex parsing, no idempotency (B2+B3) |
| PB-005 | T-007 `GetCompanyRatingsQuery` | ✅ done |
| PB-005 | T-008 Unit + integration tests | ⚠️ 3 unit tests; 0 integration tests; no refund-matrix coverage |
| PB-005 | T-009 Rating modal | ✅ done |
| PB-005 | T-010 Company dashboard ratings grid | ✅ done |
| PB-005 | T-011 Fee display in submit confirmation | ✅ done |
| PB-005 | T-012 Arabic copy review | ⚠️ AR JSONs exist but not registered (B9) |
| PB-007 | T-001 CRUD posts/replies/categories | ✅ done |
| PB-007 | T-002 Vote toggle + self-vote block | ✅ done |
| PB-007 | T-003 Flag + auto-hide at 3 distinct flags | ⚠️ logic correct, admin notification missing (I12) |
| PB-007 | T-004 Broadcast new post via `CommunityHub` | ⚠️ blocked by B1 build failure |
| PB-007 | T-005 Sanitize post content | ✅ HtmlSanitizer in place |
| PB-007 | T-006 `ChatHub` with auth + presence | ⚠️ no Redis-backed presence; broadcasts to `Clients.All` (I9) |
| PB-007 | T-007 `SendMessageCommand` | ⚠️ broken by argument mismatch (B7) |
| PB-007 | T-008 `BlockUserCommand` / `UnblockUserCommand` | ✅ + 1 test |
| PB-007 | T-009 `GetMessagesQuery` cursor paging | ⚠️ no stable cursor (N7) |
| PB-007 | T-010 Integration tests for block enforcement | ❌ unit only |
| PB-007 | T-011..T-018 Frontend Community/Chat | ✅ done (after build fix) |
| PB-007 | T-019 Arabic copy review | ❌ no `community.json` / `chat.json` AR files |
| PB-007 | T-020 E2E post-flag-hide | ❌ |
| PB-007 | T-021 E2E chat block | ❌ |

PB-005 owner is Yousra; PB-007 was assigned to Madiha per the post-rebalance constitution amendment but Yousra has done a complete vertical slice — let's coordinate scope before re-review.

---

## ✅ Things I enjoyed reviewing

- **`SubmitCompanyRatingCommandHandler`** is the cleanest handler in the slice — proper exception types (`NotFoundException`, `ForbiddenAccessException`, `ConflictException`), per-application uniqueness check, ownership check, notification dispatch. The 3 tests cover the happy path + 2 distinct conflict cases. Excellent template.
- **Refund test coverage** — the partial-refund test asserts both `RefundedAmountUsd` AND that Stripe was called with the right cents amount (5000). That's the level of money-test rigour I want everywhere.
- **`FlagPostCommand` distinct-flagger logic** — `Flags.Where(f => f.IsValid).Select(f => f.FlaggedByUserId).Distinct().Count() >= 3` correctly handles the spec's "distinct" requirement (vs. raw flag count). Plus the prior duplicate-flag rejection by the same user.
- **`ToggleVoteCommand`** swap-vs-toggle logic is correct (upvote → downvote → cleared) and self-vote is blocked at the handler level. Properly handles the cached `UpvoteCount`/`DownvoteCount` aggregates.
- **`HtmlSanitizer` (Ganss.Xss)** consistently applied on `Title` and `BodyMarkdown` for both create and update paths. Good.
- **NSubstitute migration** — the Moq → NSubstitute work in commit `c094899` aligns with the constitution's pinned versions. The two CompanyReviews test classes use the new pattern correctly.
- **`GetCompanyRatingsQuery`** filters `IsHiddenByAdmin` posts out of both the average and the list — moderation is respected in queries.
- **`ApplyConfirmation` component** does a great job explaining escrow and the 14-day refund policy in user-friendly language. The escrow shield icon is a nice touch.
- **All 3 SignalR hubs** (`ChatHub`, `CommunityHub`, `NotificationHub`) inherit from `[Authorize] AuthenticatedHub` — gating is enforced.
- **Bilingual category model** — `ForumCategory` carries both `NameEn` and `NameAr`. This is the right shape.
- **`ChatConversation.LastMessageId` denormalisation** — clever, lets `GetConversationsQuery` produce the last-message preview with one query plan.

---

## Pre-merge checklist

- [ ] **B1** — make `dotnet build` green (5 sub-fixes: layer violation, DomainEvent : INotification, missing usings, `Paginated`/`PagedResult` typo, revert `global.json`)
- [ ] **B2** — replace regex JSON parsing with strongly-typed Stripe.net event extraction
- [ ] **B3** — persist `StripeWebhookEvent` and dedupe by `StripeEventId`
- [ ] **B4** — fail loudly when partial refund Stripe call doesn't return `succeeded`
- [ ] **B5** — deterministic idempotency keys derived from payment id + operation (5 call sites)
- [ ] **B6** — `ConfigureReviewFeeCommandValidator` actually validates
- [ ] **B7** — fix `SendMessageCommand` ↔ controller arg shape; chat is broken until then
- [ ] **B8** — `[Auditable]` on the 13 missing commands
- [ ] **B9** — register `arApplications` + `arCompany` in `i18n.ts`
- [ ] **B10** — fail-fast on missing `WebhookSecret`; structured log on signature errors
- [ ] **I7** — wire `RaiseDomainEvent` → MediatR via `SaveChangesAsync` interceptor
- [ ] **I6** — derive `CompanyId` server-side; remove from `SubmitCompanyRatingCommand`
- [ ] **I8** — fix `WithdrawApplicationCommandHandler` refund decision
- [ ] **I12** — `PostAutoHiddenEvent` handler that updates `ModerationStatus` + admin notification
- [ ] Rebase onto `main` (20 commits behind — analytics changes have likely touched some shared files)
- [ ] CI green on all 3 lanes (backend + client + security)
- [ ] At least one integration test for the refund matrix and one for chat block enforcement (T-008/T-010)

I1, I2, I3, I9, I10, I11, I13 can be deferred to a follow-up PR as long as the blockers above are clean.

Yousra, this is a really ambitious slice — touching payments, real-time, moderation, and forum surface in one PR is genuinely the hardest combination on this project. The structure shows you understood the patterns. Once the build is green and the money-path bugs are fixed, this becomes one of our biggest landings of the iteration. Don't be discouraged by the blockers count — they're mechanical, the architecture is right.
