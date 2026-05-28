import { format, type Locale } from "date-fns";

/**
 * Calendar-day helpers for fields that are conceptually a *day* (a deadline,
 * a date-of-birth, etc.) but flow through the API as an ISO instant string
 * (`DateTimeOffset` on the server).
 *
 * The picker emits the user's local calendar day as `YYYY-MM-DD`, the form
 * then submits that as `YYYY-MM-DDT00:00:00Z`. The string the server hands
 * back always starts with the same `YYYY-MM-DD` prefix — but `new Date(iso)`
 * applies the viewer's timezone shift, so for any viewer west of UTC the
 * displayed day drifts one back (the off-by-one we saw on the company
 * scholarships list). Parsing just the date prefix keeps the calendar day
 * stable regardless of where the viewer sits.
 */

/** Parses the `YYYY-MM-DD` prefix of an ISO string into a local-time Date. */
export function parseCalendarDate(iso: string | null | undefined): Date | null {
  if (!iso) return null;
  const slice = iso.slice(0, 10);
  const parts = slice.split("-").map(Number);
  if (parts.length !== 3 || parts.some((n) => Number.isNaN(n))) return null;
  const [y, m, d] = parts;
  return new Date(y, m - 1, d);
}

/** Formats the calendar-day portion of an ISO string with a date-fns pattern. */
export function formatCalendarDate(
  iso: string | null | undefined,
  pattern: string,
  locale?: Locale,
): string {
  const date = parseCalendarDate(iso);
  if (!date) return "";
  return format(date, pattern, locale ? { locale } : undefined);
}
