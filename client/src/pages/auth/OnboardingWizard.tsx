import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { GraduationCap, Building2, Users } from "lucide-react";
import { motion } from "motion/react";
import { EmptyState } from "@/components/common/EmptyState";

export function OnboardingWizard() {
  const { t } = useTranslation(["auth", "common"]);

  const roles = [
    {
      key: "student",
      icon: GraduationCap,
      to: "/student",
      accent: "brand-500",
    },
    {
      key: "company",
      icon: Building2,
      to: "/company",
      accent: "warning-500",
    },
    {
      key: "consultant",
      icon: Users,
      to: "/consultant",
      accent: "success-500",
    },
  ] as const;

  return (
    <section className="mx-auto max-w-4xl px-4 py-16 sm:px-6">
      <div className="text-center">
        <h1 className="mb-3 text-4xl">{t("auth:onboarding.title")}</h1>
        <p className="text-text-secondary">{t("auth:onboarding.subtitle")}</p>
      </div>

      <div className="mt-12 grid gap-4 sm:grid-cols-3">
        {roles.map(({ key, icon: Icon, to }, idx) => (
          <motion.div
            key={key}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.32, delay: idx * 0.06 }}
            className="group relative rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs transition hover:border-brand-500 hover:shadow-md"
          >
            <div className="mb-4 flex size-10 items-center justify-center rounded-md bg-brand-50 text-brand-500">
              <Icon aria-hidden className="size-5" />
            </div>
            <h3 className="mb-1 text-xl font-semibold">{t(`auth:onboarding.role.${key}.title`)}</h3>
            <p className="mb-6 text-sm text-text-secondary">
              {t(`auth:onboarding.role.${key}.body`)}
            </p>
            <Link
              to={to}
              className="inline-flex items-center text-sm font-medium text-brand-500 transition group-hover:translate-x-0.5"
            >
              {t(`auth:onboarding.role.${key}.cta`)}
            </Link>
          </motion.div>
        ))}
      </div>

      <div className="mt-12">
        <EmptyState
          owner="@Madiha6776"
          module="PB-001 Onboarding — branch logic per role"
          specPath=".specify/specs/PB-001-auth-access-onboarding/tasks.md"
        />
      </div>
    </section>
  );
}
