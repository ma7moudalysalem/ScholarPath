import { describe, it, expect } from "vitest";
import { FLAG_REASONS, flagReasonLabel } from "@/lib/flagReasons";

describe("flagReasonLabel", () => {
  it("localizes every canonical reason key in both languages", () => {
    for (const reason of FLAG_REASONS) {
      const en = flagReasonLabel(reason, "en");
      const ar = flagReasonLabel(reason, "ar");
      // A localized label must differ from the raw key (except where a key is
      // its own word) and never be empty.
      expect(en).toBeTruthy();
      expect(ar).toBeTruthy();
      expect(ar).not.toBe(en);
    }
  });

  it("picks Arabic labels for an Arabic language code", () => {
    expect(flagReasonLabel("spam", "ar")).toBe("إعلانات أو محتوى مزعج");
  });

  it("defaults to English for a non-Arabic (or undefined) language", () => {
    expect(flagReasonLabel("harassment", "en")).toBe("Harassment or abuse");
    expect(flagReasonLabel("harassment", undefined)).toBe("Harassment or abuse");
  });

  it("renders known free-text seed reasons readably in Arabic", () => {
    expect(flagReasonLabel("Advertising", "ar")).toBe("إعلانات");
    expect(flagReasonLabel("Academic dishonesty", "ar")).toBe("غش أكاديمي");
  });

  it("falls back to the raw value for an unknown legacy reason", () => {
    expect(flagReasonLabel("something-custom", "en")).toBe("something-custom");
    expect(flagReasonLabel("something-custom", "ar")).toBe("something-custom");
  });
});
