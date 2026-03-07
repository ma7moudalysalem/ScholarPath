import api from './api';
import type {
  ConsultantUpgradeRequest,
  CompanyUpgradeRequest,
  UpgradeRequestDto,
} from '@/types';

export const upgradeService = {
  async submitConsultant(data: ConsultantUpgradeRequest): Promise<void> {
    await api.post('/upgrade-requests/consultant', data);
  },

  async submitCompany(data: CompanyUpgradeRequest): Promise<void> {
    await api.post('/upgrade-requests/company', data);
  },

  async getMyStatus(): Promise<UpgradeRequestDto | null> {
    const response = await api.get('/upgrade-requests/my-status');
    // Backend returns {} if no request exists
    if (!response.data || !response.data.id) return null;
    return response.data;
  },

  async resubmit(id: string, data: Partial<ConsultantUpgradeRequest & CompanyUpgradeRequest>): Promise<void> {
    await api.put(`/upgrade-requests/${id}/resubmit`, data);
  },

  async uploadFiles(files: File[], upgradeRequestId?: string): Promise<string[]> {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));
    if (upgradeRequestId) {
      formData.append('upgradeRequestId', upgradeRequestId);
    }
    const response = await api.post<string[]>('/files/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
};
