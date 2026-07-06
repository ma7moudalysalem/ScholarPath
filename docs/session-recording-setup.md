# Session recording — production setup runbook

The consultant-session video recording (PB-006) is **fully implemented in code** but
needs Azure wiring that the `deploy.yml` pipeline does **not** provision (the pipeline
only zip-deploys the API, syncs app settings, and builds the client — it never applies
`infra/main.bicep`). Until the steps below are done, recording **starts** (the consultant
sees the red "Recording" dot) but the finished file is **never captured**, so no recording
ever appears on the booking-details page for anyone.

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

The webhook is the **only** thing that ever writes a `SessionRecording` row. No Event Grid
subscription ⇒ webhook is never called ⇒ recording appears nowhere.

## Prerequisites (what must be true in prod)

| # | Requirement | Status / how |
|---|-------------|--------------|
| 1 | ACS **Call Recording** enabled on the ACS resource | Already effectively on — `StartAsync` succeeds (consultant sees "Recording"). |
| 2 | **`Acs__WebhookKey`** app setting on the API, equal to the `?code=` on the Event Grid subscription URL | Wire the `ACS_WEBHOOK_KEY` GitHub secret (now consumed by `deploy.yml`), then redeploy — or set it directly (below). |
| 3 | **Event Grid system topic + subscription** on the ACS resource for `RecordingFileStatusUpdated` → the webhook | **Missing.** Create it (below). Not in `deploy.yml`; optionally add to bicep for a future full infra deploy. |
| 4 | **`session-recordings` blob container** + API on the **AzureBlob** storage provider | Container declared in `infra/main.bicep`; also auto-created at runtime on the AzureBlob path. Verify prod is `Storage__Provider=AzureBlob` (it drifted to `Local` once — see the doc-vault incident). |

## One-time setup (Azure CLI)

Set these to your real resource names first:

```bash
RG="<api-resource-group>"                 # e.g. rg-scholarpath-prod
API_APP="app-scholarpath-prod-api"        # API App Service name
ACS_NAME="<acs-resource-name>"            # the ACS resource whose connection string is in ACS_CONNECTION_STRING
API_HOST="https://$API_APP.azurewebsites.net"

# A strong random webhook key (also add this same value as the ACS_WEBHOOK_KEY GitHub secret)
WEBHOOK_KEY="$(openssl rand -hex 24)"
echo "ACS_WEBHOOK_KEY = $WEBHOOK_KEY"      # store in GitHub → Settings → Secrets → Actions
```

### 2 — Set `Acs__WebhookKey` on the API

Either add `ACS_WEBHOOK_KEY` as a GitHub Actions secret and re-run **Deploy** (the sync step
now applies it), or set it directly:

```bash
az webapp config appsettings set -g "$RG" -n "$API_APP" \
  --settings "Acs__WebhookKey=$WEBHOOK_KEY" -o none
```

### 3 — Create the Event Grid system topic + subscription on the ACS resource

```bash
az eventgrid system-topic create \
  -g "$RG" --name scholarpath-acs-topic \
  --source "$(az communication show -g "$RG" -n "$ACS_NAME" --query id -o tsv)" \
  --topic-type Microsoft.Communication.CommunicationServices \
  --location global

az eventgrid system-topic event-subscription create \
  --name recording-ready \
  -g "$RG" --system-topic-name scholarpath-acs-topic \
  --endpoint "$API_HOST/api/meeting-recording/events?code=$WEBHOOK_KEY" \
  --endpoint-type webhook \
  --included-event-types Microsoft.Communication.RecordingFileStatusUpdated
```

The webhook already handles the Event Grid `SubscriptionValidationEvent` handshake, so the
subscription validates automatically **provided `Acs__WebhookKey` is already set (step 2)** —
otherwise the handshake POST is rejected 401 and the subscription fails to create.

### 4 — Confirm storage

```bash
az webapp config appsettings list -g "$RG" -n "$API_APP" \
  --query "[?name=='Storage__Provider'].value | [0]" -o tsv   # must print: AzureBlob
```

If it prints `Local`, set `Storage__Provider=AzureBlob` (the KV-referenced connection string
is already provisioned by bicep as `Storage--AzureBlob--ConnectionString`).

## Verification (end-to-end — this is the answer to "where does the recording appear?")

1. Book + confirm a session; both parties join on **Chrome/Edge** (not Brave).
2. Let the call connect (consultant sees "Recording") and run for ~30s, then both leave.
3. Within a few minutes, watch the API log stream for a `RecordingFileStatusUpdated` POST
   returning **200** (not 401 / not `LogCritical: Acs:WebhookKey is not configured`).
4. Confirm a row exists: `SELECT * FROM SessionRecordings WHERE BookingId = '<id>'`.
5. Open the booking details page as the student **and** the consultant → the **Session
   recordings** card lists the file and the **Download** link streams the `.mp4`.

Until steps 2–4 above are done, the honest status is: **recordings appear nowhere.**

## Demo fallback (if the Azure wiring can't be done in time)

Seed one `SessionRecording` row for a demo booking pointing at a short `.mp4` uploaded to the
`session-recordings` container. `GetBookingRecordings` + `BookingRecordings.tsx` then render it
on both booking-details pages, exercising the real list/download path without needing a live
Event Grid delivery.
