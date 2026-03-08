import api from './api';
import type {
  TrackApplicationRequest,
  TrackApplicationResponse,
  ApplicationListItemDto,
  ApplicationStatus,
  PaginatedResponse,
  ChecklistItem,
  UpdateRemindersRequest,
} from '@/types';

export const applicationService = {
  async trackApplication(request: TrackApplicationRequest): Promise<TrackApplicationResponse> {
    const response = await api.post<TrackApplicationResponse>('/applications/track', request);
    return response.data;
  },

  async getApplications(params?: {
    status?: ApplicationStatus;
    sortBy?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResponse<ApplicationListItemDto>> {
    const response = await api.get<PaginatedResponse<ApplicationListItemDto>>('/applications', {
      params,
    });
    return response.data;
  },

  async updateStatus(id: string, status: ApplicationStatus): Promise<{ updatedAt: string }> {
    const response = await api.put<{ updatedAt: string }>(`/applications/${id}/status`, {
      status,
    });
    return response.data;
  },

  async updateNotes(id: string, notes: string): Promise<{ updatedAt: string }> {
    const response = await api.put<{ updatedAt: string }>(`/applications/${id}/notes`, { notes });
    return response.data;
  },

  async updateChecklist(id: string, items: ChecklistItem[]): Promise<{ updatedAt: string }> {
    const response = await api.put<{ updatedAt: string }>(`/applications/${id}/checklist`, {
      items,
    });
    return response.data;
  },

  async updateReminders(id: string, request: UpdateRemindersRequest): Promise<{ updatedAt: string }> {
    const response = await api.put<{ updatedAt: string }>(`/applications/${id}/reminders`, request);
    return response.data;
  },

  async deleteApplication(id: string): Promise<void> {
    await api.delete(`/applications/${id}`);
  },
};
