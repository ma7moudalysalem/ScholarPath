import { apiClient } from "@/services/api/client";
import type { PaymentType } from "@/services/api/payments";

// ─── DTOs (PB-014 profit-share configuration) ────────────────────────────────

export interface ProfitShareConfigDto {
  id: string;
  paymentType: PaymentType;
  /** Platform share as a fraction, e.g. 0.15 = 15%. Server caps it at 0.50. */
  percentage: number;
  effectiveFrom: string;
  /** null = this row is the currently-active rate. */
  effectiveTo: string | null;
  setByAdminId: string;
  notes: string | null;
  isActive: boolean;
}

export interface SetProfitShareBody {
  /** Fraction 0..0.50. */
  percentage: number;
  notes?: string;
}

// ─── API ─────────────────────────────────────────────────────────────────────

export const profitShareApi = {
  /** The currently-active rate per payment type. */
  async active(): Promise<ProfitShareConfigDto[]> {
    const { data } = await apiClient.get<ProfitShareConfigDto[]>(
      "/api/admin/profit-share/active",
    );
    return data;
  },

  /** Full effective-dated history, newest first. Optionally filtered by type. */
  async history(paymentType?: PaymentType): Promise<ProfitShareConfigDto[]> {
    const { data } = await apiClient.get<ProfitShareConfigDto[]>(
      "/api/admin/profit-share/history",
      { params: paymentType ? { paymentType } : undefined },
    );
    return data;
  },

  /** Sets a new active rate for a payment type; closes the previous one. */
  async set(paymentType: PaymentType, body: SetProfitShareBody): Promise<ProfitShareConfigDto> {
    const { data } = await apiClient.put<ProfitShareConfigDto>(
      `/api/admin/profit-share/${paymentType}`,
      body,
    );
    return data;
  },
};
