import api from './api';
import type { DashboardSummaryDto } from '@/types';

export const dashboardService = {
  async getSummary(): Promise<DashboardSummaryDto> {
    const response = await api.get<DashboardSummaryDto>('/dashboard/summary');
    return response.data;
  },
};
