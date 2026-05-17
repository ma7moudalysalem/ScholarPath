import i18n from "i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import { initReactI18next } from "react-i18next";

import enCommon from "@/locales/en/common.json";
import enAuth from "@/locales/en/auth.json";
import enHome from "@/locales/en/home.json";
import enErrors from "@/locales/en/errors.json";
import enNav from "@/locales/en/nav.json";
import enEmptyStates from "@/locales/en/emptyStates.json";
import enPrivacy from "@/locales/en/privacy.json";
import enAdmin from "@/locales/en/admin.json";
import enAi from "@/locales/en/ai.json";
import enScholarships from "@/locales/en/scholarships.json";
import enApplications from "@/locales/en/applications.json";
import enCompany from "@/locales/en/company.json";
import enProfile from "@/locales/en/profile.json";
import enNotifications from "@/locales/en/notifications.json";
import enDashboard from "@/locales/en/dashboard.json";
import enPayments from "@/locales/en/payments.json";
import enResources from "@/locales/en/resources.json";
import enModeration from "@/locales/en/moderation.json";
import enSettings from "@/locales/en/settings.json";

import arCommon from "@/locales/ar/common.json";
import arAuth from "@/locales/ar/auth.json";
import arHome from "@/locales/ar/home.json";
import arErrors from "@/locales/ar/errors.json";
import arNav from "@/locales/ar/nav.json";
import arEmptyStates from "@/locales/ar/emptyStates.json";
import arPrivacy from "@/locales/ar/privacy.json";
import arAdmin from "@/locales/ar/admin.json";
import arAi from "@/locales/ar/ai.json";
import arScholarships from "@/locales/ar/scholarships.json";
import arApplications from "@/locales/ar/applications.json";
import arCompany from "@/locales/ar/company.json";
import arProfile from "@/locales/ar/profile.json";
import arNotifications from "@/locales/ar/notifications.json";
import arDashboard from "@/locales/ar/dashboard.json";
import arPayments from "@/locales/ar/payments.json";
import arResources from "@/locales/ar/resources.json";
import arModeration from "@/locales/ar/moderation.json";
import arSettings from "@/locales/ar/settings.json";

export const supportedLanguages = ["en", "ar"] as const;
export type SupportedLanguage = (typeof supportedLanguages)[number];

export const rtlLanguages: SupportedLanguage[] = ["ar"];

export function getDirection(lang: string): "ltr" | "rtl" {
  return rtlLanguages.includes(lang as SupportedLanguage) ? "rtl" : "ltr";
}

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: "en",
    supportedLngs: supportedLanguages,
    ns: [
      "common",
      "auth",
      "home",
      "errors",
      "nav",
      "emptyStates",
      "privacy",
      "admin",
      "ai",
      "scholarships",
      "applications",
      "company",
      "profile",
      "notifications",
      "dashboard",
      "payments",
      "resources",
      "moderation",
      "settings",
    ],
    defaultNS: "common",
    interpolation: { escapeValue: false },
    detection: {
      order: ["localStorage", "navigator"],
      caches: ["localStorage"],
      lookupLocalStorage: "scholarpath_lang",
    },
    resources: {
      en: {
        common: enCommon,
        auth: enAuth,
        home: enHome,
        errors: enErrors,
        nav: enNav,
        emptyStates: enEmptyStates,
        privacy: enPrivacy,
        admin: enAdmin,
        ai: enAi,
        scholarships: enScholarships,
        applications: enApplications,
        company: enCompany,
        profile: enProfile,
        notifications: enNotifications,
        dashboard: enDashboard,
        payments: enPayments,
        resources: enResources,
        moderation: enModeration,
        settings: enSettings,
      },
      ar: {
        common: arCommon,
        auth: arAuth,
        home: arHome,
        errors: arErrors,
        nav: arNav,
        emptyStates: arEmptyStates,
        privacy: arPrivacy,
        admin: arAdmin,
        ai: arAi,
        scholarships: arScholarships,
        applications: arApplications,
        company: arCompany,
        profile: arProfile,
        notifications: arNotifications,
        dashboard: arDashboard,
        payments: arPayments,
        resources: arResources,
        moderation: arModeration,
        settings: arSettings,
      },
    },
  });

export default i18n;
