import { describe, it, expect } from "vitest";
import { userPhotoUrl, userPhotoUrlWithVersion } from "@/lib/userPhoto";

describe("userPhotoUrl", () => {
  it("builds the stable per-user photo endpoint path", () => {
    // VITE_API_BASE_URL is unset in tests, so the base collapses to a
    // same-origin relative path — assert on the stable suffix.
    expect(userPhotoUrl("abc-123")).toBe("/api/profiles/abc-123/photo");
  });

  it("appends a cache-busting version query", () => {
    expect(userPhotoUrlWithVersion("abc-123", 42)).toBe(
      "/api/profiles/abc-123/photo?v=42",
    );
    expect(userPhotoUrlWithVersion("abc-123", "hash")).toBe(
      "/api/profiles/abc-123/photo?v=hash",
    );
  });
});
