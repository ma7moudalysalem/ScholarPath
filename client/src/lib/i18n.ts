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
import enScholarshipProvider from "@/locales/en/company.json";
import enProfile from "@/locales/en/profile.json";
import enNotifications from "@/locales/en/notifications.json";
import enDashboard from "@/locales/en/dashboard.json";
import enPayments from "@/locales/en/payments.json";
import enResources from "@/locales/en/resources.json";
import enModeration from "@/locales/en/moderation.json";
import enSettings from "@/locales/en/settings.json";
import enDocuments from "@/locales/en/documents.json";
import enConsultants from "@/locales/en/consultants.json";
import enBookings from "@/locales/en/bookings.json";
import enConsultantPortal from "@/locales/en/consultantPortal.json";
import enCommunity from "@/locales/en/community.json";
import enAnalytics from "@/locales/en/analytics.json";
import enLegal from "@/locales/en/legal.json";
import enReviews from "@/locales/en/reviews.json";

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
import arScholarshipProvider from "@/locales/ar/company.json";
import arProfile from "@/locales/ar/profile.json";
import arNotifications from "@/locales/ar/notifications.json";
import arDashboard from "@/locales/ar/dashboard.json";
import arPayments from "@/locales/ar/payments.json";
import arResources from "@/locales/ar/resources.json";
import arModeration from "@/locales/ar/moderation.json";
import arSettings from "@/locales/ar/settings.json";
import arDocuments from "@/locales/ar/documents.json";
import arConsultants from "@/locales/ar/consultants.json";
import arBookings from "@/locales/ar/bookings.json";
import arConsultantPortal from "@/locales/ar/consultantPortal.json";
import arCommunity from "@/locales/ar/community.json";
import arAnalytics from "@/locales/ar/analytics.json";
import arLegal from "@/locales/ar/legal.json";
import arReviews from "@/locales/ar/reviews.json";

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
    fallbackLng: "ar",
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
      "documents",
      "consultants",
      "bookings",
      "consultantPortal",
      "community",
      "analytics",
      "legal",
      "reviews",
    ],
    defaultNS: "common",
    interpolation: { escapeValue: false },
    detection: {
      // Arabic-first: the app defaults to Arabic; only an explicit saved
      // choice overrides it. The key is versioned so existing visitors are
      // re-defaulted to Arabic rather than kept on a stale English cache.
      order: ["localStorage"],
      caches: ["localStorage"],
      lookupLocalStorage: "scholarpath_lang_v2",
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
        company: enScholarshipProvider,
        profile: enProfile,
        notifications: enNotifications,
        dashboard: enDashboard,
        payments: enPayments,
        resources: enResources,
        moderation: enModeration,
        settings: enSettings,
        documents: enDocuments,
        consultants: enConsultants,
        bookings: enBookings,
        consultantPortal: enConsultantPortal,
        community: enCommunity,
        analytics: enAnalytics,
        legal: enLegal,
        reviews: enReviews,
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
        company: arScholarshipProvider,
        profile: arProfile,
        notifications: arNotifications,
        dashboard: arDashboard,
        payments: arPayments,
        resources: arResources,
        moderation: arModeration,
        settings: arSettings,
        documents: arDocuments,
        consultants: arConsultants,
        bookings: arBookings,
        consultantPortal: arConsultantPortal,
        community: arCommunity,
        analytics: arAnalytics,
        legal: arLegal,
        reviews: arReviews,
      },
    },
  });

export default i18n;
