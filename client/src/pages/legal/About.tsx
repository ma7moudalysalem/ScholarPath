import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { motion } from "motion/react";
import { Target, Globe2, Users, Sparkles, GraduationCap, Lock } from "lucide-react";
import { LegalLayout } from "./LegalLayout";

/**
 * Public About page. Tells the story behind ScholarPath, surfaces the values
 * the platform is built on, and pulls in the same headline stats shown on the
 * marketing home so the brand story stays consistent across surfaces.
 */
export function About() {
  const { t } = useTranslation(["legal", "home", "common"]);

  const values = [
    { icon: Target,   key: "mission" },
    { icon: Lock,     key: "trust" },
    { icon: Globe2,   key: "bilingual" },
    { icon: Users,    key: "community" },
    { icon: Sparkles, key: "intelligence" },
    { icon: GraduationCap, key: "outcomes" },
  ] as const;

  return (
    <LegalLayout
      eyebrow={t("legal:eyebrow.about")}
      title={t("legal:about.title")}
      subtitle={t("legal:about.subtitle")}
    >
      <p>{t("legal:about.story.body1")}</p>
      <p>{t("legal:about.story.body2")}</p>

      <h2>{t("legal:about.stats.heading")}</h2>
      <div className="not-prose mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {([
          { value: t("home:stats.students.value"),     label: t("home:stats.students.label") },
          { value: t("home:stats.scholarships.value"), label: t("home:stats.scholarships.label") },
          { value: t("home:stats.universities.value"), label: t("home:stats.universities.label") },
          { value: t("home:stats.funding.value"),      label: t("home:stats.funding.label") },
        ] as const).map((s, idx) => (
          <motion.div
            key={s.label}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.3, delay: idx * 0.05 }}
            className="rounded-2xl border border-border-subtle bg-bg-elevated p-5"
          >
            <div className="text-3xl font-bold tracking-tight text-text-primary">
              {s.value}
            </div>
            <div className="mt-1 text-sm text-text-tertiary">{s.label}</div>
          </motion.div>
        ))}
      </div>

      <h2>{t("legal:about.values.heading")}</h2>
      <div className="not-prose mt-6 grid gap-4 sm:grid-cols-2">
        {values.map(({ icon: Icon, key }) => (
          <div
            key={key}
            className="rounded-2xl border border-border-subtle bg-bg-elevated p-5"
          >
            <div className="inline-flex size-10 items-center justify-center rounded-lg bg-brand-50 text-brand-600 dark:bg-brand-500/10">
              <Icon aria-hidden className="size-5" />
            </div>
            <h3 className="mt-3 text-base font-semibold text-text-primary">
              {t(`legal:about.values.${key}.title`)}
            </h3>
            <p className="mt-1 text-sm leading-relaxed text-text-secondary">
              {t(`legal:about.values.${key}.body`)}
            </p>
          </div>
        ))}
      </div>

      <h2>{t("legal:about.team.heading")}</h2>
      <p>{t("legal:about.team.body")}</p>

      <h2>{t("legal:about.cta.heading")}</h2>
      <p>{t("legal:about.cta.body")}</p>
      <p className="not-prose mt-6 flex flex-wrap gap-3">
        <Link to="/register" className="cta-pill btn-brand bg-brand-500 text-white">
          {t("common:cta.getStarted")}
        </Link>
        <Link to="/legal/contact" className="cta-pill bg-bg-elevated border border-border-subtle text-text-primary hover:bg-bg-subtle">
          {t("legal:about.cta.contact")}
        </Link>
      </p>
    </LegalLayout>
  );
}
