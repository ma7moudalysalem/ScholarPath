import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import {
  paymentsApi,
  formatMoneyCents,
  type PagedPayments,
  type PaymentDto,
  type PaymentStatus,
  type PaymentType,
} from "@/services/api/payments";
import { PromptDialog } from "@/components/ui/PromptDialog";

const STATUSES: PaymentStatus[] = [
  "Pending",
  "Held",
  "Captured",
  "Refunded",
  "PartiallyRefunded",
  "Failed",
  "Cancelled",
  "Disputed",
];
const TYPES: PaymentType[] = ["ConsultantBooking", "CompanyReview"];
const PAGE_SIZE = 20;

function statusBadgeClass(s: PaymentStatus): string {
  switch (s) {
    case "Captured":
      return "bg-success-100 text-success-600";
    case "Held":
    case "Pending":
      return "bg-warning-50 text-warning-600";
    case "Refunded":
    case "PartiallyRefunded":
      return "bg-brand-50 text-brand-600";
    case "Failed":
    case "Cancelled":
      return "bg-danger-50 text-danger-500";
    case "Disputed":
      return "bg-warning-50 text-warning-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

export function AdminPayments() {
  const { t, i18n } = useTranslation(["payments", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const qc = useQueryClient();

  const [status, setStatus] = useState<PaymentStatus | "">("");
  const [type, setType] = useState<PaymentType | "">("");
  const [page, setPage] = useState(1);

  const params = {
    status: status || undefined,
    type: type || undefined,
    page,
    pageSize: PAGE_SIZE,
  };

  const { data, isLoading, isError, refetch } = useQuery<PagedPayments>({
    queryKey: ["admin", "payments", params],
    queryFn: () => paymentsApi.listPayments(params),
    placeholderData: keepPreviousData,
  });

  const [refundTargetId, setRefundTargetId] = useState<string | null>(null);

  const refundMut = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      paymentsApi.refund(id, { reason }),
    onSuccess: () => {
      toast.success(t("payments:adminPayments.refundSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "payments"] });
      setRefundTargetId(null);
    },
    onError: () => {
      toast.error(t("payments:adminPayments.refundError"));
      setRefundTargetId(null);
    },
  });

  const confirmRefund = (p: PaymentDto) => {
    setRefundTargetId(p.id);
  };

  const submitRefund = (reason: string) => {
    if (!refundTargetId) return;
    refundMut.mutate({ id: refundTargetId, reason: reason || undefined });
  };

  const canRefund = (p: PaymentDto) => p.status === "Held" || p.status === "Captured";
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;
  const currentPage = data?.page ?? page;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("payments:adminPayments.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("payments:adminPayments.subtitle")}
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <select
          value={status}
          onChange={(e) => {
            setPage(1);
            setStatus(e.target.value as PaymentStatus | "");
          }}
          className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
        >
          <option value="">{t("payments:adminPayments.allStatuses")}</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {t(`payments:paymentStatus.${s}`)}
            </option>
          ))}
        </select>

        <select
          value={type}
          onChange={(e) => {
            setPage(1);
            setType(e.target.value as PaymentType | "");
          }}
          className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
        >
          <option value="">{t("payments:adminPayments.allTypes")}</option>
          {TYPES.map((ty) => (
            <option key={ty} value={ty}>
              {t(`payments:paymentType.${ty}`)}
            </option>
          ))}
        </select>
      </div>

      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.type")}</th>
              <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.amount")}</th>
              <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.status")}</th>
              <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.profitShare")}</th>
              <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.payee")}</th>
              <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.refunded")}</th>
              <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.created")}</th>
              <th className="px-4 py-3 text-end">{t("payments:adminPayments.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={8} className="px-4 py-6 text-center text-text-tertiary">
                  {t("payments:common.loading")}
                </td>
              </tr>
            )}
            {isError && !isLoading && (
              <tr>
                <td colSpan={8} className="px-4 py-6 text-center text-text-tertiary">
                  {t("payments:common.loadError")}{" "}
                  <button
                    type="button"
                    onClick={() => void refetch()}
                    className="text-brand-500 underline"
                  >
                    {t("payments:common.retry")}
                  </button>
                </td>
              </tr>
            )}
            {!isLoading && !isError && data?.items.length === 0 && (
              <tr>
                <td colSpan={8} className="px-4 py-6 text-center text-text-tertiary">
                  {t("payments:adminPayments.empty")}
                </td>
              </tr>
            )}
            {data?.items.map((p) => (
              <tr key={p.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                <td className="px-4 py-3">{t(`payments:paymentType.${p.type}`)}</td>
                <td className="px-4 py-3 font-medium">
                  {formatMoneyCents(p.amountCents, p.currency)}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadgeClass(p.status)}`}
                  >
                    {t(`payments:paymentStatus.${p.status}`)}
                  </span>
                </td>
                <td className="px-4 py-3 text-text-secondary">
                  {formatMoneyCents(p.profitShareAmountCents, p.currency)}
                </td>
                <td className="px-4 py-3 text-text-secondary">
                  {formatMoneyCents(p.payeeAmountCents, p.currency)}
                </td>
                <td className="px-4 py-3 text-text-secondary">
                  {formatMoneyCents(p.refundedAmountCents, p.currency)}
                </td>
                <td className="px-4 py-3 text-xs text-text-tertiary">
                  {format(new Date(p.createdAt), "yyyy-MM-dd", { locale: dateLocale })}
                </td>
                <td className="px-4 py-3 text-end">
                  {canRefund(p) && (
                    <button
                      type="button"
                      disabled={refundMut.isPending}
                      onClick={() => confirmRefund(p)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500 disabled:opacity-50"
                    >
                      {t("payments:adminPayments.refund")}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-text-secondary">
          <span>{t("payments:common.pageOf", { page: currentPage, total: totalPages })}</span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={currentPage <= 1}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("payments:common.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => p + 1)}
              disabled={currentPage >= totalPages}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("payments:common.next")}
            </button>
          </div>
        </div>
      )}

      <PromptDialog
        open={refundTargetId !== null}
        onOpenChange={(open) => {
          if (!open) setRefundTargetId(null);
        }}
        title={t("payments:adminPayments.refund")}
        description={t("payments:adminPayments.refundConfirm")}
        inputLabel={t("payments:adminPayments.refundReasonPrompt")}
        inputMultiline
        variant="destructive"
        confirmLabel={t("payments:adminPayments.refund")}
        loading={refundMut.isPending}
        onConfirm={submitRefund}
      />
    </div>
  );
}
