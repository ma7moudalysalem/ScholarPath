import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { ArrowRight, Sparkles, Search, ListChecks, Users, MessageSquare } from "lucide-react";
import { motion } from "motion/react";

export function Home() {
  const { t } = useTranslation(["home", "common"]);

  return (
    <div>
      <HomeHero />
      <Pillars />
      <ReadySection />
    </div>
  );

  function HomeHero() {
    return (
      <section className="relative overflow-hidden bg-bg-canvas">
        <div className="mx-auto max-w-7xl px-4 pt-24 pb-20 text-center sm:px-6 sm:pt-28 sm:pb-24">
          <motion.p
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
            className="mb-5 inline-flex items-center gap-2 rounded-full border border-border-subtle bg-bg-subtle px-3 py-1 text-sm text-text-secondary"
          >
            <Sparkles aria-hidden className="size-3.5 text-brand-500" />
            {t("home:hero.eyebrow")}
          </motion.p>

          <motion.h1
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.05 }}
            className="mx-auto max-w-3xl text-balance text-4xl sm:text-5xl md:text-6xl"
          >
            {t("home:hero.title")}
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.12 }}
            className="mx-auto mt-5 max-w-2xl text-balance text-lg text-text-secondary"
          >
            {t("home:hero.subtitle")}
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.2 }}
            className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row"
          >
            <Link
              to="/register"
              className="cta-pill bg-text-primary px-6 py-3 text-base text-text-inverse hover:bg-text-primary/90 dark:bg-brand-500 dark:text-text-on-brand"
            >
              {t("home:hero.primaryCta")}
              <ArrowRight aria-hidden className="ms-2 size-4 transition group-hover:translate-x-1" />
            </Link>
            <a
              href="#pillars"
              className="cta-pill border border-border-default px-6 py-3 text-base text-text-primary hover:bg-bg-subtle"
            >
              {t("home:hero.secondaryCta")}
            </a>
          </motion.div>
        </div>

        <GradientBackdrop />
      </section>
    );
  }

  function Pillars() {
    const items = [
      { key: "discovery", icon: Search },
      { key: "tracking", icon: ListChecks },
      { key: "consultants", icon: Users },
      { key: "community", icon: MessageSquare },
    ] as const;

    return (
      <section id="pillars" className="bg-bg-subtle py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6">
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {items.map(({ key, icon: Icon }, idx) => (
              <motion.div
                key={key}
                initial={{ opacity: 0, y: 16 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-80px" }}
                transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: idx * 0.06 }}
                className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs"
              >
                <div className="mb-4 flex size-10 items-center justify-center rounded-md bg-brand-50 text-brand-500">
                  <Icon aria-hidden className="size-5" />
                </div>
                <h3 className="mb-2 text-xl font-semibold">{t(`home:pillars.${key}.title`)}</h3>
                <p className="text-sm text-text-secondary">{t(`home:pillars.${key}.body`)}</p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>
    );
  }

  function ReadySection() {
    return (
      <section className="bg-bg-canvas py-20">
        <div className="mx-auto max-w-3xl px-4 text-center sm:px-6">
          <h2 className="text-balance text-3xl sm:text-4xl">{t("home:ready.title")}</h2>
          <p className="mx-auto mt-4 max-w-xl text-text-secondary">{t("home:ready.body")}</p>
          <div className="mt-8">
            <Link
              to="/register"
              className="cta-pill bg-brand-500 px-6 py-3 text-base text-text-on-brand hover:bg-brand-600"
            >
              {t("home:ready.primaryCta")}
            </Link>
          </div>
        </div>
      </section>
    );
  }
}

function GradientBackdrop() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[400px]">
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,rgba(37,99,235,0.12),transparent_60%)]" />
    </div>
  );
}
