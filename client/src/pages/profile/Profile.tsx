import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";
import { PasswordInput } from "@/components/ui/PasswordInput";
import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { toast } from "sonner";
import { motion } from "motion/react";
import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
import {
  ArrowRight,
  AtSign,
  BadgeCheck,
  Briefcase,
  Camera,
  CheckCircle2,
  ChevronDown,
  GraduationCap,
  Image as ImageIcon,
  Key,
  Link as LinkIcon,
  Loader2,
  Repeat,
  Save,
  ShieldCheck,
  Sparkles,
  Trash2,
  User as UserIcon,
  X,
} from "lucide-react";
import {
  profileApi,
  validatePhotoFile,
  PHOTO_ALLOWED_MIME_TYPES,
  type UserProfile,
  type UpdateProfileRequest,
} from "@/services/api/profile";
import { useAuthStore } from "@/stores/authStore";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { authApi, applyAuthSession, postAuthPath } from "@/services/api/auth";
import { userPhotoUrl } from "@/lib/userPhoto";
import { apiErrorMessage } from "@/services/api/client";
import { DatePicker } from "@/components/ui/DatePicker";
import { cn } from "@/lib/utils";
import { COUNTRIES, countryLabel } from "@/lib/countryLabel";
import { ConsultantUpgradeModal } from "@/components/profile/ConsultantUpgradeModal";

const SWITCHABLE_ROLES = ["Student", "Consultant", "ScholarshipProvider", "Admin"] as const;

const PROFILE_KEY = ["profile", "me"] as const;

const ACADEMIC_LEVELS = [
  "HighSchool",
  "Undergrad",
  "Masters",
  "PhD",
  "PostDoc",
  "Other",
] as const;

const GPA_SCALES = ["4.0", "5.0", "10.0", "20.0", "100"] as const;
type GpaScale = (typeof GPA_SCALES)[number];

const GPA_MAX: Record<GpaScale, number> = {
  "4.0": 4,
  "5.0": 5,
  "10.0": 10,
  "20.0": 20,
  "100": 100,
};

const SESSION_DURATIONS = [30, 45, 60, 90, 120] as const;

// Canonical country list + Arabic labels live in the shared lib so the stored
// value stays the English name (matching the backend) while AR users see
// Arabic labels via countryLabel(name, lang).

interface FormState {
  firstName: string;
  lastName: string;
  biography: string;
  dateOfBirth: string;
  nationality: string;
  countryOfResidence: string;
  preferredLanguage: string;
  linkedInUrl: string;
  websiteUrl: string;
  academicLevel: string;
  fieldOfStudy: string;
  currentInstitution: string;
  gpa: string;
  gpaScale: string;
  organizationLegalName: string;
  organizationWebsite: string;
  sessionFeeUsd: string;
  sessionDurationMinutes: string;
  // Consultant professional fields (CR-PROF-08)
  professionalTitle: string;
  yearsOfExperience: string;
  expertiseTags: string[];
  languages: string[];
  timezone: string;
  // Student matching inputs — preferred study destinations + fields.
  preferredCountries: string[];
  preferredFields: string[];
}

function toForm(p: UserProfile): FormState {
  return {
    firstName: p.firstName ?? "",
    lastName: p.lastName ?? "",
    biography: p.biography ?? "",
    dateOfBirth: p.dateOfBirth ?? "",
    nationality: p.nationality ?? "",
    countryOfResidence: p.countryOfResidence ?? "",
    preferredLanguage: p.preferredLanguage ?? "",
    linkedInUrl: p.linkedInUrl ?? "",
    websiteUrl: p.websiteUrl ?? "",
    academicLevel: p.academicLevel ?? "",
    fieldOfStudy: p.fieldOfStudy ?? "",
    currentInstitution: p.currentInstitution ?? "",
    gpa: p.gpa != null ? String(p.gpa) : "",
    gpaScale: p.gpaScale ?? "",
    organizationLegalName: p.organizationLegalName ?? "",
    organizationWebsite: p.organizationWebsite ?? "",
    sessionFeeUsd: p.sessionFeeUsd != null ? String(p.sessionFeeUsd) : "",
    sessionDurationMinutes:
      p.sessionDurationMinutes != null ? String(p.sessionDurationMinutes) : "",
    professionalTitle: p.professionalTitle ?? "",
    yearsOfExperience:
      p.yearsOfExperience != null ? String(p.yearsOfExperience) : "",
    expertiseTags: p.expertiseTags ?? [],
    languages: p.languages ?? [],
    timezone: p.timezone ?? "",
    preferredCountries: p.preferredCountries ?? [],
    preferredFields: p.preferredFields ?? [],
  };
}

/** Permissive http(s)-URL check; mirrors the backend BeValidAbsoluteHttpUrl rule. */
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

const trimOrNull = (v: string): string | null => {
  const trimmed = v.trim();
  return trimmed.length > 0 ? trimmed : null;
};

const numOrNull = (v: string): number | null => {
  const trimmed = v.trim();
  if (trimmed.length === 0) return null;
  const n = Number(trimmed);
  return Number.isFinite(n) ? n : null;
};

function toRequest(f: FormState): UpdateProfileRequest {
  return {
    firstName: trimOrNull(f.firstName),
    lastName: trimOrNull(f.lastName),
    biography: trimOrNull(f.biography),
    dateOfBirth: trimOrNull(f.dateOfBirth),
    nationality: trimOrNull(f.nationality),
    countryOfResidence: trimOrNull(f.countryOfResidence),
    preferredLanguage: trimOrNull(f.preferredLanguage),
    linkedInUrl: trimOrNull(f.linkedInUrl),
    websiteUrl: trimOrNull(f.websiteUrl),
    academicLevel: trimOrNull(f.academicLevel),
    fieldOfStudy: trimOrNull(f.fieldOfStudy),
    currentInstitution: trimOrNull(f.currentInstitution),
    gpa: numOrNull(f.gpa),
    gpaScale: trimOrNull(f.gpaScale),
    organizationLegalName: trimOrNull(f.organizationLegalName),
    organizationWebsite: trimOrNull(f.organizationWebsite),
    sessionFeeUsd: numOrNull(f.sessionFeeUsd),
    sessionDurationMinutes: numOrNull(f.sessionDurationMinutes),
    professionalTitle: trimOrNull(f.professionalTitle),
    yearsOfExperience: numOrNull(f.yearsOfExperience),
    expertiseTags: f.expertiseTags.length > 0 ? f.expertiseTags : null,
    languages: f.languages.length > 0 ? f.languages : null,
    timezone: trimOrNull(f.timezone),
    preferredCountries: f.preferredCountries.length > 0 ? f.preferredCountries : null,
    preferredFields: f.preferredFields.length > 0 ? f.preferredFields : null,
  };
}

function arrayEqual(a: readonly string[], b: readonly string[]): boolean {
  if (a.length !== b.length) return false;
  for (let i = 0; i < a.length; i++) if (a[i] !== b[i]) return false;
  return true;
}

function shallowEqual(a: FormState, b: FormState): boolean {
  const keys = Object.keys(a) as (keyof FormState)[];
  for (const k of keys) {
    const av = a[k];
    const bv = b[k];
    if (Array.isArray(av) && Array.isArray(bv)) {
      if (!arrayEqual(av, bv)) return false;
    } else if (av !== bv) {
      return false;
    }
  }
  return true;
}

function initials(first: string, last: string): string {
  const value = `${first.charAt(0)}${last.charAt(0)}`.toUpperCase();
  return value.length > 0 ? value : "?";
}

// ── Layout primitives ────────────────────────────────────────────────────────

function SectionCard({
  id,
  icon,
  title,
  description,
  children,
}: {
  id: string;
  icon: ReactNode;
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <motion.section
      id={id}
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
      className="card-premium scroll-mt-24 p-6 sm:p-8"
    >
      <div className="mb-6 flex items-start gap-3">
        <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
          {icon}
        </span>
        <div className="min-w-0">
          <h2 className="text-base font-semibold text-text-primary">{title}</h2>
          {description && (
            <p className="mt-0.5 text-sm text-text-secondary">{description}</p>
          )}
        </div>
      </div>
      <div className="divide-y divide-border-subtle">{children}</div>
    </motion.section>
  );
}

function FieldRow({
  label,
  description,
  htmlFor,
  children,
}: {
  label: string;
  description?: string;
  htmlFor?: string;
  children: ReactNode;
}) {
  return (
    <div className="grid gap-2 py-5 first:pt-0 last:pb-0 md:grid-cols-[minmax(0,1fr)_minmax(0,1.5fr)] md:items-start md:gap-8">
      <div className="md:pt-2">
        <label
          htmlFor={htmlFor}
          className="block text-sm font-medium text-text-primary"
        >
          {label}
        </label>
        {description && (
          <p className="mt-1 text-xs text-text-secondary">{description}</p>
        )}
      </div>
      <div className="min-w-0">{children}</div>
    </div>
  );
}

function FieldGrid({ children }: { children: ReactNode }) {
  return (
    <div className="grid gap-3 sm:grid-cols-2 sm:gap-4">{children}</div>
  );
}

// ── Avatar uploader ──────────────────────────────────────────────────────────

function AvatarUploader({
  profile,
  photoVersion,
  photoFailed,
  setPhotoFailed,
  onSelectFile,
  isUploading,
}: {
  profile: UserProfile;
  photoVersion: number | null;
  photoFailed: boolean;
  setPhotoFailed: (v: boolean) => void;
  onSelectFile: (file: File) => void;
  isUploading: boolean;
}) {
  const { t } = useTranslation(["profile", "common"]);
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  const showImage = profile.profileImageUrl && !photoFailed;
  const imgSrc =
    photoVersion != null
      ? `${userPhotoUrl(profile.userId)}?v=${photoVersion}`
      : userPhotoUrl(profile.userId);

  // CR-PROF-10: validate type/size/extension before the upload leaves the
  // browser. The backend re-validates (magic bytes + AV scan); this is just
  // immediate UX so the user does not wait for a roundtrip to learn it failed.
  const handleFiles = (files: FileList | null) => {
    if (!files || files.length === 0) return;
    const file = files[0];
    const error = validatePhotoFile(file);
    if (error) {
      toast.error(t(`profile:photo.validation.${error}`));
      return;
    }
    onSelectFile(file);
  };

  return (
    <div
      onDragEnter={(e) => {
        e.preventDefault();
        setIsDragging(true);
      }}
      onDragOver={(e) => {
        e.preventDefault();
        setIsDragging(true);
      }}
      onDragLeave={() => setIsDragging(false)}
      onDrop={(e) => {
        e.preventDefault();
        setIsDragging(false);
        handleFiles(e.dataTransfer.files);
      }}
      className={cn(
        "flex flex-col items-center gap-4 rounded-xl border border-dashed p-5 text-center transition-colors sm:flex-row sm:items-center sm:text-start",
        isDragging
          ? "border-brand-400 bg-brand-50"
          : "border-border-default bg-bg-muted",
      )}
    >
      <div className="relative shrink-0">
        {showImage ? (
          <img
            src={imgSrc}
            alt={profile.fullName}
            onError={() => setPhotoFailed(true)}
            className="size-20 rounded-full object-cover ring-4 ring-bg-elevated"
          />
        ) : (
          <div className="flex size-20 items-center justify-center rounded-full bg-gradient-to-br from-brand-500 to-brand-700 text-2xl font-semibold text-white ring-4 ring-bg-elevated">
            {initials(profile.firstName, profile.lastName)}
          </div>
        )}
        {isUploading && (
          <span className="absolute inset-0 flex items-center justify-center rounded-full bg-text-primary/40">
            <Loader2 aria-hidden className="size-5 animate-spin text-white" />
          </span>
        )}
      </div>

      <div className="flex-1">
        <p className="text-sm font-medium text-text-primary">
          {t("profile:photo.dropTitle")}
        </p>
        <p className="mt-0.5 text-xs text-text-secondary">
          {t("profile:photo.hint")}
        </p>
        <div className="mt-3 flex flex-wrap items-center justify-center gap-2 sm:justify-start">
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            disabled={isUploading}
            className="btn btn-secondary btn-sm"
          >
            <ImageIcon aria-hidden className="size-3.5" />
            {showImage
              ? t("profile:photo.replace")
              : t("profile:photo.upload")}
          </button>
          {showImage && (
            <span className="inline-flex items-center gap-1 text-xs text-text-tertiary">
              <Camera aria-hidden className="size-3.5" />
              {t("profile:photo.dragHint")}
            </span>
          )}
        </div>
        <input
          ref={inputRef}
          type="file"
          accept={PHOTO_ALLOWED_MIME_TYPES.join(",")}
          onChange={(e) => {
            handleFiles(e.target.files);
            e.target.value = "";
          }}
          className="hidden"
        />
      </div>
    </div>
  );
}

// ── Tag input ────────────────────────────────────────────────────────────────

/**
 * Lightweight tag editor for the consultant Expertise / Languages fields
 * (CR-PROF-08). Press Enter or comma to commit a chip; backspace on an empty
 * input removes the last chip. Duplicates are ignored; the chip count is
 * capped at `max` to mirror the backend.
 */
function TagInput({
  value,
  onChange,
  placeholder,
  max,
}: {
  value: string[];
  onChange: (tags: string[]) => void;
  placeholder?: string;
  max: number;
}) {
  const [draft, setDraft] = useState("");

  const commit = (raw: string) => {
    const trimmed = raw.trim();
    if (trimmed.length === 0) return;
    if (value.length >= max) return;
    if (value.includes(trimmed)) {
      setDraft("");
      return;
    }
    onChange([...value, trimmed]);
    setDraft("");
  };

  const removeAt = (index: number) =>
    onChange(value.filter((_, i) => i !== index));

  return (
    <div className="flex flex-wrap items-center gap-1.5 rounded-md border border-border-default bg-bg-elevated px-2 py-1.5 focus-within:border-brand-500">
      {value.map((tag, i) => (
        <span
          key={`${tag}-${i}`}
          className="inline-flex items-center gap-1 rounded-md bg-brand-50 px-2 py-0.5 text-xs font-medium text-brand-700"
        >
          {tag}
          <button
            type="button"
            onClick={() => removeAt(i)}
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
            removeAt(value.length - 1);
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

// ── Change-password card ─────────────────────────────────────────────────────

// CR-PROF-05: the same policy enforced server-side. Returning the first
// failing rule's i18n key keeps the inline error message stable across runs.
function validateNewPassword(
  newPw: string,
  confirmPw: string,
  currentPw: string,
): string | null {
  if (newPw !== confirmPw) return "profile:password.mismatch";
  if (newPw.length < 8) return "profile:password.tooShort";
  if (newPw.length > 128) return "profile:password.tooLong";
  if (!/[A-Z]/.test(newPw)) return "profile:password.missingUpper";
  if (!/[a-z]/.test(newPw)) return "profile:password.missingLower";
  if (!/[0-9]/.test(newPw)) return "profile:password.missingDigit";
  if (!/[^A-Za-z0-9]/.test(newPw)) return "profile:password.missingSpecial";
  if (newPw === currentPw) return "profile:password.sameAsCurrent";
  return null;
}

function ChangePasswordSection({ hasPassword }: { hasPassword: boolean }) {
  const { t } = useTranslation(["profile", "common"]);
  const [currentPw, setCurrentPw] = useState("");
  const [newPw, setNewPw] = useState("");
  const [confirmPw, setConfirmPw] = useState("");
  const [localError, setLocalError] = useState<string | null>(null);

  const changeMut = useMutation({
    mutationFn: () => profileApi.changePassword(currentPw, newPw),
    onSuccess: () => {
      toast.success(t("profile:password.success"));
      setCurrentPw("");
      setNewPw("");
      setConfirmPw("");
      setLocalError(null);
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, t("profile:password.error"))),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const errorKey = validateNewPassword(newPw, confirmPw, currentPw);
    if (errorKey) {
      setLocalError(t(errorKey));
      return;
    }
    setLocalError(null);
    changeMut.mutate();
  };

  // CR-PROF-06: an SSO-only account has no PasswordHash on the server, so the
  // backend will reject any change attempt. We render a clear unavailable
  // message instead of a form that is guaranteed to fail.
  if (!hasPassword) {
    return (
      <SectionCard
        id="security"
        icon={<ShieldCheck aria-hidden className="size-5" />}
        title={t("profile:sections.security")}
        description={t("profile:sections.securityDesc")}
      >
        <div className="rounded-lg border border-border-default bg-bg-muted p-4 text-sm">
          <p className="font-medium text-text-primary">
            {t("profile:sso.passwordUnavailable")}
          </p>
          <p className="mt-1 text-text-secondary">
            {t("profile:sso.passwordUnavailableDesc")}
          </p>
        </div>
      </SectionCard>
    );
  }

  return (
    <SectionCard
      id="security"
      icon={<ShieldCheck aria-hidden className="size-5" />}
      title={t("profile:sections.security")}
      description={t("profile:sections.securityDesc")}
    >
      <form onSubmit={handleSubmit}>
        <FieldRow
          label={t("profile:password.current")}
          description={t("profile:password.currentDesc")}
          htmlFor="pw-current"
        >
          <PasswordInput
            id="pw-current"
            required
            autoComplete="current-password"
            className="input-premium"
            value={currentPw}
            onChange={(e) => setCurrentPw(e.target.value)}
          />
        </FieldRow>
        <FieldRow
          label={t("profile:password.new")}
          description={t("profile:password.newDesc")}
          htmlFor="pw-new"
        >
          <PasswordInput
            id="pw-new"
            required
            autoComplete="new-password"
            className="input-premium"
            value={newPw}
            onChange={(e) => setNewPw(e.target.value)}
          />
        </FieldRow>
        <FieldRow
          label={t("profile:password.confirm")}
          htmlFor="pw-confirm"
        >
          <PasswordInput
            id="pw-confirm"
            required
            autoComplete="new-password"
            className="input-premium"
            value={confirmPw}
            onChange={(e) => setConfirmPw(e.target.value)}
          />
        </FieldRow>

        {localError && (
          <p className="pt-3 text-xs font-medium text-danger-500">
            {localError}
          </p>
        )}

        <div className="flex justify-end pt-4">
          <button
            type="submit"
            disabled={changeMut.isPending}
            className="btn btn-primary"
          >
            {changeMut.isPending ? (
              <Loader2 aria-hidden className="size-4 animate-spin" />
            ) : (
              <Key aria-hidden className="size-4" />
            )}
            {changeMut.isPending
              ? t("profile:password.changing")
              : t("profile:password.change")}
          </button>
        </div>
      </form>
    </SectionCard>
  );
}

// ── Sticky save bar ──────────────────────────────────────────────────────────

function SaveBar({
  dirty,
  saving,
  onSave,
  onReset,
}: {
  dirty: boolean;
  saving: boolean;
  onSave: () => void;
  onReset: () => void;
}) {
  const { t } = useTranslation(["profile", "common"]);
  if (!dirty) return null;
  return (
    <motion.div
      initial={{ y: 24, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      exit={{ y: 24, opacity: 0 }}
      transition={{ duration: 0.24, ease: [0.22, 1, 0.36, 1] }}
      className="sticky bottom-4 z-30 mx-auto mt-8 flex w-full max-w-3xl items-center justify-between gap-3 rounded-2xl border border-border-default bg-bg-elevated/95 p-3 pl-5 shadow-elevation-3 backdrop-blur-md"
    >
      <p className="text-sm font-medium text-text-primary">
        {t("profile:saveBar.unsaved")}
      </p>
      <div className="flex items-center gap-2">
        <button
          type="button"
          onClick={onReset}
          disabled={saving}
          className="btn btn-ghost btn-sm"
        >
          <X aria-hidden className="size-3.5" />
          {t("profile:saveBar.discard")}
        </button>
        <button
          type="button"
          onClick={onSave}
          disabled={saving}
          className="btn btn-primary btn-sm"
        >
          {saving ? (
            <Loader2 aria-hidden className="size-3.5 animate-spin" />
          ) : (
            <Save aria-hidden className="size-3.5" />
          )}
          {saving ? t("profile:saving") : t("profile:save")}
        </button>
      </div>
    </motion.div>
  );
}

// ── Active-role switcher ─────────────────────────────────────────────────────

function ActiveRoleSwitcher() {
  const { t, i18n } = useTranslation(["profile", "common"]);
  const navigate = useNavigate();
  const isRtl = i18n.language.startsWith("ar");
  const user = useAuthStore((s) => s.user);

  const switchRoleMut = useMutation({
    mutationFn: (targetRole: string) => authApi.switchRole(targetRole),
    onSuccess: (res) => {
      const next = applyAuthSession(res);
      toast.success(
        t("common:roleSwitch.success", "Active role updated to {{role}}.", {
          role: t(`common:roles.${next.activeRole}`, next.activeRole ?? ""),
        }),
      );
      navigate(postAuthPath(next), { replace: true });
    },
    onError: (err) =>
      toast.error(
        apiErrorMessage(err, t("common:roleSwitch.error", "Could not switch role.")),
      ),
  });

  if (!user) return null;
  const activeRole = user.activeRole ?? user.roles[0] ?? null;
  if (!activeRole) return null;

  const switchableRoles = user.roles.filter((r) =>
    (SWITCHABLE_ROLES as readonly string[]).includes(r),
  );
  const hasMultiple = switchableRoles.length > 1;
  const activeLabel = t(`common:roles.${activeRole}`, activeRole);

  if (!hasMultiple) {
    return (
      <span
        className="badge badge-brand"
        aria-label={t("common:roleSwitch.activeBadge", "Active: {{role}}", { role: activeLabel })}
      >
        {activeLabel}
      </span>
    );
  }

  const targets = switchableRoles.filter((r) => r !== activeRole);

  return (
    <DropdownMenu.Root dir={isRtl ? "rtl" : "ltr"}>
      <DropdownMenu.Trigger asChild>
        <button
          type="button"
          disabled={switchRoleMut.isPending}
          aria-label={t("profile:role.switcherAria", "Switch active role")}
          className="inline-flex items-center gap-1.5 rounded-full border border-brand-200 bg-brand-50 px-3 py-1 text-xs font-semibold text-brand-700 transition-colors hover:bg-brand-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {switchRoleMut.isPending ? (
            <Loader2 aria-hidden className="size-3 animate-spin" />
          ) : (
            <Repeat aria-hidden className="size-3" />
          )}
          <span>{activeLabel}</span>
          <ChevronDown aria-hidden className="size-3" />
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content
          side="bottom"
          align="start"
          sideOffset={6}
          collisionPadding={16}
          className="z-50 min-w-[200px] overflow-hidden rounded-md border border-border-subtle bg-bg-elevated p-1 text-sm text-text-primary shadow-lg text-start"
        >
          <DropdownMenu.Label className="px-3 pb-1 pt-1 text-[11px] uppercase tracking-wide text-text-tertiary">
            {t("common:roleSwitch.heading", "Switch role")}
          </DropdownMenu.Label>
          {targets.map((role) => (
            <DropdownMenu.Item
              key={role}
              disabled={switchRoleMut.isPending}
              onSelect={(e) => {
                e.preventDefault();
                switchRoleMut.mutate(role);
              }}
              aria-label={t("common:roleSwitch.optionAria", "Switch to {{role}}", {
                role: t(`common:roles.${role}`, role),
              })}
              className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start outline-none data-[disabled]:cursor-not-allowed data-[disabled]:opacity-50 data-[highlighted]:bg-bg-subtle"
            >
              <Repeat aria-hidden className="size-4 text-text-tertiary" />
              {t("common:roleSwitch.option", "Switch to {{role}}", {
                role: t(`common:roles.${role}`, role),
              })}
            </DropdownMenu.Item>
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}

// ── Become-a-Consultant card ─────────────────────────────────────────────────

function ConsultantUpgradeCard({
  onRequest,
  submitted,
}: {
  onRequest: () => void;
  submitted: boolean;
}) {
  const { t } = useTranslation(["profile"]);
  return (
    <motion.section
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
      className="card-premium relative overflow-hidden border-brand-200 bg-gradient-to-br from-brand-50 to-bg-elevated p-6 sm:p-8"
    >
      <div className="flex items-start gap-4">
        <span className="mt-0.5 flex size-10 shrink-0 items-center justify-center rounded-lg bg-brand-100 text-brand-600">
          <Sparkles aria-hidden className="size-5" />
        </span>
        <div className="min-w-0 flex-1">
          <h2 className="text-base font-semibold text-text-primary">
            {t("profile:upgrade.cardTitle", "Become a Consultant")}
          </h2>
          <p className="mt-1 text-sm text-text-secondary">
            {t(
              "profile:upgrade.cardDescription",
              "Share your experience with other students and offer consultation sessions.",
            )}
          </p>
          <button
            type="button"
            onClick={onRequest}
            disabled={submitted}
            className="mt-4 inline-flex items-center gap-2 rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <Sparkles aria-hidden className="size-4" />
            {submitted
              ? t("profile:upgrade.pendingButton", "Upgrade request pending")
              : t("profile:upgrade.cardCta", "Request Consultant Upgrade")}
          </button>
          {submitted && (
            <p className="mt-2 text-xs text-text-tertiary">
              {t(
                "profile:upgrade.pendingHint",
                "An admin will review your request and you'll be notified once it's approved.",
              )}
            </p>
          )}
        </div>
      </div>
    </motion.section>
  );
}

// ── Sidebar navigation ───────────────────────────────────────────────────────

interface SidebarEntry {
  id: string;
  label: string;
  icon: ReactNode;
}

function ProfileSidebar({
  items,
  activeId,
  onSelect,
}: {
  items: SidebarEntry[];
  activeId: string | null;
  onSelect: (id: string) => void;
}) {
  return (
    <nav className="hidden lg:sticky lg:top-24 lg:block">
      <ul className="space-y-1">
        {items.map((entry) => {
          const active = entry.id === activeId;
          return (
            <li key={entry.id}>
              <button
                type="button"
                onClick={() => onSelect(entry.id)}
                className={cn(
                  "group flex w-full items-center gap-2 rounded-lg px-3 py-2 text-start text-sm transition-colors",
                  active
                    ? "bg-brand-50 font-semibold text-brand-700"
                    : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
                )}
              >
                <span
                  className={cn(
                    "flex size-7 items-center justify-center rounded-md transition-colors",
                    active
                      ? "bg-brand-100 text-brand-600"
                      : "bg-bg-subtle text-text-tertiary group-hover:text-text-secondary",
                  )}
                >
                  {entry.icon}
                </span>
                {entry.label}
              </button>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function Profile() {
  const { t, i18n } = useTranslation(["profile", "common"]);
  const qc = useQueryClient();
  const activeRole = useAuthStore((s) => s.user?.activeRole ?? null);
  // Master payments switch — when off, the consultant session fee field is
  // hidden because the server will force the stored value to 0 anyway.
  const paymentsEnabled = usePaymentsEnabled();

  const {
    data: profile,
    isLoading,
    isError,
  } = useQuery<UserProfile>({
    queryKey: PROFILE_KEY,
    queryFn: () => profileApi.getMine(),
  });

  // Populate the editable form from the fetched profile, re-syncing when a
  // refetch brings fresh data — adjusting state during render, not in an effect.
  const [form, setForm] = useState<FormState | null>(null);
  const [original, setOriginal] = useState<FormState | null>(null);
  const [syncedProfile, setSyncedProfile] = useState<UserProfile | null>(null);
  if (profile && profile !== syncedProfile) {
    setSyncedProfile(profile);
    const f = toForm(profile);
    setForm(f);
    setOriginal(f);
  }

  // The photo-serve URL is stable per user, so after a successful upload we
  // bump this token to cache-bust the <img> and show the new photo at once.
  const [photoVersion, setPhotoVersion] = useState<number | null>(null);
  const [photoFailed, setPhotoFailed] = useState(false);

  const [activeSection, setActiveSection] = useState<string | null>("personal");

  // Consultant upgrade UI state — modal open + locally-tracked submission so
  // the card switches to a "pending" hint after a successful request, even
  // though the auth store still shows the user as a Student.
  const [upgradeModalOpen, setUpgradeModalOpen] = useState(false);
  const [upgradeSubmitted, setUpgradeSubmitted] = useState(false);
  const authUser = useAuthStore((s) => s.user);
  const userRoles = authUser?.roles ?? [];
  const showConsultantUpgrade =
    !!authUser &&
    userRoles.includes("Student") &&
    !userRoles.includes("Consultant") &&
    authUser.accountStatus === "Active";

  const syncAuthUser = (updated: UserProfile) => {
    const current = useAuthStore.getState().user;
    if (current) {
      useAuthStore.getState().setUser({
        ...current,
        firstName: updated.firstName,
        lastName: updated.lastName,
        fullName: updated.fullName,
        profileImageUrl: updated.profileImageUrl,
      });
    }
  };

  const updateMut = useMutation({
    mutationFn: (payload: UpdateProfileRequest) => profileApi.update(payload),
    onSuccess: (updated) => {
      qc.setQueryData(PROFILE_KEY, updated);
      syncAuthUser(updated);
      toast.success(t("profile:saved"));
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("profile:error"))),
  });

  const photoMut = useMutation({
    mutationFn: (file: File) => profileApi.uploadPhoto(file),
    onSuccess: (url) => {
      const fresh = qc.getQueryData<UserProfile>(PROFILE_KEY);
      if (fresh) {
        const updated = { ...fresh, profileImageUrl: url };
        qc.setQueryData(PROFILE_KEY, updated);
        syncAuthUser(updated);
      }
      setPhotoVersion(Date.now());
      setPhotoFailed(false);
      toast.success(t("profile:photo.saved"));
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("profile:photo.error"))),
  });

  // Sections list for the sidebar — adapts to the active role
  const sections = useMemo<SidebarEntry[]>(() => {
    const items: SidebarEntry[] = [
      {
        id: "personal",
        label: t("profile:sections.personal"),
        icon: <UserIcon aria-hidden className="size-4" />,
      },
      {
        id: "about",
        label: t("profile:sections.about"),
        icon: <Sparkles aria-hidden className="size-4" />,
      },
      {
        id: "links",
        label: t("profile:sections.links"),
        icon: <LinkIcon aria-hidden className="size-4" />,
      },
    ];
    if (activeRole === "Student") {
      items.push({
        id: "academic",
        label: t("profile:sections.academic"),
        icon: <GraduationCap aria-hidden className="size-4" />,
      });
    }
    if (activeRole === "ScholarshipProvider") {
      items.push({
        id: "organization",
        label: t("profile:sections.organization"),
        icon: <Briefcase aria-hidden className="size-4" />,
      });
    }
    if (activeRole === "Consultant") {
      items.push({
        id: "consultant",
        label: t("profile:sections.consultant"),
        icon: <Briefcase aria-hidden className="size-4" />,
      });
    }
    items.push({
      id: "security",
      label: t("profile:sections.security"),
      icon: <ShieldCheck aria-hidden className="size-4" />,
    });
    return items;
  }, [activeRole, t]);

  // Spy on section visibility to update sidebar highlight
  useEffect(() => {
    if (!form) return;
    const targets = sections
      .map((s) => document.getElementById(s.id))
      .filter((el): el is HTMLElement => el !== null);
    if (targets.length === 0) return;
    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((e) => e.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
        if (visible[0]) setActiveSection(visible[0].target.id);
      },
      { rootMargin: "-30% 0px -55% 0px", threshold: 0 },
    );
    targets.forEach((el) => observer.observe(el));
    return () => observer.disconnect();
  }, [sections, form]);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-10">
        <div className="skeleton mb-3 h-10 w-48" />
        <div className="skeleton mb-8 h-5 w-72" />
        <div className="skeleton h-40 w-full rounded-xl" />
      </div>
    );
  }

  if (isError || !profile || !form || !original) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-10">
        <div className="card-premium border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
          {t("profile:loadError")}
        </div>
      </div>
    );
  }

  const set = <K extends keyof FormState>(key: K, value: string) =>
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev));

  const setTags = (
    key: "expertiseTags" | "languages" | "preferredCountries" | "preferredFields",
    tags: string[],
  ) => setForm((prev) => (prev ? { ...prev, [key]: tags } : prev));

  const dirty = !shallowEqual(form, original);

  // Inline validation hints — match the backend rules so a Save attempt that
  // would have failed server-side fails immediately in the UI instead.
  const linkedInError =
    form.linkedInUrl && !isValidHttpUrl(form.linkedInUrl)
      ? t("profile:validation.url")
      : null;
  const websiteError =
    form.websiteUrl && !isValidHttpUrl(form.websiteUrl)
      ? t("profile:validation.url")
      : null;
  const orgWebsiteError =
    form.organizationWebsite && !isValidHttpUrl(form.organizationWebsite)
      ? t("profile:validation.url")
      : null;

  const gpaNumber = form.gpa.trim().length > 0 ? Number(form.gpa) : null;
  const gpaMax =
    form.gpaScale in GPA_MAX ? GPA_MAX[form.gpaScale as GpaScale] : null;
  const gpaError =
    gpaNumber == null
      ? null
      : !form.gpaScale
        ? t("profile:validation.gpaScaleRequired")
        : gpaMax != null && (gpaNumber < 0 || gpaNumber > gpaMax)
          ? t("profile:validation.gpaRange", { max: gpaMax })
          : null;

  const feeNumber =
    form.sessionFeeUsd.trim().length > 0 ? Number(form.sessionFeeUsd) : null;
  const feeError =
    feeNumber == null
      ? null
      : !Number.isFinite(feeNumber) || feeNumber < 0
        ? t("profile:validation.sessionFeePositive")
        : Math.round(feeNumber * 100) !== feeNumber * 100
          ? t("profile:validation.sessionFeeDecimals")
          : null;

  const onSave = () => {
    // Surface the first *specific* reason a save is blocked. Falling back to
    // the generic "couldn't save your profile" toast hid which field was at
    // fault — and the offending field is often in a section that isn't on
    // screen (e.g. a stale session-fee), so the save looked like an
    // unexplained server error.
    const firstError =
      linkedInError ?? websiteError ?? orgWebsiteError ?? gpaError ?? feeError;
    if (firstError) {
      toast.error(firstError);
      return;
    }
    updateMut.mutate(toRequest(form), {
      onSuccess: () => setOriginal(form),
    });
  };

  const onReset = () => setForm(original);

  const onScrollTo = (id: string) => {
    const el = document.getElementById(id);
    if (el) el.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const pct = Math.max(0, Math.min(100, profile.completenessPercent));

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      {/* Page title */}
      <motion.div
        initial={{ opacity: 0, y: -8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
        className="mb-8"
      >
        <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
          {t("profile:title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("profile:subtitle")}
        </p>
      </motion.div>

      {/* Hero header: photo + identity + completeness */}
      <motion.section
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.36, ease: [0.22, 1, 0.36, 1] }}
        className="card-premium relative mb-6 overflow-hidden p-6 sm:p-8"
      >
        <div
          aria-hidden
          className="absolute inset-0 -z-0 opacity-60"
          style={{
            background:
              "radial-gradient(at 0% 0%, rgba(23,96,240,0.10) 0px, transparent 50%)," +
              "radial-gradient(at 100% 100%, rgba(91,147,255,0.06) 0px, transparent 55%)",
          }}
        />
        <div className="relative flex flex-col gap-6 sm:flex-row sm:items-center">
          {/* Avatar */}
          <div className="relative shrink-0 self-start sm:self-center">
            {profile.profileImageUrl && !photoFailed ? (
              <img
                src={
                  photoVersion != null
                    ? `${userPhotoUrl(profile.userId)}?v=${photoVersion}`
                    : userPhotoUrl(profile.userId)
                }
                alt={profile.fullName}
                onError={() => setPhotoFailed(true)}
                className="size-24 rounded-full object-cover ring-4 ring-bg-elevated"
              />
            ) : (
              <div className="flex size-24 items-center justify-center rounded-full bg-gradient-to-br from-brand-500 to-brand-700 text-3xl font-bold text-white ring-4 ring-bg-elevated">
                {initials(profile.firstName, profile.lastName)}
              </div>
            )}
            {profile.accountStatus === "Active" && (
              <span
                aria-hidden
                className="absolute -end-1 -bottom-1 flex size-7 items-center justify-center rounded-full bg-success-500 ring-4 ring-bg-elevated"
              >
                <BadgeCheck className="size-4 text-white" />
              </span>
            )}
          </div>

          {/* Identity */}
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="truncate text-xl font-semibold text-text-primary">
                {profile.fullName}
              </h2>
              <ActiveRoleSwitcher />
              <span
                className={cn(
                  "badge",
                  profile.accountStatus === "Active"
                    ? "badge-success"
                    : "badge-neutral",
                )}
              >
                {profile.accountStatus}
              </span>
            </div>
            <p className="mt-1 inline-flex items-center gap-1.5 text-sm text-text-secondary">
              <AtSign aria-hidden className="size-3.5" />
              {profile.email}
            </p>
            {profile.biography && (
              <p className="mt-3 line-clamp-2 max-w-xl text-sm text-text-secondary">
                {profile.biography}
              </p>
            )}
          </div>

          {/* Completeness */}
          <div className="w-full shrink-0 sm:w-56">
            <div className="mb-2 flex items-center justify-between text-xs">
              <span className="font-medium text-text-secondary">
                {t("profile:completenessLabel")}
              </span>
              <span className="font-semibold text-brand-700">{pct}%</span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-bg-subtle">
              <motion.div
                initial={{ width: 0 }}
                animate={{ width: `${pct}%` }}
                transition={{ duration: 0.6, ease: [0.22, 1, 0.36, 1] }}
                className="h-full rounded-full bg-gradient-to-r from-brand-500 to-brand-700"
              />
            </div>
            <p className="mt-1.5 text-xs text-text-tertiary">
              {pct === 100
                ? t("profile:completenessDone")
                : t("profile:completenessHint")}
            </p>
          </div>
        </div>
      </motion.section>

      {/* Consultant upgrade — Students-only, Active accounts only, not yet Consultants */}
      {showConsultantUpgrade && (
        <div className="mb-6">
          <ConsultantUpgradeCard
            submitted={upgradeSubmitted}
            onRequest={() => setUpgradeModalOpen(true)}
          />
        </div>
      )}

      {/* Two-column layout: sticky sidebar + content */}
      <div className="grid gap-8 lg:grid-cols-[200px_minmax(0,1fr)]">
        <ProfileSidebar
          items={sections}
          activeId={activeSection}
          onSelect={onScrollTo}
        />

        <div className="space-y-6">
          {/* Personal */}
          <SectionCard
            id="personal"
            icon={<UserIcon aria-hidden className="size-5" />}
            title={t("profile:sections.personal")}
            description={t("profile:sections.personalDesc")}
          >
            <FieldRow
              label={t("profile:fields.photo")}
              description={t("profile:fields.photoDesc")}
            >
              <AvatarUploader
                profile={profile}
                photoVersion={photoVersion}
                photoFailed={photoFailed}
                setPhotoFailed={setPhotoFailed}
                onSelectFile={(file) => photoMut.mutate(file)}
                isUploading={photoMut.isPending}
              />
            </FieldRow>
            <FieldRow label={t("profile:fields.name")}>
              <FieldGrid>
                <input
                  id="firstName"
                  className="input-premium"
                  placeholder={t("profile:fields.firstName")}
                  value={form.firstName}
                  onChange={(e) => set("firstName", e.target.value)}
                />
                <input
                  id="lastName"
                  className="input-premium"
                  placeholder={t("profile:fields.lastName")}
                  value={form.lastName}
                  onChange={(e) => set("lastName", e.target.value)}
                />
              </FieldGrid>
            </FieldRow>
            <FieldRow
              label={t("profile:fields.email")}
              description={t("profile:fields.emailDesc")}
            >
              <input
                className="input-premium cursor-not-allowed opacity-60"
                value={profile.email}
                disabled
              />
            </FieldRow>
            {/*
              Date of Birth and Nationality are individual-only fields. A
              ScholarshipProvider account has no DOB and no nationality (it has an
              organization country, a tax number, etc — already covered by
              the role-specific ScholarshipProvider section). Admins don't either. Hide
              both rows for those roles so the form stops asking a business
              for a date of birth.
            */}
            {(activeRole === "Student" || activeRole === "Consultant") && (
              <>
                <FieldRow label={t("profile:fields.dateOfBirth")}>
                  <DatePicker
                    value={form.dateOfBirth}
                    onChange={(v) => set("dateOfBirth", v)}
                    max={new Date().toISOString().slice(0, 10)}
                  />
                </FieldRow>
                <FieldRow label={t("profile:fields.nationality")}>
                  <select
                    className="input-premium"
                    value={form.nationality}
                    onChange={(e) => set("nationality", e.target.value)}
                  >
                    <option value="">{t("profile:countryOption.none")}</option>
                    {COUNTRIES.map((c) => (
                      <option key={c} value={c}>
                        {countryLabel(c, i18n.language)}
                      </option>
                    ))}
                  </select>
                </FieldRow>
              </>
            )}
            <FieldRow label={t("profile:fields.countryOfResidence")}>
              <select
                className="input-premium"
                value={form.countryOfResidence}
                onChange={(e) => set("countryOfResidence", e.target.value)}
              >
                <option value="">{t("profile:countryOption.none")}</option>
                {COUNTRIES.map((c) => (
                  <option key={c} value={c}>
                    {countryLabel(c, i18n.language)}
                  </option>
                ))}
              </select>
            </FieldRow>
            <FieldRow
              label={t("profile:fields.preferredLanguage")}
              description={t("profile:fields.preferredLanguageDesc")}
            >
              <select
                className="input-premium"
                value={form.preferredLanguage}
                onChange={(e) => set("preferredLanguage", e.target.value)}
              >
                <option value="">{t("profile:languageOption.none")}</option>
                <option value="en">
                  {t("profile:languageOption.en")}
                </option>
                <option value="ar">
                  {t("profile:languageOption.ar")}
                </option>
              </select>
            </FieldRow>
          </SectionCard>

          {/* About / Biography */}
          <SectionCard
            id="about"
            icon={<Sparkles aria-hidden className="size-5" />}
            title={t("profile:sections.about")}
            description={t("profile:sections.aboutDesc")}
          >
            <FieldRow
              label={t("profile:fields.biography")}
              description={t("profile:fields.biographyDesc")}
            >
              <div>
                <textarea
                  rows={5}
                  maxLength={500}
                  className="input-premium font-normal"
                  placeholder={t("profile:fields.biographyPlaceholder")}
                  value={form.biography}
                  onChange={(e) => set("biography", e.target.value)}
                />
                <div className="mt-1.5 flex items-center justify-between text-xs">
                  <p className="text-text-tertiary">
                    {t("profile:fields.biographyHelp")}
                  </p>
                  <p className="text-text-tertiary">
                    {form.biography.length}/500
                  </p>
                </div>
              </div>
            </FieldRow>
          </SectionCard>

          {/* Links */}
          <SectionCard
            id="links"
            icon={<LinkIcon aria-hidden className="size-5" />}
            title={t("profile:sections.links")}
            description={t("profile:sections.linksDesc")}
          >
            <FieldRow label={t("profile:fields.linkedInUrl")}>
              <div>
                <input
                  type="url"
                  className="input-premium"
                  placeholder="https://linkedin.com/in/your-handle"
                  value={form.linkedInUrl}
                  onChange={(e) => set("linkedInUrl", e.target.value)}
                  aria-invalid={linkedInError ? true : undefined}
                />
                {linkedInError && (
                  <p className="mt-1.5 text-xs font-medium text-danger-500">
                    {linkedInError}
                  </p>
                )}
              </div>
            </FieldRow>
            <FieldRow label={t("profile:fields.websiteUrl")}>
              <div>
                <input
                  type="url"
                  className="input-premium"
                  placeholder="https://example.com"
                  value={form.websiteUrl}
                  onChange={(e) => set("websiteUrl", e.target.value)}
                  aria-invalid={websiteError ? true : undefined}
                />
                {websiteError && (
                  <p className="mt-1.5 text-xs font-medium text-danger-500">
                    {websiteError}
                  </p>
                )}
              </div>
            </FieldRow>
          </SectionCard>

          {/* Academic — students */}
          {activeRole === "Student" && (
            <SectionCard
              id="academic"
              icon={<GraduationCap aria-hidden className="size-5" />}
              title={t("profile:sections.academic")}
              description={t("profile:sections.academicDesc")}
            >
              <FieldRow label={t("profile:fields.academicLevel")}>
                <select
                  className="input-premium"
                  value={form.academicLevel}
                  onChange={(e) => set("academicLevel", e.target.value)}
                >
                  <option value="">
                    {t("profile:academicLevelOption.none")}
                  </option>
                  {ACADEMIC_LEVELS.map((lvl) => (
                    <option key={lvl} value={lvl}>
                      {t(`profile:academicLevelOption.${lvl}`)}
                    </option>
                  ))}
                </select>
              </FieldRow>
              <FieldRow label={t("profile:fields.fieldOfStudy")}>
                <input
                  className="input-premium"
                  value={form.fieldOfStudy}
                  onChange={(e) => set("fieldOfStudy", e.target.value)}
                  placeholder={t("profile:fields.fieldOfStudyPlaceholder")}
                />
              </FieldRow>
              <FieldRow label={t("profile:fields.currentInstitution")}>
                <input
                  className="input-premium"
                  value={form.currentInstitution}
                  onChange={(e) =>
                    set("currentInstitution", e.target.value)
                  }
                  placeholder={t(
                    "profile:fields.currentInstitutionPlaceholder",
                  )}
                />
              </FieldRow>
              <FieldRow
                label={t("profile:fields.gpa")}
                description={t("profile:fields.gpaDesc")}
              >
                <div>
                  <FieldGrid>
                    <select
                      className="input-premium"
                      value={form.gpaScale}
                      onChange={(e) => {
                        set("gpaScale", e.target.value);
                        const newMax = GPA_MAX[e.target.value as GpaScale];
                        if (newMax !== undefined && form.gpa !== "") {
                          const val = Number(form.gpa);
                          if (val > newMax) set("gpa", "");
                        }
                      }}
                    >
                      <option value="">
                        {t("profile:gpaScaleOption.none")}
                      </option>
                      {GPA_SCALES.map((scale) => (
                        <option key={scale} value={scale}>
                          {t(`profile:gpaScaleOption.${scale}`)}
                        </option>
                      ))}
                    </select>
                    <input
                      type="number"
                      step="0.01"
                      min={0}
                      max={GPA_MAX[form.gpaScale as GpaScale] ?? undefined}
                      className="input-premium"
                      value={form.gpa}
                      onChange={(e) => set("gpa", e.target.value)}
                      placeholder={
                        form.gpaScale in GPA_MAX
                          ? `0 – ${GPA_MAX[form.gpaScale as GpaScale]}`
                          : undefined
                      }
                      aria-invalid={gpaError ? true : undefined}
                    />
                  </FieldGrid>
                  {gpaError && (
                    <p className="mt-1.5 text-xs font-medium text-danger-500">
                      {gpaError}
                    </p>
                  )}
                </div>
              </FieldRow>

              {/*
                Matching inputs — where the student WANTS to study and which
                fields interest them. These feed the AI recommender (it overlaps
                them against each scholarship's target countries / fields).
                Distinct from nationality / country of residence above, which
                describe where the student is FROM (used for eligibility, not
                for ranking destinations).
              */}
              <FieldRow
                label={t("profile:fields.preferredCountries")}
                description={t("profile:fields.preferredCountriesDesc")}
              >
                <TagInput
                  value={form.preferredCountries}
                  onChange={(tags) => setTags("preferredCountries", tags)}
                  placeholder={t("profile:fields.preferredCountriesPlaceholder")}
                  max={15}
                />
              </FieldRow>
              <FieldRow
                label={t("profile:fields.preferredFields")}
                description={t("profile:fields.preferredFieldsDesc")}
              >
                <TagInput
                  value={form.preferredFields}
                  onChange={(tags) => setTags("preferredFields", tags)}
                  placeholder={t("profile:fields.preferredFieldsPlaceholder")}
                  max={15}
                />
              </FieldRow>
            </SectionCard>
          )}

          {/* Organization — companies */}
          {activeRole === "ScholarshipProvider" && (
            <SectionCard
              id="organization"
              icon={<Briefcase aria-hidden className="size-5" />}
              title={t("profile:sections.organization")}
              description={t("profile:sections.organizationDesc")}
            >
              <FieldRow label={t("profile:fields.organizationLegalName")}>
                <input
                  className="input-premium"
                  value={form.organizationLegalName}
                  onChange={(e) =>
                    set("organizationLegalName", e.target.value)
                  }
                />
              </FieldRow>
              <FieldRow label={t("profile:fields.organizationWebsite")}>
                <div>
                  <input
                    type="url"
                    className="input-premium"
                    value={form.organizationWebsite}
                    onChange={(e) =>
                      set("organizationWebsite", e.target.value)
                    }
                    aria-invalid={orgWebsiteError ? true : undefined}
                  />
                  {orgWebsiteError && (
                    <p className="mt-1.5 text-xs font-medium text-danger-500">
                      {orgWebsiteError}
                    </p>
                  )}
                </div>
              </FieldRow>
              {profile.organizationVerificationStatus && (
                <FieldRow label={t("profile:fields.verificationStatus")}>
                  <div className="flex items-center gap-2">
                    <span
                      className={cn(
                        "badge",
                        profile.organizationVerificationStatus === "Verified"
                          ? "badge-success"
                          : "badge-warning",
                      )}
                    >
                      {profile.organizationVerificationStatus === "Verified" ? (
                        <CheckCircle2 aria-hidden className="size-3.5" />
                      ) : null}
                      {profile.organizationVerificationStatus}
                    </span>
                  </div>
                </FieldRow>
              )}
            </SectionCard>
          )}

          {/* Consultant settings */}
          {activeRole === "Consultant" && (
            <SectionCard
              id="consultant"
              icon={<Briefcase aria-hidden className="size-5" />}
              title={t("profile:sections.consultant")}
              description={t("profile:sections.consultantDesc")}
            >
              <FieldRow label={t("profile:fields.professionalTitle")}>
                <input
                  className="input-premium"
                  value={form.professionalTitle}
                  onChange={(e) => set("professionalTitle", e.target.value)}
                  placeholder={t("profile:fields.professionalTitlePlaceholder")}
                  maxLength={150}
                />
              </FieldRow>
              <FieldRow label={t("profile:fields.yearsOfExperience")}>
                <input
                  type="number"
                  min={0}
                  max={70}
                  step={1}
                  className="input-premium"
                  value={form.yearsOfExperience}
                  onChange={(e) => set("yearsOfExperience", e.target.value)}
                />
              </FieldRow>
              <FieldRow
                label={t("profile:fields.expertiseTags")}
                description={t("profile:fields.expertiseTagsDesc")}
              >
                <TagInput
                  value={form.expertiseTags}
                  onChange={(tags) => setTags("expertiseTags", tags)}
                  placeholder={t("profile:fields.expertiseTagsPlaceholder")}
                  max={20}
                />
              </FieldRow>
              <FieldRow
                label={t("profile:fields.languages")}
                description={t("profile:fields.languagesDesc")}
              >
                <TagInput
                  value={form.languages}
                  onChange={(tags) => setTags("languages", tags)}
                  placeholder={t("profile:fields.languagesPlaceholder")}
                  max={20}
                />
              </FieldRow>
              <FieldRow label={t("profile:fields.timezone")}>
                <input
                  className="input-premium"
                  value={form.timezone}
                  onChange={(e) => set("timezone", e.target.value)}
                  placeholder={t("profile:fields.timezonePlaceholder")}
                  maxLength={100}
                />
              </FieldRow>
              {paymentsEnabled ? (
                <FieldRow
                  label={t("profile:fields.sessionFeeUsd")}
                  description={t("profile:fields.sessionFeeUsdDesc")}
                >
                  <div>
                    <div className="relative">
                      <span className="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 text-sm text-text-tertiary">
                        $
                      </span>
                      <input
                        type="number"
                        step="0.01"
                        min={0}
                        className="input-premium ps-7"
                        value={form.sessionFeeUsd}
                        onChange={(e) => set("sessionFeeUsd", e.target.value)}
                        aria-invalid={feeError ? true : undefined}
                      />
                    </div>
                    {feeError && (
                      <p className="mt-1.5 text-xs font-medium text-danger-500">
                        {feeError}
                      </p>
                    )}
                  </div>
                </FieldRow>
              ) : (
                // Payments disabled — the field is hidden because the server
                // will overwrite the stored fee with 0 on the next save.
                <FieldRow
                  label={t("profile:fields.sessionFeeUsd")}
                  description={t("profile:paymentsDisabledNotice")}
                >
                  <p className="text-sm font-medium text-brand-600">
                    {t("scholarships:freeListing")}
                  </p>
                </FieldRow>
              )}
              <FieldRow label={t("profile:fields.sessionDurationMinutes")}>
                <select
                  className="input-premium"
                  value={form.sessionDurationMinutes}
                  onChange={(e) =>
                    set("sessionDurationMinutes", e.target.value)
                  }
                >
                  <option value="">
                    {t("profile:sessionDurationOption.none")}
                  </option>
                  {SESSION_DURATIONS.map((d) => (
                    <option key={d} value={d}>
                      {t(`profile:sessionDurationOption.${d}`)}
                    </option>
                  ))}
                </select>
              </FieldRow>
            </SectionCard>
          )}

          {/* Security — change password (hidden for SSO-only accounts; CR-PROF-06) */}
          <ChangePasswordSection hasPassword={profile.hasPasswordCredential} />

          {/* Danger zone link to data privacy */}
          <motion.section
            initial={{ opacity: 0, y: 8 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-80px" }}
            transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
            className="card-premium border-danger-200 bg-danger-50/40 p-6 sm:p-8"
          >
            <div className="flex items-start gap-3">
              <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-danger-50 text-danger-500">
                <Trash2 aria-hidden className="size-5" />
              </span>
              <div className="min-w-0 flex-1">
                <h2 className="text-base font-semibold text-text-primary">
                  {t("profile:sections.dangerZone")}
                </h2>
                <p className="mt-0.5 text-sm text-text-secondary">
                  {t("profile:sections.dangerZoneDesc")}
                </p>
                <a
                  href="/profile/privacy"
                  className="mt-3 inline-flex items-center gap-1.5 text-sm font-semibold text-danger-500 hover:underline"
                >
                  {t("profile:sections.dangerZoneCta")}
                  <ArrowRight aria-hidden className="size-3.5 rtl:rotate-180" />
                </a>
              </div>
            </div>
          </motion.section>
        </div>
      </div>

      {/* Sticky save bar */}
      <SaveBar
        dirty={dirty}
        saving={updateMut.isPending}
        onSave={onSave}
        onReset={onReset}
      />

      {showConsultantUpgrade && (
        <ConsultantUpgradeModal
          isOpen={upgradeModalOpen}
          onOpenChange={setUpgradeModalOpen}
          onSubmitted={() => setUpgradeSubmitted(true)}
        />
      )}
    </div>
  );
}
