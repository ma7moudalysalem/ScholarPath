/**
 * Format a date as a localized relative time string ("2 hours ago").
 *
 * Returns an empty string when the input is falsy or unparseable — defensive
 * because the ScholarshipProvider / Consultant dashboards iterate over API rows where
 * optional timestamp fields (createdAt, sentAt, lastReviewedAt, etc.) can
 * legitimately be null. Crashing the whole dashboard for a missing date on
 * one row would erase the entire page.
 */
export function formatRelativeTime(
  isoOrDate: string | Date | null | undefined,
  locale = "en",
): string {
  if (!isoOrDate) return "";
  const d = typeof isoOrDate === "string" ? new Date(isoOrDate) : isoOrDate;
  if (Number.isNaN(d.getTime())) return "";

  const diff = Date.now() - d.getTime();
  const seconds = Math.round(diff / 1000);
  const minutes = Math.round(seconds / 60);
  const hours = Math.round(minutes / 60);
  const days = Math.round(hours / 24);

  const rtf = new Intl.RelativeTimeFormat(locale === "ar" ? "ar-EG" : "en-US", { numeric: "auto" });

  if (Math.abs(seconds) < 60) return rtf.format(-seconds, "second");
  if (Math.abs(minutes) < 60) return rtf.format(-minutes, "minute");
  if (Math.abs(hours) < 24) return rtf.format(-hours, "hour");
  if (Math.abs(days) < 30) return rtf.format(-days, "day");
  const months = Math.round(days / 30);
  if (Math.abs(months) < 12) return rtf.format(-months, "month");
  return rtf.format(-Math.round(months / 12), "year");
}
