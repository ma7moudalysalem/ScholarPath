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

import arCommon from "@/locales/ar/common.json";
import arAuth from "@/locales/ar/auth.json";
import arHome from "@/locales/ar/home.json";
import arErrors from "@/locales/ar/errors.json";
import arNav from "@/locales/ar/nav.json";
import arEmptyStates from "@/locales/ar/emptyStates.json";
import arPrivacy from "@/locales/ar/privacy.json";
import arAdmin from "@/locales/ar/admin.json";

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
    ns: ["common", "auth", "home", "errors", "nav", "emptyStates", "privacy", "admin"],
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
      },
    },
  });

export default i18n;
