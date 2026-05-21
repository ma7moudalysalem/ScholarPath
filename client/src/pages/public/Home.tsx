import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import {
  ArrowRight,
  Sparkles,
  Search,
  ListChecks,
  Users,
  MessageSquare,
  BrainCircuit,
  ShieldCheck,
  Quote,
} from "lucide-react";
import { motion } from "motion/react";

export function Home() {
  const { t } = useTranslation(["home", "common"]);

  return (
    <div>
      <HomeHero />
      <StatsSection />
      <Pillars />
      <FeatureShowcase />
      <Testimonials />
      <ReadySection />
    </div>
  );

  // ─── Hero ─────────────────────────────────────────────────────────

  function HomeHero() {
    return (
      <section className="relative overflow-hidden bg-bg-canvas">
        <HeroBackdrop />

        <div className="mx-auto max-w-7xl px-4 pt-28 pb-24 text-center sm:px-6 sm:pt-36 sm:pb-32">
          {/* Eyebrow badge — premium shimmer with live dot */}
          <motion.div
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
            className="mb-7 inline-flex"
          >
            <span className="inline-flex items-center gap-2 rounded-full border border-brand-200/60 bg-gradient-to-r from-brand-50 to-brand-100/50 px-4 py-1.5 text-sm font-medium text-brand-700 shadow-xs dark:border-brand-500/30 dark:from-brand-500/10 dark:to-brand-500/5 dark:text-brand-400">
              <span className="relative inline-flex">
                <span className="size-1.5 rounded-full bg-success-500" />
                <span
                  aria-hidden
                  className="absolute inset-0 inline-flex size-1.5 animate-ping rounded-full bg-success-500 opacity-75"
                />
              </span>
              <Sparkles aria-hidden className="size-3.5" />
              {t("home:hero.eyebrow")}
            </span>
          </motion.div>

          {/* Headline */}
          <motion.h1
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1], delay: 0.06 }}
            className="mx-auto max-w-4xl text-balance text-5xl tracking-[-0.04em] leading-[1.04] sm:text-6xl md:text-7xl md:tracking-[-0.05em] lg:text-[5.5rem] lg:leading-[1.02]"
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
              <ArrowRight
                aria-hidden
                className="ms-2 size-4 rtl:rotate-180"
              />
            </Link>
            <a
              href="#pillars"
              className="cta-pill border border-border-default bg-bg-elevated px-7 py-3.5 text-base text-text-primary hover:border-border-strong hover:bg-bg-subtle"
            >
              {t("home:hero.secondaryCta")}
            </a>
          </motion.div>

          {/* Trust strip */}
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1], delay: 0.34 }}
            className="mt-20"
          >
            <p className="text-xs font-medium uppercase tracking-wider text-text-tertiary">
              {t("home:hero.trustedBy")}
            </p>
            <div className="mt-6 grid grid-cols-2 items-center justify-items-center gap-x-6 gap-y-4 opacity-70 sm:grid-cols-3 md:grid-cols-5">
              {Array.from({ length: 5 }).map((_, idx) => (
                <div
                  key={idx}
                  aria-hidden
                  className="h-7 w-28 rounded-md bg-gradient-to-r from-border-subtle via-border-default to-border-subtle dark:from-border-default dark:via-border-subtle dark:to-border-default"
                />
              ))}
            </div>
          </motion.div>
        </div>
      </section>
    );
  }

  // ─── Stats counter ────────────────────────────────────────────────

  function StatsSection() {
    const stats = [
      "students",
      "scholarships",
      "universities",
      "funding",
    ] as const;

    return (
      <section className="relative border-y border-border-subtle bg-bg-elevated py-20">
        <div className="mx-auto max-w-7xl px-4 sm:px-6">
          <motion.p
            initial={{ opacity: 0, y: 8 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
            className="mb-12 text-center text-xs font-semibold uppercase tracking-widest text-text-tertiary"
          >
            {t("home:stats.sectionLabel")}
          </motion.p>

          <div className="grid grid-cols-2 gap-x-8 gap-y-12 md:grid-cols-4">
            {stats.map((key, idx) => (
              <motion.div
                key={key}
                initial={{ opacity: 0, y: 16 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-60px" }}
                transition={{
                  duration: 0.4,
                  ease: [0.22, 1, 0.36, 1],
                  delay: idx * 0.08,
                }}
                className="text-center"
              >
                <div className="font-display text-5xl font-bold tracking-tight text-gradient sm:text-6xl">
                  {t(`home:stats.${key}.value`)}
                </div>
                <div className="mt-3 text-sm font-medium text-text-secondary">
                  {t(`home:stats.${key}.label`)}
                </div>
              </motion.div>
            ))}
          </div>
        </div>
      </section>
    );
  }

  // ─── Pillars (existing — preserved) ───────────────────────────────

  function Pillars() {
    const items = [
      { key: "discovery", icon: Search },
      { key: "tracking", icon: ListChecks },
      { key: "consultants", icon: Users },
      { key: "community", icon: MessageSquare },
    ] as const;

    return (
      <section id="pillars" className="relative bg-bg-subtle py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6">
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
                transition={{
                  duration: 0.4,
                  ease: [0.22, 1, 0.36, 1],
                  delay: idx * 0.07,
                }}
                className="group rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm transition-all duration-200 hover:-translate-y-1 hover:border-brand-200 hover:shadow-md"
              >
                <div className="mb-5 flex size-11 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-50 to-brand-100 text-brand-600 shadow-xs transition-all duration-200 group-hover:from-brand-500 group-hover:to-brand-700 group-hover:text-white group-hover:shadow-brand">
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

  // ─── Feature showcase ─────────────────────────────────────────────

  function FeatureShowcase() {
    const cards = [
      {
        key: "ai",
        icon: BrainCircuit,
        gradient:
          "from-brand-500 to-brand-700",
      },
      {
        key: "consultants",
        icon: Users,
        gradient:
          "from-brand-400 to-brand-600",
      },
      {
        key: "verified",
        icon: ShieldCheck,
        gradient:
          "from-brand-600 to-brand-800",
      },
    ] as const;

    return (
      <section className="relative overflow-hidden bg-bg-canvas py-28">
        {/* Soft mesh backdrop */}
        <div
          aria-hidden
          className="pointer-events-none absolute -start-32 top-20 size-[420px] rounded-full bg-brand-500/5 blur-[120px]"
        />
        <div
          aria-hidden
          className="pointer-events-none absolute -end-32 bottom-10 size-[420px] rounded-full bg-brand-700/5 blur-[120px]"
        />

        <div className="relative mx-auto max-w-7xl px-4 sm:px-6">
          <div className="mx-auto max-w-3xl text-center">
            <motion.p
              initial={{ opacity: 0, y: 8 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
              className="mb-4 text-xs font-semibold uppercase tracking-widest text-brand-600 dark:text-brand-400"
            >
              {t("home:showcase.sectionLabel")}
            </motion.p>
            <motion.h2
              initial={{ opacity: 0, y: 12 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.08 }}
              className="text-balance text-4xl font-bold tracking-tight sm:text-5xl"
            >
              {t("home:showcase.heading")}
            </motion.h2>
            <motion.p
              initial={{ opacity: 0, y: 12 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.14 }}
              className="mt-5 text-lg leading-relaxed text-text-secondary"
            >
              {t("home:showcase.subtitle")}
            </motion.p>
          </div>

          <div className="mt-16 grid gap-6 md:grid-cols-3">
            {cards.map(({ key, icon: Icon, gradient }, idx) => (
              <motion.article
                key={key}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-60px" }}
                transition={{
                  duration: 0.45,
                  ease: [0.22, 1, 0.36, 1],
                  delay: idx * 0.09,
                }}
                className="group relative overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated p-8 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-border-default hover:shadow-xl"
              >
                {/* Hover glow */}
                <div
                  aria-hidden
                  className="pointer-events-none absolute inset-x-0 -top-px h-px bg-gradient-to-r from-transparent via-brand-500/40 to-transparent opacity-0 transition-opacity duration-300 group-hover:opacity-100"
                />

                <div
                  className={`mb-6 inline-flex size-14 items-center justify-center rounded-2xl bg-gradient-to-br ${gradient} text-white shadow-brand`}
                >
                  <Icon aria-hidden className="size-7" />
                </div>
                <h3 className="text-xl font-semibold tracking-tight">
                  {t(`home:showcase.${key}.title`)}
                </h3>
                <p className="mt-3 text-sm leading-relaxed text-text-secondary">
                  {t(`home:showcase.${key}.body`)}
                </p>
                <div className="mt-6 inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 transition-all duration-200 group-hover:gap-2.5 dark:text-brand-400">
                  {t(`home:showcase.${key}.cta`)}
                  <ArrowRight
                    aria-hidden
                    className="size-4 rtl:rotate-180"
                  />
                </div>
              </motion.article>
            ))}
          </div>
        </div>
      </section>
    );
  }

  // ─── Testimonials ─────────────────────────────────────────────────

  function Testimonials() {
    const items = ["first", "second", "third"] as const;

    const avatarGradients = [
      "from-brand-400 to-brand-700",
      "from-brand-500 to-brand-800",
      "from-brand-300 to-brand-600",
    ];

    return (
      <section className="relative overflow-hidden bg-bg-subtle py-28">
        <div className="mx-auto max-w-7xl px-4 sm:px-6">
          <div className="mx-auto max-w-3xl text-center">
            <motion.p
              initial={{ opacity: 0, y: 8 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
              className="mb-4 text-xs font-semibold uppercase tracking-widest text-brand-600 dark:text-brand-400"
            >
              {t("home:testimonials.sectionLabel")}
            </motion.p>
            <motion.h2
              initial={{ opacity: 0, y: 12 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.08 }}
              className="text-balance text-4xl font-bold tracking-tight sm:text-5xl"
            >
              {t("home:testimonials.heading")}
            </motion.h2>
            <motion.p
              initial={{ opacity: 0, y: 12 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.14 }}
              className="mt-5 text-lg leading-relaxed text-text-secondary"
            >
              {t("home:testimonials.subtitle")}
            </motion.p>
          </div>

          <div className="mt-16 grid gap-6 md:grid-cols-3">
            {items.map((key, idx) => (
              <motion.figure
                key={key}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-60px" }}
                transition={{
                  duration: 0.45,
                  ease: [0.22, 1, 0.36, 1],
                  delay: idx * 0.09,
                }}
                className="relative overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated p-8 shadow-sm transition-all duration-200 hover:-translate-y-1 hover:shadow-md"
              >
                {/* Decorative quote mark */}
                <Quote
                  aria-hidden
                  className="pointer-events-none absolute -end-2 -top-2 size-24 text-brand-100 opacity-70 dark:text-brand-500/10"
                />

                <blockquote className="relative text-base leading-relaxed text-text-primary">
                  &ldquo;{t(`home:testimonials.items.${key}.quote`)}&rdquo;
                </blockquote>

                <figcaption className="relative mt-6 flex items-center gap-3">
                  <span
                    aria-hidden
                    className={`flex size-11 items-center justify-center rounded-full bg-gradient-to-br ${avatarGradients[idx]} text-sm font-semibold text-white shadow-sm`}
                  >
                    {t(`home:testimonials.items.${key}.name`).slice(0, 1)}
                  </span>
                  <div>
                    <div className="text-sm font-semibold text-text-primary">
                      {t(`home:testimonials.items.${key}.name`)}
                    </div>
                    <div className="text-xs text-text-tertiary">
                      {t(`home:testimonials.items.${key}.role`)}
                    </div>
                  </div>
                </figcaption>
              </motion.figure>
            ))}
          </div>
        </div>
      </section>
    );
  }

  // ─── Ready / CTA banner ───────────────────────────────────────────

  function ReadySection() {
    return (
      <section className="relative overflow-hidden bg-bg-canvas py-28">
        <div className="mx-auto max-w-5xl px-4 sm:px-6">
          <motion.div
            initial={{ opacity: 0, y: 16 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ duration: 0.45, ease: [0.22, 1, 0.36, 1] }}
            className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-600 to-brand-800 p-12 text-center text-white shadow-xl sm:p-16"
          >
            {/* Animated decorative orbs */}
            <div
              aria-hidden
              className="pointer-events-none absolute -start-16 -top-16 size-72 rounded-full bg-white/10 blur-3xl"
              style={{ animation: "ready-orb 16s ease-in-out infinite" }}
            />
            <div
              aria-hidden
              className="pointer-events-none absolute -bottom-20 -end-20 size-80 rounded-full bg-brand-400/30 blur-3xl"
              style={{ animation: "ready-orb 20s ease-in-out infinite reverse" }}
            />

            {/* Dot pattern */}
            <div
              aria-hidden
              className="pointer-events-none absolute inset-0 opacity-20"
              style={{
                backgroundImage:
                  "radial-gradient(circle, rgba(255,255,255,0.2) 1px, transparent 1px)",
                backgroundSize: "26px 26px",
              }}
            />

            <style>{`
              @keyframes ready-orb {
                0%, 100% { transform: translate(0, 0) scale(1); }
                50% { transform: translate(24px, -20px) scale(1.05); }
              }
            `}</style>

            <div className="relative">
              <motion.h2
                initial={{ opacity: 0, y: 12 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
                className="text-balance text-4xl font-bold tracking-tight sm:text-5xl"
              >
                {t("home:ready.title")}
              </motion.h2>

              <motion.p
                initial={{ opacity: 0, y: 12 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.08 }}
                className="mx-auto mt-5 max-w-xl text-lg text-white/80"
              >
                {t("home:ready.body")}
              </motion.p>

              <motion.div
                initial={{ opacity: 0, y: 12 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.16 }}
                className="mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row"
              >
                <Link
                  to="/register"
                  className="inline-flex items-center rounded-xl bg-white px-8 py-4 text-base font-semibold text-brand-700 shadow-sm transition-all duration-150 hover:bg-brand-50 hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/50"
                >
                  {t("home:ready.primaryCta")}
                  <ArrowRight
                    aria-hidden
                    className="ms-2 size-4 rtl:rotate-180"
                  />
                </Link>
                <Link
                  to="/login"
                  className="inline-flex items-center rounded-xl border border-white/30 bg-white/10 px-8 py-4 text-base font-semibold text-white backdrop-blur-md transition-all duration-150 hover:border-white/50 hover:bg-white/15 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/50"
                >
                  {t("home:ready.secondaryCta")}
                </Link>
              </motion.div>
            </div>
          </motion.div>
        </div>
      </section>
    );
  }
}

function HeroBackdrop() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
      {/* Animated mesh gradient — top-left */}
      <div
        className="absolute -start-32 -top-32 size-[640px] rounded-full bg-brand-500/12 blur-[140px]"
        style={{ animation: "hero-mesh 22s ease-in-out infinite" }}
      />
      {/* Animated mesh gradient — bottom-right */}
      <div
        className="absolute -bottom-40 -end-32 size-[560px] rounded-full bg-brand-700/10 blur-[140px]"
        style={{ animation: "hero-mesh 26s ease-in-out infinite reverse" }}
      />
      {/* Soft accent */}
      <div className="absolute end-1/3 top-10 size-72 rounded-full bg-brand-300/8 blur-[100px]" />

      <style>{`
        @keyframes hero-mesh {
          0%, 100% { transform: translate(0, 0) scale(1); }
          33% { transform: translate(40px, -30px) scale(1.08); }
          66% { transform: translate(-30px, 40px) scale(0.95); }
        }
      `}</style>

      {/* Subtle dot grid */}
      <div
        className="absolute inset-0 opacity-[0.22] dark:opacity-[0.12]"
        style={{
          backgroundImage:
            "radial-gradient(circle, rgb(11 15 30 / 0.14) 1px, transparent 1px)",
          backgroundSize: "28px 28px",
        }}
      />
    </div>
  );
}
