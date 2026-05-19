import { useState } from "react";
import { useNavigate } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { GraduationCap, Building2, Users, Clock, ArrowLeft, Upload, FileText, Trash2 } from "lucide-react";
import { motion } from "motion/react";
import { toast } from "sonner";
import {
  authApi,
  applyAuthSession,
  postAuthPath,
  type OnboardingDetails,
} from "@/services/api/auth";
import { apiErrorMessage } from "@/services/api/client";
import { documentsApi } from "@/services/api/documents";
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

/**
 * Awaiting-review screen for a Company / Consultant. Beyond the status message
 * it lets the applicant upload supporting verification documents (registration
 * certificate, credentials) for the admin reviewer — UAT TC-001/002.
 */
function PendingReview() {
  const { t } = useTranslation(["auth", "common"]);
  const qc = useQueryClient();

  const { data: docs = [], isLoading } = useQuery({
    queryKey: ["onboarding-documents"],
    queryFn: () => documentsApi.list("OnboardingDocument"),
  });

  const uploadMut = useMutation({
    mutationFn: (file: File) =>
      documentsApi.upload({ file, category: "OnboardingDocument" }),
    onSuccess: () => {
      toast.success(t("auth:onboarding.documents.uploaded"));
      void qc.invalidateQueries({ queryKey: ["onboarding-documents"] });
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, t("auth:onboarding.documents.uploadError"))),
  });

  const removeMut = useMutation({
    mutationFn: (id: string) => documentsApi.remove(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["onboarding-documents"] }),
    onError: () => toast.error(t("common:status.error")),
  });

  function onFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = ""; // let the same file be re-picked after an error
    if (!file) return;
    if (file.size > 25 * 1024 * 1024) {
      toast.error(t("auth:onboarding.documents.tooLarge"));
      return;
    }
    uploadMut.mutate(file);
  }

  return (
    <section className="mx-auto max-w-xl px-4 py-20 sm:px-6">
      <div className="text-center">
        <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full bg-brand-50 text-brand-500">
          <Clock aria-hidden className="size-6" />
        </div>
        <h1 className="mb-3 text-3xl">{t("auth:onboarding.pending.title")}</h1>
        <p className="text-text-secondary">{t("auth:onboarding.pending.body")}</p>
      </div>

      <div className="mt-10 rounded-xl border border-border-subtle bg-bg-elevated p-6 text-start">
        <h2 className="text-lg font-semibold text-text-primary">
          {t("auth:onboarding.documents.title")}
        </h2>
        <p className="mt-1 text-sm text-text-secondary">
          {t("auth:onboarding.documents.subtitle")}
        </p>

        <label className="mt-4 inline-flex h-10 cursor-pointer items-center gap-2 rounded-lg border border-border-subtle px-4 text-sm font-medium text-text-primary transition hover:border-brand-500 hover:text-brand-500">
          <Upload aria-hidden className="size-4" />
          {uploadMut.isPending
            ? t("auth:onboarding.documents.uploading")
            : t("auth:onboarding.documents.choose")}
          <input
            type="file"
            accept=".pdf,.doc,.docx,.jpg,.jpeg,.png,.webp"
            onChange={onFileChange}
            disabled={uploadMut.isPending}
            className="hidden"
          />
        </label>
        <p className="mt-2 text-xs text-text-tertiary">
          {t("auth:onboarding.documents.hint")}
        </p>

        <ul className="mt-4 space-y-2">
          {isLoading && (
            <li className="text-sm text-text-tertiary">{t("common:status.loading")}</li>
          )}
          {!isLoading && docs.length === 0 && (
            <li className="text-sm text-text-tertiary">
              {t("auth:onboarding.documents.empty")}
            </li>
          )}
          {docs.map((d) => (
            <li
              key={d.id}
              className="flex items-center justify-between gap-3 rounded-lg border border-border-subtle px-3 py-2"
            >
              <span className="flex min-w-0 items-center gap-2 text-sm text-text-primary">
                <FileText aria-hidden className="size-4 shrink-0 text-text-tertiary" />
                <span className="truncate">{d.fileName}</span>
              </span>
              <button
                type="button"
                onClick={() => removeMut.mutate(d.id)}
                disabled={removeMut.isPending}
                aria-label={t("auth:onboarding.documents.remove")}
                className="shrink-0 text-text-tertiary transition hover:text-danger-500 disabled:opacity-50"
              >
                <Trash2 aria-hidden className="size-4" />
              </button>
            </li>
          ))}
        </ul>
      </div>
    </section>
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
    return <PendingReview />;
  }

  async function submitRole(role: RoleKey, details?: OnboardingDetails) {
    if (submitting) return;
    setSubmitting(true);
    try {
      const session = applyAuthSession(await authApi.selectRole(role, details));
      // A new student lands on profile completion — they must fill their
      // academic details before they can apply for scholarships (UAT TC-003).
      navigate(role === "Student" ? "/profile" : postAuthPath(session), {
        replace: true,
      });
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
