/**
 * A subtle, asset-free notification chime played on a new real-time notification,
 * plus a device-level on/off preference. Sound is inherently per-device (you may
 * want it on your phone but not a shared laptop), so the toggle lives in
 * localStorage rather than the server-side notification settings.
 */

const STORAGE_KEY = "scholarpath_notif_sound";

/** Default ON — the app makes a soft ping unless the user has muted it here. */
export function isNotificationSoundEnabled(): boolean {
  return localStorage.getItem(STORAGE_KEY) !== "off";
}

export function setNotificationSoundEnabled(enabled: boolean): void {
  localStorage.setItem(STORAGE_KEY, enabled ? "on" : "off");
}

/**
 * Plays a short, gentle two-note chime via the Web Audio API — no audio file to
 * ship or fetch. No-ops silently when sound is disabled or audio is unavailable
 * (e.g. the browser blocked it before any user gesture).
 */
export function playNotificationChime(): void {
  if (!isNotificationSoundEnabled()) return;
  try {
    const ctx = new AudioContext();
    const now = ctx.currentTime;
    // A5 → D6, a soft ascending ding.
    const notes = [
      { freq: 880, at: 0 },
      { freq: 1174.66, at: 0.11 },
    ];
    for (const note of notes) {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = "sine";
      osc.frequency.value = note.freq;
      gain.gain.setValueAtTime(0.0001, now + note.at);
      gain.gain.exponentialRampToValueAtTime(0.13, now + note.at + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, now + note.at + 0.28);
      osc.connect(gain).connect(ctx.destination);
      osc.start(now + note.at);
      osc.stop(now + note.at + 0.3);
    }
    // Release the context once the chime has finished.
    window.setTimeout(() => void ctx.close(), 700);
  } catch {
    /* audio context unavailable or blocked — a missing ping is harmless */
  }
}
