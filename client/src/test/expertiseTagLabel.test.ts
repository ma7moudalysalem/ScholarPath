import { describe, it, expect } from "vitest";
import {
  expertiseTagLabelByLang,
  languageNameByLang,
} from "@/lib/expertiseTagLabel";

describe("expertiseTagLabelByLang", () => {
  it("returns the Arabic label for a canonical English key in Arabic mode", () => {
    expect(expertiseTagLabelByLang("Interview Prep", "ar")).toBe("التحضير للمقابلة");
  });

  it("returns the canonical English key unchanged in English mode", () => {
    expect(expertiseTagLabelByLang("Interview Prep", "en")).toBe("Interview Prep");
  });

  it("normalises an Arabic-stored value back to English for the English UI", () => {
    // A consultant who typed their specialisation in Arabic must not surface a
    // stray Arabic chip among English ones.
    expect(expertiseTagLabelByLang("التحضير للمقابلة", "en")).toBe("Interview Prep");
  });

  it("keeps an Arabic-stored value Arabic in the Arabic UI", () => {
    expect(expertiseTagLabelByLang("التحضير للمقابلة", "ar")).toBe("التحضير للمقابلة");
  });

  it("maps known free-text Arabic aliases to their canonical English key", () => {
    // "التحضير للمقابلات" (plural) is not an exact map value but is pinned.
    expect(expertiseTagLabelByLang("التحضير للمقابلات", "en")).toBe("Interview Prep");
    expect(expertiseTagLabelByLang("التحضير للمقابلات", "ar")).toBe("التحضير للمقابلة");
  });

  it("falls back to the raw key when no translation is known", () => {
    expect(expertiseTagLabelByLang("Quantum Basketry", "ar")).toBe("Quantum Basketry");
    expect(expertiseTagLabelByLang("Quantum Basketry", "en")).toBe("Quantum Basketry");
  });

  it("treats an undefined language as English", () => {
    expect(expertiseTagLabelByLang("Interview Prep", undefined)).toBe("Interview Prep");
  });

  it("handles slug-style resource tags", () => {
    expect(expertiseTagLabelByLang("pre-departure", "ar")).toBe("ما قبل السفر");
    expect(expertiseTagLabelByLang("checklist", "ar")).toBe("قائمة تحقق");
  });
});

describe("languageNameByLang", () => {
  it("renders an ISO code as a localized full name", () => {
    expect(languageNameByLang("en", "ar")).toBe("الإنجليزية");
    expect(languageNameByLang("ar", "en")).toBe("Arabic");
  });

  it("is case-insensitive on the code", () => {
    expect(languageNameByLang("EN", "en")).toBe("English");
  });

  it("falls back to the upper-cased code when unknown", () => {
    expect(languageNameByLang("xx", "en")).toBe("XX");
    expect(languageNameByLang("xx", "ar")).toBe("XX");
  });
});
