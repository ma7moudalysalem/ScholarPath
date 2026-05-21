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
import { documentsApi } from "@/services/api/documents";
import { useAuthStore } from "@/stores/authStore";

type RoleKey = "Student" | "Company" | "Consultant";

const ROLES: { key: RoleKey; i18n: string; icon: typeof GraduationCap }[] = [
  { key: "Student", i18n: "student", icon: GraduationCap },
  { key: "Company", i18n: "company", icon: Building2 },
  { key: "Consultant", i18n: "consultant", icon: Users },
];

const COMPANY_TYPES = ["University", "NGO", "Company", "Foundation", "Government", "Other"] as const;
const SESSION_DURATIONS = [30, 45, 60, 90] as const;

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
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
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

// ── Company onboarding form ────────────────────────────────────────────────
interface CompanyFormState {
  legalName: string;
  website: string;
  email: string;
  country: string;
  companyType: string;
  description: string;
  registrationNumber: string;
  taxNumber: string;
  contactName: string;
  contactPosition: string;
  contactPhone: string;
}

function emptyCompany(): CompanyFormState {
  return {
    legalName: "",
    website: "",
    email: "",
    country: "",
    companyType: "",
    description: "",
    registrationNumber: "",
    taxNumber: "",
    contactName: "",
    contactPosition: "",
    contactPhone: "",
  };
}

function validateCompany(c: CompanyFormState): Record<string, string> {
  const errs: Record<string, string> = {};
  if (!c.legalName.trim()) errs.legalName = "Required";
  else if (c.legalName.length > 200) errs.legalName = "Max 200 characters";
  if (!c.website.trim()) errs.website = "Required";
  else if (!/^https?:\/\/.+/.test(c.website)) errs.website = "Must be a valid URL";
  if (!c.email.trim()) errs.email = "Required";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(c.email)) errs.email = "Must be a valid email";
  if (!c.country.trim()) errs.country = "Required";
  if (!c.companyType) errs.companyType = "Required";
  if (!c.description.trim()) errs.description = "Required";
  else if (c.description.length > 1000) errs.description = "Max 1000 characters";
  if (!c.contactName.trim()) errs.contactName = "Required";
  if (!c.contactPosition.trim()) errs.contactPosition = "Required";
  if (!c.contactPhone.trim()) errs.contactPhone = "Required";
  else if (!/^[+0-9 ()-]{6,40}$/.test(c.contactPhone)) errs.contactPhone = "Invalid phone format";
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

function validateConsultant(c: ConsultantFormState): Record<string, string> {
  const errs: Record<string, string> = {};
  if (!c.bio.trim()) errs.bio = "Required";
  else if (c.bio.length > 2000) errs.bio = "Max 2000 characters";
  if (!c.title.trim()) errs.title = "Required";
  if (!c.highestDegree.trim()) errs.highestDegree = "Required";
  if (!c.fieldOfExpertise.trim()) errs.fieldOfExpertise = "Required";
  const years = Number(c.yearsExperience);
  if (!c.yearsExperience || Number.isNaN(years) || years < 0) errs.yearsExperience = "Must be 0 or greater";
  const tagCount = c.expertiseTags.split(",").map((s) => s.trim()).filter(Boolean).length;
  if (tagCount === 0) errs.expertiseTags = "At least one tag required";
  const fee = Number(c.fee);
  if (!c.fee || Number.isNaN(fee) || fee <= 0) errs.fee = "Must be greater than zero";
  if (!c.durationMinutes) errs.durationMinutes = "Required";
  const langCount = c.languages.split(",").map((s) => s.trim()).filter(Boolean).length;
  if (langCount === 0) errs.languages = "At least one language required";
  if (!c.country.trim()) errs.country = "Required";
  if (!c.timezone.trim()) errs.timezone = "Required";
  if (c.linkedIn && !/^https?:\/\/.+/.test(c.linkedIn)) errs.linkedIn = "Must be a valid URL";
  if (c.portfolio && !/^https?:\/\/.+/.test(c.portfolio)) errs.portfolio = "Must be a valid URL";
  return errs;
}

export function OnboardingWizard() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);

  const [step, setStep] = useState<"role" | "details">("role");
  const [detailsRole, setDetailsRole] = useState<"Company" | "Consultant">("Company");
  const [submitting, setSubmitting] = useState(false);

  const [company, setCompany] = useState<CompanyFormState>(emptyCompany);
  const [companyErrors, setCompanyErrors] = useState<Record<string, string>>({});
  const [consultant, setConsultant] = useState<ConsultantFormState>(emptyConsultant);
  const [consultantErrors, setConsultantErrors] = useState<Record<string, string>>({});

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
    if (detailsRole === "Company") {
      const errs = validateCompany(company);
      setCompanyErrors(errs);
      if (Object.keys(errs).length > 0) {
        toast.error(t("auth:onboarding.details.required"));
        return;
      }
      void submitRole("Company", {
        organizationLegalName: company.legalName.trim(),
        organizationWebsite: company.website.trim(),
        organizationEmail: company.email.trim(),
        organizationCountry: company.country.trim(),
        companyType: company.companyType,
        companyDescription: company.description.trim(),
        organizationRegistrationNumber: company.registrationNumber.trim() || null,
        organizationTaxNumber: company.taxNumber.trim() || null,
        contactPersonFullName: company.contactName.trim(),
        contactPersonPosition: company.contactPosition.trim(),
        contactPhoneNumber: company.contactPhone.trim(),
      });
    } else {
      const errs = validateConsultant(consultant);
      setConsultantErrors(errs);
      if (Object.keys(errs).length > 0) {
        toast.error(t("auth:onboarding.details.required"));
        return;
      }
      void submitRole("Consultant", {
        biography: consultant.bio.trim(),
        professionalTitle: consultant.title.trim(),
        highestDegree: consultant.highestDegree.trim(),
        fieldOfExpertise: consultant.fieldOfExpertise.trim(),
        yearsOfExperience: Number(consultant.yearsExperience),
        sessionFeeUsd: Number(consultant.fee),
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

  // ── Step 2 — Company / Consultant profile details ──────────────────────────
  if (step === "details") {
    const isCompany = detailsRole === "Company";
    return (
      <section className="mx-auto max-w-2xl px-4 py-16 sm:px-6">
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

        <form onSubmit={submitDetails} className="space-y-5" noValidate>
          {isCompany ? (
            <CompanyForm value={company} onChange={setCompany} errors={companyErrors} />
          ) : (
            <ConsultantForm value={consultant} onChange={setConsultant} errors={consultantErrors} />
          )}

          <p className="text-xs text-text-tertiary">
            {t(
              "auth:onboarding.details.docsNotice",
              "After submitting, you'll be asked to upload supporting verification documents on the next screen.",
            )}
          </p>

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

function CompanyForm({
  value,
  onChange,
  errors,
}: {
  value: CompanyFormState;
  onChange: (v: CompanyFormState) => void;
  errors: Record<string, string>;
}) {
  const set = <K extends keyof CompanyFormState>(k: K, v: CompanyFormState[K]) =>
    onChange({ ...value, [k]: v });
  const optional = "optional";
  return (
    <div className="grid gap-5 sm:grid-cols-2">
      <Labeled label="Organization legal name" required error={errors.legalName}>
        <input className={`h-11 ${fieldClass}`} value={value.legalName}
          onChange={(e) => set("legalName", e.target.value)} maxLength={200} />
      </Labeled>
      <Labeled label="Organization website" required error={errors.website}>
        <input className={`h-11 ${fieldClass}`} value={value.website} placeholder="https://"
          onChange={(e) => set("website", e.target.value)} type="url" />
      </Labeled>
      <Labeled label="Organization email" required error={errors.email}>
        <input className={`h-11 ${fieldClass}`} value={value.email} type="email"
          onChange={(e) => set("email", e.target.value)} />
      </Labeled>
      <Labeled label="Country" required error={errors.country}>
        <input className={`h-11 ${fieldClass}`} value={value.country}
          onChange={(e) => set("country", e.target.value)} maxLength={80} />
      </Labeled>
      <Labeled label="Company type" required error={errors.companyType}>
        <select className={`h-11 ${fieldClass}`} value={value.companyType}
          onChange={(e) => set("companyType", e.target.value)}>
          <option value="">—</option>
          {COMPANY_TYPES.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
      </Labeled>
      <Labeled label="Business registration number" hint={optional} error={errors.registrationNumber}>
        <input className={`h-11 ${fieldClass}`} value={value.registrationNumber}
          onChange={(e) => set("registrationNumber", e.target.value)} maxLength={100} />
      </Labeled>
      <Labeled label="Tax registration number" hint={optional} error={errors.taxNumber}>
        <input className={`h-11 ${fieldClass}`} value={value.taxNumber}
          onChange={(e) => set("taxNumber", e.target.value)} maxLength={100} />
      </Labeled>
      <div className="sm:col-span-2">
        <Labeled label="Company description" required error={errors.description}>
          <textarea rows={4} className={`py-2.5 ${fieldClass}`} value={value.description}
            onChange={(e) => set("description", e.target.value)} maxLength={1000} />
        </Labeled>
      </div>
      <Labeled label="Contact person full name" required error={errors.contactName}>
        <input className={`h-11 ${fieldClass}`} value={value.contactName}
          onChange={(e) => set("contactName", e.target.value)} maxLength={100} />
      </Labeled>
      <Labeled label="Contact person position" required error={errors.contactPosition}>
        <input className={`h-11 ${fieldClass}`} value={value.contactPosition}
          onChange={(e) => set("contactPosition", e.target.value)} maxLength={100} />
      </Labeled>
      <Labeled label="Contact phone number" required error={errors.contactPhone}>
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
  const optional = "optional";
  return (
    <div className="grid gap-5 sm:grid-cols-2">
      <div className="sm:col-span-2">
        <Labeled label="Short bio" required error={errors.bio}>
          <textarea rows={4} className={`py-2.5 ${fieldClass}`} value={value.bio}
            onChange={(e) => set("bio", e.target.value)} maxLength={2000} />
        </Labeled>
      </div>
      <Labeled label="Professional title" required error={errors.title}>
        <input className={`h-11 ${fieldClass}`} value={value.title}
          onChange={(e) => set("title", e.target.value)} maxLength={150}
          placeholder="e.g. Senior Admissions Consultant" />
      </Labeled>
      <Labeled label="Highest degree" required error={errors.highestDegree}>
        <input className={`h-11 ${fieldClass}`} value={value.highestDegree}
          onChange={(e) => set("highestDegree", e.target.value)} maxLength={150}
          placeholder="e.g. PhD, MSc, MBA" />
      </Labeled>
      <Labeled label="Field of expertise" required error={errors.fieldOfExpertise}>
        <input className={`h-11 ${fieldClass}`} value={value.fieldOfExpertise}
          onChange={(e) => set("fieldOfExpertise", e.target.value)} maxLength={200} />
      </Labeled>
      <Labeled label="Years of experience" required error={errors.yearsExperience}>
        <input type="number" min={0} max={80} className={`h-11 ${fieldClass}`}
          value={value.yearsExperience}
          onChange={(e) => set("yearsExperience", e.target.value)} />
      </Labeled>
      <Labeled label="Session fee (USD)" required error={errors.fee}>
        <input type="number" min={1} className={`h-11 ${fieldClass}`} value={value.fee}
          onChange={(e) => set("fee", e.target.value)} />
      </Labeled>
      <Labeled label="Session duration (minutes)" required error={errors.durationMinutes}>
        <select className={`h-11 ${fieldClass}`} value={value.durationMinutes}
          onChange={(e) => set("durationMinutes", e.target.value)}>
          {SESSION_DURATIONS.map((d) => (
            <option key={d} value={String(d)}>{d}</option>
          ))}
        </select>
      </Labeled>
      <div className="sm:col-span-2">
        <Labeled label="Expertise tags" required error={errors.expertiseTags}>
          <input className={`h-11 ${fieldClass}`} value={value.expertiseTags}
            onChange={(e) => set("expertiseTags", e.target.value)}
            placeholder="Statement of Purpose, Interview Prep, …" />
          <p className="mt-1 text-xs text-text-tertiary">Separate with commas.</p>
        </Labeled>
      </div>
      <div className="sm:col-span-2">
        <Labeled label="Languages" required error={errors.languages}>
          <input className={`h-11 ${fieldClass}`} value={value.languages}
            onChange={(e) => set("languages", e.target.value)}
            placeholder="English, Arabic, …" />
          <p className="mt-1 text-xs text-text-tertiary">Separate with commas.</p>
        </Labeled>
      </div>
      <Labeled label="Country" required error={errors.country}>
        <input className={`h-11 ${fieldClass}`} value={value.country}
          onChange={(e) => set("country", e.target.value)} maxLength={80} />
      </Labeled>
      <Labeled label="Time zone" required error={errors.timezone}>
        <input list="tz-list" className={`h-11 ${fieldClass}`} value={value.timezone}
          onChange={(e) => set("timezone", e.target.value)} />
        <datalist id="tz-list">
          {tzOptions.map((tz) => (
            <option key={tz} value={tz} />
          ))}
        </datalist>
      </Labeled>
      <Labeled label="LinkedIn URL" hint={optional} error={errors.linkedIn}>
        <input type="url" className={`h-11 ${fieldClass}`} value={value.linkedIn} placeholder="https://linkedin.com/in/…"
          onChange={(e) => set("linkedIn", e.target.value)} />
      </Labeled>
      <Labeled label="Portfolio URL" hint={optional} error={errors.portfolio}>
        <input type="url" className={`h-11 ${fieldClass}`} value={value.portfolio} placeholder="https://"
          onChange={(e) => set("portfolio", e.target.value)} />
      </Labeled>
    </div>
  );
}
