/**
 * Maps the canonical English expertise tag keys stored in
 * `UserProfile.ExpertiseTagsJson` (and `RequiredDocumentsJson`) to a
 * localized label. Falls back to the original key when no translation is
 * known so freshly-seeded values still render.
 */

import type { TFunction } from "i18next";

/** Canonical English keys → Arabic translations. */
const TAG_TRANSLATIONS_AR: Record<string, string> = {
  // Expertise tags (consultants)
  "University Selection": "اختيار الجامعة",
  "Interview Prep": "التحضير للمقابلة",
  "Interview Preparation": "التحضير للمقابلة",
  "Statement of Purpose": "خطاب الغرض",
  "Recommendation Letters": "خطابات التوصية",
  "Master's Applications": "تقديمات الماجستير",
  "PhD Applications": "تقديمات الدكتوراه",
  "Undergraduate Applications": "تقديمات البكالوريوس",
  "Visa Guidance": "إرشاد التأشيرة",
  "Letter of Recommendation": "خطاب التوصية",
  "Personal Statement": "البيان الشخصي",
  "Essay Writing": "كتابة المقالات",
  "Scholarship Search": "البحث عن منح",
  "Application Strategy": "استراتيجية التقديم",
  "Test Preparation": "التحضير للاختبارات",
  "IELTS Preparation": "تحضير الآيلتس",
  "TOEFL Preparation": "تحضير التوفل",
  "GRE Preparation": "تحضير GRE",
  "Resume Review": "مراجعة السيرة الذاتية",
  "CV Review": "مراجعة السيرة الذاتية",

  // Additional consultant specializations (seeded in CuratedConsultantBios
  // and the generated-user expertise sets). Add new entries here whenever a
  // new tag value lands in the seed data.
  "Scholarship Strategy": "استراتيجية المنح",
  "Fully Funded Scholarships": "المنح الممولة بالكامل",
  "Funding Strategy": "استراتيجية التمويل",
  "UK Admissions": "القبول في المملكة المتحدة",
  "US Admissions": "القبول في الولايات المتحدة",
  "EU Admissions": "القبول في أوروبا",
  "Canada Admissions": "القبول في كندا",
  "Australia Admissions": "القبول في أستراليا",
  "Germany Admissions": "القبول في ألمانيا",
  "Research Proposals": "مقترحات البحث",
  "Research Proposal": "مقترح البحث",
  "PostDoc Applications": "تقديمات ما بعد الدكتوراه",
  "Postdoc Applications": "تقديمات ما بعد الدكتوراه",
  "Fellowships": "الزمالات",
  "Fellowship": "زمالة",
  "Application Review": "مراجعة الطلب",
  "Personal Statements": "البيانات الشخصية",
  "STEM Applications": "تقديمات العلوم والهندسة",
  "STEM": "العلوم والهندسة",
  "Humanities": "العلوم الإنسانية",
  "Arts and Humanities": "الفنون والعلوم الإنسانية",
  "Business Admissions": "القبول في كليات الأعمال",
  "MBA Applications": "تقديمات الماجستير في إدارة الأعمال",
  "Medical Admissions": "القبول في كليات الطب",
  "Law School Admissions": "القبول في كليات الحقوق",
  "Engineering Applications": "تقديمات الهندسة",
  "Computer Science Applications": "تقديمات علوم الحاسب",
  "Data Science Applications": "تقديمات علم البيانات",
  "General Guidance": "إرشاد عام",
  "Career Guidance": "التوجيه المهني",
  "Mentorship": "الإرشاد",
  "Mentoring": "الإرشاد",
  "College Counseling": "إرشاد الكلية",
  "Admissions Counseling": "إرشاد القبول",
  "Study Abroad": "الدراسة بالخارج",
  "Pre-Departure": "ما قبل السفر",
  "Cultural Adaptation": "التأقلم الثقافي",
  "Networking": "التشبيك المهني",
  "Letter Writing": "كتابة الخطابات",
  "Cover Letter": "خطاب التغطية",
  "Cover Letters": "خطابات التغطية",
  "DAAD": "DAAD",
  "Erasmus": "إيراسموس",
  "Chevening": "تشيفنينج",
  "Fulbright": "فولبرايت",

  // Required documents
  "Transcript": "كشف الدرجات",
  "Transcripts": "كشوف الدرجات",
  "RecommendationLetter": "خطاب التوصية",
  "Recommendation Letter": "خطاب التوصية",
  "PersonalStatement": "البيان الشخصي",
  "StatementOfPurpose": "خطاب الغرض",
  "CV": "السيرة الذاتية",
  "Resume": "السيرة الذاتية",
  "Passport": "جواز السفر",
  "Passport Copy": "صورة جواز السفر",
  "IDCard": "بطاقة الهوية",
  "ID Card": "بطاقة الهوية",
  "EnglishTestScore": "درجة اختبار اللغة",
  "Photo": "صورة شخصية",
  "Diploma": "شهادة التخرج",
  "Certificate": "شهادة",
  "Portfolio": "ملف الأعمال",
  "Reference": "مرجع",
  "MotivationLetter": "خطاب الدافع",
  "Motivation Letter": "خطاب الدافع",

  // Resource tags (slug-style — match seed data in DbSeeder.Resources.cs)
  "pre-departure": "ما قبل السفر",
  "guide": "دليل",
  "checklist": "قائمة تحقق",
  "visa": "التأشيرة",
  "ielts": "الآيلتس",
  "toefl": "التوفل",
  "gre": "GRE",
  "sop": "خطاب الغرض",
  "statement-of-purpose": "خطاب الغرض",
  "recommendation": "خطاب التوصية",
  "recommendation-letter": "خطاب التوصية",
  "interview": "المقابلة",
  "interview-prep": "تحضير المقابلة",
  "scholarship": "المنح",
  "scholarships": "المنح",
  "funding": "التمويل",
  "application": "التقديم",
  "applications": "التقديمات",
  "essay": "المقال",
  "essay-writing": "كتابة المقال",
  "english": "اللغة الإنجليزية",
  "language": "اللغة",
  "language-test": "اختبار اللغة",
  "cultural-adaptation": "التأقلم الثقافي",
  "country-guide": "دليل الدول",
  "germany": "ألمانيا",
  "canada": "كندا",
  "uk": "المملكة المتحدة",
  "usa": "الولايات المتحدة",
  "europe": "أوروبا",
  "research": "البحث",
  "phd": "الدكتوراه",
  "masters": "الماجستير",
  "undergrad": "البكالوريوس",
  "tips": "نصائح",
  "video": "فيديو",
  "webinar": "ندوة",
  "workshop": "ورشة عمل",
  "panel": "حلقة نقاش",
  "financial-aid": "المساعدات المالية",
  "budgeting": "الميزانية",
  // Article / video / checklist topic tags that were still rendering their
  // raw English slug in Arabic (flagged on the Resources page). Keys match
  // the TagsJson values seeded in DbSeeder.Resources.cs.
  "beginner": "مبتدئ",
  "article": "مقال",
  "essays": "المقالات",
  "writing": "الكتابة",
  "recommendations": "خطابات التوصية",
  "finance": "المالية",
  "life-abroad": "الحياة بالخارج",
  "wellbeing": "الرفاهية",
  "planning": "التخطيط",
  "shortlist": "القائمة المختصرة",
  "documents": "المستندات",
  "draft": "مسودة",
};

// Reverse index (Arabic value → canonical English key), built once from the
// map above. Lets us normalise a tag that was stored in Arabic back to its
// English canonical so the English UI never shows a stray Arabic chip among
// English ones (a consultant typed their specialisation in Arabic).
const AR_TO_EN: Record<string, string> = (() => {
  const rev: Record<string, string> = {};
  for (const [en, ar] of Object.entries(TAG_TRANSLATIONS_AR)) {
    if (!(ar in rev)) rev[ar] = en; // first English key wins for duplicate AR values
  }
  return rev;
})();

// Known free-text Arabic variants that aren't exact map values, pinned to their
// canonical English key. Extend when a new stray value shows up in real data.
const NON_CANONICAL_ALIASES: Record<string, string> = {
  "التحضير للمقابلات": "Interview Prep",
};

/** Normalise any stored tag (English or Arabic) to its canonical English key. */
function toCanonicalEnglish(tag: string): string {
  return NON_CANONICAL_ALIASES[tag] ?? AR_TO_EN[tag] ?? tag;
}

/**
 * Returns a localized label for an expertise tag / required document key.
 * Pass the current i18n `t` (or its `language`) so we can pick AR/EN.
 * Normalises Arabic-stored values to their English canonical first, so the
 * English UI stays all-English and the Arabic UI stays all-Arabic.
 */
export function expertiseTagLabel(tag: string, t: TFunction): string {
  // i18next exposes language via t.lng on the i18n instance; the safest
  // pattern is to check i18next directly. We accept either signature.
  const lang = (t as unknown as { lng?: string }).lng
    ?? (typeof window !== "undefined" ? document.documentElement.lang : "en");
  return expertiseTagLabelByLang(tag, lang);
}

/** Same as above but takes an explicit language code. */
export function expertiseTagLabelByLang(tag: string, lang: string | undefined): string {
  const canonical = toCanonicalEnglish(tag);
  if (lang?.startsWith("ar")) {
    return TAG_TRANSLATIONS_AR[canonical] ?? canonical;
  }
  return canonical;
}

/** ISO 639-1 → display name in EN / AR. */
const LANGUAGE_NAMES_AR: Record<string, string> = {
  ar: "العربية",
  en: "الإنجليزية",
  fr: "الفرنسية",
  de: "الألمانية",
  es: "الإسبانية",
  it: "الإيطالية",
  tr: "التركية",
  ur: "الأردية",
  hi: "الهندية",
  zh: "الصينية",
  ja: "اليابانية",
  ko: "الكورية",
  ru: "الروسية",
  pt: "البرتغالية",
  nl: "الهولندية",
  fa: "الفارسية",
};
const LANGUAGE_NAMES_EN: Record<string, string> = {
  ar: "Arabic",
  en: "English",
  fr: "French",
  de: "German",
  es: "Spanish",
  it: "Italian",
  tr: "Turkish",
  ur: "Urdu",
  hi: "Hindi",
  zh: "Chinese",
  ja: "Japanese",
  ko: "Korean",
  ru: "Russian",
  pt: "Portuguese",
  nl: "Dutch",
  fa: "Persian",
};

/** Render an ISO language code (ar / en / etc) as a localized full name. */
export function languageNameByLang(code: string, lang: string | undefined): string {
  const lower = code.toLowerCase();
  if (lang?.startsWith("ar")) {
    return LANGUAGE_NAMES_AR[lower] ?? code.toUpperCase();
  }
  return LANGUAGE_NAMES_EN[lower] ?? code.toUpperCase();
}
