import { apiClient } from "@/services/api/client";

/** Role-agnostic profile view (PB-002) — mirrors UserProfileDto. */
export interface UserProfile {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  profileImageUrl: string | null;
  accountStatus: string;
  countryOfResidence: string | null;
  preferredLanguage: string | null;
  biography: string | null;
  dateOfBirth: string | null;
  nationality: string | null;
  linkedInUrl: string | null;
  websiteUrl: string | null;
  academicLevel: string | null;
  fieldOfStudy: string | null;
  currentInstitution: string | null;
  gpa: number | null;
  gpaScale: string | null;
  organizationLegalName: string | null;
  organizationWebsite: string | null;
  organizationVerificationStatus: string | null;
  sessionFeeUsd: number | null;
  sessionDurationMinutes: number | null;
  // ── Consultant professional fields (consumed by the role-aware profile
  // panel introduced in 6a363af). Optional because legacy users predate
  // these columns and the GetProfile DTO may not surface them yet — every
  // call site uses `?? …` to default. ───────────────────────────────────
  professionalTitle: string | null;
  yearsOfExperience: number | null;
  expertiseTags: string[] | null;
  languages: string[] | null;
  timezone: string | null;
  // True when the account has a password set (i.e. not an SSO-only login);
  // gates the Change Password section per CR-PROF-06.
  hasPasswordCredential: boolean;
  completenessPercent: number;
}

/** Partial-update payload — every field is optional (PATCH semantics). */
export interface UpdateProfileRequest {
  firstName?: string | null;
  lastName?: string | null;
  countryOfResidence?: string | null;
  preferredLanguage?: string | null;
  biography?: string | null;
  dateOfBirth?: string | null;
  nationality?: string | null;
  linkedInUrl?: string | null;
  websiteUrl?: string | null;
  academicLevel?: string | null;
  fieldOfStudy?: string | null;
  currentInstitution?: string | null;
  gpa?: number | null;
  gpaScale?: string | null;
  organizationLegalName?: string | null;
  organizationWebsite?: string | null;
  sessionFeeUsd?: number | null;
  sessionDurationMinutes?: number | null;
  // Consultant professional fields written from the role-aware editor.
  professionalTitle?: string | null;
  yearsOfExperience?: number | null;
  expertiseTags?: string[] | null;
  languages?: string[] | null;
  timezone?: string | null;
}

/**
 * MIME types accepted by the profile-photo uploader. Matches the page hint
 * "JPG, PNG or WebP, up to 5 MB.". Kept as a tuple so React's
 * <input accept> can join it with commas.
 */
export const PHOTO_ALLOWED_MIME_TYPES = [
  "image/jpeg",
  "image/png",
  "image/webp",
] as const;

const MAX_PHOTO_BYTES = 5 * 1024 * 1024;

/**
 * Validates a profile-photo file before upload. Returns null if the file is
 * acceptable, or an i18n key suffix (`unsupportedType` / `tooLarge`) that the
 * caller renders via `t('profile:photo.validation.<suffix>')`.
 */
export function validatePhotoFile(file: File): "unsupportedType" | "tooLarge" | null {
  if (!(PHOTO_ALLOWED_MIME_TYPES as readonly string[]).includes(file.type)) {
    return "unsupportedType";
  }
  if (file.size > MAX_PHOTO_BYTES) {
    return "tooLarge";
  }
  return null;
}

export const profileApi = {
  async getMine(): Promise<UserProfile> {
    const { data } = await apiClient.get<UserProfile>("/api/profiles/me");
    return data;
  },
  async update(payload: UpdateProfileRequest): Promise<UserProfile> {
    const { data } = await apiClient.patch<UserProfile>("/api/profiles/me", payload);
    return data;
  },
  async uploadPhoto(file: File): Promise<string> {
    const form = new FormData();
    form.append("file", file);
    const { data } = await apiClient.post<{ url: string }>("/api/profiles/me/photo", form);
    return data.url;
  },

  /** Change the signed-in user's password. Revokes all refresh tokens on success. */
  async changePassword(
    currentPassword: string,
    newPassword: string,
  ): Promise<void> {
    await apiClient.post("/api/profiles/me/change-password", {
      currentPassword,
      newPassword,
    });
  },
};
