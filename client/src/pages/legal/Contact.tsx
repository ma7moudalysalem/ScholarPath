import { useTranslation } from "react-i18next";
import { Mail, MapPin, MessageSquare, Phone } from "lucide-react";
import { LegalLayout } from "./LegalLayout";

/**
 * Public Contact page. Lists every official way to reach the team — we don't
 * accept form submissions here yet, so the page is plain and points to email
 * + in-app channels.
 */
export function Contact() {
  const { t } = useTranslation(["legal"]);

  const channels = [
    {
      icon: Mail,
      title: t("legal:contact.email.title"),
      body: t("legal:contact.email.body"),
      cta: "support@scholarpath.local",
      href: "mailto:support@scholarpath.local",
    },
    {
      icon: MessageSquare,
      title: t("legal:contact.community.title"),
      body: t("legal:contact.community.body"),
      cta: t("legal:contact.community.cta"),
      href: "/student/community",
    },
    {
      icon: Phone,
      title: t("legal:contact.phone.title"),
      body: t("legal:contact.phone.body"),
      cta: t("legal:contact.phone.cta"),
      href: "mailto:partnerships@scholarpath.local",
    },
    {
      icon: MapPin,
      title: t("legal:contact.office.title"),
      body: t("legal:contact.office.body"),
      cta: t("legal:contact.office.cta"),
      href: null,
    },
  ];

  return (
    <LegalLayout
      eyebrow={t("legal:eyebrow.support")}
      title={t("legal:contact.title")}
      subtitle={t("legal:contact.subtitle")}
    >
      <div className="not-prose grid gap-4 sm:grid-cols-2">
        {channels.map(({ icon: Icon, title, body, cta, href }) => {
          const card = (
            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 transition hover:border-brand-200 hover:shadow-elevation-2 dark:hover:border-brand-500/40 h-full">
              <div className="inline-flex size-10 items-center justify-center rounded-lg bg-brand-50 text-brand-600 dark:bg-brand-500/10">
                <Icon aria-hidden className="size-5" />
              </div>
              <h3 className="mt-3 text-base font-semibold text-text-primary">{title}</h3>
              <p className="mt-1 text-sm leading-relaxed text-text-secondary">{body}</p>
              <p className="mt-3 text-sm font-semibold text-brand-600">{cta}</p>
            </div>
          );
          return href ? (
            <a key={title} href={href} className="block">
              {card}
            </a>
          ) : (
            <div key={title}>{card}</div>
          );
        })}
      </div>

      <h2>{t("legal:contact.hours.heading")}</h2>
      <p>{t("legal:contact.hours.body")}</p>

      <h2>{t("legal:contact.press.heading")}</h2>
      <p>{t("legal:contact.press.body")}</p>
    </LegalLayout>
  );
}
