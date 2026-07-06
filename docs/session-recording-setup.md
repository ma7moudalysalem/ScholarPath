# Session recording — production setup & runbook

Status: **PROVISIONED AND VERIFIED WORKING in production (2026-07-06).** A real
recording (`SessionRecording` row + `.mp4` blob) was confirmed for a live booking.
This doc records how the wiring is set up and how to reproduce/verify it — the
`deploy.yml` pipeline does **not** apply `infra/main.bicep`, so the ACS resource and
its Event Grid subscription were provisioned out-of-band (they are NOT in bicep — a
known IaC drift; only the `session-recordings` container and the `Acs__WebhookKey`
deploy wiring are in the repo).

## How the pipeline works

```
Meeting.tsx ensureRecording()                      // client, on call Connected
  → POST /api/bookings/{id}/meeting/start-recording
    → StartMeetingRecordingCommand
      → ACS Call Automation StartAsync(serverCallId)   // only STARTS recording; returns RecordingId
        ...ACS records asynchronously...
  → ACS emits Event Grid event: Microsoft.Communication.RecordingFileStatusUpdated
    → POST https://<api>/api/meeting-recording/events?code=<Acs:WebhookKey>   // MeetingRecordingWebhookController
      → StoreSessionRecordingCommand
        → download from ACS contentLocation → upload to blob container `session-recordings`
        → insert SessionRecording row
  → GetBookingRecordings → BookingRecordings.tsx card on Student/Consultant booking details
```

The webhook is the **only** thing that ever writes a `SessionRecording` row. The red
"Recording" dot in the call only means `StartAsync` succeeded — not that the file was
captured. The recording appears on the **booking-details page** (student, consultant,
and admin) once the webhook has stored it.

## Provisioned prod resources (verified 2026-07-06)

| Requirement | Prod state |
|-------------|-----------|
| ACS resource (Call Recording enabled) | `ScholarPath` in RG `NetworkWatcherRG`, endpoint `scholarpath.unitedstates.communication.azure.com` |
| `Acs__WebhookKey` app setting on the API | **Set** on `app-scholarpath-prod-api` |
| Event Grid system topic on the ACS resource | `scholarpath-fe074b88-…` (RG `networkwatcherrg`), Succeeded |
| Event subscription for `RecordingFileStatusUpdated` → webhook | `scholarpath-recording-ready` → `https://app-scholarpath-prod-api.azurewebsites.net/api/meeting-recording/events`, Succeeded |
| `session-recordings` blob container + AzureBlob provider | Container exists on `stscholarpath`; `Storage__Provider=AzureBlob` |

`Acs__WebhookKey` is also wired into `deploy.yml` (from the `ACS_WEBHOOK_KEY` GitHub
secret) so a redeploy re-applies it — set that secret to the **same** value as the
`?code=` on the Event Grid subscription URL if you ever rotate it.

## Reproduction runbook (if recreating in a fresh environment)

Set your real resource names first:

```bash
RG="scholarpath"
API_APP="app-scholarpath-prod-api"
ACS_RG="NetworkWatcherRG"
ACS_NAME="ScholarPath"
API_HOST="https://$API_APP.azurewebsites.net"

# Webhook key — also add as the ACS_WEBHOOK_KEY GitHub secret
WEBHOOK_KEY="$(openssl rand -hex 24)"
```

### 1 — Set `Acs__WebhookKey` on the API

```bash
az webapp config appsettings set -g "$RG" -n "$API_APP" \
  --settings "Acs__WebhookKey=$WEBHOOK_KEY" -o none
```

### 2 — Event Grid system topic + subscription on the ACS resource

`Acs__WebhookKey` must be set FIRST — the subscription-validation handshake goes
through the same `?code=` auth gate, so it is rejected 401 without a matching key.

```bash
ACS_ID=$(az resource show -g "$ACS_RG" -n "$ACS_NAME" \
  --resource-type Microsoft.Communication/CommunicationServices --query id -o tsv)

az eventgrid system-topic create \
  -g "$ACS_RG" --name scholarpath-acs-topic \
  --source "$ACS_ID" \
  --topic-type Microsoft.Communication.CommunicationServices \
  --location global

az eventgrid system-topic event-subscription create \
  --name scholarpath-recording-ready \
  -g "$ACS_RG" --system-topic-name scholarpath-acs-topic \
  --endpoint "$API_HOST/api/meeting-recording/events?code=$WEBHOOK_KEY" \
  --endpoint-type webhook \
  --included-event-types Microsoft.Communication.RecordingFileStatusUpdated
```

### 3 — Confirm storage

```bash
az webapp config appsettings list -g "$RG" -n "$API_APP" \
  --query "[?name=='Storage__Provider'].value | [0]" -o tsv   # must print: AzureBlob
```

## Verification (the answer to "where does the recording appear?")

1. Book + confirm a session; both parties join on **Chrome/Edge** (not Brave — Brave
   shields block WebRTC and the ACS SDK, which is what produced the student's "could
   not connect" screen even though the server-side join succeeded).
2. Let the call connect (consultant sees "Recording") and run ~30s, then both leave.
3. Within a few minutes a `SessionRecording` row + a `.mp4` in the `session-recordings`
   container are created, and the **Session recordings** card with a **Download** link
   appears on the booking-details page for both the student and the consultant.

Confirmed working 2026-07-06 (booking `3ddb74d0-dfe0-4a03-a7fd-97a14eb9f8da`: 308 KB
`.mp4`, `RecordedAt` 16:24:44 UTC).

To inspect the DB directly (SQL auth `scholarpathadmin`, connstr in KV
`kv-scholarpath-prod-fpa7`), add a temporary firewall rule for your IP on
`sql-scholarpath-prod-fpa7x7`, query `SessionRecordings`, then delete the rule.
