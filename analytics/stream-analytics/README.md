# analytics/stream-analytics/

Azure Stream Analytics + the PagerDuty bridge that consumes its output.

## Files

| File                          | Purpose                                             |
|-------------------------------|-----------------------------------------------------|
| `anomaly-detection.asaql`     | Main Stream Analytics query (PB-018 US-002)         |
| `pagerduty-bridge.js`         | Azure Function forwarding alerts → PagerDuty        |
| `live-tile-passthrough.asaql` | Trivial pass-through for Tasneem's live tile (T-004)|

## Deploy

The Stream Analytics job is named `sa-anomaly` in the `scholarpath-rg-prod`
resource group. Its definition lives here as the source of truth — the CI job
`analytics-ci` validates the T-SQL syntax, and a human promotes the file to
the job via:

```bash
az stream-analytics job update \
  --resource-group scholarpath-rg-prod \
  --name sa-anomaly \
  --transformation-query-file analytics/stream-analytics/anomaly-detection.asaql
```

After promoting, restart the job (it'll resume from the input Event Hub's
last checkpoint).

## Inputs + outputs at deploy time

| Alias              | Kind           | Target                                |
|--------------------|----------------|---------------------------------------|
| `eh_domain_events` | Event Hub (in) | `scholarpath-eh-prod:domain-events`   |
| `eh_alerts`        | Event Hub (out)| `scholarpath-eh-prod:alerts`          |
| `pbs_live`         | Power BI       | Streaming dataset `Live Firehose`      |
| `blob_audit`       | ADLS Gen2      | `lake-prod/silver/stream-audit/...`   |

See `docs/runbooks/anomaly-response.md` for first-responder instructions.
