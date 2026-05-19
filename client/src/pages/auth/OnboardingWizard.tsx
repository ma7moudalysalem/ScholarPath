import { useState } from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { GraduationCap, Building2, Users, Clock, ArrowLeft } from "lucide-react";
import { motion } from "motion/react";
import { toast } from "sonner";
import {
  authApi,
  applyAuthSession,
  postAuthPath,
  type OnboardingDetails,
} from "@/services/api/auth";
import { useAuthStore } from "@/stores/authStore";

type RoleKey = "Student" | "Company" | "Consultant";

const ROLES: { key: RoleKey; i18n: string; icon: typeof GraduationCap }[] = [
  { key: "Student", i18n: "student", icon: GraduationCap },
  { key: "Company", i18n: "company", icon: Building2 },
  { key: "Consultant", i18n: "consultant", icon: Users },
];

const fieldClass =
  "w-full rounded-lg border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20";

function Labeled({
  label,
  hint,
  children,
}: {
  label: string;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-1 flex items-center gap-2 text-sm font-medium text-text-primary">
        {label}
        {hint && <span className="text-xs font-normal text-text-tertiary">({hint})</span>}
      </label>
      {children}
    </div>
  );
}

export function OnboardingWizard() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);

  const [step, setStep] = useState<"role" | "details">("role");
  const [detailsRole, setDetailsRole] = useState<"Company" | "Consultant">("Company");
  const [submitting, setSubmitting] = useState(false);

  // Company / Consultant onboarding detail fields.
  const [legalName, setLegalName] = useState("");
  const [website, setWebsite] = useState("");
  const [bio, setBio] = useState("");
  const [fee, setFee] = useState("");
  const [expertise, setExpertise] = useState("");

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

  async function submitRole(role: RoleKey, details?: OnboardingDetails) {
    if (submitting) return;
    setSubmitting(true);
    try {
      const session = applyAuthSession(await authApi.selectRole(role, details));
      navigate(postAuthPath(session), { replace: true });
    } catch {
      toast.error(t("auth:errors.generic"));
      setSubmitting(false);
    }
  }

  function pickRole(role: RoleKey) {
    if (submitting) return;
    if (role === "Student") {
      // Students activate instantly — no profile review needed.
      void submitRole("Student");
    } else {
      setDetailsRole(role);
      setStep("details");
    }
  }

  function submitDetails(e: React.FormEvent) {
    e.preventDefault();
    if (detailsRole === "Company") {
      if (!legalName.trim()) {
        toast.error(t("auth:onboarding.details.required"));
        return;
      }
      void submitRole("Company", {
        organizationLegalName: legalName.trim(),
        organizationWebsite: website.trim() || null,
      });
    } else {
      const feeValue = Number(fee);
      if (!bio.trim() || !(feeValue > 0)) {
        toast.error(t("auth:onboarding.details.required"));
        return;
      }
      void submitRole("Consultant", {
        biography: bio.trim(),
        sessionFeeUsd: feeValue,
        expertiseTags: expertise
          .split(",")
          .map((tag) => tag.trim())
          .filter(Boolean),
      });
    }
  }

  // ── Step 2 — Company / Consultant profile details ──────────────────────────
  if (step === "details") {
    const isCompany = detailsRole === "Company";
    return (
      <section className="mx-auto max-w-xl px-4 py-16 sm:px-6">
        <button
          type="button"
          onClick={() => setStep("role")}
          className="mb-6 inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary"
        >
          <ArrowLeft aria-hidden className="size-4 rtl:rotate-180" />
          {t("auth:onboarding.details.back")}
        </button>

        <h1 className="mb-1 text-3xl">
          {t(
            isCompany
              ? "auth:onboarding.details.companyHeading"
              : "auth:onboarding.details.consultantHeading",
          )}
        </h1>
        <p className="mb-8 text-text-secondary">
          {t(
            isCompany
              ? "auth:onboarding.details.companySubtitle"
              : "auth:onboarding.details.consultantSubtitle",
          )}
        </p>

        <form onSubmit={submitDetails} className="space-y-5">
          {isCompany ? (
            <>
              <Labeled label={t("auth:onboarding.details.legalName")}>
                <input
                  type="text"
                  value={legalName}
                  onChange={(e) => setLegalName(e.target.value)}
                  className={`h-11 ${fieldClass}`}
                />
              </Labeled>
              <Labeled
                label={t("auth:onboarding.details.website")}
                hint={t("auth:onboarding.details.optional")}
              >
                <input
                  type="url"
                  value={website}
                  onChange={(e) => setWebsite(e.target.value)}
                  placeholder="https://"
                  className={`h-11 ${fieldClass}`}
                />
              </Labeled>
            </>
          ) : (
            <>
              <Labeled label={t("auth:onboarding.details.bio")}>
                <textarea
                  rows={4}
                  value={bio}
                  onChange={(e) => setBio(e.target.value)}
                  placeholder={t("auth:onboarding.details.bioPlaceholder")}
                  className={`py-2.5 ${fieldClass}`}
                />
              </Labeled>
              <Labeled label={t("auth:onboarding.details.fee")}>
                <input
                  type="number"
                  min={1}
                  value={fee}
                  onChange={(e) => setFee(e.target.value)}
                  className={`h-11 ${fieldClass}`}
                />
              </Labeled>
              <Labeled
                label={t("auth:onboarding.details.expertise")}
                hint={t("auth:onboarding.details.optional")}
              >
                <input
                  type="text"
                  value={expertise}
                  onChange={(e) => setExpertise(e.target.value)}
                  className={`h-11 ${fieldClass}`}
                />
                <p className="mt-1 text-xs text-text-tertiary">
                  {t("auth:onboarding.details.expertiseHint")}
                </p>
              </Labeled>
            </>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="inline-flex h-11 w-full items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600 disabled:opacity-50"
          >
            {submitting
              ? t("auth:onboarding.details.submitting")
              : t("auth:onboarding.details.submit")}
          </button>
        </form>
      </section>
    );
  }

  // ── Step 1 — role selection ────────────────────────────────────────────────
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
              onClick={() => pickRole(key)}
              disabled={submitting}
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
