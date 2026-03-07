import api from './api';
import type {
  UpgradeRequestDetailDto,
  UpgradeReviewRequest,
  UpgradeRejectRequest,
  PaginatedResponse,
  UpgradeRequestDto,
} from '@/types';

export interface UpgradeRequestFilters {
  status?: string;
  type?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const adminService = {
  async getUpgradeRequests(filters: UpgradeRequestFilters = {}): Promise<PaginatedResponse<UpgradeRequestDto>> {
    const params = new URLSearchParams();
    if (filters.status) params.set('status', filters.status);
    if (filters.type) params.set('type', filters.type);
    if (filters.search) params.set('search', filters.search);
    params.set('page', String(filters.page ?? 1));
    params.set('pageSize', String(filters.pageSize ?? 10));

    const response = await api.get<PaginatedResponse<UpgradeRequestDto>>(
      `/admin/upgrade-requests?${params.toString()}`
    );
    return response.data;
  },

  async getUpgradeRequestDetail(id: string): Promise<UpgradeRequestDetailDto> {
    const response = await api.get<UpgradeRequestDetailDto>(`/admin/upgrade-requests/${id}`);
    return response.data;
  },

  async approveRequest(id: string, data?: UpgradeReviewRequest): Promise<void> {
    await api.put(`/admin/upgrade-requests/${id}/approve`, data ?? {});
  },

  async rejectRequest(id: string, data: UpgradeRejectRequest): Promise<void> {
    await api.put(`/admin/upgrade-requests/${id}/reject`, data);
  },

  async requestMoreInfo(id: string, data: UpgradeReviewRequest): Promise<void> {
    await api.put(`/admin/upgrade-requests/${id}/request-info`, data);
  },
};
