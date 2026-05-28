import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation } from "@tanstack/react-query";
import * as Dialog from "@radix-ui/react-dialog";
import { Loader2, X } from "lucide-react";
import { toast } from "sonner";
import { ApiError } from "@/services/api/client";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import {
  upgradeRequestsApi,
  type SubmitConsultantUpgradeRequestPayload,
} from "@/services/api/upgradeRequests";

const SESSION_DURATIONS = [30, 45, 60, 90] as const;

interface FormState {
  biography: string;
  professionalTitle: string;
  highestDegree: string;
  fieldOfExpertise: string;
  yearsOfExperience: string;
  expertiseTags: string[];
  sessionFeeUsd: string;
  sessionDurationMinutes: string;
  languages: string[];
  country: string;
  timezone: string;
  linkedInUrl: string;
  portfolioUrl: string;
}

function emptyState(): FormState {
  return {
    biography: "",
    professionalTitle: "",
    highestDegree: "",
    fieldOfExpertise: "",
    yearsOfExperience: "",
    expertiseTags: [],
    sessionFeeUsd: "",
    sessionDurationMinutes: "60",
    languages: [],
    country: "",
    timezone:
      typeof Intl !== "undefined"
        ? Intl.DateTimeFormat().resolvedOptions().timeZone ?? ""
        : "",
    linkedInUrl: "",
    portfolioUrl: "",
  };
}

function isValidHttpUrl(value: string): boolean {
  const trimmed = value.trim();
  if (trimmed.length === 0) return true;
  try {
    const parsed = new URL(trimmed);
    return parsed.protocol === "http:" || parsed.protocol === "https:";
  } catch {
    return false;
  }
}

interface TagInputProps {
  value: string[];
  onChange: (tags: string[]) => void;
  placeholder?: string;
  max: number;
  invalid?: boolean;
}

function TagInput({ value, onChange, placeholder, max, invalid }: TagInputProps) {
  const [draft, setDraft] = useState("");
  const commit = (raw: string) => {
    const trimmed = raw.trim();
    if (!trimmed || value.length >= max || value.includes(trimmed)) {
      setDraft("");
      return;
    }
    onChange([...value, trimmed]);
    setDraft("");
  };
  return (
    <div
      className={`flex flex-wrap items-center gap-1.5 rounded-md border bg-bg-elevated px-2 py-1.5 focus-within:border-brand-500 ${
        invalid ? "border-danger-500" : "border-border-default"
      }`}
    >
      {value.map((tag, i) => (
        <span
          key={`${tag}-${i}`}
          className="inline-flex items-center gap-1 rounded-md bg-brand-50 px-2 py-0.5 text-xs font-medium text-brand-700"
        >
          {tag}
          <button
            type="button"
            onClick={() => onChange(value.filter((_, j) => j !== i))}
            className="rounded-sm text-brand-700/70 hover:text-brand-700"
            aria-label={`Remove ${tag}`}
          >
            <X aria-hidden className="size-3" />
          </button>
        </span>
      ))}
      <input
        type="text"
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === ",") {
            e.preventDefault();
            commit(draft);
          } else if (
            e.key === "Backspace" &&
            draft.length === 0 &&
            value.length > 0
          ) {
            onChange(value.slice(0, -1));
          }
        }}
        onBlur={() => {
          if (draft.trim().length > 0) commit(draft);
        }}
        placeholder={value.length === 0 ? placeholder : undefined}
        className="min-w-[8rem] flex-1 bg-transparent px-1 py-0.5 text-sm outline-none"
      />
    </div>
  );
}

interface FieldProps {
  label: string;
  htmlFor?: string;
  required?: boolean;
  error?: string | null;
  hint?: string;
  children: React.ReactNode;
}

function Field({ label, htmlFor, required, error, hint, children }: FieldProps) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={htmlFor} className="block text-sm font-medium text-text-primary">
        {label}
        {required && <span className="ms-1 text-danger-500">*</span>}
      </label>
      {children}
      {hint && !error && (
        <p className="text-xs text-text-tertiary">{hint}</p>
      )}
      {error && <p className="text-xs font-medium text-danger-500">{error}</p>}
    </div>
  );
}

export interface ConsultantUpgradeModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmitted: () => void;
}

export function ConsultantUpgradeModal({
  isOpen,
  onOpenChange,
  onSubmitted,
}: ConsultantUpgradeModalProps) {
  const { t, i18n } = useTranslation(["profile", "common"]);
  const isRtl = i18n.dir() === "rtl";
  // Master payments switch — when off, hide the fee field; the server will
  // clamp the submitted value to 0 regardless.
  const paymentsEnabled = usePaymentsEnabled();

  const [form, setForm] = useState<FormState>(emptyState);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  const inputClass =
    "w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30";
  const invalidClass =
    "w-full rounded-md border border-danger-500 bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none";

  const validate = (): Record<string, string> => {
    const e: Record<string, string> = {};
    const bio = form.biography.trim();
    if (!bio) e.biography = t("profile:upgrade.validation.required");
    else if (bio.length > 2000)
      e.biography = t("profile:upgrade.validation.maxChars", { max: 2000 });

    if (!form.professionalTitle.trim())
      e.professionalTitle = t("profile:upgrade.validation.required");
    else if (form.professionalTitle.length > 150)
      e.professionalTitle = t("profile:upgrade.validation.maxChars", { max: 150 });

    if (!form.highestDegree.trim())
      e.highestDegree = t("profile:upgrade.validation.required");
    else if (form.highestDegree.length > 150)
      e.highestDegree = t("profile:upgrade.validation.maxChars", { max: 150 });

    if (!form.fieldOfExpertise.trim())
      e.fieldOfExpertise = t("profile:upgrade.validation.required");
    else if (form.fieldOfExpertise.length > 200)
      e.fieldOfExpertise = t("profile:upgrade.validation.maxChars", { max: 200 });

    const years = Number(form.yearsOfExperience);
    if (
      !form.yearsOfExperience ||
      !Number.isFinite(years) ||
      years < 0 ||
      years > 80 ||
      !Number.isInteger(years)
    )
      e.yearsOfExperience = t("profile:upgrade.validation.yearsRange");

    if (form.expertiseTags.length === 0)
      e.expertiseTags = t("profile:upgrade.validation.atLeastOneTag");

    // Master switch — when payments are off the field is hidden and the
    // server forces the value to 0 on save, so skip the validation entirely.
    if (paymentsEnabled) {
      const fee = Number(form.sessionFeeUsd);
      if (!form.sessionFeeUsd || !Number.isFinite(fee) || fee < 0)
        e.sessionFeeUsd = t("profile:upgrade.validation.feePositive");
    }

    const duration = Number(form.sessionDurationMinutes);
    if (![30, 45, 60, 90].includes(duration))
      e.sessionDurationMinutes = t("profile:upgrade.validation.duration");

    if (form.languages.length === 0)
      e.languages = t("profile:upgrade.validation.atLeastOneLanguage");

    if (!form.country.trim())
      e.country = t("profile:upgrade.validation.required");
    else if (form.country.length > 80)
      e.country = t("profile:upgrade.validation.maxChars", { max: 80 });

    if (!form.timezone.trim())
      e.timezone = t("profile:upgrade.validation.required");
    else if (form.timezone.length > 64)
      e.timezone = t("profile:upgrade.validation.maxChars", { max: 64 });

    if (form.linkedInUrl && !isValidHttpUrl(form.linkedInUrl))
      e.linkedInUrl = t("profile:upgrade.validation.url");

    if (form.portfolioUrl && !isValidHttpUrl(form.portfolioUrl))
      e.portfolioUrl = t("profile:upgrade.validation.url");

    return e;
  };

  const toPayload = (): SubmitConsultantUpgradeRequestPayload => ({
    biography: form.biography.trim(),
    professionalTitle: form.professionalTitle.trim(),
    highestDegree: form.highestDegree.trim(),
    fieldOfExpertise: form.fieldOfExpertise.trim(),
    yearsOfExperience: Number(form.yearsOfExperience),
    // When the master switch is off the form hides the fee field — submit 0
    // so the server's validator + handler don't trip on an empty string.
    sessionFeeUsd: paymentsEnabled ? Number(form.sessionFeeUsd) : 0,
    sessionDurationMinutes: Number(form.sessionDurationMinutes),
    expertiseTags: form.expertiseTags,
    languages: form.languages,
    country: form.country.trim(),
    timezone: form.timezone.trim(),
    linkedInUrl: form.linkedInUrl.trim() || null,
    portfolioUrl: form.portfolioUrl.trim() || null,
  });

  const submitMut = useMutation({
    mutationFn: () => upgradeRequestsApi.submitConsultantUpgradeRequest(toPayload()),
    onSuccess: () => {
      toast.success(t("profile:upgrade.success"));
      setForm(emptyState());
      setErrors({});
      onSubmitted();
      onOpenChange(false);
    },
    onError: (err) => {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          toast.error(t("profile:upgrade.errorConflict"));
          return;
        }
        if (err.status === 403) {
          toast.error(t("profile:upgrade.errorForbidden"));
          return;
        }
        if (err.status === 400 || err.status === 422) {
          const fieldErrors: Record<string, string> = {};
          const apiErrors = err.payload.errors;
          if (apiErrors) {
            for (const [key, msgs] of Object.entries(apiErrors)) {
              const camel = key.charAt(0).toLowerCase() + key.slice(1);
              if (msgs && msgs.length > 0) fieldErrors[camel] = msgs[0];
            }
          }
          if (Object.keys(fieldErrors).length > 0) {
            setErrors((prev) => ({ ...prev, ...fieldErrors }));
          }
          toast.error(err.payload.detail ?? t("profile:upgrade.errorValidation"));
          return;
        }
      }
      toast.error(t("profile:upgrade.errorGeneric"));
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const v = validate();
    setErrors(v);
    if (Object.keys(v).length > 0) {
      toast.error(t("profile:upgrade.errorValidation"));
      return;
    }
    submitMut.mutate();
  };

  const durationOptions = useMemo(
    () =>
      SESSION_DURATIONS.map((d) => ({
        value: String(d),
        label: t(`profile:sessionDurationOption.${d}`),
      })),
    [t],
  );

  return (
    <Dialog.Root open={isOpen} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content
          dir={isRtl ? "rtl" : "ltr"}
          className="fixed left-1/2 top-1/2 z-50 max-h-[90vh] w-[calc(100vw-2rem)] max-w-2xl -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-2xl"
        >
          <div className="mb-1 flex items-start justify-between gap-4">
            <Dialog.Title className="text-xl font-semibold text-text-primary">
              {t("profile:upgrade.modalTitle")}
            </Dialog.Title>
            <Dialog.Close
              className="rounded-md p-1 text-text-tertiary transition-colors hover:bg-bg-subtle hover:text-text-primary"
              aria-label={t("common:cta.cancel", "Cancel")}
            >
              <X size={20} />
            </Dialog.Close>
          </div>
          <Dialog.Description className="mb-5 text-sm text-text-secondary">
            {t("profile:upgrade.modalSubtitle")}
          </Dialog.Description>

          <form onSubmit={handleSubmit} className="space-y-4">
            <Field
              label={t("profile:upgrade.fields.biography")}
              htmlFor="upg-bio"
              required
              error={errors.biography}
              hint={t("profile:upgrade.hints.biography")}
            >
              <textarea
                id="upg-bio"
                rows={4}
                maxLength={2000}
                value={form.biography}
                onChange={(e) => set("biography", e.target.value)}
                className={errors.biography ? invalidClass : inputClass}
              />
              <div className="text-end text-xs text-text-tertiary">
                {form.biography.length}/2000
              </div>
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                label={t("profile:upgrade.fields.professionalTitle")}
                htmlFor="upg-title"
                required
                error={errors.professionalTitle}
              >
                <input
                  id="upg-title"
                  type="text"
                  maxLength={150}
                  value={form.professionalTitle}
                  onChange={(e) => set("professionalTitle", e.target.value)}
                  className={errors.professionalTitle ? invalidClass : inputClass}
                />
              </Field>
              <Field
                label={t("profile:upgrade.fields.highestDegree")}
                htmlFor="upg-degree"
                required
                error={errors.highestDegree}
              >
                <input
                  id="upg-degree"
                  type="text"
                  maxLength={150}
                  value={form.highestDegree}
                  onChange={(e) => set("highestDegree", e.target.value)}
                  className={errors.highestDegree ? invalidClass : inputClass}
                />
              </Field>
              <Field
                label={t("profile:upgrade.fields.fieldOfExpertise")}
                htmlFor="upg-field"
                required
                error={errors.fieldOfExpertise}
              >
                <input
                  id="upg-field"
                  type="text"
                  maxLength={200}
                  value={form.fieldOfExpertise}
                  onChange={(e) => set("fieldOfExpertise", e.target.value)}
                  className={errors.fieldOfExpertise ? invalidClass : inputClass}
                />
              </Field>
              <Field
                label={t("profile:upgrade.fields.yearsOfExperience")}
                htmlFor="upg-years"
                required
                error={errors.yearsOfExperience}
              >
                <input
                  id="upg-years"
                  type="number"
                  min={0}
                  max={80}
                  step={1}
                  value={form.yearsOfExperience}
                  onChange={(e) => set("yearsOfExperience", e.target.value)}
                  className={errors.yearsOfExperience ? invalidClass : inputClass}
                />
              </Field>
            </div>

            <Field
              label={t("profile:upgrade.fields.expertiseTags")}
              required
              error={errors.expertiseTags}
              hint={t("profile:upgrade.hints.tags")}
            >
              <TagInput
                value={form.expertiseTags}
                onChange={(tags) => set("expertiseTags", tags)}
                placeholder={t("profile:upgrade.fields.expertiseTagsPlaceholder")}
                max={20}
                invalid={!!errors.expertiseTags}
              />
            </Field>

            <div className={paymentsEnabled ? "grid gap-4 sm:grid-cols-2" : "grid gap-4"}>
              {paymentsEnabled && (
                <Field
                  label={t("profile:upgrade.fields.sessionFeeUsd")}
                  htmlFor="upg-fee"
                  required
                  error={errors.sessionFeeUsd}
                >
                  <div className="relative">
                    <span className="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 text-sm text-text-tertiary">
                      $
                    </span>
                    <input
                      id="upg-fee"
                      type="number"
                      min={0}
                      step="0.01"
                      value={form.sessionFeeUsd}
                      onChange={(e) => set("sessionFeeUsd", e.target.value)}
                      className={`ps-7 ${errors.sessionFeeUsd ? invalidClass : inputClass}`}
                    />
                  </div>
                </Field>
              )}
              <Field
                label={t("profile:upgrade.fields.sessionDurationMinutes")}
                htmlFor="upg-duration"
                required
                error={errors.sessionDurationMinutes}
              >
                <select
                  id="upg-duration"
                  value={form.sessionDurationMinutes}
                  onChange={(e) => set("sessionDurationMinutes", e.target.value)}
                  className={errors.sessionDurationMinutes ? invalidClass : inputClass}
                >
                  {durationOptions.map((o) => (
                    <option key={o.value} value={o.value}>
                      {o.label}
                    </option>
                  ))}
                </select>
              </Field>
            </div>

            <Field
              label={t("profile:upgrade.fields.languages")}
              required
              error={errors.languages}
              hint={t("profile:upgrade.hints.languages")}
            >
              <TagInput
                value={form.languages}
                onChange={(langs) => set("languages", langs)}
                placeholder={t("profile:upgrade.fields.languagesPlaceholder")}
                max={20}
                invalid={!!errors.languages}
              />
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                label={t("profile:upgrade.fields.country")}
                htmlFor="upg-country"
                required
                error={errors.country}
              >
                <input
                  id="upg-country"
                  type="text"
                  maxLength={80}
                  value={form.country}
                  onChange={(e) => set("country", e.target.value)}
                  className={errors.country ? invalidClass : inputClass}
                />
              </Field>
              <Field
                label={t("profile:upgrade.fields.timezone")}
                htmlFor="upg-timezone"
                required
                error={errors.timezone}
              >
                <input
                  id="upg-timezone"
                  type="text"
                  maxLength={64}
                  value={form.timezone}
                  onChange={(e) => set("timezone", e.target.value)}
                  placeholder="e.g. Europe/London"
                  className={errors.timezone ? invalidClass : inputClass}
                />
              </Field>
              <Field
                label={t("profile:upgrade.fields.linkedInUrl")}
                htmlFor="upg-linkedin"
                error={errors.linkedInUrl}
              >
                <input
                  id="upg-linkedin"
                  type="url"
                  value={form.linkedInUrl}
                  onChange={(e) => set("linkedInUrl", e.target.value)}
                  placeholder="https://linkedin.com/in/…"
                  className={errors.linkedInUrl ? invalidClass : inputClass}
                />
              </Field>
              <Field
                label={t("profile:upgrade.fields.portfolioUrl")}
                htmlFor="upg-portfolio"
                error={errors.portfolioUrl}
              >
                <input
                  id="upg-portfolio"
                  type="url"
                  value={form.portfolioUrl}
                  onChange={(e) => set("portfolioUrl", e.target.value)}
                  placeholder="https://"
                  className={errors.portfolioUrl ? invalidClass : inputClass}
                />
              </Field>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <Dialog.Close asChild>
                <button
                  type="button"
                  className="rounded-md px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle"
                >
                  {t("common:cta.cancel", "Cancel")}
                </button>
              </Dialog.Close>
              <button
                type="submit"
                disabled={submitMut.isPending}
                className="inline-flex items-center gap-2 rounded-md bg-brand-500 px-5 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {submitMut.isPending && (
                  <Loader2 aria-hidden className="size-4 animate-spin" />
                )}
                {submitMut.isPending
                  ? t("profile:upgrade.submitting")
                  : t("profile:upgrade.submit")}
              </button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
