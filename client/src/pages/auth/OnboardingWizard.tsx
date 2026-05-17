import { useState } from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { GraduationCap, Building2, Users, Clock } from "lucide-react";
import { motion } from "motion/react";
import { toast } from "sonner";
import { authApi, applyAuthSession, postAuthPath } from "@/services/api/auth";
import { useAuthStore } from "@/stores/authStore";

type RoleKey = "Student" | "Company" | "Consultant";

const ROLES: { key: RoleKey; i18n: string; icon: typeof GraduationCap }[] = [
  { key: "Student", i18n: "student", icon: GraduationCap },
  { key: "Company", i18n: "company", icon: Building2 },
  { key: "Consultant", i18n: "consultant", icon: Users },
];

export function OnboardingWizard() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const [submitting, setSubmitting] = useState<RoleKey | null>(null);

  // A Company/Consultant who already chose their role is awaiting admin review.
  if (user?.accountStatus === "PendingApproval") {
    return (
      <section className="mx-auto max-w-xl px-4 py-20 text-center sm:px-6">
        <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full bg-brand-50 text-brand-500">
          <Clock aria-hidden className="size-6" />
        </div>
        <h1 className="mb-3 text-3xl">{t("auth:onboarding.pending.title")}</h1>
        <p className="text-text-secondary">{t("auth:onboarding.pending.body")}</p>
      </section>
    );
  }

  async function select(role: RoleKey) {
    if (submitting !== null) return;
    setSubmitting(role);
    try {
      const session = applyAuthSession(await authApi.selectRole(role));
      navigate(postAuthPath(session), { replace: true });
    } catch {
      toast.error(t("auth:errors.generic"));
      setSubmitting(null);
    }
  }

  return (
    <section className="mx-auto max-w-4xl px-4 py-16 sm:px-6">
      <div className="text-center">
        <h1 className="mb-3 text-4xl">{t("auth:onboarding.title")}</h1>
        <p className="text-text-secondary">{t("auth:onboarding.subtitle")}</p>
      </div>

      <div className="mt-12 grid gap-4 sm:grid-cols-3">
        {ROLES.map(({ key, i18n, icon: Icon }, idx) => (
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
            <h3 className="mb-1 text-xl font-semibold">{t(`auth:onboarding.role.${i18n}.title`)}</h3>
            <p className="mb-6 text-sm text-text-secondary">
              {t(`auth:onboarding.role.${i18n}.body`)}
            </p>
            <button
              type="button"
              onClick={() => select(key)}
              disabled={submitting !== null}
              className="inline-flex items-center text-sm font-medium text-brand-500 transition group-hover:translate-x-0.5 disabled:opacity-50"
            >
              {t(`auth:onboarding.role.${i18n}.cta`)}
            </button>
          </motion.div>
        ))}
      </div>
    </section>
  );
}
