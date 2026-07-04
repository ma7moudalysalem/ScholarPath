import type { BookingStatus } from "@/services/api/bookings";

// ── Shared formatting + status helpers for the PB-006 booking pages ───────────
//
// The booking pages render real API data: ISO-8601 date strings, USD decimals,
// and the server `BookingStatus` enum. These helpers centralise the wire → UI
// mapping so the student and consultant pages stay consistent.

/** Locale used for date/number formatting, derived from the i18n language. */
function intlLocale(lang: string): string {
  return lang.startsWith("ar") ? "ar-EG" : "en-GB";
}

/** Formats an ISO-8601 instant as a day-level label (e.g. "25 Apr 2026").
 *  Returns an empty string when the input is missing — callers iterate over
 *  API rows where optional timestamp fields can be null and crashing the
 *  whole page over a missing date is hostile. */
export function formatDate(iso: string | null | undefined, lang: string): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString(intlLocale(lang), {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

/** Formats an ISO-8601 instant as a time label (e.g. "6:30 PM"). */
export function formatTime(iso: string | null | undefined, lang: string): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleTimeString(intlLocale(lang), {
    hour: "numeric",
    minute: "2-digit",
  });
}

/** Formats an ISO-8601 instant as a time label with timezone abbreviation (e.g. "6:30 PM GMT+3"). */
export function formatTimeWithTz(iso: string | null | undefined, lang: string): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleTimeString(intlLocale(lang), {
    hour: "numeric",
    minute: "2-digit",
    timeZoneName: "short",
  });
}

/** Formats an ISO-8601 instant as a combined date + time label. */
export function formatDateTime(iso: string, lang: string): string {
  return `${formatDate(iso, lang)} · ${formatTime(iso, lang)}`;
}

/** Formats a USD decimal amount (e.g. `35` → "$35.00"). */
export function formatUsd(amount: number): string {
  return `$${amount.toFixed(2)}`;
}

/** Formats a minute count using the localised `duration.minutes` plural key. */
export function durationLabel(
  minutes: number,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  return t("duration.minutes", { count: minutes });
}

// ── Booking status grouping ───────────────────────────────────────────────────

/** The coarse filter buckets the booking-list pages expose. */
export type BookingStatusBucket = "pending" | "confirmed" | "completed" | "closed";

/** Maps a server `BookingStatus` to its coarse filter bucket. */
export function statusBucket(status: BookingStatus): BookingStatusBucket {
  switch (status) {
    case "Requested":
      return "pending";
    case "Confirmed":
      return "confirmed";
    case "NoShowReported":
      // Frozen pending admin validation — still an open item for both parties.
      return "pending";
    case "Completed":
      return "completed";
    case "Rejected":
    case "Expired":
    case "Cancelled":
    case "NoShowStudent":
    case "NoShowConsultant":
      return "closed";
    default:
      return "closed";
  }
}

/** Tailwind badge classes for a booking status pill. */
export function statusBadgeClass(status: BookingStatus): string {
  switch (status) {
    case "Requested":
      return "bg-warning-50 text-warning-600";
    case "Confirmed":
      return "bg-brand-50 text-brand-600";
    case "NoShowReported":
      return "bg-warning-50 text-warning-600";
    case "Completed":
      return "bg-success-50 text-success-600";
    case "Rejected":
    case "NoShowStudent":
    case "NoShowConsultant":
      return "bg-danger-50 text-danger-500";
    case "Expired":
    case "Cancelled":
      return "bg-bg-subtle text-text-secondary";
    default:
      return "bg-bg-subtle text-text-secondary";
  }
}

/**
 * The `bookings`/`consultantPortal` locale key for a status label. All eight
 * server statuses have a dedicated key under `statusLabels`.
 */
export function statusLabelKey(status: BookingStatus): string {
  return `statusLabels.${status}`;
}
