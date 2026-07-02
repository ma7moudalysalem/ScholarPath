import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { AlertTriangle, Check, ShieldOff, Star } from "lucide-react";
import {
  adminApi,
  type LowRatedScholarshipProviderRow,
  type PagedResult,
} from "@/services/api/admin";
import { apiErrorMessage } from "@/services/api/client";

/**
 * Admin queue for Companies whose average rating dropped below the platform's
 * low-rating threshold (PB-005R). Two terminal actions per row:
 *   • Clear flag   — admin reviewed, no further action needed
 *   • Suspend      — reuses the existing SetUserStatus endpoint with the
 *                    Suspended status and a contextual reason
 *
 * Sorting is server-side: newest flag first. Pagination follows the same
 * pattern as the upgrade and onboarding queues.
 */
export function AdminLowRatedCompanies() {
  const { t, i18n } = useTranslation(["admin", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery<PagedResult<LowRatedScholarshipProviderRow>>({
    queryKey: ["admin", "low-rated-companies", page],
    queryFn: () => adminApi.getLowRatedCompanies(page),
    placeholderData: keepPreviousData,
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["admin", "low-rated-companies"] });

  const clearMut = useMutation({
    mutationFn: (scholarshipProviderId: string) => adminApi.clearScholarshipProviderLowRatingFlag(scholarshipProviderId),
    onSuccess: () => {
      toast.success(t("admin:lowRatedCompanies.clearedToast"));
      void invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const suspendMut = useMutation({
    mutationFn: (scholarshipProviderId: string) =>
      adminApi.setUserStatus(scholarshipProviderId, {
        status: "Suspended",
        reason: t("admin:lowRatedCompanies.suspendReason"),
      }),
    onSuccess: () => {
      toast.success(t("admin:lowRatedCompanies.suspendedToast"));
      void invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const rows = data?.items ?? [];
  const pageSize = data?.pageSize ?? 25;
  // The wire DTO carries `totalCount`; pages are computed client-side.
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1;

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
      <header className="mb-6 flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-full bg-warning-50 text-warning-600">
          <AlertTriangle aria-hidden className="size-5" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {t("admin:lowRatedCompanies.title")}
          </h1>
          <p className="text-sm text-text-secondary">
            {t("admin:lowRatedCompanies.subtitle")}
          </p>
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
          {t("admin:lowRatedCompanies.empty")}
        </div>
      )}

      <ul className="space-y-3">
        {rows.map((row) => {
          const busy = clearMut.isPending || suspendMut.isPending;
          return (
            <li
              key={row.scholarshipProviderId}
              className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="text-base font-semibold text-text-primary">
                    {row.organizationLegalName ?? row.scholarshipProviderName}
                  </p>
                  <p className="mt-0.5 text-xs text-text-tertiary">{row.email}</p>
                </div>
                <span
                  className={
                    row.accountStatus === "Suspended"
                      ? "badge badge-danger"
                      : "badge badge-warning"
                  }
                >
                  {t(`admin:accountStatus.${row.accountStatus}`, row.accountStatus)}
                </span>
              </div>

              <dl className="mt-4 grid gap-3 text-xs text-text-secondary sm:grid-cols-2 lg:grid-cols-4">
                <Stat
                  icon={<Star className="size-3" />}
                  label={t("admin:lowRatedCompanies.average")}
                  value={row.averageRating?.toFixed(2) ?? "—"}
                  tone="warning"
                />
                <Stat
                  label={t("admin:lowRatedCompanies.reviews")}
                  value={row.reviewCount.toString()}
                />
                <Stat
                  label={t("admin:lowRatedCompanies.flaggedAt")}
                  value={format(new Date(row.flaggedAt), "dd MMM yyyy", { locale: dateLocale })}
                />
                <Stat
                  label={t("admin:lowRatedCompanies.lastReviewAt")}
                  value={
                    row.lastReviewAt
                      ? format(new Date(row.lastReviewAt), "dd MMM yyyy", { locale: dateLocale })
                      : "—"
                  }
                />
              </dl>

              <div className="mt-3 flex flex-wrap justify-end gap-2">
                <button
                  type="button"
                  onClick={() => clearMut.mutate(row.scholarshipProviderId)}
                  disabled={busy}
                  className="inline-flex items-center gap-1.5 rounded-md border border-success-200 bg-success-50 px-3 py-1.5 text-xs font-medium text-success-700 hover:bg-success-100 disabled:opacity-50"
                >
                  <Check aria-hidden className="size-3" />
                  {t("admin:lowRatedCompanies.clear")}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    if (!window.confirm(t("admin:lowRatedCompanies.suspendConfirm"))) return;
                    suspendMut.mutate(row.scholarshipProviderId);
                  }}
                  disabled={busy || row.accountStatus === "Suspended"}
                  className="inline-flex items-center gap-1.5 rounded-md border border-danger-200 bg-bg-canvas px-3 py-1.5 text-xs font-medium text-danger-500 hover:bg-danger-50 disabled:opacity-50"
                >
                  <ShieldOff aria-hidden className="size-3" />
                  {t("admin:lowRatedCompanies.suspend")}
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

function Stat({
  icon,
  label,
  value,
  tone = "neutral",
}: {
  icon?: React.ReactNode;
  label: string;
  value: string;
  tone?: "neutral" | "warning";
}) {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2">
      <dt className="flex items-center gap-1 text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
        {icon}
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
