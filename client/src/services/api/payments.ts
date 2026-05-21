import { apiClient } from "@/services/api/client";

// ─── enums (mirroring server; the API serialises enums as strings) ───────────

export type PaymentType = "ConsultantBooking" | "CompanyReview";

export type PaymentStatus =
  | "Pending"
  | "Held"
  | "Captured"
  | "Refunded"
  | "PartiallyRefunded"
  | "Failed"
  | "Cancelled"
  | "Disputed";

export type PayoutStatus = "Pending" | "InTransit" | "Paid" | "Failed";

// ─── DTOs ────────────────────────────────────────────────────────────────────

export interface PaymentDto {
  id: string;
  type: PaymentType;
  status: PaymentStatus;
  amountCents: number;
  currency: string;
  profitShareAmountCents: number;
  payeeAmountCents: number;
  refundedAmountCents: number;
  payerUserId: string;
  payeeUserId: string | null;
  stripePaymentIntentId: string | null;
  stripeChargeId: string | null;
  relatedBookingId: string | null;
  relatedApplicationId: string | null;
  heldAt: string | null;
  capturedAt: string | null;
  refundedAt: string | null;
  refundReason: string | null;
  failureReason: string | null;
  createdAt: string;
}

export interface PayoutDto {
  id: string;
  amountCents: number;
  currency: string;
  status: PayoutStatus;
  stripePayoutId: string | null;
  includedPaymentCount: number;
  initiatedAt: string | null;
  paidAt: string | null;
  failureReason: string | null;
  createdAt: string;
}

/** Server paged shape — matches the .NET PagedResult record on the wire. */
export interface PagedPayments {
  items: PaymentDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ListPaymentsParams {
  status?: PaymentStatus;
  type?: PaymentType;
  page?: number;
  pageSize?: number;
}

export interface RefundPaymentBody {
  /** null / omitted = full refund. */
  amountCents?: number | null;
  reason?: string;
}

// ─── intent creation (used by the Stripe checkout widget) ────────────────────

/**
 * Body for `POST /api/payments/intent` — mirrors the server's
 * `CreatePaymentIntentCommand`. The capture method is derived server-side from
 * `type`, so it is not sent. The payer is always the authenticated caller.
 */
export interface CreateIntentRequest {
  type: PaymentType;
  amountCents: number;
  currency: string;
  payeeUserId?: string | null;
  relatedBookingId?: string | null;
  relatedApplicationId?: string | null;
}

/** Mirrors the server's `CreatePaymentIntentResult`. */
export interface CreateIntentResponse {
  paymentId: string;
  clientSecret: string;
  paymentIntentId: string;
}

// ─── helpers ─────────────────────────────────────────────────────────────────

/**
 * Formats an integer-cents amount as a currency string (e.g. 5000 → "$50.00").
 * Pass `locale` (e.g. "ar-EG") to control digit script — when omitted, the
 * browser default is used. Hardcoding "en-US" stripped Arabic-Indic digits for
 * AR users on the admin payments grid.
 */
export function formatMoneyCents(
  cents: number,
  currency = "USD",
  locale?: string,
): string {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency: currency || "USD",
  }).format((cents ?? 0) / 100);
}

// ─── API ─────────────────────────────────────────────────────────────────────

export const paymentsApi = {
  async createIntent(req: CreateIntentRequest): Promise<CreateIntentResponse> {
    const { data } = await apiClient.post<CreateIntentResponse>("/api/payments/intent", req);
    return data;
  },

  /** Lists payments newest-first. Admins see all; others see only their own. */
  async listPayments(params: ListPaymentsParams = {}): Promise<PagedPayments> {
    const { data } = await apiClient.get<PagedPayments>("/api/payments", { params });
    return data;
  },

  async getPayment(id: string): Promise<PaymentDto> {
    const { data } = await apiClient.get<PaymentDto>(`/api/payments/${id}`);
    return data;
  },

  /** The authenticated payee's own payouts (consultants and companies). */
  async listMyPayouts(): Promise<PayoutDto[]> {
    const { data } = await apiClient.get<PayoutDto[]>("/api/payments/payouts");
    return data;
  },

  /** Admin-only: refund a held or captured payment (full or partial). */
  async refund(id: string, body: RefundPaymentBody): Promise<void> {
    await apiClient.post(`/api/payments/${id}/refund`, body);
  },

  /**
   * Creates or reuses the caller's Stripe Connect account and returns a fresh
   * onboarding link. The caller should redirect to `onboardingUrl` to complete
   * onboarding on Stripe's hosted flow.
   */
  async connectOnboard(
    returnUrl: string,
    refreshUrl: string,
  ): Promise<{ connectAccountId: string; status: string; onboardingUrl: string }> {
    const { data } = await apiClient.post<{
      connectAccountId: string;
      status: string;
      onboardingUrl: string;
    }>("/api/payments/connect/onboard", { returnUrl, refreshUrl });
    return data;
  },
};
