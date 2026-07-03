import { useMemo, useState } from "react";
import { useNavigate } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  GraduationCap,
  Building2,
  Users,
  Clock,
  ArrowLeft,
  Upload,
  FileText,
  Trash2,
  AlertTriangle,
} from "lucide-react";
import { motion } from "motion/react";
import { toast } from "sonner";
import {
  authApi,
  applyAuthSession,
  postAuthPath,
  type OnboardingDetails,
} from "@/services/api/auth";
import { apiErrorMessage } from "@/services/api/client";
import {
  documentsApi,
  requiredOnboardingDocTypes,
  optionalOnboardingDocTypes,
  type OnboardingDocumentType,
} from "@/services/api/documents";
import { useAuthStore } from "@/stores/authStore";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";

type RoleKey = "Student" | "ScholarshipProvider" | "Consultant";

const ROLES: { key: RoleKey; i18n: string; icon: typeof GraduationCap }[] = [
  { key: "Student", i18n: "student", icon: GraduationCap },
  { key: "ScholarshipProvider", i18n: "company", icon: Building2 },
  { key: "Consultant", i18n: "consultant", icon: Users },
];

const COMPANY_TYPES = ["University", "NGO", "Corporation", "Foundation", "Government", "Other"] as const;
// AUTH-CODE-04 — canonical session-duration list shared with the Profile module.
// Backend mirror: SelectRoleCommandValidator.AllowedSessionDurations.
const SESSION_DURATIONS = [30, 45, 60, 90, 120] as const;

/** 2-step progress indicator used on both onboarding screens. Module-scope so
 * each render reuses the same component identity (react-hooks/static-components). */
function ProgressBar({ current, detailsLabel }: { current: 1 | 2; detailsLabel: string }) {
  return (
    <div className="mx-auto mb-10 flex max-w-md items-center gap-2 px-4">
      <div className="flex flex-1 items-center gap-2">
        <div className="flex size-7 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-brand-500 to-brand-700 text-white text-xs font-bold shadow-brand-sm">
          1
        </div>
        <span className="text-xs font-semibold text-text-primary">Role</span>
      </div>
      <div className={`h-0.5 flex-1 rounded-full ${current >= 2 ? "bg-gradient-to-r from-brand-500 to-brand-700" : "bg-border-default"}`} />
      <div className="flex flex-1 items-center gap-2 justify-end">
        <span className={`text-xs font-semibold ${current >= 2 ? "text-text-primary" : "text-text-tertiary"}`}>
          {detailsLabel}
        </span>
        <div className={`flex size-7 shrink-0 items-center justify-center rounded-full text-xs font-bold transition-all ${
          current >= 2
            ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm"
            : "bg-bg-subtle text-text-tertiary border border-border-default"
        }`}>
          2
        </div>
      </div>
    </div>
  );
}

const fieldClass =
  "w-full rounded-lg border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20";

function Labeled({
  label,
  hint,
  required,
  error,
  children,
}: {
  label: string;
  hint?: string;
  required?: boolean;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-1 flex items-center gap-2 text-sm font-medium text-text-primary">
        {label}
        {required && <span className="text-danger-500" aria-hidden>*</span>}
        {hint && <span className="text-xs font-normal text-text-tertiary">({hint})</span>}
      </label>
      {children}
      {error && <p className="mt-1 text-xs text-danger-500">{error}</p>}
    </div>
  );
}

/**
 * Awaiting-review screen for a ScholarshipProvider / Consultant. Beyond the status message
 * it lets the applicant upload supporting verification documents (registration
 * certificate, credentials) for the admin reviewer — UAT TC-001/002.
 */
function PendingReview() {
  const { t } = useTranslation(["auth", "common"]);
  const qc = useQueryClient();
  // FR-ONB-12 — the applicant tags each verification upload with its type so the
  // admin can review by type (FR-ONB-14). Options depend on the role under review.
  const activeRole = useAuthStore((s) => s.user?.activeRole);
  const roleKey: "ScholarshipProvider" | "Consultant" =
    activeRole === "Consultant" ? "Consultant" : "ScholarshipProvider";
  const docTypeOptions: OnboardingDocumentType[] = [
    ...requiredOnboardingDocTypes[roleKey],
    ...optionalOnboardingDocTypes[roleKey],
  ];
  const [docType, setDocType] = useState<OnboardingDocumentType | "">("");

  const { data: docs = [], isLoading } = useQuery({
    queryKey: ["onboarding-documents"],
    queryFn: () => documentsApi.list("OnboardingDocument"),
  });

  const uploadMut = useMutation({
    mutationFn: ({ file, type }: { file: File; type: OnboardingDocumentType }) =>
      documentsApi.upload({ file, category: "OnboardingDocument", onboardingType: type }),
    onSuccess: () => {
      toast.success(t("auth:onboarding.documents.uploaded"));
      setDocType("");
      void qc.invalidateQueries({ queryKey: ["onboarding-documents"] });
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, t("auth:onboarding.documents.uploadError"))),
  });

  const removeMut = useMutation({
    mutationFn: (id: string) => documentsApi.remove(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ["onboarding-documents"] }),
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  function onFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = ""; // let the same file be re-picked after an error
    if (!file) return;
    if (!docType) {
      toast.error(t("auth:onboarding.documents.selectTypeFirst"));
      return;
    }
    if (file.size > 25 * 1024 * 1024) {
      toast.error(t("auth:onboarding.documents.tooLarge"));
      return;
    }
    uploadMut.mutate({ file, type: docType });
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

        {/* FR-ONB-12 — pick the document type before choosing the file. */}
        <select
          value={docType}
          onChange={(e) => setDocType(e.target.value as OnboardingDocumentType)}
          className="mt-4 block h-10 w-full max-w-sm rounded-lg border border-border-subtle bg-bg-elevated px-3 text-sm text-text-primary"
        >
          <option value="">{t("auth:onboarding.documents.selectType")}</option>
          {docTypeOptions.map((tp) => (
            <option key={tp} value={tp}>
              {t(`auth:onboarding.docTypes.${tp}`, tp)}
              {requiredOnboardingDocTypes[roleKey].includes(tp) ? " *" : ""}
            </option>
          ))}
        </select>

        <label className={`mt-3 inline-flex h-10 items-center gap-2 rounded-lg border border-border-subtle px-4 text-sm font-medium text-text-primary transition ${docType ? "cursor-pointer hover:border-brand-500 hover:text-brand-500" : "cursor-not-allowed opacity-50"}`}>
          <Upload aria-hidden className="size-4" />
          {uploadMut.isPending
            ? t("auth:onboarding.documents.uploading")
            : t("auth:onboarding.documents.choose")}
          <input
            type="file"
            accept=".pdf,.doc,.docx,.jpg,.jpeg,.png,.webp"
            onChange={onFileChange}
            disabled={uploadMut.isPending || !docType}
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

// ── ScholarshipProvider onboarding form ────────────────────────────────────────────────
// AUTH-CODE-03 — applicability is a tri-state on the wire: null = "not asked",
// true = "yes, registered", false = "no, with a reason". The form uses
// "" | "yes" | "no" for the radio selection and maps to bool/null on submit.
type Applicability = "" | "yes" | "no";

interface ScholarshipProviderFormState {
  legalName: string;
  website: string;
  email: string;
  country: string;
  scholarshipProviderType: string;
  description: string;
  registrationNumber: string;
  taxNumber: string;
  contactName: string;
  contactPosition: string;
  contactPhone: string;
  // AUTH-CODE-03 — conditional applicability.
  isLegallyRegistered: Applicability;
  legalNotApplicableReason: string;
  isTaxRegistered: Applicability;
  taxNotApplicableReason: string;
}

function emptyScholarshipProvider(): ScholarshipProviderFormState {
  return {
    legalName: "",
    website: "",
    email: "",
    country: "",
    scholarshipProviderType: "",
    description: "",
    registrationNumber: "",
    taxNumber: "",
    contactName: "",
    contactPosition: "",
    contactPhone: "",
    isLegallyRegistered: "",
    legalNotApplicableReason: "",
    isTaxRegistered: "",
    taxNotApplicableReason: "",
  };
}

function validateScholarshipProvider(c: ScholarshipProviderFormState): Record<string, string> {
  const errs: Record<string, string> = {};
  if (!c.legalName.trim()) errs.legalName = "Required";
  else if (c.legalName.length > 200) errs.legalName = "Max 200 characters";
  // AUTH-CODE-04 — website must be a valid absolute URL (http/https).
  if (!c.website.trim()) errs.website = "Required";
  else if (!/^https?:\/\/.+/i.test(c.website)) errs.website = "Must be a valid absolute URL (http:// or https://)";
  if (!c.email.trim()) errs.email = "Required";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(c.email)) errs.email = "Must be a valid email";
  if (!c.country.trim()) errs.country = "Required";
  if (!c.scholarshipProviderType) errs.scholarshipProviderType = "Required";
  if (!c.description.trim()) errs.description = "Required";
  // AUTH-CODE-04 — description max is 2000 (was 1000).
  else if (c.description.length > 2000) errs.description = "Max 2000 characters";
  if (!c.contactName.trim()) errs.contactName = "Required";
  if (!c.contactPosition.trim()) errs.contactPosition = "Required";
  if (!c.contactPhone.trim()) errs.contactPhone = "Required";
  else if (!/^[+0-9 ()-]{6,40}$/.test(c.contactPhone)) errs.contactPhone = "Invalid phone format";

  // AUTH-CODE-03 — conditional applicability.
  if (c.isLegallyRegistered === "yes" && !c.registrationNumber.trim()) {
    errs.registrationNumber = "Required when the organization is legally registered";
  }
  if (c.isLegallyRegistered === "no" && !c.legalNotApplicableReason.trim()) {
    errs.legalNotApplicableReason = "Tell us why a legal registration does not apply";
  }
  if (c.isTaxRegistered === "yes" && !c.taxNumber.trim()) {
    errs.taxNumber = "Required when the organization is tax-registered";
  }
  if (c.isTaxRegistered === "no" && !c.taxNotApplicableReason.trim()) {
    errs.taxNotApplicableReason = "Tell us why a tax registration does not apply";
  }
  return errs;
}

// ── Consultant onboarding form ─────────────────────────────────────────────
interface ConsultantFormState {
  bio: string;
  title: string;
  highestDegree: string;
  fieldOfExpertise: string;
  yearsExperience: string;
  expertiseTags: string;
  fee: string;
  durationMinutes: string;
  languages: string;
  country: string;
  timezone: string;
  linkedIn: string;
  portfolio: string;
}

function emptyConsultant(): ConsultantFormState {
  return {
    bio: "",
    title: "",
    highestDegree: "",
    fieldOfExpertise: "",
    yearsExperience: "",
    expertiseTags: "",
    fee: "",
    durationMinutes: "60",
    languages: "",
    country: "",
    timezone: typeof Intl !== "undefined" ? Intl.DateTimeFormat().resolvedOptions().timeZone ?? "" : "",
    linkedIn: "",
    portfolio: "",
  };
}

function validateConsultant(
  c: ConsultantFormState,
  paymentsEnabled: boolean,
): Record<string, string> {
  const errs: Record<string, string> = {};
  if (!c.bio.trim()) errs.bio = "Required";
  else if (c.bio.length > 2000) errs.bio = "Max 2000 characters";
  if (!c.title.trim()) errs.title = "Required";
  if (!c.highestDegree.trim()) errs.highestDegree = "Required";
  if (!c.fieldOfExpertise.trim()) errs.fieldOfExpertise = "Required";
  const years = Number(c.yearsExperience);
  // AUTH-CODE-04 — minimum 1 year of experience (was 0).
  if (!c.yearsExperience || Number.isNaN(years) || years < 1) errs.yearsExperience = "Must be at least 1";
  const tagCount = c.expertiseTags.split(",").map((s) => s.trim()).filter(Boolean).length;
  if (tagCount === 0) errs.expertiseTags = "At least one tag required";
  // Master payments switch: when off, the fee field is hidden and the
  // value is irrelevant — the server forces it to 0 on submit. Only enforce
  // the ≥0 rule when payments are actually on.
  if (paymentsEnabled) {
    const fee = Number(c.fee);
    if (!c.fee || Number.isNaN(fee) || fee < 0) errs.fee = "Must be zero or greater";
  }
  if (!c.durationMinutes) errs.durationMinutes = "Required";
  const langCount = c.languages.split(",").map((s) => s.trim()).filter(Boolean).length;
  if (langCount === 0) errs.languages = "At least one language required";
  if (!c.country.trim()) errs.country = "Required";
  if (!c.timezone.trim()) errs.timezone = "Required";
  if (c.linkedIn && !/^https?:\/\/.+/i.test(c.linkedIn)) errs.linkedIn = "Must be a valid URL";
  if (c.portfolio && !/^https?:\/\/.+/i.test(c.portfolio)) errs.portfolio = "Must be a valid URL";
  return errs;
}

/**
 * AUTH-CODE-06 — banner shown to a previously-rejected applicant so they see
 * the admin's rejection note before resubmitting (FR-ONB-07). Only renders when
 * the backend exposes a non-empty `lastOnboardingRejectionReason` on the
 * current user (cleared on resubmit / approval).
 */
function RejectionBanner({ reason, rejectedAt }: { reason: string; rejectedAt?: string | null }) {
  const { t, i18n } = useTranslation(["auth"]);
  const when = rejectedAt
    ? new Date(rejectedAt).toLocaleDateString(i18n.language)
    : null;
  return (
    <div
      role="alert"
      className="mx-auto mb-6 flex max-w-2xl items-start gap-3 rounded-xl border border-danger-300 bg-danger-50/60 px-4 py-3 text-start"
    >
      <AlertTriangle aria-hidden className="mt-0.5 size-5 shrink-0 text-danger-500" />
      <div className="min-w-0">
        <p className="text-sm font-semibold text-danger-700">
          {t("auth:onboarding.rejection.title", "Your previous submission was rejected")}
          {when && (
            <span className="ms-2 text-xs font-normal text-danger-600">({when})</span>
          )}
        </p>
        <p className="mt-1 whitespace-pre-line text-sm text-danger-700">{reason}</p>
        <p className="mt-2 text-xs text-danger-600">
          {t(
            "auth:onboarding.rejection.help",
            "Update the affected information and resubmit. Reach out to support if you need clarification.",
          )}
        </p>
      </div>
    </div>
  );
}

export function OnboardingWizard() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const rejectionReason = user?.lastOnboardingRejectionReason ?? null;
  const rejectionAt = user?.lastOnboardingRejectedAt ?? null;
  // Master payments switch — controls whether the consultant session-fee
  // field is rendered + validated.
  const paymentsEnabled = usePaymentsEnabled();

  const [step, setStep] = useState<"role" | "details">("role");
  const [detailsRole, setDetailsRole] = useState<"ScholarshipProvider" | "Consultant">("ScholarshipProvider");
  const [submitting, setSubmitting] = useState(false);

  const [company, setScholarshipProvider] = useState<ScholarshipProviderFormState>(emptyScholarshipProvider);
  const [scholarshipProviderErrors, setScholarshipProviderErrors] = useState<Record<string, string>>({});
  const [consultant, setConsultant] = useState<ConsultantFormState>(emptyConsultant);
  const [consultantErrors, setConsultantErrors] = useState<Record<string, string>>({});

  // A ScholarshipProvider/Consultant who already chose their role is awaiting admin review.
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
    } catch (err) {
      toast.error(apiErrorMessage(err, t("auth:errors.generic")));
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
    if (detailsRole === "ScholarshipProvider") {
      const errs = validateScholarshipProvider(company);
      setScholarshipProviderErrors(errs);
      if (Object.keys(errs).length > 0) {
        toast.error(t("auth:onboarding.details.required"));
        return;
      }
      void submitRole("ScholarshipProvider", {
        organizationLegalName: company.legalName.trim(),
        organizationWebsite: company.website.trim(),
        organizationEmail: company.email.trim(),
        organizationCountry: company.country.trim(),
        scholarshipProviderType: company.scholarshipProviderType,
        scholarshipProviderDescription: company.description.trim(),
        organizationRegistrationNumber: company.registrationNumber.trim() || null,
        organizationTaxNumber: company.taxNumber.trim() || null,
        contactPersonFullName: company.contactName.trim(),
        contactPersonPosition: company.contactPosition.trim(),
        contactPhoneNumber: company.contactPhone.trim(),
        // AUTH-CODE-03 — conditional applicability.
        isLegallyRegistered: company.isLegallyRegistered === ""
          ? null
          : company.isLegallyRegistered === "yes",
        legalRegistrationNotApplicableReason:
          company.isLegallyRegistered === "no"
            ? company.legalNotApplicableReason.trim()
            : null,
        isTaxRegistered: company.isTaxRegistered === ""
          ? null
          : company.isTaxRegistered === "yes",
        taxNotApplicableReason:
          company.isTaxRegistered === "no"
            ? company.taxNotApplicableReason.trim()
            : null,
      });
    } else {
      const errs = validateConsultant(consultant, paymentsEnabled);
      setConsultantErrors(errs);
      if (Object.keys(errs).length > 0) {
        toast.error(t("auth:onboarding.details.required"));
        return;
      }
      // When payments are disabled platform-wide, force the fee to 0 — the
      // server does the same on its side, but submitting 0 here keeps the
      // wire payload consistent with the validator and avoids NaN from an
      // empty string.
      const feeNumber = paymentsEnabled ? Number(consultant.fee) : 0;
      void submitRole("Consultant", {
        biography: consultant.bio.trim(),
        professionalTitle: consultant.title.trim(),
        highestDegree: consultant.highestDegree.trim(),
        fieldOfExpertise: consultant.fieldOfExpertise.trim(),
        yearsOfExperience: Number(consultant.yearsExperience),
        sessionFeeUsd: feeNumber,
        sessionDurationMinutes: Number(consultant.durationMinutes),
        expertiseTags: consultant.expertiseTags.split(",").map((s) => s.trim()).filter(Boolean),
        languages: consultant.languages.split(",").map((s) => s.trim()).filter(Boolean),
        country: consultant.country.trim(),
        timezone: consultant.timezone.trim(),
        linkedInUrl: consultant.linkedIn.trim() || null,
        portfolioUrl: consultant.portfolio.trim() || null,
      });
    }
  }

  // Progress step indicator — used on both screens for orientation.
  // (ProgressBar component is declared at module scope below to satisfy
  // react-hooks/static-components — components must not be created on each render.)

  // ── Step 2 — ScholarshipProvider / Consultant profile details ──────────────────────────
  if (step === "details") {
    const isScholarshipProvider = detailsRole === "ScholarshipProvider";
    return (
      <section className="mx-auto max-w-2xl px-4 py-12 sm:px-6">
        <ProgressBar current={2} detailsLabel={t("auth:onboarding.documents.title", "Details")} />
        {rejectionReason && <RejectionBanner reason={rejectionReason} rejectedAt={rejectionAt} />}

        <motion.div
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
        >
          <button
            type="button"
            onClick={() => setStep("role")}
            className="mb-6 inline-flex items-center gap-1.5 text-sm font-semibold text-text-secondary hover:text-text-primary transition-colors group"
          >
            <ArrowLeft aria-hidden className="size-4 rtl:rotate-180 group-hover:-translate-x-0.5 rtl:group-hover:translate-x-0.5 transition-transform" />
            {t("auth:onboarding.details.back")}
          </button>

          <div className="card-premium p-6 sm:p-8">
            <h1 className="mb-2 text-3xl font-bold tracking-tight">
              {t(
                isScholarshipProvider
                  ? "auth:onboarding.details.scholarshipProviderHeading"
                  : "auth:onboarding.details.consultantHeading",
              )}
            </h1>
            <p className="mb-8 text-text-secondary">
              {t(
                isScholarshipProvider
                  ? "auth:onboarding.details.scholarshipProviderSubtitle"
                  : "auth:onboarding.details.consultantSubtitle",
              )}
            </p>

            <form onSubmit={submitDetails} className="space-y-6" noValidate>
              {isScholarshipProvider ? (
                <ScholarshipProviderForm value={company} onChange={setScholarshipProvider} errors={scholarshipProviderErrors} />
              ) : (
                <ConsultantForm value={consultant} onChange={setConsultant} errors={consultantErrors} />
              )}

              <div className="rounded-xl border border-brand-200 bg-brand-50/60 px-4 py-3">
                <p className="text-xs text-brand-700 leading-relaxed">
                  {t(
                    "auth:onboarding.details.docsNotice",
                    "After submitting, you'll be asked to upload supporting verification documents on the next screen.",
                  )}
                </p>
              </div>

              <button
                type="submit"
                disabled={submitting}
                className="btn btn-primary btn-lg w-full"
              >
                {submitting
                  ? t("auth:onboarding.details.submitting")
                  : t("auth:onboarding.details.submit")}
              </button>
            </form>
          </div>
        </motion.div>
      </section>
    );
  }

  // ── Step 1 — role selection ────────────────────────────────────────────────
  return (
    <section className="mx-auto max-w-4xl px-4 py-12 sm:px-6">
      <ProgressBar current={1} detailsLabel={t("auth:onboarding.documents.title", "Details")} />
      {rejectionReason && <RejectionBanner reason={rejectionReason} rejectedAt={rejectionAt} />}

      <div className="text-center max-w-2xl mx-auto">
        <h1 className="mb-3 text-4xl font-bold tracking-tight">{t("auth:onboarding.title")}</h1>
        <p className="text-text-secondary text-lg">{t("auth:onboarding.subtitle")}</p>
      </div>

      <div className="mt-12 grid gap-5 sm:grid-cols-3">
        {ROLES.map(({ key, i18n, icon: Icon }, idx) => (
          <motion.button
            type="button"
            key={key}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.32, delay: idx * 0.06 }}
            onClick={() => pickRole(key)}
            disabled={submitting}
            className="group relative text-start card-premium p-6 hover:border-brand-300 disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            <div className="mb-5 flex size-12 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm group-hover:shadow-brand-md transition-shadow">
              <Icon aria-hidden className="size-6" />
            </div>
            <h3 className="mb-2 text-xl font-bold tracking-tight">{t(`auth:onboarding.role.${i18n}.title`)}</h3>
            <p className="mb-5 text-sm text-text-secondary leading-relaxed">
              {t(`auth:onboarding.role.${i18n}.body`)}
            </p>
            <span className="inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 group-hover:text-brand-700 transition-colors">
              {t(`auth:onboarding.role.${i18n}.cta`)}
              <ArrowLeft aria-hidden className="size-4 rotate-180 rtl:rotate-0 group-hover:translate-x-0.5 rtl:group-hover:-translate-x-0.5 transition-transform" />
            </span>
          </motion.button>
        ))}
      </div>
    </section>
  );
}

/**
 * AUTH-CODE-03 — yes/no applicability switch. When the answer is "yes" the user
 * must supply the corresponding registration number; when "no" they must supply
 * a not-applicable reason. Backend mirror lives in SelectRoleCommandValidator.
 */
function ApplicabilityField({
  legend,
  value,
  onChange,
  numberLabel,
  numberValue,
  onNumberChange,
  numberError,
  reasonLabel,
  reasonValue,
  onReasonChange,
  reasonError,
}: {
  legend: string;
  value: Applicability;
  onChange: (v: Applicability) => void;
  numberLabel: string;
  numberValue: string;
  onNumberChange: (v: string) => void;
  numberError?: string;
  reasonLabel: string;
  reasonValue: string;
  onReasonChange: (v: string) => void;
  reasonError?: string;
}) {
  const { t } = useTranslation(["auth", "common"]);
  return (
    <fieldset className="rounded-xl border border-border-subtle bg-bg-subtle/40 p-4">
      <legend className="px-1 text-sm font-medium text-text-primary">{legend}</legend>
      <div className="mb-3 flex gap-4">
        <label className="inline-flex items-center gap-2 text-sm text-text-primary">
          <input
            type="radio"
            checked={value === "yes"}
            onChange={() => onChange("yes")}
            className="size-4 accent-brand-500"
          />
          {t("common:yes", "Yes")}
        </label>
        <label className="inline-flex items-center gap-2 text-sm text-text-primary">
          <input
            type="radio"
            checked={value === "no"}
            onChange={() => onChange("no")}
            className="size-4 accent-brand-500"
          />
          {t("common:no", "No")}
        </label>
      </div>
      {value === "yes" && (
        <Labeled label={numberLabel} required error={numberError}>
          <input
            className={`h-10 ${fieldClass}`}
            value={numberValue}
            onChange={(e) => onNumberChange(e.target.value)}
            maxLength={100}
          />
        </Labeled>
      )}
      {value === "no" && (
        <Labeled label={reasonLabel} required error={reasonError}>
          <textarea
            rows={2}
            className={`py-2 ${fieldClass}`}
            value={reasonValue}
            onChange={(e) => onReasonChange(e.target.value)}
            maxLength={500}
          />
        </Labeled>
      )}
    </fieldset>
  );
}

function ScholarshipProviderForm({
  value,
  onChange,
  errors,
}: {
  value: ScholarshipProviderFormState;
  onChange: (v: ScholarshipProviderFormState) => void;
  errors: Record<string, string>;
}) {
  const { t } = useTranslation(["auth"]);
  const set = <K extends keyof ScholarshipProviderFormState>(k: K, v: ScholarshipProviderFormState[K]) =>
    onChange({ ...value, [k]: v });
  // `optional` was used by the previous registration/tax inputs, kept here to
  // preserve the legacy hint helper in case the form is extended.
  void t("auth:onboarding.details.optional");
  return (
    <div className="grid gap-5 sm:grid-cols-2">
      <Labeled label={t("auth:onboarding.company.legalName")} required error={errors.legalName}>
        <input className={`h-11 ${fieldClass}`} value={value.legalName}
          onChange={(e) => set("legalName", e.target.value)} maxLength={200} />
      </Labeled>
      <Labeled label={t("auth:onboarding.company.website")} required error={errors.website}>
        <input className={`h-11 ${fieldClass}`} value={value.website}
          placeholder={t("auth:onboarding.company.websitePlaceholder")}
          onChange={(e) => set("website", e.target.value)} type="url" />
      </Labeled>
      <Labeled label={t("auth:onboarding.company.email")} required error={errors.email}>
        <input className={`h-11 ${fieldClass}`} value={value.email} type="email"
          onChange={(e) => set("email", e.target.value)} />
      </Labeled>
      <Labeled label={t("auth:onboarding.company.country")} required error={errors.country}>
        <input className={`h-11 ${fieldClass}`} value={value.country}
          onChange={(e) => set("country", e.target.value)} maxLength={80} />
      </Labeled>
      <Labeled label={t("auth:onboarding.company.scholarshipProviderType")} required error={errors.scholarshipProviderType}>
        <select className={`h-11 ${fieldClass}`} value={value.scholarshipProviderType}
          onChange={(e) => set("scholarshipProviderType", e.target.value)}>
          <option value="">—</option>
          {COMPANY_TYPES.map((ct) => (
            <option key={ct} value={ct}>{t(`auth:onboarding.scholarshipProviderTypes.${ct}`)}</option>
          ))}
        </select>
      </Labeled>
      {/*
        AUTH-CODE-03 — conditional applicability blocks.
        Each block: a yes/no radio + either the registration number input (yes)
        or a "not applicable" reason textbox (no). When the user has made no
        selection, both downstream fields are optional, matching backend rules.
      */}
      <div className="sm:col-span-2 grid gap-5 sm:grid-cols-2">
        <ApplicabilityField
          legend={t("auth:onboarding.company.isLegallyRegistered", "Is the organization legally registered?")}
          value={value.isLegallyRegistered}
          onChange={(v) => set("isLegallyRegistered", v)}
          numberLabel={t("auth:onboarding.company.registrationNumber")}
          numberValue={value.registrationNumber}
          onNumberChange={(v) => set("registrationNumber", v)}
          numberError={errors.registrationNumber}
          reasonLabel={t("auth:onboarding.company.legalNotApplicableReason", "Why is a legal registration not applicable?")}
          reasonValue={value.legalNotApplicableReason}
          onReasonChange={(v) => set("legalNotApplicableReason", v)}
          reasonError={errors.legalNotApplicableReason}
        />
        <ApplicabilityField
          legend={t("auth:onboarding.company.isTaxRegistered", "Is the organization tax-registered?")}
          value={value.isTaxRegistered}
          onChange={(v) => set("isTaxRegistered", v)}
          numberLabel={t("auth:onboarding.company.taxNumber")}
          numberValue={value.taxNumber}
          onNumberChange={(v) => set("taxNumber", v)}
          numberError={errors.taxNumber}
          reasonLabel={t("auth:onboarding.company.taxNotApplicableReason", "Why is a tax registration not applicable?")}
          reasonValue={value.taxNotApplicableReason}
          onReasonChange={(v) => set("taxNotApplicableReason", v)}
          reasonError={errors.taxNotApplicableReason}
        />
      </div>
      <div className="sm:col-span-2">
        <Labeled label={t("auth:onboarding.company.description")} required error={errors.description}>
          <textarea rows={4} className={`py-2.5 ${fieldClass}`} value={value.description}
            onChange={(e) => set("description", e.target.value)} maxLength={2000} />
        </Labeled>
      </div>
      <Labeled label={t("auth:onboarding.company.contactName")} required error={errors.contactName}>
        <input className={`h-11 ${fieldClass}`} value={value.contactName}
          onChange={(e) => set("contactName", e.target.value)} maxLength={100} />
      </Labeled>
      <Labeled label={t("auth:onboarding.company.contactPosition")} required error={errors.contactPosition}>
        <input className={`h-11 ${fieldClass}`} value={value.contactPosition}
          onChange={(e) => set("contactPosition", e.target.value)} maxLength={100} />
      </Labeled>
      <Labeled label={t("auth:onboarding.company.contactPhone")} required error={errors.contactPhone}>
        <input className={`h-11 ${fieldClass}`} value={value.contactPhone} type="tel"
          onChange={(e) => set("contactPhone", e.target.value)} maxLength={40} />
      </Labeled>
    </div>
  );
}

function ConsultantForm({
  value,
  onChange,
  errors,
}: {
  value: ConsultantFormState;
  onChange: (v: ConsultantFormState) => void;
  errors: Record<string, string>;
}) {
  const { t } = useTranslation(["auth"]);
  // Master payments switch — when off, the session-fee field is hidden so a
  // new consultant doesn't fill in a price they can't actually charge.
  const paymentsEnabled = usePaymentsEnabled();
  const set = <K extends keyof ConsultantFormState>(k: K, v: ConsultantFormState[K]) =>
    onChange({ ...value, [k]: v });
  // Common IANA time zones — kept short; the input is still free-text so the
  // user can type anything not listed.
  const tzOptions = useMemo(
    () => [
      "UTC", "Africa/Cairo", "Africa/Nairobi", "Asia/Riyadh", "Asia/Dubai", "Asia/Karachi",
      "Asia/Kolkata", "Asia/Singapore", "Asia/Tokyo", "Europe/London", "Europe/Paris",
      "Europe/Berlin", "Europe/Istanbul", "America/New_York", "America/Chicago",
      "America/Los_Angeles", "America/Sao_Paulo", "Australia/Sydney",
    ],
    [],
  );
  const optional = t("auth:onboarding.details.optional");
  return (
    <div className="grid gap-5 sm:grid-cols-2">
      <div className="sm:col-span-2">
        <Labeled label={t("auth:onboarding.consultant.bio")} required error={errors.bio}>
          <textarea rows={4} className={`py-2.5 ${fieldClass}`} value={value.bio}
            onChange={(e) => set("bio", e.target.value)} maxLength={2000} />
        </Labeled>
      </div>
      <Labeled label={t("auth:onboarding.consultant.title")} required error={errors.title}>
        <input className={`h-11 ${fieldClass}`} value={value.title}
          onChange={(e) => set("title", e.target.value)} maxLength={150}
          placeholder={t("auth:onboarding.consultant.titlePlaceholder")} />
      </Labeled>
      <Labeled label={t("auth:onboarding.consultant.highestDegree")} required error={errors.highestDegree}>
        <input className={`h-11 ${fieldClass}`} value={value.highestDegree}
          onChange={(e) => set("highestDegree", e.target.value)} maxLength={150}
          placeholder={t("auth:onboarding.consultant.highestDegreePlaceholder")} />
      </Labeled>
      <Labeled label={t("auth:onboarding.consultant.fieldOfExpertise")} required error={errors.fieldOfExpertise}>
        <input className={`h-11 ${fieldClass}`} value={value.fieldOfExpertise}
          onChange={(e) => set("fieldOfExpertise", e.target.value)} maxLength={200} />
      </Labeled>
      <Labeled label={t("auth:onboarding.consultant.yearsExperience")} required error={errors.yearsExperience}>
        <input type="number" min={1} max={80} className={`h-11 ${fieldClass}`}
          value={value.yearsExperience}
          onChange={(e) => set("yearsExperience", e.target.value)} />
      </Labeled>
      {paymentsEnabled ? (
        <Labeled label={t("auth:onboarding.consultant.fee")} required error={errors.fee}>
          <input type="number" min={0} className={`h-11 ${fieldClass}`} value={value.fee}
            onChange={(e) => set("fee", e.target.value)} />
        </Labeled>
      ) : (
        // Payments disabled platform-wide — the fee is auto-0 on the server.
        // We stash 0 in the form state so the validator + submit handler
        // (which still expect a number) keep working.
        <Labeled label={t("auth:onboarding.consultant.fee")}>
          <input type="number" className={`h-11 ${fieldClass}`} value="0" disabled readOnly />
        </Labeled>
      )}
      <Labeled label={t("auth:onboarding.consultant.duration")} required error={errors.durationMinutes}>
        <select className={`h-11 ${fieldClass}`} value={value.durationMinutes}
          onChange={(e) => set("durationMinutes", e.target.value)}>
          {SESSION_DURATIONS.map((d) => (
            <option key={d} value={String(d)}>{d}</option>
          ))}
        </select>
      </Labeled>
      <div className="sm:col-span-2">
        <Labeled label={t("auth:onboarding.consultant.expertiseTags")} required error={errors.expertiseTags}>
          <input className={`h-11 ${fieldClass}`} value={value.expertiseTags}
            onChange={(e) => set("expertiseTags", e.target.value)}
            placeholder={t("auth:onboarding.consultant.expertiseTagsPlaceholder")} />
          <p className="mt-1 text-xs text-text-tertiary">{t("auth:onboarding.consultant.commaSeparated")}</p>
        </Labeled>
      </div>
      <div className="sm:col-span-2">
        <Labeled label={t("auth:onboarding.consultant.languages")} required error={errors.languages}>
          <input className={`h-11 ${fieldClass}`} value={value.languages}
            onChange={(e) => set("languages", e.target.value)}
            placeholder={t("auth:onboarding.consultant.languagesPlaceholder")} />
          <p className="mt-1 text-xs text-text-tertiary">{t("auth:onboarding.consultant.commaSeparated")}</p>
        </Labeled>
      </div>
      <Labeled label={t("auth:onboarding.consultant.country")} required error={errors.country}>
        <input className={`h-11 ${fieldClass}`} value={value.country}
          onChange={(e) => set("country", e.target.value)} maxLength={80} />
      </Labeled>
      <Labeled label={t("auth:onboarding.consultant.timezone")} required error={errors.timezone}>
        <input list="tz-list" className={`h-11 ${fieldClass}`} value={value.timezone}
          onChange={(e) => set("timezone", e.target.value)} />
        <datalist id="tz-list">
          {tzOptions.map((tz) => (
            <option key={tz} value={tz} />
          ))}
        </datalist>
      </Labeled>
      <Labeled label={t("auth:onboarding.consultant.linkedIn")} hint={optional} error={errors.linkedIn}>
        <input type="url" className={`h-11 ${fieldClass}`} value={value.linkedIn}
          placeholder={t("auth:onboarding.consultant.linkedInPlaceholder")}
          onChange={(e) => set("linkedIn", e.target.value)} />
      </Labeled>
      <Labeled label={t("auth:onboarding.consultant.portfolio")} hint={optional} error={errors.portfolio}>
        <input type="url" className={`h-11 ${fieldClass}`} value={value.portfolio}
          placeholder={t("auth:onboarding.consultant.portfolioPlaceholder")}
          onChange={(e) => set("portfolio", e.target.value)} />
      </Labeled>
    </div>
  );
}
