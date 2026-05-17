import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useRef, useState, type ReactNode } from "react";
import { toast } from "sonner";
import { Camera, Loader2, Save } from "lucide-react";
import {
  profileApi,
  type UserProfile,
  type UpdateProfileRequest,
} from "@/services/api/profile";
import { useAuthStore } from "@/stores/authStore";
import { userPhotoUrl } from "@/lib/userPhoto";

const PROFILE_KEY = ["profile", "me"] as const;

const inputClass =
  "w-full rounded-md border border-border-default bg-bg-elevated p-2 text-sm focus:border-brand-500 focus:outline-none";

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
  };
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
  };
}

function initials(first: string, last: string): string {
  const value = `${first.charAt(0)}${last.charAt(0)}`.toUpperCase();
  return value.length > 0 ? value : "?";
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="mb-6 rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
      <h2 className="mb-4 text-lg font-semibold">{title}</h2>
      <div className="grid gap-4 sm:grid-cols-2">{children}</div>
    </section>
  );
}

function Field({
  label,
  children,
  full,
}: {
  label: string;
  children: ReactNode;
  full?: boolean;
}) {
  return (
    <label className={`block text-sm ${full ? "sm:col-span-2" : ""}`}>
      <span className="mb-1 block font-medium text-text-secondary">{label}</span>
      {children}
    </label>
  );
}

export function Profile() {
  const { t } = useTranslation(["profile", "common"]);
  const qc = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const activeRole = useAuthStore((s) => s.user?.activeRole ?? null);

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
  const [syncedProfile, setSyncedProfile] = useState<UserProfile | null>(null);
  if (profile && profile !== syncedProfile) {
    setSyncedProfile(profile);
    setForm(toForm(profile));
  }

  // The photo-serve URL is stable per user, so after a successful upload we
  // bump this token to cache-bust the <img> and show the new photo at once.
  const [photoVersion, setPhotoVersion] = useState<number | null>(null);
  // Set when the photo <img> fails to load (e.g. the user has no photo, 404) —
  // we then fall back to the initials placeholder.
  const [photoFailed, setPhotoFailed] = useState(false);

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
    onError: () => toast.error(t("profile:error")),
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
      // Cache-bust the serve URL so the new photo appears immediately, and
      // clear any earlier load failure so the fresh photo gets to render.
      setPhotoVersion(Date.now());
      setPhotoFailed(false);
      toast.success(t("profile:photo.saved"));
    },
    onError: () => toast.error(t("profile:photo.error")),
  });

  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-10 text-sm text-text-tertiary">
        {t("profile:loading")}
      </div>
    );
  }

  if (isError || !profile || !form) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-10 text-sm text-danger-500">
        {t("profile:loadError")}
      </div>
    );
  }

  const set = <K extends keyof FormState>(key: K, value: string) =>
    setForm((prev) => (prev ? { ...prev, [key]: value } : prev));

  const onPhotoSelected = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) photoMut.mutate(file);
    e.target.value = "";
  };

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMut.mutate(toRequest(form));
  };

  const pct = Math.max(0, Math.min(100, profile.completenessPercent));

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="mb-2 text-3xl">{t("profile:title")}</h1>
      <p className="mb-8 text-text-secondary">{t("profile:subtitle")}</p>

      {/* Header card: photo + identity + completeness */}
      <section className="mb-6 rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
        <div className="flex items-center gap-4">
          <div className="relative shrink-0">
            {profile.profileImageUrl && !photoFailed ? (
              <img
                src={
                  photoVersion != null
                    ? `${userPhotoUrl(profile.userId)}?v=${photoVersion}`
                    : userPhotoUrl(profile.userId)
                }
                alt={profile.fullName}
                onError={() => setPhotoFailed(true)}
                className="size-20 rounded-full object-cover"
              />
            ) : (
              <div className="flex size-20 items-center justify-center rounded-full bg-brand-100 text-2xl font-semibold text-brand-600">
                {initials(profile.firstName, profile.lastName)}
              </div>
            )}
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={photoMut.isPending}
              aria-label={t("profile:photo.change")}
              className="absolute -bottom-1 -end-1 flex size-8 items-center justify-center rounded-full border border-border-subtle bg-bg-elevated shadow-xs hover:bg-bg-subtle disabled:opacity-50"
            >
              {photoMut.isPending ? (
                <Loader2 aria-hidden className="size-4 animate-spin" />
              ) : (
                <Camera aria-hidden className="size-4 text-brand-500" />
              )}
            </button>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/png,image/jpeg,image/webp"
              onChange={onPhotoSelected}
              className="hidden"
            />
          </div>

          <div className="min-w-0">
            <p className="truncate text-xl font-semibold">{profile.fullName}</p>
            <p className="truncate text-sm text-text-secondary">{profile.email}</p>
            <span className="mt-1 inline-block rounded-full bg-bg-subtle px-2 py-0.5 text-xs text-text-secondary">
              {profile.accountStatus}
            </span>
          </div>
        </div>

        <p className="mt-4 mb-1 text-xs text-text-tertiary">{t("profile:photo.hint")}</p>
        <div className="mt-3">
          <div className="mb-1 text-xs text-text-secondary">
            {t("profile:completeness", { percent: pct })}
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className="h-full rounded-full bg-brand-500 transition-[width]"
              style={{ width: `${pct}%` }}
            />
          </div>
        </div>
      </section>

      <form onSubmit={onSubmit}>
        {/* Personal — always shown */}
        <Section title={t("profile:sections.personal")}>
          <Field label={t("profile:fields.firstName")}>
            <input
              className={inputClass}
              value={form.firstName}
              onChange={(e) => set("firstName", e.target.value)}
            />
          </Field>
          <Field label={t("profile:fields.lastName")}>
            <input
              className={inputClass}
              value={form.lastName}
              onChange={(e) => set("lastName", e.target.value)}
            />
          </Field>
          <Field label={t("profile:fields.email")}>
            <input className={`${inputClass} opacity-60`} value={profile.email} disabled />
          </Field>
          <Field label={t("profile:fields.dateOfBirth")}>
            <input
              type="date"
              className={inputClass}
              value={form.dateOfBirth}
              onChange={(e) => set("dateOfBirth", e.target.value)}
            />
          </Field>
          <Field label={t("profile:fields.nationality")}>
            <input
              className={inputClass}
              value={form.nationality}
              onChange={(e) => set("nationality", e.target.value)}
            />
          </Field>
          <Field label={t("profile:fields.countryOfResidence")}>
            <input
              className={inputClass}
              value={form.countryOfResidence}
              onChange={(e) => set("countryOfResidence", e.target.value)}
            />
          </Field>
          <Field label={t("profile:fields.preferredLanguage")}>
            <select
              className={inputClass}
              value={form.preferredLanguage}
              onChange={(e) => set("preferredLanguage", e.target.value)}
            >
              <option value="">{t("profile:languageOption.none")}</option>
              <option value="en">{t("profile:languageOption.en")}</option>
              <option value="ar">{t("profile:languageOption.ar")}</option>
            </select>
          </Field>
          <Field label={t("profile:fields.linkedInUrl")}>
            <input
              className={inputClass}
              value={form.linkedInUrl}
              onChange={(e) => set("linkedInUrl", e.target.value)}
            />
          </Field>
          <Field label={t("profile:fields.websiteUrl")}>
            <input
              className={inputClass}
              value={form.websiteUrl}
              onChange={(e) => set("websiteUrl", e.target.value)}
            />
          </Field>
          <Field label={t("profile:fields.biography")} full>
            <textarea
              rows={3}
              className={inputClass}
              value={form.biography}
              onChange={(e) => set("biography", e.target.value)}
            />
          </Field>
        </Section>

        {/* Academic — students */}
        {activeRole === "Student" && (
          <Section title={t("profile:sections.academic")}>
            <Field label={t("profile:fields.academicLevel")}>
              <input
                className={inputClass}
                value={form.academicLevel}
                onChange={(e) => set("academicLevel", e.target.value)}
              />
            </Field>
            <Field label={t("profile:fields.fieldOfStudy")}>
              <input
                className={inputClass}
                value={form.fieldOfStudy}
                onChange={(e) => set("fieldOfStudy", e.target.value)}
              />
            </Field>
            <Field label={t("profile:fields.currentInstitution")} full>
              <input
                className={inputClass}
                value={form.currentInstitution}
                onChange={(e) => set("currentInstitution", e.target.value)}
              />
            </Field>
            <Field label={t("profile:fields.gpa")}>
              <input
                type="number"
                step="0.01"
                className={inputClass}
                value={form.gpa}
                onChange={(e) => set("gpa", e.target.value)}
              />
            </Field>
            <Field label={t("profile:fields.gpaScale")}>
              <input
                className={inputClass}
                value={form.gpaScale}
                onChange={(e) => set("gpaScale", e.target.value)}
              />
            </Field>
          </Section>
        )}

        {/* Organization — companies */}
        {activeRole === "Company" && (
          <Section title={t("profile:sections.organization")}>
            <Field label={t("profile:fields.organizationLegalName")}>
              <input
                className={inputClass}
                value={form.organizationLegalName}
                onChange={(e) => set("organizationLegalName", e.target.value)}
              />
            </Field>
            <Field label={t("profile:fields.organizationWebsite")}>
              <input
                className={inputClass}
                value={form.organizationWebsite}
                onChange={(e) => set("organizationWebsite", e.target.value)}
              />
            </Field>
            {profile.organizationVerificationStatus && (
              <Field label={t("profile:fields.verificationStatus")}>
                <input
                  className={`${inputClass} opacity-60`}
                  value={profile.organizationVerificationStatus}
                  disabled
                />
              </Field>
            )}
          </Section>
        )}

        {/* Consultant settings */}
        {activeRole === "Consultant" && (
          <Section title={t("profile:sections.consultant")}>
            <Field label={t("profile:fields.sessionFeeUsd")}>
              <input
                type="number"
                step="0.01"
                className={inputClass}
                value={form.sessionFeeUsd}
                onChange={(e) => set("sessionFeeUsd", e.target.value)}
              />
            </Field>
            <Field label={t("profile:fields.sessionDurationMinutes")}>
              <input
                type="number"
                className={inputClass}
                value={form.sessionDurationMinutes}
                onChange={(e) => set("sessionDurationMinutes", e.target.value)}
              />
            </Field>
          </Section>
        )}

        <button
          type="submit"
          disabled={updateMut.isPending}
          className="cta-pill inline-flex items-center gap-2 bg-brand-500 px-6 py-2.5 text-sm text-white hover:bg-brand-600 disabled:opacity-50"
        >
          {updateMut.isPending ? (
            <Loader2 aria-hidden className="size-4 animate-spin" />
          ) : (
            <Save aria-hidden className="size-4" />
          )}
          {updateMut.isPending ? t("profile:saving") : t("profile:save")}
        </button>
      </form>
    </div>
  );
}
