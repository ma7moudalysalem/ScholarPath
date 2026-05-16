import { apiClient } from "@/services/api/client";
import type { PagedResult } from "@/types/api";

export type ApplicationStatus =
  | "Draft"
  | "Pending"
  | "UnderReview"
  | "Accepted"
  | "Rejected"
  | "Withdrawn"
  | "Intending"
  | "Applied"
  | "WaitingResult";

export interface StudentApplicationRow {
  applicationId: string;
  scholarshipId: string;
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

export interface CompanyApplicationRow {
  id: string;
  studentId: string;
  studentName: string;
  studentEmail: string;
  scholarshipId: string;
  scholarshipTitle: string;
  status: ApplicationStatus;
  createdAt: string;
}

export const applicationsApi = {
  async getMyApplications(): Promise<StudentApplicationRow[]> {
    const { data } = await apiClient.get<StudentApplicationRow[]>("/api/applications/me");
    return data;
  },

  async updateExternalStatus(id: string, status: ApplicationStatus): Promise<void> {
    await apiClient.patch(`/api/applications/${id}/external-status`, { status });
  },

  async submitReview(applicationId: string, companyId: string, rating: number, comment: string): Promise<void> {
    await apiClient.post(`/api/company-reviews`, {
      applicationId,
      companyId,
      rating,
      comment,
    });
  },

  async getCompanyApplications(scholarshipId?: string, page = 1, pageSize = 25): Promise<PagedResult<CompanyApplicationRow>> {
    const { data } = await apiClient.get<PagedResult<CompanyApplicationRow>>("/api/applications/company", {
      params: { scholarshipId, page, pageSize },
    });
    return data;
  },

  async getCompanyApplicationDetails(id: string): Promise<CompanyApplicationRow> {
    const { data } = await apiClient.get<CompanyApplicationRow>(`/api/applications/company/${id}`);
    return data;
  },

  async reviewApplication(id: string, status: ApplicationStatus, decisionReason?: string): Promise<void> {
    await apiClient.post(`/api/applications/${id}/review`, { status, decisionReason });
  },
};
