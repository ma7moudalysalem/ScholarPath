import { apiClient } from "@/services/api/client";
import type { PaymentStatus } from "@/services/api/payments";

/**
 * Paid CompanyReview support-request lifecycle (PB-005). Apply Now creates a
 * request in Submitted, Stripe Elements authorises the card, the Student
 * confirms the hold → Pending, then the Company accepts (capture) or rejects
 * (release hold). Student cancellation is allowed in Submitted / Pending
 * (no charge) and UnderReview (50% refund).
 *
 * The DTO numbers come from the Payment row paired with the request, exposed
 * via the API so dashboards always show held / captured / refunded / retained
 * / commission / share consistently.
 */
export type CompanyReviewRequestStatus =
  | "Draft"
  | "Submitted"
  | "Pending"
  | "UnderReview"
  | "Completed"
  | "Closed"
  | "Cancelled"
  | "Failed"
  | "CancelledByStudent"
  | "RejectedByCompany"
  | "Expired";

export interface CompanyReviewRequestDto {
  id: string;
  scholarshipId: string;
  scholarshipTitle: string;
  studentId: string;
  studentName?: string | null;
  companyId: string;
  companyName?: string | null;
  status: CompanyReviewRequestStatus;
  reviewFeeUsdSnapshot: number;
  currency: string;
  submittedAt?: string | null;
  acceptedAt?: string | null;
  rejectedAt?: string | null;
  completedAt?: string | null;
  cancelledAt?: string | null;
  expiredAt?: string | null;
  pendingExpiresAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  cancelReason?: string | null;
  rejectReason?: string | null;
  paymentId?: string | null;
  paymentStatus?: PaymentStatus | null;
  amountCents: number;
  heldAmountCents: number;
  capturedAmountCents: number;
  refundedAmountCents: number;
  retainedAmountCents: number;
  platformCommissionCents: number;
  companyShareCents: number;
  paymentReference?: string | null;
}

export interface StartCompanyReviewRequestResult {
  requestId: string;
  paymentId: string;
  clientSecret: string;
  paymentIntentId: string;
  amountCents: number;
  currency: string;
}

/** Terminal statuses — the UI must hide cancel/accept/reject actions on these. */
export const TERMINAL_REQUEST_STATUSES: ReadonlySet<CompanyReviewRequestStatus> = new Set([
  "Completed",
  "Closed",
  "Cancelled",
  "Failed",
  "CancelledByStudent",
  "RejectedByCompany",
  "Expired",
]);

export function isRequestCancellableByStudent(
  status: CompanyReviewRequestStatus,
): boolean {
  return status === "Submitted" || status === "Pending" || status === "UnderReview";
}

/** True when cancelling from this status triggers a 50% refund. */
export function refundsHalfOnCancel(status: CompanyReviewRequestStatus): boolean {
  return status === "UnderReview";
}

export const companyReviewRequestsApi = {
  /** Apply Now — creates the request and returns the Stripe client secret. */
  async start(scholarshipId: string): Promise<StartCompanyReviewRequestResult> {
    const { data } = await apiClient.post<StartCompanyReviewRequestResult>(
      "/api/company-review-requests",
      { scholarshipId },
    );
    return data;
  },

  /** Called after Stripe Elements resolves the manual-capture confirmation. */
  async confirmPayment(requestId: string): Promise<void> {
    await apiClient.post(`/api/company-review-requests/${requestId}/confirm-payment`);
  },

  /** Student cancellation — refund policy resolved server-side from status. */
  async cancel(requestId: string, reason?: string): Promise<void> {
    await apiClient.post(`/api/company-review-requests/${requestId}/cancel`, {
      reason: reason ?? null,
    });
  },

  /** Lists the authenticated Student's review requests, newest first. */
  async listMineAsStudent(): Promise<CompanyReviewRequestDto[]> {
    const { data } = await apiClient.get<CompanyReviewRequestDto[]>(
      "/api/company-review-requests/me/student",
    );
    return data;
  },

  /** Lists the authenticated Company's incoming review requests, newest first. */
  async listMineAsCompany(): Promise<CompanyReviewRequestDto[]> {
    const { data } = await apiClient.get<CompanyReviewRequestDto[]>(
      "/api/company-review-requests/me/company",
    );
    return data;
  },

  async getById(id: string): Promise<CompanyReviewRequestDto> {
    const { data } = await apiClient.get<CompanyReviewRequestDto>(
      `/api/company-review-requests/${id}`,
    );
    return data;
  },

  async accept(requestId: string): Promise<void> {
    await apiClient.post(`/api/company-review-requests/${requestId}/accept`);
  },

  async reject(requestId: string, reason?: string): Promise<void> {
    await apiClient.post(`/api/company-review-requests/${requestId}/reject`, {
      reason: reason ?? null,
    });
  },

  async complete(requestId: string): Promise<void> {
    await apiClient.post(`/api/company-review-requests/${requestId}/complete`);
  },
};
