import { apiClient } from "@/services/api/client";
import type { PaymentType } from "@/services/api/payments";

// ─── DTOs (FR-163..176 financial-configuration rules) ────────────────────────

export type FeeKind = "Percentage" | "FixedAmount";
export type FinancialRuleStatus = "Draft" | "Active" | "Archived";

export interface FinancialConfigRuleDto {
  id: string;
  paymentType: PaymentType;
  feeKind: FeeKind;
  /** Fraction 0..1 when feeKind is "Percentage"; null otherwise. */
  feePercentage: number | null;
  /** Cents when feeKind is "FixedAmount"; null otherwise. */
  feeAmountCents: number | null;
  /** Profit-share as a fraction 0..1. */
  profitSharePercentage: number;
  status: FinancialRuleStatus;
  effectiveFrom: string;
  effectiveTo: string | null;
  setByAdminId: string;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface FinancialCalculationPreviewDto {
  ruleId: string | null;
  paymentType: PaymentType;
  grossAmountCents: number;
  feeKind: FeeKind;
  feeCents: number;
  profitShareCents: number;
  platformTotalCents: number;
  payeeNetCents: number;
  /** Fee as a fraction of the gross — informative when the fee is fixed. */
  effectiveFeeRate: number;
  /** False when the platform take exceeds the gross. */
  isViable: boolean;
  /** True when no rule was on file and the legacy default was used. */
  usedFallback: boolean;
}

export interface CreateFinancialRuleBody {
  paymentType: PaymentType;
  feeKind: FeeKind;
  feePercentage: number | null;
  feeAmountCents: number | null;
  profitSharePercentage: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  notes: string | null;
}

/** The payment type is immutable once a rule exists, so an update omits it. */
export type UpdateFinancialRuleBody = Omit<CreateFinancialRuleBody, "paymentType">;

export interface FinancialRuleFilters {
  paymentType?: PaymentType;
  status?: FinancialRuleStatus;
}

export interface PreviewParams {
  grossAmountCents: number;
  paymentType?: PaymentType;
  ruleId?: string;
}

const BASE = "/api/admin/financial-config";

// ─── API ─────────────────────────────────────────────────────────────────────

export const financialConfigApi = {
  /** Lists rules, newest first, optionally filtered by payment type and/or status. */
  async list(filters?: FinancialRuleFilters): Promise<FinancialConfigRuleDto[]> {
    const { data } = await apiClient.get<FinancialConfigRuleDto[]>(BASE, { params: filters });
    return data;
  },

  /** A single rule by id. */
  async get(id: string): Promise<FinancialConfigRuleDto> {
    const { data } = await apiClient.get<FinancialConfigRuleDto>(`${BASE}/${id}`);
    return data;
  },

  /** Simulates how a gross amount would be split under a rule. */
  async preview(params: PreviewParams): Promise<FinancialCalculationPreviewDto> {
    const { data } = await apiClient.get<FinancialCalculationPreviewDto>(`${BASE}/preview`, {
      params,
    });
    return data;
  },

  /** Creates a new rule in Draft state; returns its id. */
  async create(body: CreateFinancialRuleBody): Promise<string> {
    const { data } = await apiClient.post<string>(BASE, body);
    return data;
  },

  /** Edits a Draft rule. */
  async update(id: string, body: UpdateFinancialRuleBody): Promise<void> {
    await apiClient.put(`${BASE}/${id}`, body);
  },

  /** Activates a Draft rule, archiving the rule currently in force. */
  async activate(id: string): Promise<void> {
    await apiClient.post(`${BASE}/${id}/activate`);
  },

  /** Returns an Active rule to Draft. */
  async deactivate(id: string): Promise<void> {
    await apiClient.post(`${BASE}/${id}/deactivate`);
  },

  /** Archives a rule — the retire path; rules are never hard-deleted. */
  async archive(id: string): Promise<void> {
    await apiClient.post(`${BASE}/${id}/archive`);
  },
};
