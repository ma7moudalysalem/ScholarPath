import { apiClient } from "@/services/api/client";

/** React Query key for the unread-notification count (header bell badge). */
export const UNREAD_COUNT_QUERY_KEY = ["notifications", "unread-count"] as const;

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

/** One notification delivery preference — mirrors NotificationPreferenceDto. */
export interface NotificationPreference {
  type: string;
  channel: string;
  isEnabled: boolean;
}

/** Global "do not disturb" settings — mirrors NotificationSettingsDto. */
export interface NotificationSettings {
  muted: boolean;
  quietHoursEnabled: boolean;
  quietStart: string | null; // "HH:mm" in quietTimezone
  quietEnd: string | null;
  quietTimezone: string | null;
}

/** Full preference matrix + DND settings — mirrors NotificationPreferencesDto. */
export interface NotificationPreferences {
  preferences: NotificationPreference[];
  settings: NotificationSettings;
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

  /** Returns the current user's full preference matrix (default = enabled). */
  async getPreferences(): Promise<NotificationPreferences> {
    const { data } = await apiClient.get<NotificationPreferences>("/api/notifications/preferences");
    return data;
  },

  /** Enables or disables one delivery channel for one notification type. */
  async updatePreference(type: string, channel: string, isEnabled: boolean): Promise<void> {
    await apiClient.put("/api/notifications/preferences", { type, channel, isEnabled });
  },

  /** Updates the global do-not-disturb settings (mute-all + quiet hours). */
  async updateSettings(settings: NotificationSettings): Promise<void> {
    await apiClient.put("/api/notifications/settings", settings);
  },

  /** Sends the current user a one-off test notification. */
  async sendTest(): Promise<void> {
    await apiClient.post("/api/notifications/test");
  },
};
