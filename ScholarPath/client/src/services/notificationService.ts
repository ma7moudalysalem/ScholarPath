import api from './api';
import type { NotificationDto, PaginatedResponse } from '@/types';

export const notificationService = {
  async getNotifications(
    page = 1,
    pageSize = 20
  ): Promise<PaginatedResponse<NotificationDto>> {
    const response = await api.get<PaginatedResponse<NotificationDto>>(
      '/notifications',
      { params: { page, pageSize } }
    );
    return response.data;
  },

  async markAsRead(notificationId: string): Promise<void> {
    await api.put(`/notifications/${notificationId}/read`);
  },

  async markAllAsRead(): Promise<void> {
    await api.put('/notifications/read-all');
  },
};
