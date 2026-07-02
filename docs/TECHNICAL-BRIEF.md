# ScholarPath — Technical Brief (Architecture, Security & Key Features)

> Supervisor walkthrough. Each section is talking points: **what it is**, **how
> we built it**, and **why we built it that way**. Everything below is the
> implementation as it actually exists in the codebase (`server/src/...`), not a
> plan.

---

## 0. One-paragraph framing (the opener)

ScholarPath is a bilingual (EN/AR) scholarship platform built as a **.NET 10
Clean-Architecture monolith** with a **React 19** single-page client, deployed on
**Azure**. It serves four overlapping user roles — Student, Company, Consultant,
Admin — across scholarships, applications, paid company reviews, paid consultant
video sessions, a community forum, 1-to-1 chat, a resources hub, notifications,
and an AI assistant with Retrieval-Augmented Generation. The engineering theme we
want to highlight today is **defense-in-depth security and provider-swappable
infrastructure**: every external dependency (database, AI, payments, video,
storage, mail, key vault) sits behind an interface (a *port*), so the same
application code runs against a real cloud service in production or a local stub
in development.

---

## 1. Architecture (the foundation)

**Clean Architecture, 4 layers, dependencies point inward:**

```
API  ──►  Application  ──►  Domain
                ▲
        Infrastructure ──► (implements Application & Domain ports)
```

- **Domain** — entities, enums, domain events, two domain interfaces
  (`ICurrentUserService`, `IDateTimeService`). No framework dependencies.
- **Application** — use-cases as **CQRS** commands/queries via **MediatR**,
  FluentValidation validators, and the **ports** (≈ 33 interfaces) the outside
  world must satisfy. Cross-cutting AI cost control (`AiCostGate`) lives here.
- **Infrastructure** — the **adapters** (≈ 46 classes) that implement the ports:
  EF Core, Stripe, Azure OpenAI, ACS, Blob, Key Vault, Event Hub, SignalR, jobs.
- **API** — ASP.NET Core controllers + Stripe webhook receiver, ASP.NET Identity
  + JWT bearer, three SignalR hubs.

**Why it matters:** the *ports & adapters* seam is the single most important
design decision — it makes the system testable (swap a stub), portable (swap a
provider by configuration, no code change), and keeps business rules independent
of Azure. Selection is by configuration at start-up
(`Ai:Provider`, `Storage:Provider`, `Email:Provider`, `Acs:ConnectionString`,
`FileScanning:Enabled`, `Hangfire:Enabled`, …).

**Scale:** ≈ 55 EF-managed entities (48 domain + ASP.NET Identity tables), 18
product-backlog modules, persisted to Azure SQL via EF Core.

---

## 2. The security model (defense in depth)

We layer independent controls so no single failure is catastrophic:

| Layer | Control |
|---|---|
| Transport | HTTPS everywhere; JWT bearer on the API; WebSocket auth on hubs |
| Identity | ASP.NET Core Identity, hashed passwords, lockout, email confirmation |
| Tokens | Short-lived access JWT + rotating refresh tokens (stored **hashed**) |
| Authorization | Role-based (`Student/Company/Consultant/Admin`) + resource ownership checks |
| Secrets & keys | Azure **Key Vault** key providers for field-encryption key and JWT signing key |
| Data at rest | **AES-256-GCM** application-level field encryption for sensitive columns |
| Uploads | **Scan-before-store** with ClamAV, **fail-closed** |
| Privacy / PII | Prompt **redaction** before AI, **loose references** so deletes never cascade-break history, GDPR export/delete |
| Integrity | Idempotency keys, filtered unique indexes, optimistic concurrency (`RowVersion`) |
| Auditability | Append-only `AuditLog` (before/after JSON), per-call `AiInteraction` log |
| Soft delete | Global EF query filters (`!IsDeleted`) hide deleted rows from every query |

The next sections drill into the ones you asked about.

---

## 3. Encryption at rest (`IFieldEncryptionService` → `AesGcmFieldEncryptionService`)

**What:** column-level encryption for sensitive free-text (e.g. a user's
`Biography`, an application's `PersonalNotes`), applied transparently through an
EF Core value converter — the property is plaintext in C#, ciphertext in the
database.

**How — AES-256-GCM, an AEAD (authenticated) cipher:**

- 256-bit key, a **fresh random 12-byte nonce per encryption**, and a 16-byte
  authentication **tag**.
- Stored value is a self-describing envelope:
  `enc:v1:` + Base64( `nonce(12) ‖ tag(16) ‖ ciphertext` ).
- **Tamper detection:** on decrypt, GCM verifies the tag; if a single bit was
  flipped, the value was truncated, or it was encrypted under a different key,
  decryption *throws* rather than returning garbage.
- **No pattern leakage:** because the nonce is random per call, encrypting the
  same plaintext twice produces different ciphertext.
- **Versioned (`v1`)** so a future key rotation can introduce `v2` while still
  decrypting old `v1` rows.
- **Safe rollout:** any value without the `enc:v1:` prefix (legacy plaintext) is
  returned unchanged and re-encrypted on its next write — zero-downtime adoption.

**Why GCM and not just "encrypt":** GCM gives confidentiality **and** integrity in
one pass; a plain cipher (e.g. AES-CBC) would protect secrecy but not detect
tampering. The key never lives in source or config — it comes from a key
provider (next section).

> **Likely question — "Isn't TDE / SQL encryption enough?"** TDE protects the
> data *files*; it does nothing once a query returns a row. Our field encryption
> protects the *value* end-to-end, so even a DB admin or a leaked backup sees
> only ciphertext for those columns.

---

## 4. Key management (`IFieldEncryptionKeyProvider`, `IJwtKeyProvider`)

Two independent key providers, each with a production and a dev adapter:

- **Field-encryption key** — `KeyVaultFieldEncryptionKeyProvider` (Azure Key
  Vault) in production, `LocalFieldEncryptionKeyProvider` for offline dev. Must
  return exactly 32 bytes (enforced at start-up).
- **JWT signing key** — `KeyVaultJwtKeyProvider` / `LocalJwtKeyProvider` (PEM).
  Surfacing the signing key behind its own port is what makes **key rotation**
  possible without redeploying.

**Why:** secrets/keys are externalized into Key Vault, never committed; rotation
is an operations action, not a code change.

---

## 5. Authentication & authorization

- **ASP.NET Core Identity** with `ApplicationUser : IdentityUser<Guid>`
  (hashed passwords via `IdentityPasswordHasher`, lockout, email confirmation).
- **`TokenService`** issues a short-lived **access JWT** plus a **refresh token**;
  refresh tokens are **rotated** on use, can be revoked individually or for the
  whole user, and are stored **hashed** (`RefreshToken.TokenHash`, unique) — a DB
  leak does not yield usable tokens. Same for `PasswordResetToken`.
- **SSO** — `SsoService` exchanges OAuth codes with **Google** and **Microsoft**.
- **Authorization** — role-based (the overlapping `Student/Company/Consultant/
  Admin` specialization, realized as the `UserRoles` join) plus per-resource
  ownership checks in handlers.

---

## 6. File upload pipeline (`IBlobStorageService` → `FileStorageService`)

**Storage abstraction:** one interface, two providers chosen by
`Storage:Provider` — `Local` (folder on disk, dev) or `AzureBlob` (Azure Storage
container). Stored objects use an opaque, provider-tagged path
`provider:container/key`, where `key` is **GUID-prefixed** (`{guid}/{filename}`)
so two same-named uploads never collide. Azure containers are created with
`PublicAccessType.None` — **never public**; downloads always go through the
authenticated API, never a public URL.

**The critical rule — scan before store:** the upload handler runs the file
through the scanner *while it is still a stream in memory*; **only a `Clean`
verdict is persisted.** This is the document vault for application attachments,
upgrade-request files, forum attachments, session recordings, etc.

---

## 7. Malware scanning (`IFileScanService` → `ClamAvFileScanService`)

**How:** uploaded bytes are streamed to a **ClamAV `clamd`** daemon over its
INSTREAM protocol (via the `nClam` client) before the file touches storage. The
scanner returns one of three verdicts:

- **Clean** → upload proceeds; the stream is rewound so the same bytes are stored.
- **Infected** → rejected, the malware signature name is logged.
- **ScanUnavailable** → rejected.

**Fail-closed is the key property:** if the daemon is unreachable, times out, or
returns an error, we map it to `ScanUnavailable` and the upload is **rejected** —
an unscanned file is *never* stored. (Feature-flagged via `FileScanning:Enabled`;
a `NoOpFileScanService` is used in dev where no daemon runs.)

> **Likely question — "Why not scan after upload, async?"** Storing first means a
> window where a malicious file is reachable. Scanning the in-memory stream first,
> fail-closed, removes that window entirely.

---

## 8. PII handling

PII protection is layered across the AI path, the data model, and lifecycle:

**(a) AI prompt redaction (before persist *and* before any external provider).**
In the chatbot handler, the user's message is run through `RedactPii()` *first*:
- email addresses → `[redacted-email]`
- 13–19-digit runs (card numbers) → `[redacted-card]`
- 10+-digit runs (phone numbers) → `[redacted-phone]`

The **redacted** text is what we store in `AiInteraction.PromptText` and what we
send to OpenAI — the raw message never leaves the process. (Regex order matters:
emails, then cards, then the most permissive phone pattern last.)

**(b) Personalization without contact PII.** When we inject the student's profile
into the prompt for personalized answers, we include only academic facts
(nationality, level, field, GPA, preferred countries) — **never email or phone**.

**(c) Per-user isolation of AI memory.** Conversation history replayed into the
model is filtered to the requesting user, so a leaked `SessionId` cannot leak
another user's transcript into the LLM.

**(d) Redaction QA.** `AiRedactionAuditSample` stores periodic samples of redacted
prompts for human review (`RedactedPrompt`, `Verdict`, reviewer) via a sampling
job — we can *prove* the redactor is working.

**(e) Loose references so deletes/anonymization never break.** High-volume,
audit, and analytics tables (payments, notifications, votes, chat, audit log, AI
interactions, knowledge `SourceId`) intentionally have **no FK constraint** on
their user column — they hold the `Guid` as a loose reference. This is what lets
us delete or anonymize a user **without cascade-breaking history** and avoids SQL
Server's multiple-cascade-path error (1785).

**(f) GDPR / data-subject rights.** `UserDataRequest` drives **export** and
**delete** flows (`DataExportJob`, `DataDeleteJob`). Sensitive free-text columns
are encrypted at rest (§3). Profile fields are role-gated in the UI.

---

## 9. Meetings — live video sessions (`IMeetingService` → `AzureCommunicationMeetingService`)

Paid consultant sessions run on **Azure Communication Services (ACS)**:

- A booking's "room" is just an **ACS group-call id (a GUID)** — no server-side
  room object to manage.
- When a participant joins, the API mints a **short-lived, VoIP-scoped access
  token** per user from the ACS Identity service (`CreateUserAndTokenAsync`,
  `CommunicationTokenScope.VoIP`, with an explicit expiry). The room id and token
  are handed **only to the two booking participants** through the authorized join
  endpoint.
- **Recording** uses ACS **Call Automation** (`StartRecordingAsync` → a
  `RecordingId`; `DownloadRecordingAsync` streams the file), persisted as a
  `SessionRecording` row + Blob object.
- **Attendance & no-shows:** the booking records `RecordingStartedAt` (PB-006) and
  `StudentJoinedAt` / `ConsultantJoinedAt` (FR-217); a background job
  (`MeetingNoShowSweepJob`) marks `NoShowMarkedAt` and drives the no-show /
  refund logic.
- A `StubMeetingService` provides fake rooms/tokens in dev (no ACS needed).

> **Why per-user, short-lived tokens:** a video token is a bearer credential.
> Minting one per participant, scoped to VoIP, and expiring it means a leaked
> token is useless after the session and can't be replayed for another call.

---

## 10. The AI subsystem (`IAiService` → Azure / OpenAI-direct / Local)

**Three interchangeable providers**, chosen by `Ai:Provider`:
`AzureOpenAiService`, `OpenAiService` (OpenAI direct), and `LocalAiService` (a
deterministic, offline implementation). Embeddings mirror this
(Azure / OpenAI / Local).

**Three operations** on the port: `Recommend()`, `CheckEligibility()`, `Ask()`.

- **Recommendations are *not* an expensive LLM call.** They are produced by the
  deterministic `LocalAiService`, scoring scholarships against the student's
  profile + metadata (plus RAG retrieval), returning the top-N (default 5). This
  keeps recommendation volume off the OpenAI bill; the LLM backs **chat / Q&A**.
- **Chat (`Ask`)** carries: session memory (last 20 turns, user-scoped), an
  injected student-profile system turn for personalization, RAG grounding, and
  returns the answer + a disclaimer + the **sources** it used.

**Cost control — `AiCostGate`.** Before *every* AI call we enforce a **rolling
24-hour per-user budget** (`DailyUserCostLimitUsd`, default **$1.00**): we sum the
user's `AiInteraction.CostUsd` over the last 24 h and reject the call if this one
would exceed the cap. Every interaction is logged (provider, model, prompt /
completion tokens, cost, and a metadata blob recording the RAG sources) — full
spend observability and an audit trail.

> **Likely question — "What stops the AI bill from exploding?"** Two things: the
> per-user daily cost gate, and the fact that the high-frequency feature
> (recommendations) is deterministic/local, not an LLM call.

---

## 11. RAG — Retrieval-Augmented Generation

**Goal:** ground the assistant in *our* data (real scholarships, resources,
consultants, top community posts) so answers cite facts instead of hallucinating.

- **Index side — `KnowledgeBaseIndexer`** turns those domain rows into
  `KnowledgeDocument` records: bilingual title/content, a `ContentHash`, and an
  **embedding vector** (`byte[]`) tagged with the embedding `ModelName` and
  `Dimensions`. `SourceType + SourceKey` is unique, so re-indexing upserts.
- **Retrieve side — `KnowledgeRetriever`** embeds the user's query, then ranks the
  corpus by **cosine similarity** (`VectorMath`) and returns the **top-K**. It
  only considers documents embedded with the **currently active model** — vectors
  from a different model live in a different space, so they're skipped until the
  KB is re-indexed.
- **Why in-process (no dedicated vector DB):** the corpus is a few hundred rows,
  so a full in-memory similarity scan per query is more than fast enough and
  avoids operating a separate vector store — a deliberate simplicity choice.
- **Auditability:** which KB documents grounded each answer is recorded in
  `AiInteraction.MetadataJson` (a RAG audit trail) and surfaced as "sources" in
  the UI.

---

## 12. Cross-cutting engineering (quick hits to talk about)

- **Audit log** — `AuditService` writes append-only `AuditLog` rows with
  before/after JSON and a polymorphic `(TargetType, TargetId)`.
- **Soft delete** — `ISoftDeletable` + EF **global query filters** (`!IsDeleted`)
  on ~15 entities, so deleted rows are invisible to ordinary queries by default.
- **Domain events** — `BaseEntity.RaiseDomainEvent` decouples side effects.
- **Idempotency** — `Payment.IdempotencyKey` (unique) and
  `Notification.IdempotencyKey` make money and messaging **exactly-once** under
  retries; Stripe webhooks dedupe via `StripeWebhookEvent.StripeEventId`.
- **Filtered unique indexes** enforce real business invariants in the DB:
  one *active* application per student per scholarship (FR-057), one *active*
  booking per consultant per slot, one *active* financial config per type.
- **Money** is stored as integer **cents**; payouts use **Stripe Connect** with a
  profit-share split (`ProfitShareConfig` / `FinancialConfigRule`).
- **Real-time** — three SignalR hubs (Chat, Notification, Community).
- **Background jobs** — 13 Hangfire jobs (reminders, scholarship auto-close,
  no-show sweep, payouts, GDPR export/delete, redaction sampling, integrity
  checks…), **feature-flagged** (`Hangfire:Enabled`).
- **Bilingual** EN/AR with RTL throughout.

---

## 13. Documentation rigor (worth mentioning)

We produced four formal design reports — **EERD** and **Relational Mapping** in
Elmasri & Navathe notation, **Class** and **Component** diagrams in Sommerville
UML — and then ran a **gap analysis of every diagram against the live code**
(entity-by-entity, port-by-port) and closed the gaps. The diagrams are
**reverse-engineered from and verified against the actual schema and services**,
not idealized.

---

## 14. Suggested 4-minute spine (if time is short)

1. Clean Architecture + ports/adapters (provider-swappable) — §1.
2. Defense-in-depth table — §2.
3. AES-256-GCM field encryption + Key Vault keys — §3–4.
4. Scan-before-store, fail-closed uploads — §6–7.
5. PII redaction before the LLM + loose-reference privacy + GDPR — §8.
6. ACS video with per-user short-lived tokens + recording — §9.
7. AI: 3 providers, per-user cost gate, recommendations are local not LLM — §10.
8. RAG: embed → cosine top-K, grounded & audited — §11.
