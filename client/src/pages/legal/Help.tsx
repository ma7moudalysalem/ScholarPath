import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useState } from "react";
import { motion, AnimatePresence } from "motion/react";
import { ChevronDown, LifeBuoy, Mail, MessageSquare, ArrowRight } from "lucide-react";
import { LegalLayout } from "./LegalLayout";

/**
 * Help center — collapsible FAQ + clear escalation paths to support. We keep
 * the answer copy in i18n JSON so each FAQ ships in both English and Arabic
 * without code changes.
 */
export function Help() {
  const { t } = useTranslation(["legal", "common"]);

  // Each FAQ entry is `legal:help.faq.<key>.{q,a}` in the locale file.
  const faqKeys = [
    "account",
    "matching",
    "applications",
    "consultants",
    "refunds",
    "documents",
    "languages",
    "privacy",
  ] as const;

  return (
    <LegalLayout
      eyebrow={t("legal:eyebrow.support")}
      title={t("legal:help.title")}
      subtitle={t("legal:help.subtitle")}
    >
      <section className="not-prose mb-12 grid gap-4 sm:grid-cols-3">
        <ChannelCard
          icon={Mail}
          title={t("legal:help.channels.email.title")}
          body={t("legal:help.channels.email.body")}
          actionLabel={t("legal:help.channels.email.cta")}
          href="mailto:support@scholarpath.local"
        />
        <ChannelCard
          icon={MessageSquare}
          title={t("legal:help.channels.chat.title")}
          body={t("legal:help.channels.chat.body")}
          actionLabel={t("legal:help.channels.chat.cta")}
          href="/student/messages"
        />
        <ChannelCard
          icon={LifeBuoy}
          title={t("legal:help.channels.community.title")}
          body={t("legal:help.channels.community.body")}
          actionLabel={t("legal:help.channels.community.cta")}
          href="/student/community"
        />
      </section>

      <h2>{t("legal:help.faqHeading")}</h2>
      <div className="not-prose mt-6 space-y-3">
        {faqKeys.map((key) => (
          <FaqItem
            key={key}
            question={t(`legal:help.faq.${key}.q`)}
            answer={t(`legal:help.faq.${key}.a`)}
          />
        ))}
      </div>

      <h2>{t("legal:help.stillStuck.heading")}</h2>
      <p>{t("legal:help.stillStuck.body")}</p>
    </LegalLayout>
  );
}

function ChannelCard({
  icon: Icon,
  title,
  body,
  actionLabel,
  href,
}: {
  icon: typeof LifeBuoy;
  title: string;
  body: string;
  actionLabel: string;
  href: string;
}) {
  const isExternal = href.startsWith("mailto:") || /^https?:/.test(href);
  const content = (
    <>
      <div className="mb-3 inline-flex size-10 items-center justify-center rounded-lg bg-brand-50 text-brand-600 transition-transform group-hover:scale-110 dark:bg-brand-500/10">
        <Icon aria-hidden className="size-5" />
      </div>
      <h3 className="text-base font-semibold text-text-primary">{title}</h3>
      <p className="mt-1 text-sm leading-relaxed text-text-secondary">{body}</p>
      <span className="mt-3 inline-flex items-center gap-1 text-sm font-semibold text-brand-600 group-hover:gap-2 transition-all">
        {actionLabel}
        <ArrowRight aria-hidden className="size-3.5 rtl:rotate-180" />
      </span>
    </>
  );

  const className =
    "group block rounded-2xl border border-border-subtle bg-bg-elevated p-5 transition hover:border-brand-200 hover:shadow-elevation-2 dark:hover:border-brand-500/40";

  return isExternal ? (
    <a href={href} className={className}>
      {content}
    </a>
  ) : (
    <Link to={href} className={className}>
      {content}
    </Link>
  );
}

function FaqItem({ question, answer }: { question: string; answer: string }) {
  const [open, setOpen] = useState(false);
  return (
    <div className="rounded-xl border border-border-subtle bg-bg-elevated">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center justify-between gap-4 px-5 py-4 text-start"
        aria-expanded={open}
      >
        <span className="text-sm font-semibold text-text-primary">{question}</span>
        <ChevronDown
          aria-hidden
          className={`size-4 shrink-0 text-text-tertiary transition-transform duration-200 ${
            open ? "rotate-180" : ""
          }`}
        />
      </button>
      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            key="content"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.22, ease: [0.22, 1, 0.36, 1] }}
            className="overflow-hidden"
          >
            <p className="px-5 pb-4 text-sm leading-relaxed text-text-secondary">
              {answer}
            </p>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
