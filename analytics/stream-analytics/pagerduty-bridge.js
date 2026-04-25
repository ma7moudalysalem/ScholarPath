// PB-018 T-007 / FR-267 — PagerDuty bridge.
//
// Azure Function triggered off eh_alerts (the output Event Hub from the
// Stream Analytics anomaly-detection job). Forwards each alert to PagerDuty's
// Events API v2 as a "trigger" with a dedup_key so a sustained spike raises
// one incident, not thousands.
//
// Env:
//   PAGERDUTY_ROUTING_KEY  — integration key from the ScholarPath PD service
//   DEDUP_WINDOW_MINUTES   — default 10; a second spike within this window on
//                            the same event_type updates the existing incident
//                            instead of creating a new one.
//
// Deployed as function-name `pagerduty-bridge` in the `func-scholarpath-prod`
// Function App.

const https = require('https');

const PD_EVENTS_URL = 'https://events.pagerduty.com/v2/enqueue';

module.exports = async function (context, events) {
    const routingKey = process.env.PAGERDUTY_ROUTING_KEY;
    if (!routingKey) {
        context.log.error('PAGERDUTY_ROUTING_KEY missing — cannot forward alerts.');
        return;
    }

    const window = Number(process.env.DEDUP_WINDOW_MINUTES || '10');
    const results = await Promise.allSettled(events.map(evt => trigger(context, evt, routingKey, window)));

    const failed = results.filter(r => r.status === 'rejected');
    if (failed.length > 0) {
        context.log.error(`${failed.length}/${events.length} alerts failed to forward`);
        failed.forEach(r => context.log.error(r.reason));
    }
};

function trigger(context, evt, routingKey, windowMinutes) {
    const bucket = Math.floor(new Date(evt.triggered_at).getTime() / (windowMinutes * 60 * 1000));
    const dedupKey = `scholarpath-${evt.component}-${bucket}`;

    const payload = JSON.stringify({
        routing_key: routingKey,
        event_action: 'trigger',
        dedup_key: dedupKey,
        payload: {
            summary:  evt.summary,
            severity: evt.severity,
            source:   'ScholarPath Stream Analytics',
            component: evt.component,
            group:    'anomaly-detection',
            class:    evt.component,
            custom_details: {
                event_count:  evt.event_count,
                baseline_mean: evt.mean_count,
                baseline_stddev: evt.stddev_count,
                z_score: evt.z_score,
                window_end: evt.triggered_at,
                runbook: 'https://github.com/ma7moudalysalem/ScholarPath/blob/main/docs/runbooks/anomaly-response.md'
            }
        }
    });

    return new Promise((resolve, reject) => {
        const req = https.request(PD_EVENTS_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(payload) }
        }, res => {
            const chunks = [];
            res.on('data', c => chunks.push(c));
            res.on('end', () => {
                const body = Buffer.concat(chunks).toString('utf8');
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    context.log.info(`[pd] ${evt.component} → ${dedupKey} (status ${res.statusCode})`);
                    resolve();
                } else {
                    reject(new Error(`[pd] ${res.statusCode} ${body}`));
                }
            });
        });
        req.on('error', reject);
        req.write(payload);
        req.end();
    });
}
