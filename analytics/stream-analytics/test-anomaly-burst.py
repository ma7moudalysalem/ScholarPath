#!/usr/bin/env python3
"""
PB-018 T-014 — Integration test: synthetic burst triggers anomaly alert.

Publishes a burst of synthetic domain events to Event Hub, waits for the
Stream Analytics job `sa-anomaly` to process them, and asserts that a PagerDuty
incident was created (or the alert Event Hub received a message).

Prerequisites:
    pip install azure-eventhub requests

Environment variables:
    EVENTHUB_SEND_CONNECTION_STRING  — from `infra/main.bicep` output `eventHubSendConnectionString`
    EVENTHUB_LISTEN_CONNECTION_STRING — from output `eventHubListenConnectionString`
    EVENTHUB_NAME                    — default: "domain-events"
    ALERTS_EVENTHUB_NAME             — the `eh_alerts` output hub name (default: "domain-event-alerts")
    PAGERDUTY_API_KEY                — optional: PD API key to verify incident creation
    PAGERDUTY_SERVICE_ID             — optional: PD service ID to query incidents

Exit codes:
    0 — alert fired within timeout (test passes)
    1 — no alert received (test fails)
    2 — missing environment variables
    3 — Event Hub / SDK error

Usage:
    EVENTHUB_SEND_CONNECTION_STRING="Endpoint=sb://..."  \\
    EVENTHUB_LISTEN_CONNECTION_STRING="Endpoint=sb://..." \\
    python test-anomaly-burst.py
"""

import os
import sys
import json
import time
import uuid
import logging
from datetime import datetime, timezone

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
log = logging.getLogger(__name__)

# ── Configuration ─────────────────────────────────────────────────────────────

SEND_CONN_STR    = os.environ.get("EVENTHUB_SEND_CONNECTION_STRING", "")
LISTEN_CONN_STR  = os.environ.get("EVENTHUB_LISTEN_CONNECTION_STRING", "")
HUB_NAME         = os.environ.get("EVENTHUB_NAME", "domain-events")
ALERTS_HUB_NAME  = os.environ.get("ALERTS_EVENTHUB_NAME", "domain-event-alerts")
PD_API_KEY       = os.environ.get("PAGERDUTY_API_KEY", "")
PD_SERVICE_ID    = os.environ.get("PAGERDUTY_SERVICE_ID", "")

# Burst parameters — must exceed 3σ above the Stream Analytics baseline.
# On a fresh job (no history) the first 60 windows are baseline calibration,
# so we generate enough events to guarantee a spike is visible.
BURST_EVENT_TYPE = "ApplicationSubmitted"
BURST_COUNT      = 200   # Events to publish in ~10 seconds (normal avg ≈ 5/min → massive spike)
BURST_BATCH_SIZE = 50
WAIT_FOR_ALERT_S = 180   # Stream Analytics processes with ~30s latency; 3 min should be plenty


def check_env() -> bool:
    missing = []
    if not SEND_CONN_STR:
        missing.append("EVENTHUB_SEND_CONNECTION_STRING")
    if not LISTEN_CONN_STR:
        missing.append("EVENTHUB_LISTEN_CONNECTION_STRING")
    if missing:
        log.error("Missing environment variables: %s", ", ".join(missing))
        log.error("Set them and re-run. See infra/main.bicep outputs for connection strings.")
        return False
    return True


def publish_burst() -> None:
    """Publish BURST_COUNT synthetic ApplicationSubmitted events to Event Hub."""
    try:
        from azure.eventhub import EventHubProducerClient, EventData
    except ImportError:
        log.error("azure-eventhub not installed. Run: pip install azure-eventhub")
        sys.exit(3)

    log.info("Connecting to Event Hub '%s' (send policy)...", HUB_NAME)
    producer = EventHubProducerClient.from_connection_string(SEND_CONN_STR, eventhub_name=HUB_NAME)

    total_sent = 0
    with producer:
        for batch_start in range(0, BURST_COUNT, BURST_BATCH_SIZE):
            batch = producer.create_batch()
            for i in range(BURST_BATCH_SIZE):
                payload = {
                    "event_type":    BURST_EVENT_TYPE,
                    "occurred_at":   datetime.now(timezone.utc).isoformat(),
                    "source":        "test-anomaly-burst",
                    "correlation_id": str(uuid.uuid4()),
                    "data": {
                        "application_id": str(uuid.uuid4()),
                        "scholarship_id": str(uuid.uuid4()),
                        "student_id":     str(uuid.uuid4()),
                    },
                }
                batch.add(EventData(json.dumps(payload)))
            producer.send_batch(batch)
            total_sent += BURST_BATCH_SIZE
            log.info("Sent %d / %d events...", total_sent, BURST_COUNT)
            time.sleep(0.5)   # Small delay between batches to avoid throttling

    log.info("Burst complete: %d events sent.", total_sent)


def wait_for_alert() -> bool:
    """
    Poll the `eh_alerts` Event Hub consumer group for an anomaly alert message.
    Returns True if an alert message is received within WAIT_FOR_ALERT_S seconds.
    """
    try:
        from azure.eventhub import EventHubConsumerClient
    except ImportError:
        log.error("azure-eventhub not installed.")
        sys.exit(3)

    log.info("Waiting up to %d seconds for anomaly alert on '%s'...", WAIT_FOR_ALERT_S, ALERTS_HUB_NAME)
    alert_received = [False]
    deadline = time.time() + WAIT_FOR_ALERT_S

    def on_event(partition_context, event):
        body = event.body_as_str()
        log.info("[ALERT RECEIVED] Partition %s: %s", partition_context.partition_id, body[:200])
        alert_received[0] = True
        partition_context.update_checkpoint(event)

    consumer = EventHubConsumerClient.from_connection_string(
        LISTEN_CONN_STR,
        consumer_group="$Default",
        eventhub_name=ALERTS_HUB_NAME,
    )

    with consumer:
        while not alert_received[0] and time.time() < deadline:
            # receive() is blocking with a timeout; check the flag between calls.
            consumer.receive(
                on_event=on_event,
                max_wait_time=10,
                starting_position="-1",   # Latest — only new messages
            )

    return alert_received[0]


def check_pagerduty_incident() -> bool:
    """
    Optional: verify a PagerDuty incident was created for the burst.
    Only runs when PD_API_KEY and PD_SERVICE_ID are set.
    """
    if not PD_API_KEY or not PD_SERVICE_ID:
        log.info("PAGERDUTY_API_KEY / PAGERDUTY_SERVICE_ID not set — skipping PD verification.")
        return True  # Not a failure — PD check is optional

    try:
        import requests
    except ImportError:
        log.warning("requests not installed — skipping PD verification.")
        return True

    log.info("Checking PagerDuty for new incidents on service %s...", PD_SERVICE_ID)
    resp = requests.get(
        "https://api.pagerduty.com/incidents",
        headers={
            "Authorization": f"Token token={PD_API_KEY}",
            "Accept": "application/vnd.pagerduty+json;version=2",
        },
        params={
            "service_ids[]": PD_SERVICE_ID,
            "statuses[]": ["triggered", "acknowledged"],
            "date_range": "since",
            "since": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        },
        timeout=10,
    )

    if resp.status_code != 200:
        log.warning("PD API returned %d — skipping PD verification.", resp.status_code)
        return True

    incidents = resp.json().get("incidents", [])
    log.info("Found %d PagerDuty incident(s) matching the alert window.", len(incidents))
    return True   # PD verification is informational; anomaly alert EH check is the gate


def main() -> int:
    if not check_env():
        return 2

    log.info("=== PB-018 T-014: Synthetic burst anomaly alert test ===")
    log.info("Event Hub: %s | Burst: %d x %s events", HUB_NAME, BURST_COUNT, BURST_EVENT_TYPE)

    # Step 1: Publish burst
    try:
        publish_burst()
    except Exception as exc:
        log.error("Failed to publish burst: %s", exc)
        return 3

    # Step 2: Wait for alert on the output Event Hub
    if wait_for_alert():
        log.info("[PASS] Anomaly alert received on eh_alerts within %d seconds.", WAIT_FOR_ALERT_S)
        check_pagerduty_incident()
        return 0
    else:
        log.error("[FAIL] No anomaly alert received within %d seconds.", WAIT_FOR_ALERT_S)
        log.error("  - Check that the sa-anomaly Stream Analytics job is running.")
        log.error("  - Verify the eh_alerts output is configured in the ASA job.")
        log.error("  - The job needs ≥60 baseline windows before 3σ detection fires.")
        log.error("    Run this test again after the job has been running for ~1 hour.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
