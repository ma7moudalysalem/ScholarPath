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

export interface CreateIntentRequest {
  amountCents: number;
  currency: string;
  captureMethod: "manual" | "automatic";
  bookingId?: string;
  applicationId?: string;
}

export interface CreateIntentResponse {
  paymentIntentId: string;
  clientSecret: string;
  status: string;
}

// ─── helpers ─────────────────────────────────────────────────────────────────

/** Formats an integer-cents amount as a currency string (e.g. 5000 → "$50.00"). */
export function formatMoneyCents(cents: number, currency = "USD"): string {
  return new Intl.NumberFormat("en-US", {
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
};
