/**
 * Canonical country list + Arabic localization.
 *
 * The profile / onboarding country selectors store the canonical English
 * country name as the value (matching what the backend persists), but Arabic
 * users should see Arabic labels. `countryLabel(name, lang)` returns the
 * localized display string while the stored value stays English, so no
 * migration or backend change is needed.
 */

/** Canonical English country names — the stored value for every selector. */
export const COUNTRIES = [
  "Afghanistan", "Albania", "Algeria", "Argentina", "Armenia", "Australia",
  "Austria", "Azerbaijan", "Bahrain", "Bangladesh", "Belarus", "Belgium",
  "Bolivia", "Bosnia and Herzegovina", "Brazil", "Bulgaria", "Cambodia",
  "Cameroon", "Canada", "Chile", "China", "Colombia", "Croatia", "Cuba",
  "Czech Republic", "Denmark", "Ecuador", "Egypt", "Ethiopia", "Finland",
  "France", "Georgia", "Germany", "Ghana", "Greece", "Guatemala", "Hungary",
  "India", "Indonesia", "Iran", "Iraq", "Ireland", "Italy", "Japan", "Jordan",
  "Kazakhstan", "Kenya", "Kuwait", "Kyrgyzstan", "Lebanon", "Libya", "Malaysia",
  "Mexico", "Morocco", "Myanmar", "Nepal", "Netherlands", "New Zealand",
  "Nigeria", "Norway", "Oman", "Pakistan", "Palestine", "Peru", "Philippines",
  "Poland", "Portugal", "Qatar", "Romania", "Russia", "Saudi Arabia", "Senegal",
  "Serbia", "South Africa", "South Korea", "Spain", "Sri Lanka", "Sudan",
  "Sweden", "Switzerland", "Syria", "Taiwan", "Tajikistan", "Tanzania",
  "Thailand", "Tunisia", "Turkey", "Turkmenistan", "Uganda", "Ukraine",
  "United Arab Emirates", "United Kingdom", "United States", "Uruguay",
  "Uzbekistan", "Venezuela", "Vietnam", "Yemen", "Zimbabwe",
] as const;

/** Canonical English name → Arabic display name. */
const COUNTRY_AR: Record<string, string> = {
  "Afghanistan": "أفغانستان", "Albania": "ألبانيا", "Algeria": "الجزائر",
  "Argentina": "الأرجنتين", "Armenia": "أرمينيا", "Australia": "أستراليا",
  "Austria": "النمسا", "Azerbaijan": "أذربيجان", "Bahrain": "البحرين",
  "Bangladesh": "بنغلاديش", "Belarus": "بيلاروسيا", "Belgium": "بلجيكا",
  "Bolivia": "بوليفيا", "Bosnia and Herzegovina": "البوسنة والهرسك",
  "Brazil": "البرازيل", "Bulgaria": "بلغاريا", "Cambodia": "كمبوديا",
  "Cameroon": "الكاميرون", "Canada": "كندا", "Chile": "تشيلي", "China": "الصين",
  "Colombia": "كولومبيا", "Croatia": "كرواتيا", "Cuba": "كوبا",
  "Czech Republic": "التشيك", "Denmark": "الدنمارك", "Ecuador": "الإكوادور",
  "Egypt": "مصر", "Ethiopia": "إثيوبيا", "Finland": "فنلندا", "France": "فرنسا",
  "Georgia": "جورجيا", "Germany": "ألمانيا", "Ghana": "غانا", "Greece": "اليونان",
  "Guatemala": "غواتيمالا", "Hungary": "المجر", "India": "الهند",
  "Indonesia": "إندونيسيا", "Iran": "إيران", "Iraq": "العراق", "Ireland": "أيرلندا",
  "Italy": "إيطاليا", "Japan": "اليابان", "Jordan": "الأردن",
  "Kazakhstan": "كازاخستان", "Kenya": "كينيا", "Kuwait": "الكويت",
  "Kyrgyzstan": "قيرغيزستان", "Lebanon": "لبنان", "Libya": "ليبيا",
  "Malaysia": "ماليزيا", "Mexico": "المكسيك", "Morocco": "المغرب",
  "Myanmar": "ميانمار", "Nepal": "نيبال", "Netherlands": "هولندا",
  "New Zealand": "نيوزيلندا", "Nigeria": "نيجيريا", "Norway": "النرويج",
  "Oman": "عُمان", "Pakistan": "باكستان", "Palestine": "فلسطين", "Peru": "بيرو",
  "Philippines": "الفلبين", "Poland": "بولندا", "Portugal": "البرتغال",
  "Qatar": "قطر", "Romania": "رومانيا", "Russia": "روسيا",
  "Saudi Arabia": "السعودية", "Senegal": "السنغال", "Serbia": "صربيا",
  "South Africa": "جنوب أفريقيا", "South Korea": "كوريا الجنوبية",
  "Spain": "إسبانيا", "Sri Lanka": "سريلانكا", "Sudan": "السودان",
  "Sweden": "السويد", "Switzerland": "سويسرا", "Syria": "سوريا",
  "Taiwan": "تايوان", "Tajikistan": "طاجيكستان", "Tanzania": "تنزانيا",
  "Thailand": "تايلاند", "Tunisia": "تونس", "Turkey": "تركيا",
  "Turkmenistan": "تركمانستان", "Uganda": "أوغندا", "Ukraine": "أوكرانيا",
  "United Arab Emirates": "الإمارات العربية المتحدة",
  "United Kingdom": "المملكة المتحدة", "United States": "الولايات المتحدة",
  "Uruguay": "الأوروغواي", "Uzbekistan": "أوزبكستان", "Venezuela": "فنزويلا",
  "Vietnam": "فيتنام", "Yemen": "اليمن", "Zimbabwe": "زيمبابوي",
};

/**
 * Localized country label. Falls back to the canonical English name when no
 * Arabic translation exists (so a freshly-added country still renders).
 */
export function countryLabel(name: string, lang: string | undefined): string {
  if (lang?.startsWith("ar")) {
    return COUNTRY_AR[name] ?? name;
  }
  return name;
}
