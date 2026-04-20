import { apiClient } from "@/services/api/client";

export type DataRequestType = "Export" | "Delete";
export type DataRequestStatus =
  | "Pending"
  | "Processing"
  | "Completed"
  | "Cancelled"
  | "Failed";

export interface DataRequestDto {
  id: string;
  type: DataRequestType;
  status: DataRequestStatus;
  requestedAt: string;
  scheduledProcessAt: string | null;
  completedAt: string | null;
  cancelledAt: string | null;
  downloadUrl: string | null;
  downloadExpiresAt: string | null;
}

export const dataPrivacyApi = {
  async listMine(): Promise<DataRequestDto[]> {
    const { data } = await apiClient.get<DataRequestDto[]>("/api/users/me/data-requests");
    return data;
  },
  async requestExport(): Promise<DataRequestDto> {
    const { data } = await apiClient.post<DataRequestDto>("/api/users/me/data-export");
    return data;
  },
  async requestDelete(reason?: string): Promise<DataRequestDto> {
    const { data } = await apiClient.post<DataRequestDto>("/api/users/me/data-delete", { reason });
    return data;
  },
  async cancelDelete(): Promise<void> {
    await apiClient.post("/api/users/me/data-delete/cancel");
  },
};
