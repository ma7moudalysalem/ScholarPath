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
        <HeroBackdrop />

        <div className="mx-auto max-w-7xl px-4 pt-28 pb-24 text-center sm:px-6 sm:pt-36 sm:pb-32">
          {/* Eyebrow badge */}
          <motion.div
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
            className="mb-7 inline-flex"
          >
            <span className="inline-flex items-center gap-2 rounded-full border border-brand-200 bg-brand-50 px-4 py-1.5 text-sm font-medium text-brand-600">
              <Sparkles aria-hidden className="size-3.5" />
              {t("home:hero.eyebrow")}
            </span>
          </motion.div>

          {/* Headline */}
          <motion.h1
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1], delay: 0.06 }}
            className="mx-auto max-w-4xl text-balance text-5xl leading-[1.06] sm:text-6xl md:text-7xl"
          >
            {t("home:hero.title")}
          </motion.h1>

          {/* Subtitle */}
          <motion.p
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1], delay: 0.14 }}
            className="mx-auto mt-6 max-w-2xl text-balance text-lg leading-relaxed text-text-secondary sm:text-xl"
          >
            {t("home:hero.subtitle")}
          </motion.p>

          {/* CTAs */}
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1], delay: 0.22 }}
            className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row"
          >
            <Link
              to="/register"
              className="cta-pill btn-brand bg-brand-500 px-7 py-3.5 text-base text-white"
            >
              {t("home:hero.primaryCta")}
              <ArrowRight aria-hidden className="ms-2 size-4" />
            </Link>
            <a
              href="#pillars"
              className="cta-pill border border-border-default bg-bg-elevated px-7 py-3.5 text-base text-text-primary hover:border-border-strong hover:bg-bg-subtle"
            >
              {t("home:hero.secondaryCta")}
            </a>
          </motion.div>
        </div>
      </section>
    );
  }

  function Pillars() {
    const items = [
      { key: "discovery",   icon: Search },
      { key: "tracking",    icon: ListChecks },
      { key: "consultants", icon: Users },
      { key: "community",   icon: MessageSquare },
    ] as const;

    return (
      <section id="pillars" className="relative bg-bg-subtle py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6">
          {/* Section label */}
          <motion.p
            initial={{ opacity: 0, y: 8 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
            className="mb-10 text-center text-xs font-semibold uppercase tracking-widest text-text-tertiary"
          >
            {t("home:pillars.sectionLabel", "Everything you need")}
          </motion.p>

          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {items.map(({ key, icon: Icon }, idx) => (
              <motion.div
                key={key}
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-60px" }}
                transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: idx * 0.07 }}
                className="group rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm transition-all duration-200 hover:-translate-y-1 hover:border-brand-200 hover:shadow-md"
              >
                <div className="mb-5 flex size-11 items-center justify-center rounded-xl bg-brand-50 text-brand-500 transition-all duration-200 group-hover:bg-brand-500 group-hover:text-white">
                  <Icon aria-hidden className="size-5" />
                </div>
                <h3 className="mb-2 text-lg font-semibold tracking-tight">
                  {t(`home:pillars.${key}.title`)}
                </h3>
                <p className="text-sm leading-relaxed text-text-secondary">
                  {t(`home:pillars.${key}.body`)}
                </p>
              </motion.div>
            ))}
          </div>
        </div>
      </section>
    );
  }

  function ReadySection() {
    return (
      <section className="relative overflow-hidden bg-bg-canvas py-28">
        <ReadyBackdrop />

        <div className="relative mx-auto max-w-3xl px-4 text-center sm:px-6">
          <motion.h2
            initial={{ opacity: 0, y: 12 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
            className="text-balance text-4xl sm:text-5xl"
          >
            {t("home:ready.title")}
          </motion.h2>

          <motion.p
            initial={{ opacity: 0, y: 12 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.08 }}
            className="mx-auto mt-5 max-w-xl text-lg text-text-secondary"
          >
            {t("home:ready.body")}
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 12 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.16 }}
            className="mt-8"
          >
            <Link
              to="/register"
              className="cta-pill btn-brand bg-brand-500 px-8 py-4 text-base text-white"
            >
              {t("home:ready.primaryCta")}
              <ArrowRight aria-hidden className="ms-2 size-4" />
            </Link>
          </motion.div>
        </div>
      </section>
    );
  }
}

function HeroBackdrop() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
      {/* Primary radial glow from top */}
      <div className="absolute inset-x-0 top-0 h-[640px] bg-[radial-gradient(ellipse_80%_55%_at_50%_-5%,rgba(23,96,240,0.13),transparent)]" />
      {/* Secondary warm glow bottom-left */}
      <div className="absolute -bottom-24 -start-24 h-96 w-96 rounded-full bg-brand-500/5 blur-3xl" />
      {/* Accent right */}
      <div className="absolute -top-8 -end-16 h-80 w-80 rounded-full bg-brand-300/8 blur-3xl" />
      {/* Subtle dot grid */}
      <div
        className="absolute inset-0 opacity-[0.22]"
        style={{
          backgroundImage:
            "radial-gradient(circle, rgb(11 15 30 / 0.14) 1px, transparent 1px)",
          backgroundSize: "28px 28px",
        }}
      />
    </div>
  );
}

function ReadyBackdrop() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 -z-10">
      <div className="absolute inset-x-0 bottom-0 h-[480px] bg-[radial-gradient(ellipse_70%_60%_at_50%_110%,rgba(23,96,240,0.1),transparent)]" />
    </div>
  );
}
