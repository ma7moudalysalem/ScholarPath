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
};
