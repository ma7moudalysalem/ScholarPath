import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Check, Loader2, X, CheckCircle2, MessageSquare, FileText } from "lucide-react";
import {
  scholarshipProviderReviewRequestsApi,
  type ScholarshipProviderReviewRequestDto,
  type ScholarshipProviderReviewRequestStatus,
} from "@/services/api/scholarshipProviderReviewRequests";
import { ar } from "date-fns/locale";
import { documentsApi } from "@/services/api/documents";
import { apiErrorMessage } from "@/services/api/client";
import { formatMoneyCents } from "@/services/api/payments";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { formatCalendarDate } from "@/lib/dates";
import { PromptDialog } from "@/components/ui/PromptDialog";

/**
 * ScholarshipProvider-side queue of incoming paid ScholarshipProviderReview requests. Pending rows
 * get Accept (captures payment, splits 10/90) and Reject (releases hold)
 * actions; UnderReview rows get Complete; everything else is read-only.
 *
 * Earnings row shows the locked-in commission and ScholarshipProvider share from the
 * Payment record so the company always sees the same numbers the platform
 * will pay out.
 */
export function ScholarshipProviderReviewRequests() {
  const { t, i18n } = useTranslation(["payments", "common", "scholarships"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const queryClient = useQueryClient();
  const [busyId, setBusyId] = useState<string | null>(null);
  // A reject needs a required, typed reason — captured via PromptDialog, not
  // window.prompt (which allowed an empty reason to slip through).
  const [rejectTargetId, setRejectTargetId] = useState<string | null>(null);
  // Complete opens an OPTIONAL completeness-feedback prompt for the student.
  const [completeTargetId, setCompleteTargetId] = useState<string | null>(null);
  // Master payments switch — collapses the money breakdown to a single Free
  // row and hides commission / share columns when the platform is free-mode.
  const paymentsEnabled = usePaymentsEnabled();

  const query = useQuery<ScholarshipProviderReviewRequestDto[]>({
    queryKey: ["scholarshipProviderReviewRequests", "mine", "company"],
    queryFn: () => scholarshipProviderReviewRequestsApi.listMineAsScholarshipProvider(),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({
      queryKey: ["scholarshipProviderReviewRequests", "mine", "company"],
    });

  const downloadDoc = async (id: string, name: string) => {
    try {
      const blob = await documentsApi.download(id);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = name;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      toast.error(apiErrorMessage(err, t("common:status.error")));
    }
  };

  const acceptMut = useMutation({
    mutationFn: (id: string) => scholarshipProviderReviewRequestsApi.accept(id),
    onMutate: (id) => setBusyId(id),
    onSettled: () => setBusyId(null),
    onSuccess: () => {
      toast.success(t("payments:reviewRequest.acceptSuccess"));
      void invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const rejectMut = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      scholarshipProviderReviewRequestsApi.reject(id, reason),
    onMutate: ({ id }) => setBusyId(id),
    onSettled: () => setBusyId(null),
    onSuccess: () => {
      toast.success(t("payments:reviewRequest.rejectSuccess"));
      setRejectTargetId(null);
      void invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const completeMut = useMutation({
    mutationFn: ({ id, feedback }: { id: string; feedback?: string }) =>
      scholarshipProviderReviewRequestsApi.complete(id, feedback),
    onMutate: ({ id }) => setBusyId(id),
    onSettled: () => { setBusyId(null); setCompleteTargetId(null); },
    onSuccess: () => {
      toast.success(t("payments:reviewRequest.completeSuccess"));
      void invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  return (
    <div className="mx-auto w-full max-w-5xl px-4 py-8 sm:px-6">
      <header className="mb-6">
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("payments:reviewRequest.scholarshipProviderTitle")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t(
            paymentsEnabled
              ? "payments:reviewRequest.scholarshipProviderSubtitle"
              : "payments:reviewRequest.scholarshipProviderSubtitleFree",
          )}
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
          {t("payments:reviewRequest.scholarshipProviderEmpty")}
        </div>
      )}

      <ul className="space-y-3">
        {query.data?.map((req) => {
          const isPending = req.status === "Pending";
          const isUnderReview = req.status === "UnderReview";
          const isBusy = busyId === req.id;
          return (
            <li
              key={req.id}
              className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="text-base font-semibold text-text-primary">
                    {req.scholarshipTitle}
                  </p>
                  <p className="mt-1 text-xs text-text-tertiary">
                    {req.studentName ?? t("payments:reviewRequest.unknownStudent")}
                  </p>
                  <p className="mt-1 text-xs text-text-tertiary">
                    {t("payments:reviewRequest.requestedOn", {
                      date: formatCalendarDate(
                        req.submittedAt ?? req.createdAt,
                        "dd MMM yyyy",
                        dateLocale,
                      ),
                    })}
                    {isPending && req.pendingExpiresAt && (
                      <span className="text-warning-600">
                        {" · "}
                        {t("payments:reviewRequest.respondBy", {
                          date: formatCalendarDate(req.pendingExpiresAt, "dd MMM yyyy", dateLocale),
                        })}
                      </span>
                    )}
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
                {/* Retained / commission / share are money-flow metrics that
                    don't apply in free mode — collapse the row to a single
                    Free fee when the platform is free-only. */}
                {paymentsEnabled && !req.isFree && (
                  <>
                    <Stat
                      label={t("payments:reviewRequest.retained")}
                      value={formatMoneyCents(req.retainedAmountCents, req.currency, i18n.language)}
                    />
                    <Stat
                      label={t("payments:reviewRequest.commission")}
                      value={formatMoneyCents(req.platformCommissionCents, req.currency, i18n.language)}
                    />
                    <Stat
                      label={t("payments:reviewRequest.share")}
                      value={formatMoneyCents(req.scholarshipProviderShareCents, req.currency, i18n.language)}
                      tone="success"
                    />
                  </>
                )}
              </dl>

              {/* Files the student attached for review (PB-005). */}
              {req.documents.length > 0 && (
                <div className="mt-3 space-y-1.5 border-t border-border-subtle pt-3">
                  <p className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
                    {t("payments:reviewRequest.attach.studentFiles")}
                  </p>
                  <ul className="space-y-1.5">
                    {req.documents.map((d) => (
                      <li key={d.id} className="flex items-center gap-2 text-sm">
                        <FileText aria-hidden className="size-4 shrink-0 text-text-tertiary" />
                        <button
                          type="button"
                          onClick={() => void downloadDoc(d.id, d.fileName)}
                          className="text-brand-600 hover:underline"
                        >
                          {d.fileName}
                        </button>
                        <span className="text-xs text-text-tertiary">{(d.sizeBytes / 1024).toFixed(0)} KB</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <div className="mt-3 flex flex-wrap items-center justify-between gap-3 text-xs text-text-tertiary">
                {paymentsEnabled && !req.isFree && (
                  <span>
                    {t("payments:reviewRequest.reference")}:{" "}
                    <code className="font-mono">{req.paymentReference ?? "—"}</code>
                  </span>
                )}
                <div className="flex flex-wrap gap-2">
                  {(isPending || isUnderReview) && (
                    <Link
                      to={`/company/messages?with=${req.studentId}&name=${encodeURIComponent(req.studentName ?? "")}`}
                      className="inline-flex items-center gap-1.5 rounded-md border border-brand-200 bg-bg-canvas px-3 py-1.5 text-xs font-medium text-brand-600 hover:bg-brand-50"
                    >
                      <MessageSquare aria-hidden className="size-3" />
                      {t("payments:reviewRequest.message")}
                    </Link>
                  )}
                  {isPending && (
                    <>
                      <button
                        type="button"
                        onClick={() => acceptMut.mutate(req.id)}
                        disabled={isBusy}
                        className="inline-flex items-center gap-1.5 rounded-md border border-success-200 bg-success-50 px-3 py-1.5 text-xs font-medium text-success-700 hover:bg-success-100 disabled:opacity-50"
                      >
                        {isBusy ? (
                          <Loader2 aria-hidden className="size-3 animate-spin" />
                        ) : (
                          <Check aria-hidden className="size-3" />
                        )}
                        {t("payments:reviewRequest.accept")}
                      </button>
                      <button
                        type="button"
                        onClick={() => setRejectTargetId(req.id)}
                        disabled={isBusy}
                        className="inline-flex items-center gap-1.5 rounded-md border border-danger-200 bg-bg-canvas px-3 py-1.5 text-xs font-medium text-danger-500 hover:bg-danger-50 disabled:opacity-50"
                      >
                        <X aria-hidden className="size-3" />
                        {t("payments:reviewRequest.reject")}
                      </button>
                    </>
                  )}
                  {isUnderReview && (
                    <button
                      type="button"
                      onClick={() => setCompleteTargetId(req.id)}
                      disabled={isBusy}
                      className="inline-flex items-center gap-1.5 rounded-md border border-brand-200 bg-brand-50 px-3 py-1.5 text-xs font-medium text-brand-700 hover:bg-brand-100 disabled:opacity-50"
                    >
                      {isBusy ? (
                        <Loader2 aria-hidden className="size-3 animate-spin" />
                      ) : (
                        <CheckCircle2 aria-hidden className="size-3" />
                      )}
                      {t("payments:reviewRequest.complete")}
                    </button>
                  )}
                </div>
              </div>
            </li>
          );
        })}
      </ul>

      <PromptDialog
        open={rejectTargetId !== null}
        onOpenChange={(open) => { if (!open) setRejectTargetId(null); }}
        title={t("payments:reviewRequest.reject")}
        inputLabel={t("payments:reviewRequest.rejectPrompt")}
        inputMultiline
        requireInput
        variant="destructive"
        confirmLabel={t("payments:reviewRequest.reject")}
        loading={rejectMut.isPending}
        onConfirm={(reason) => {
          const trimmed = reason.trim();
          if (!rejectTargetId || !trimmed) return;
          rejectMut.mutate({ id: rejectTargetId, reason: trimmed });
        }}
      />

      <PromptDialog
        open={completeTargetId !== null}
        onOpenChange={(open) => { if (!open) setCompleteTargetId(null); }}
        title={t("payments:reviewRequest.complete")}
        inputLabel={t("payments:reviewRequest.feedbackPrompt")}
        inputMultiline
        confirmLabel={t("payments:reviewRequest.complete")}
        loading={completeMut.isPending}
        onConfirm={(feedback) => {
          if (!completeTargetId) return;
          completeMut.mutate({ id: completeTargetId, feedback: feedback.trim() || undefined });
        }}
      />
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
  tone?: "neutral" | "success";
}) {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2">
      <dt className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
        {label}
      </dt>
      <dd
        className={
          tone === "success"
            ? "mt-0.5 text-sm font-semibold text-success-700"
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
