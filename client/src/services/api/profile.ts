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
  // Consultant professional fields (CR-PROF-08)
  professionalTitle: string | null;
  yearsOfExperience: number | null;
  expertiseTags: string[] | null;
  languages: string[] | null;
  timezone: string | null;
  completenessPercent: number;
  // CR-PROF-06: false for users that signed in only via SSO (no PasswordHash).
  hasPasswordCredential: boolean;
}

/** Partial-update payload — every field is optional (PATCH semantics).
 *
 * Role, ActiveRole, AccountStatus and verification-status fields are NOT on
 * this interface on purpose: profile edits must not be able to escalate or
 * change a user's standing (CR-PROF-11). The backend DTO mirrors that.
 */
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
  // Consultant professional fields (CR-PROF-08)
  professionalTitle?: string | null;
  yearsOfExperience?: number | null;
  expertiseTags?: string[] | null;
  languages?: string[] | null;
  timezone?: string | null;
}

// Profile photo upload limits (CR-PROF-10) — mirror the backend constants
// (UploadProfilePhotoCommandHandler.MaxBytes / AllowedContentTypes).
export const PHOTO_MAX_BYTES = 5 * 1024 * 1024;
export const PHOTO_ALLOWED_MIME_TYPES = [
  "image/jpeg",
  "image/png",
  "image/webp",
] as const;
export type AllowedPhotoMime = (typeof PHOTO_ALLOWED_MIME_TYPES)[number];

const PHOTO_EXTENSIONS_BY_MIME: Record<AllowedPhotoMime, ReadonlyArray<string>> = {
  "image/jpeg": [".jpg", ".jpeg"],
  "image/png": [".png"],
  "image/webp": [".webp"],
};

export type PhotoValidationError =
  | "type"
  | "size"
  | "extensionMismatch"
  | "empty";

/**
 * Client-side photo guard (CR-PROF-10). Mirrors the backend allow-list so the
 * user gets immediate feedback before the upload even leaves the browser. The
 * backend still re-validates everything (magic bytes + AV scan) — this is UX,
 * not security.
 */
export function validatePhotoFile(file: File): PhotoValidationError | null {
  if (file.size <= 0) return "empty";
  if (file.size > PHOTO_MAX_BYTES) return "size";

  const mime = file.type.toLowerCase();
  if (!PHOTO_ALLOWED_MIME_TYPES.includes(mime as AllowedPhotoMime)) {
    return "type";
  }

  // Extension must match the MIME so a renamed file is caught up front.
  const lower = file.name.toLowerCase();
  const allowedExts = PHOTO_EXTENSIONS_BY_MIME[mime as AllowedPhotoMime];
  if (!allowedExts.some((ext) => lower.endsWith(ext))) {
    return "extensionMismatch";
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
