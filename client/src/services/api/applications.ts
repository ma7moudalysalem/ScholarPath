import { apiClient } from "@/services/api/client";
import type { PagedResult } from "@/types/api";

export type ApplicationStatus =
  | "Draft"
  | "Pending"
  | "UnderReview"
  | "Shortlisted"
  | "Accepted"
  | "Rejected"
  | "Withdrawn"
  | "Intending"
  | "Applied"
  | "WaitingResult";

export interface StudentApplicationRow {
  applicationId: string;
  /** Null when the tracker is a purely off-platform scholarship (no catalogue link). */
  scholarshipId: string | null;
  scholarshipTitle: string;
  companyId: string | null;
  companyName: string | null;
  status: ApplicationStatus;
  mode: "InApp" | "External";
  updatedAt: string;
}

export interface UpdateStatusRequest {
  status: ApplicationStatus;
}

/**
 * Body for `POST /api/applications/external` — registers an application the
 * student is pursuing on an external scholarship listing's own website.
 * Mirrors the server's `ExternalIntentCommand`.
 *
 * Two modes:
 *   1) `scholarshipId` set — the listing exists in the ScholarPath catalogue
 *      (legacy "external-mode platform listing" flow).
 *   2) `scholarshipId` null — a purely off-platform scholarship; `title` is
 *      required, `provider` and `deadline` are optional free-text.
 */
export interface CreateExternalApplicationRequest {
  scholarshipId?: string | null;
  externalTrackingUrl?: string | null;
  externalReferenceId?: string | null;
  personalNotes?: string | null;
  title?: string | null;
  provider?: string | null;
  deadline?: string | null;
}

/**
 * Mirrors the server's `CompanyApplicationRow` record (camelCase on the wire).
 * The server returns `applicationId` + `submittedAt`; the latter is nullable
 * because drafts have no submission date yet. There is no `studentEmail` /
 * `createdAt` on this DTO — earlier shapes referenced them but they never
 * existed in the wire payload.
 */
export interface CompanyApplicationRow {
  applicationId: string;
  studentId: string;
  studentName: string;
  scholarshipId: string;
  scholarshipTitle: string;
  status: ApplicationStatus;
  submittedAt: string | null;
}

export interface CompanyDocumentInfo {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

/** Full application details visible to the company reviewer. */
export interface CompanyApplicationDetails {
  applicationId: string;
  studentId: string;
  studentName: string;
  scholarshipId: string;
  scholarshipTitle: string;
  status: ApplicationStatus;
  submittedAt: string | null;
  formDataJson: string | null;
  attachedDocumentsJson: string | null;
  /** Vault documents uploaded by the student for this application (with IDs). */
  documents: CompanyDocumentInfo[];
}

export interface ApplicationDetail {
  id: string;
  scholarshipId: string | null;
  scholarshipTitleEn: string;
  scholarshipTitleAr: string;
  companyName: string | null;
  status: ApplicationStatus;
  mode: "InApp" | "External";
  formDataJson: string | null;
  attachedDocumentsJson: string | null;
  externalTrackingUrl: string | null;
  externalReferenceId: string | null;
  decisionReason: string | null;
  personalNotes: string | null;
  deadline: string | null;
  createdAt: string;
  updatedAt: string | null;
  submittedAt: string | null;
  reviewStartedAt: string | null;
  decisionAt: string | null;
  /**
   * CompanyReview fee in USD. Null/0 = no review fee, submit directly;
   * > 0 = the submit flow must first collect a manual-capture payment held
   * in escrow until the company finalises the review (PB-005 v1).
   */
  reviewFeeUsd: number | null;
}

/** Result of `POST /api/applications` — mirrors the server's StartApplicationResult. */
export interface StartApplicationResult {
  applicationId: string;
  /** True when an existing non-terminal application was resumed, not created. */
  alreadyExisted: boolean;
}

/** Body for `PUT /api/applications/{id}/draft` — mirrors SaveApplicationDraftCommand. */
export interface SaveApplicationDraftBody {
  formDataJson: string | null;
  attachedDocumentsJson: string | null;
  personalNotes: string | null;
}

export const applicationsApi = {
  async getMyApplications(): Promise<StudentApplicationRow[]> {
    const { data } = await apiClient.get<StudentApplicationRow[]>("/api/applications/me");
    return data;
  },

  async updateExternalStatus(id: string, status: ApplicationStatus): Promise<void> {
    await apiClient.patch(`/api/applications/${id}/external-status`, { status });
  },

  /**
   * Registers an external-listing application (the student applies on the
   * provider's own website; ScholarPath tracks it manually). Returns the new
   * application id.
   */
  async createExternal(req: CreateExternalApplicationRequest): Promise<string> {
    const { data } = await apiClient.post<string>("/api/applications/external", req);
    return data;
  },

  async submitReview(applicationId: string, companyId: string, rating: number, comment: string): Promise<void> {
    await apiClient.post(`/api/company-reviews`, {
      applicationId,
      companyId,
      rating,
      comment,
    });
  },

  async getCompanyApplications(scholarshipId?: string, page = 1, pageSize = 25, status?: ApplicationStatus): Promise<PagedResult<CompanyApplicationRow>> {
    const { data } = await apiClient.get<PagedResult<CompanyApplicationRow>>("/api/applications/company", {
      params: { scholarshipId, page, pageSize, status },
    });
    return data;
  },

  async getCompanyApplicationDetails(id: string): Promise<CompanyApplicationDetails> {
    const { data } = await apiClient.get<CompanyApplicationDetails>(`/api/applications/company/${id}`);
    return data;
  },

  async reviewApplication(id: string, status: ApplicationStatus, decisionReason?: string): Promise<void> {
    await apiClient.post(`/api/applications/${id}/review`, { status, decisionReason });
  },

  /** Single-application detail — owning student or admin. */
  async getById(id: string): Promise<ApplicationDetail> {
    const { data } = await apiClient.get<ApplicationDetail>(`/api/applications/${id}`);
    return data;
  },

  /**
   * Starts — or resumes — an in-app Draft application for the given scholarship.
   * Idempotent: if the student already has a non-terminal application, the
   * server returns it with `alreadyExisted: true` instead of erroring. External
   * listings and closed scholarships are still rejected with 409.
   */
  async start(
    scholarshipId: string,
    personalNotes?: string | null,
  ): Promise<StartApplicationResult> {
    const { data } = await apiClient.post<StartApplicationResult>("/api/applications", {
      scholarshipId,
      personalNotes: personalNotes ?? null,
    });
    return data;
  },

  /**
   * Saves the in-progress form answers, attached documents and notes on a
   * Draft application. The server rejects a non-Draft application with 409.
   */
  async saveDraft(id: string, body: SaveApplicationDraftBody): Promise<void> {
    await apiClient.put(`/api/applications/${id}/draft`, { applicationId: id, ...body });
  },

  /** Submits a Draft application — transitions it to Pending. */
  async submit(id: string): Promise<void> {
    await apiClient.put(`/api/applications/${id}/submit`);
  },

  /** Withdraws an active application — transitions it to Withdrawn. */
  async withdraw(id: string): Promise<void> {
    await apiClient.post(`/api/applications/${id}/withdraw`);
  },
};
