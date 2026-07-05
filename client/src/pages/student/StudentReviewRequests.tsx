import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Loader2, MessageSquare } from "lucide-react";
import {
  scholarshipProviderReviewRequestsApi,
  isRequestCancellableByStudent,
  refundsHalfOnCancel,
  TERMINAL_REQUEST_STATUSES,
  type ScholarshipProviderReviewRequestDto,
  type ScholarshipProviderReviewRequestStatus,
} from "@/services/api/scholarshipProviderReviewRequests";
import { apiErrorMessage } from "@/services/api/client";
import { formatMoneyCents } from "@/services/api/payments";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";

/**
 * Student-facing list of paid ScholarshipProviderReview support requests. Shows the
 * request status, the payment numbers (held / captured / refunded /
 * retained), and a Cancel action whose semantics depend on the current
 * status (no-charge while Pending, 50% refund while UnderReview).
 *
 * Spec PART 14: "Allow cancellation — Pending: cancel hold/no charge,
 * UnderReview: 50% refund, Completed: cancellation/refund not allowed."
 */
export function StudentReviewRequests() {
  const { t, i18n } = useTranslation(["payments", "common", "scholarships"]);
  const queryClient = useQueryClient();
  // Master payments switch — when off the whole list reads as "Free", and
  // the held/captured/refunded breakdown collapses to a single fee row.
  const paymentsEnabled = usePaymentsEnabled();

  const query = useQuery<ScholarshipProviderReviewRequestDto[]>({
    queryKey: ["scholarshipProviderReviewRequests", "mine", "student"],
    queryFn: () => scholarshipProviderReviewRequestsApi.listMineAsStudent(),
  });

  const [pendingCancelId, setPendingCancelId] = useState<string | null>(null);

  const cancelMut = useMutation({
    mutationFn: (id: string) => scholarshipProviderReviewRequestsApi.cancel(id),
    onSuccess: () => {
      toast.success(t("payments:reviewRequest.cancelSuccess"));
      void queryClient.invalidateQueries({
        queryKey: ["scholarshipProviderReviewRequests", "mine", "student"],
      });
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, t("common:status.error"))),
    onSettled: () => setPendingCancelId(null),
  });

  return (
    <div className="mx-auto w-full max-w-5xl px-4 py-8 sm:px-6">
      <header className="mb-6">
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("payments:reviewRequest.studentTitle")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("payments:reviewRequest.studentSubtitle")}
        </p>
      </header>

      {query.isLoading && (
        <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 text-sm text-text-tertiary">
          {t("common:status.loading")}
        </div>
      )}

      {query.isError && (
        <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
          {t("common:status.error")}
        </div>
      )}

      {query.data && query.data.length === 0 && (
        <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-8 text-center text-sm text-text-secondary">
          {t("payments:reviewRequest.studentEmpty")}
        </div>
      )}

      <ul className="space-y-3">
        {query.data?.map((req) => (
          <li
            key={req.id}
            className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs"
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0">
                <Link
                  to={`/student/scholarships/${req.scholarshipId}`}
                  className="text-base font-semibold text-text-primary hover:underline"
                >
                  {req.scholarshipTitle}
                </Link>
                <p className="mt-1 text-xs text-text-tertiary">
                  {req.scholarshipProviderName ?? t("payments:reviewRequest.unknownScholarshipProvider")}
                </p>
              </div>
              <StatusBadge status={req.status} />
            </div>

            <dl className="mt-4 grid gap-3 text-xs text-text-secondary sm:grid-cols-2 lg:grid-cols-4">
              <Stat
                label={t("payments:reviewRequest.fee")}
                value={!paymentsEnabled || req.isFree
                  ? t("scholarships:freeListing")
                  : formatMoneyCents(req.amountCents, req.currency, i18n.language)}
              />
              {/* Held / captured / refunded only make sense in paid mode.
                  Hide them when the platform is in free mode — there's no
                  payment flow to report on. */}
              {paymentsEnabled && !req.isFree && (
                <>
                  <Stat
                    label={t("payments:reviewRequest.held")}
                    value={formatMoneyCents(req.heldAmountCents, req.currency, i18n.language)}
                  />
                  <Stat
                    label={t("payments:reviewRequest.captured")}
                    value={formatMoneyCents(req.capturedAmountCents, req.currency, i18n.language)}
                  />
                  <Stat
                    label={t("payments:reviewRequest.refunded")}
                    value={formatMoneyCents(req.refundedAmountCents, req.currency, i18n.language)}
                    tone={req.refundedAmountCents > 0 ? "warning" : "neutral"}
                  />
                </>
              )}
            </dl>

            <div className="mt-3 flex flex-wrap items-center justify-between gap-3 text-xs text-text-tertiary">
              {paymentsEnabled && !req.isFree ? (
                <span>
                  {t("payments:reviewRequest.reference")}:{" "}
                  <code className="font-mono">{req.paymentReference ?? "—"}</code>
                </span>
              ) : (
                <span />
              )}
              {TERMINAL_REQUEST_STATUSES.has(req.status) && (
                <Link
                  to={`/student/scholarships/${req.scholarshipId}`}
                  className="inline-flex items-center gap-1.5 rounded-md border border-brand-200 bg-bg-canvas px-3 py-1.5 text-xs font-medium text-brand-600 hover:bg-brand-50"
                >
                  {t("payments:reviewRequest.applyAgain")}
                </Link>
              )}
              {!TERMINAL_REQUEST_STATUSES.has(req.status) && (
                <Link
                  to={`/student/messages?with=${req.scholarshipProviderId}&name=${encodeURIComponent(req.scholarshipProviderName ?? "")}`}
                  className="inline-flex items-center gap-1.5 rounded-md border border-brand-200 bg-bg-canvas px-3 py-1.5 text-xs font-medium text-brand-600 hover:bg-brand-50"
                >
                  <MessageSquare aria-hidden className="size-3.5" />
                  {t("payments:reviewRequest.message")}
                </Link>
              )}
              {isRequestCancellableByStudent(req.status) && (
                <button
                  type="button"
                  onClick={() => {
                    // Skip the 50%-refund confirmation when the platform is
                    // in free mode or the request itself is free — there's no
                    // money to refund.
                    const showRefundConfirm = paymentsEnabled && !req.isFree
                      && refundsHalfOnCancel(req.status);
                    if (showRefundConfirm
                      && !window.confirm(t("payments:reviewRequest.cancelConfirm50"))) {
                      return;
                    }
                    setPendingCancelId(req.id);
                    cancelMut.mutate(req.id);
                  }}
                  disabled={cancelMut.isPending && pendingCancelId === req.id}
                  className="inline-flex items-center gap-1.5 rounded-md border border-danger-200 bg-bg-canvas px-3 py-1.5 text-xs font-medium text-danger-500 hover:bg-danger-50 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {cancelMut.isPending && pendingCancelId === req.id && (
                    <Loader2 aria-hidden className="size-3 animate-spin" />
                  )}
                  {paymentsEnabled && !req.isFree && refundsHalfOnCancel(req.status)
                    ? t("payments:reviewRequest.cancelWithRefund")
                    : t("payments:reviewRequest.cancel")}
                </button>
              )}
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

function Stat({
  label,
  value,
  tone = "neutral",
}: {
  label: string;
  value: string;
  tone?: "neutral" | "warning";
}) {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2">
      <dt className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
        {label}
      </dt>
      <dd
        className={
          tone === "warning"
            ? "mt-0.5 text-sm font-semibold text-warning-600"
            : "mt-0.5 text-sm font-semibold text-text-primary"
        }
      >
        {value}
      </dd>
    </div>
  );
}

function StatusBadge({ status }: { status: ScholarshipProviderReviewRequestStatus }) {
  const { t } = useTranslation(["payments"]);
  const cls: Record<ScholarshipProviderReviewRequestStatus, string> = {
    Draft: "badge-neutral",
    Submitted: "badge-brand",
    Pending: "badge-brand",
    UnderReview: "badge-warning",
    Completed: "badge-success",
    Closed: "badge-neutral",
    Cancelled: "badge-neutral",
    Failed: "badge-danger",
    CancelledByStudent: "badge-neutral",
    RejectedByScholarshipProvider: "badge-danger",
    Expired: "badge-neutral",
  };
  return (
    <span className={`badge ${cls[status]}`}>
      {t(`payments:reviewRequest.status.${status}`)}
    </span>
  );
}
