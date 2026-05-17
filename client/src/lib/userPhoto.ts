/**
 * Helpers for rendering user profile photos.
 *
 * A user's profile photo is served by the backend at a stable per-user URL —
 * `GET /api/profiles/{userId}/photo` — rather than exposed as a raw storage
 * path. The endpoint is anonymous-accessible (photos appear on the public
 * consultant-browse pages) and returns 404 when the user has no photo, so every
 * `<img>` rendered through this helper should pair it with an `onError`
 * fallback to the initials/placeholder.
 */

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";

/** Absolute URL that serves the given user's profile photo. */
export function userPhotoUrl(userId: string): string {
  return `${API_BASE_URL}/api/profiles/${userId}/photo`;
}

/**
 * Profile-photo URL with a cache-busting `?v=` query so a freshly uploaded
 * photo replaces the previously cached one immediately. Pass a value that
 * changes on upload (e.g. `Date.now()`).
 */
export function userPhotoUrlWithVersion(userId: string, version: string | number): string {
  return `${userPhotoUrl(userId)}?v=${version}`;
}
