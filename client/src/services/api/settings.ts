import { apiClient } from "@/services/api/client";

// ─── DTOs (PB-011 platform settings) ─────────────────────────────────────────

/** Mirrors the server enum — the API serialises it as a string on the wire. */
export type PlatformSettingType = "Text" | "Boolean" | "Number";

export interface PlatformSettingDto {
  id: string;
  /** Unique dotted key, e.g. "maintenance.enabled". */
  key: string;
  /** Value, always a string; parse per `valueType`. */
  value: string;
  valueType: PlatformSettingType;
  descriptionEn: string | null;
  descriptionAr: string | null;
  /** Grouping bucket for the UI, e.g. "Access". */
  category: string;
  /** null = seeded, never edited. */
  updatedAt: string | null;
}

// ─── API ─────────────────────────────────────────────────────────────────────

export const settingsApi = {
  /** Every platform setting, ordered by category then key. */
  async getSettings(): Promise<PlatformSettingDto[]> {
    const { data } = await apiClient.get<PlatformSettingDto[]>(
      "/api/admin/settings",
    );
    return data;
  },

  /** Updates one setting by key; the value is validated server-side per type. */
  async updateSetting(key: string, value: string): Promise<PlatformSettingDto> {
    const { data } = await apiClient.put<PlatformSettingDto>(
      "/api/admin/settings",
      { key, value },
    );
    return data;
  },
};
