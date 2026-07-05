import { describe, it, expect } from "vitest";
import {
  statusBucket,
  statusBadgeClass,
  statusLabelKey,
  formatUsd,
  formatDate,
  formatTime,
} from "@/lib/bookingFormat";
import type { BookingStatus } from "@/services/api/bookings";

describe("statusBucket", () => {
  it.each<[BookingStatus, string]>([
    ["Requested", "pending"],
    ["NoShowReported", "pending"], // frozen pending admin validation
    ["Confirmed", "confirmed"],
    ["Completed", "completed"],
    ["Rejected", "closed"],
    ["Expired", "closed"],
    ["Cancelled", "closed"],
    ["NoShowStudent", "closed"],
    ["NoShowConsultant", "closed"],
  ])("maps %s → %s bucket", (status, bucket) => {
    expect(statusBucket(status)).toBe(bucket);
  });
});

describe("statusBadgeClass", () => {
  it("uses the danger palette for the punitive no-show / rejected statuses", () => {
    for (const s of ["Rejected", "NoShowStudent", "NoShowConsultant"] as BookingStatus[]) {
      expect(statusBadgeClass(s)).toContain("danger");
    }
  });

  it("uses the success palette for a completed booking", () => {
    expect(statusBadgeClass("Completed")).toContain("success");
  });

  it("uses the muted palette for expired / cancelled", () => {
    expect(statusBadgeClass("Cancelled")).toContain("text-text-secondary");
    expect(statusBadgeClass("Expired")).toContain("text-text-secondary");
  });
});

describe("statusLabelKey", () => {
  it("namespaces the status under statusLabels", () => {
    expect(statusLabelKey("Confirmed")).toBe("statusLabels.Confirmed");
  });
});

describe("formatUsd", () => {
  it("formats whole and fractional amounts to two decimals", () => {
    expect(formatUsd(35)).toBe("$35.00");
    expect(formatUsd(35.5)).toBe("$35.50");
    expect(formatUsd(0)).toBe("$0.00");
  });
});

describe("formatDate / formatTime null-safety", () => {
  it("returns an empty string for a missing timestamp instead of crashing", () => {
    expect(formatDate(null, "en")).toBe("");
    expect(formatDate(undefined, "en")).toBe("");
    expect(formatTime(null, "en")).toBe("");
  });

  it("echoes an unparseable string back rather than rendering 'Invalid Date'", () => {
    expect(formatDate("not-a-date", "en")).toBe("not-a-date");
  });

  it("formats a valid ISO instant to a day-level label", () => {
    // Assert on stable parts (locale/runtime can vary the exact separators).
    const out = formatDate("2026-04-25T18:30:00Z", "en");
    expect(out).toContain("2026");
    expect(out).toContain("Apr");
    expect(out).toContain("25");
  });
});
