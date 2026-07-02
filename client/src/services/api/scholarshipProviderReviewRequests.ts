import { apiClient } from "@/services/api/client";
import type { PaymentStatus } from "@/services/api/payments";

/**
 * Paid ScholarshipProviderReview support-request lifecycle (PB-005). Apply Now creates a
 * request in Submitted, Stripe Elements authorises the card, the Student
 * confirms the hold → Pending, then the ScholarshipProvider accepts (capture) or rejects
 * (release hold). Student cancellation is allowed in Submitted / Pending
 * (no charge) and UnderReview (50% refund).
 *
 * The DTO numbers come from the Payment row paired with the request, exposed
 * via the API so dashboards always show held / captured / refunded / retained
 * / commission / share consistently.
 */
export type ScholarshipProviderReviewRequestStatus =
  | "Draft"
  | "Submitted"
  | "Pending"
  | "UnderReview"
  | "Completed"
  | "Closed"
  | "Cancelled"
  | "Failed"
  | "CancelledByStudent"
  | "RejectedByScholarshipProvider"
  | "Expired";

export interface ScholarshipProviderReviewRequestDto {
  id: string;
  scholarshipId: string;
  scholarshipTitle: string;
  studentId: string;
  studentName?: string | null;
  scholarshipProviderId: string;
  scholarshipProviderName?: string | null;
  status: ScholarshipProviderReviewRequestStatus;
  reviewFeeUsdSnapshot: number;
  /** True when the request was started free (snapshot fee = 0, no payment row). */
  isFree: boolean;
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
  scholarshipProviderShareCents: number;
  paymentReference?: string | null;
}

export interface StartScholarshipProviderReviewRequestResult {
  requestId: string;
  /** True for free scholarships — skip Stripe Elements entirely. */
  isFree: boolean;
  paymentId: string | null;
  clientSecret: string | null;
  paymentIntentId: string | null;
  amountCents: number;
  currency: string;
}

/** Terminal statuses — the UI must hide cancel/accept/reject actions on these. */
export const TERMINAL_REQUEST_STATUSES: ReadonlySet<ScholarshipProviderReviewRequestStatus> = new Set([
  "Completed",
  "Closed",
  "Cancelled",
  "Failed",
  "CancelledByStudent",
  "RejectedByScholarshipProvider",
  "Expired",
]);

export function isRequestCancellableByStudent(
  status: ScholarshipProviderReviewRequestStatus,
): boolean {
  return status === "Submitted" || status === "Pending" || status === "UnderReview";
}

/** True when cancelling from this status triggers a 50% refund. */
export function refundsHalfOnCancel(status: ScholarshipProviderReviewRequestStatus): boolean {
  return status === "UnderReview";
}

export const scholarshipProviderReviewRequestsApi = {
  /** Apply Now — creates the request and returns the Stripe client secret. */
  async start(scholarshipId: string): Promise<StartScholarshipProviderReviewRequestResult> {
    const { data } = await apiClient.post<StartScholarshipProviderReviewRequestResult>(
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
  async listMineAsStudent(): Promise<ScholarshipProviderReviewRequestDto[]> {
    const { data } = await apiClient.get<ScholarshipProviderReviewRequestDto[]>(
      "/api/company-review-requests/me/student",
    );
    return data;
  },

  /** Lists the authenticated ScholarshipProvider's incoming review requests, newest first. */
  async listMineAsScholarshipProvider(): Promise<ScholarshipProviderReviewRequestDto[]> {
    const { data } = await apiClient.get<ScholarshipProviderReviewRequestDto[]>(
      "/api/company-review-requests/me/company",
    );
    return data;
  },

  async getById(id: string): Promise<ScholarshipProviderReviewRequestDto> {
    const { data } = await apiClient.get<ScholarshipProviderReviewRequestDto>(
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
