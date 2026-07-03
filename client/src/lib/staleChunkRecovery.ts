/**
 * Stale-chunk recovery.
 *
 * A new client deployment replaces the content-hashed JS chunks. An already-open
 * tab that lazy-loads a route AFTER a deploy then requests a chunk filename the
 * new build deleted. Azure Static Web Apps serves the SPA fallback (index.html,
 * `text/html`) for the missing `.js`, so the dynamic import fails with
 * "Failed to fetch dynamically imported module" / a strict-MIME error and the
 * page crashes. Recover by reloading ONCE to pull the fresh index.html + assets.
 */

const RELOAD_AT_KEY = "sp_chunk_reload_at";
// Reload at most once per cooldown so a genuinely-broken chunk can't loop.
const RELOAD_COOLDOWN_MS = 10_000;

/** Reloads the page once (rate-limited) to fetch freshly-deployed assets. */
export function recoverFromStaleChunk(): void {
  try {
    const last = Number(sessionStorage.getItem(RELOAD_AT_KEY) || 0);
    if (Date.now() - last < RELOAD_COOLDOWN_MS) return; // avoid reload loops
    sessionStorage.setItem(RELOAD_AT_KEY, String(Date.now()));
  } catch {
    // sessionStorage unavailable (rare private-mode edge) — fall through and
    // reload anyway; the browser will de-dupe rapid reloads.
  }
  window.location.reload();
}

const CHUNK_ERROR_RE =
  /(dynamically imported module|importing a module script failed|error loading dynamically imported module|failed to fetch dynamically|chunkloaderror|expected a javascript-or-wasm module script)/i;

/** Heuristic: is this error a stale/missing lazy chunk (vs a real app bug)? */
export function isChunkLoadError(error: unknown): boolean {
  const msg =
    error instanceof Error ? `${error.name} ${error.message}` : String(error ?? "");
  return CHUNK_ERROR_RE.test(msg);
}

/**
 * Registers global listeners for dynamic-import/preload failures so a stale-chunk
 * error after a deploy self-heals instead of showing a crash screen. Call once at
 * startup.
 */
export function installStaleChunkRecovery(): void {
  // Vite fires this for its module-preload helper failures.
  window.addEventListener("vite:preloadError", (event) => {
    event.preventDefault(); // suppress the unhandled rejection; we recover instead
    recoverFromStaleChunk();
  });

  // React.lazy import() rejections surface as unhandled promise rejections before
  // (or instead of) reaching the ErrorBoundary in some paths.
  window.addEventListener("unhandledrejection", (event) => {
    if (isChunkLoadError(event.reason)) {
      event.preventDefault();
      recoverFromStaleChunk();
    }
  });
}
