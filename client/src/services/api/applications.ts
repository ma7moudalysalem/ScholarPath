import { apiClient } from "@/services/api/client";
import type { ApplicationStatus, ListingMode } from "@/types/domain";

// ── DTOs ──────────────────────────────────────────────────────────────────────

export interface ApplicationListItem {
  id: string;
  scholarshipId: string;
  scholarshipTitleEn: string;
  scholarshipTitleAr: string;
  scholarshipDeadline: string;
  scholarshipMode: ListingMode;
  status: ApplicationStatus;
  mode: "InApp" | "External";
  submittedAt?: string | null;
  withdrawnAt?: string | null;
  personalNotes?: string | null;
  isReadOnly: boolean;
  createdAt: string;
}

export interface ApplicationDetail extends ApplicationListItem {
  formDataJson?: string | null;
  attachedDocumentsJson?: string | null;
  externalTrackingUrl?: string | null;
  externalReferenceId?: string | null;
  decisionAt?: string | null;
  decisionReason?: string | null;
  statusHistory: StatusHistoryItem[];
}

export interface StatusHistoryItem {
  id: string;
  fromStatus: ApplicationStatus;
  toStatus: ApplicationStatus;
  changedAt: string;
  changedByName?: string | null;
  note?: string | null;
}

export interface StartApplicationRequest {
  scholarshipId: string;
  personalNotes?: string | null;
}

export interface WithdrawApplicationRequest {
  reason?: string | null;
}

export interface UpdateExternalStatusRequest {
  status: "Intending" | "Applied" | "WaitingResult" | "Accepted" | "Rejected";
  notes?: string | null;
  externalReferenceId?: string | null;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const applicationsApi = {
  async getMyApplications(): Promise<ApplicationListItem[]> {
    const { data } = await apiClient.get<ApplicationListItem[]>(
      "/api/applications",
    );
    return data;
  },

  async getById(id: string): Promise<ApplicationDetail> {
    const { data } = await apiClient.get<ApplicationDetail>(
      `/api/applications/${id}`,
    );
    return data;
  },

  async start(req: StartApplicationRequest): Promise<{ id: string }> {
    const { data } = await apiClient.post<{ id: string }>(
      "/api/applications",
      req,
    );
    return data;
  },

  async submit(id: string): Promise<void> {
    await apiClient.put(`/api/applications/${id}/submit`);
  },

  async withdraw(id: string, req: WithdrawApplicationRequest): Promise<void> {
    await apiClient.post(`/api/applications/${id}/withdraw`, req);
  },

  async updateExternalStatus(
    id: string,
    req: UpdateExternalStatusRequest,
  ): Promise<void> {
    await apiClient.put(`/api/applications/${id}/external-status`, req);
  },
};
