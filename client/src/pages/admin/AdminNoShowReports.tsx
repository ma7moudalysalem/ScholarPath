import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { Clock, Check, X } from "lucide-react";
import { adminApi, type NoShowReportRow, type PagedResult } from "@/services/api/admin";
import { apiErrorMessage } from "@/services/api/client";

/**
 * Admin queue for no-show reports awaiting validation (PB-006R, FR-CBR-25..32).
 * Two terminal actions per row:
 *   • Validate no-show — applies the block / rating deduction / refund on the accused
 *   • Reject as false  — penalises the reporter instead (block or rating deduction)
 * Oldest report first, so the longest-waiting dispute is triaged first.
 */
export function AdminNoShowReports() {
  const { t, i18n } = useTranslation(["admin", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery<PagedResult<NoShowReportRow>>({
    queryKey: ["admin", "no-show-reports", page],
    queryFn: () => adminApi.getNoShowReports(page),
    placeholderData: keepPreviousData,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["admin", "no-show-reports"] });

  const resolveMut = useMutation({
    mutationFn: ({ reportId, isValid }: { reportId: string; isValid: boolean }) =>
      adminApi.resolveNoShowReport(reportId, { isValid }),
    onSuccess: (_r, variables) => {
      toast.success(
        variables.isValid
          ? t("admin:noShowReports.validatedToast")
          : t("admin:noShowReports.rejectedToast"),
      );
      void invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const rows = data?.items ?? [];
  const pageSize = data?.pageSize ?? 25;
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1;

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
      <header className="mb-6 flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-full bg-warning-50 text-warning-600">
          <Clock aria-hidden className="size-5" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {t("admin:noShowReports.title")}
          </h1>
          <p className="text-sm text-text-secondary">{t("admin:noShowReports.subtitle")}</p>
        </div>
      </header>

      {isLoading && (
        <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 text-sm text-text-tertiary">
          {t("common:status.loading")}
        </div>
      )}

      {isError && (
        <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
          {t("common:status.error")}
        </div>
      )}

      {data && rows.length === 0 && (
        <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-8 text-center text-sm text-text-secondary">
          {t("admin:noShowReports.empty")}
        </div>
      )}

      <ul className="space-y-3">
        {rows.map((row) => {
          const busy = resolveMut.isPending;
          const accusedLabel = t(`admin:noShowReports.role.${row.accusedRole}`, row.accusedRole);
          return (
            <li
              key={row.reportId}
              className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="text-base font-semibold text-text-primary">
                    {t("admin:noShowReports.accusedLine", {
                      accused: row.accusedName,
                      role: accusedLabel,
                    })}
                  </p>
                  <p className="mt-0.5 text-xs text-text-tertiary">
                    {t("admin:noShowReports.reporterLine", { reporter: row.reporterName })}
                  </p>
                </div>
                <span className="badge badge-warning">{accusedLabel}</span>
              </div>

              <dl className="mt-4 grid gap-3 text-xs text-text-secondary sm:grid-cols-2 lg:grid-cols-3">
                <div className="rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2">
                  <dt className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
                    {t("admin:noShowReports.sessionAt")}
                  </dt>
                  <dd className="mt-0.5 text-sm font-semibold text-text-primary">
                    {format(new Date(row.scheduledStartAt), "dd MMM yyyy, HH:mm", { locale: dateLocale })}
                  </dd>
                </div>
                <div className="rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2">
                  <dt className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
                    {t("admin:noShowReports.reportedAt")}
                  </dt>
                  <dd className="mt-0.5 text-sm font-semibold text-text-primary">
                    {format(new Date(row.reportedAt), "dd MMM yyyy, HH:mm", { locale: dateLocale })}
                  </dd>
                </div>
                {row.reporterNote && (
                  <div className="rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2 sm:col-span-2 lg:col-span-1">
                    <dt className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
                      {t("admin:noShowReports.note")}
                    </dt>
                    <dd className="mt-0.5 text-sm text-text-secondary">{row.reporterNote}</dd>
                  </div>
                )}
              </dl>

              <div className="mt-3 flex flex-wrap justify-end gap-2">
                <button
                  type="button"
                  onClick={() => {
                    if (!window.confirm(t("admin:noShowReports.validateConfirm"))) return;
                    resolveMut.mutate({ reportId: row.reportId, isValid: true });
                  }}
                  disabled={busy}
                  className="inline-flex items-center gap-1.5 rounded-md border border-danger-200 bg-danger-50 px-3 py-1.5 text-xs font-medium text-danger-700 hover:bg-danger-100 disabled:opacity-50"
                >
                  <Check aria-hidden className="size-3" />
                  {t("admin:noShowReports.validate")}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    if (!window.confirm(t("admin:noShowReports.rejectConfirm"))) return;
                    resolveMut.mutate({ reportId: row.reportId, isValid: false });
                  }}
                  disabled={busy}
                  className="inline-flex items-center gap-1.5 rounded-md border border-border-default bg-bg-canvas px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-bg-subtle disabled:opacity-50"
                >
                  <X aria-hidden className="size-3" />
                  {t("admin:noShowReports.reject")}
                </button>
              </div>
            </li>
          );
        })}
      </ul>

      {totalPages > 1 && (
        <div className="mt-6 flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
            className="rounded-md border border-border-default bg-bg-canvas px-3 py-1.5 text-xs font-medium hover:bg-bg-subtle disabled:opacity-50"
          >
            {t("common:pagination.previous")}
          </button>
          <span className="text-xs text-text-tertiary">
            {t("common:pagination.pageOf", { page, totalPages })}
          </span>
          <button
            type="button"
            onClick={() => setPage((p) => p + 1)}
            disabled={page >= totalPages}
            className="rounded-md border border-border-default bg-bg-canvas px-3 py-1.5 text-xs font-medium hover:bg-bg-subtle disabled:opacity-50"
          >
            {t("common:pagination.next")}
          </button>
        </div>
      )}
    </div>
  );
}
