import { apiClient } from "@/services/api/client";

/** A single in-app notification (PB-010) — mirrors NotificationDto. */
export interface NotificationItem {
  id: string;
  type: string;
  titleEn: string;
  titleAr: string;
  bodyEn: string;
  bodyAr: string;
  deepLink: string | null;
  isRead: boolean;
  readAt: string | null;
  priority: number;
  createdAt: string;
}

/** Paged notification list — mirrors NotificationsPageDto. */
export interface NotificationsPage {
  items: NotificationItem[];
  page: number;
  pageSize: number;
  total: number;
  unreadCount: number;
}

export const notificationsApi = {
  async list(page = 1, pageSize = 20): Promise<NotificationsPage> {
    const { data } = await apiClient.get<NotificationsPage>("/api/notifications", {
      params: { page, pageSize },
    });
    return data;
  },
  async unreadCount(): Promise<number> {
    const { data } = await apiClient.get<number>("/api/notifications/unread-count");
    return data;
  },
  async markRead(id: string): Promise<void> {
    await apiClient.patch(`/api/notifications/${id}/read`);
  },
  async markAllRead(): Promise<number> {
    const { data } = await apiClient.post<{ marked: number }>("/api/notifications/read-all");
    return data.marked;
  },
};
