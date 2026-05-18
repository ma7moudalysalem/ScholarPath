import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import {
  scholarshipsApi,
  type PaginatedMyScholarships,
  type ScholarshipStatus,
} from "@/services/api/scholarships";

const STATUSES: ScholarshipStatus[] = [
  "UnderReview",
  "Draft",
  "Open",
  "Closed",
  "Archived",
];
const PAGE_SIZE = 20;

function statusBadgeClass(s: ScholarshipStatus): string {
  switch (s) {
    case "Open":
      return "bg-success-100 text-success-600";
    case "UnderReview":
      return "bg-warning-50 text-warning-600";
    case "Closed":
    case "Archived":
      return "bg-danger-50 text-danger-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

export function AdminScholarships() {
  const { t, i18n } = useTranslation(["moderation", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  const [status, setStatus] = useState<ScholarshipStatus>("UnderReview");
  const [page, setPage] = useState(1);

  const { data, isLoading, isError, refetch } = useQuery<PaginatedMyScholarships>({
    queryKey: ["admin", "scholarships", status, page],
    queryFn: () => scholarshipsApi.getForModeration(status, page, PAGE_SIZE),
    placeholderData: keepPreviousData,
  });

  const approveMut = useMutation({
    mutationFn: (id: string) => scholarshipsApi.approve(id),
    onSuccess: () => {
      toast.success(t("moderation:scholarshipModeration.approveSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "scholarships"] });
    },
    onError: () => toast.error(t("moderation:scholarshipModeration.approveError")),
  });

  const rejectMut = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      scholarshipsApi.reject(id, reason),
    onSuccess: () => {
      toast.success(t("moderation:scholarshipModeration.rejectSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "scholarships"] });
    },
    onError: () => toast.error(t("moderation:scholarshipModeration.rejectError")),
  });

  const confirmReject = (id: string) => {
    const reason = window.prompt(t("moderation:scholarshipModeration.rejectPrompt"));
    if (reason && reason.trim()) rejectMut.mutate({ id, reason: reason.trim() });
  };

  const busy = approveMut.isPending || rejectMut.isPending;
  const totalPages = data?.totalPages ?? 1;
  const currentPage = data?.pageNumber ?? page;
  const canModerate = status === "UnderReview";

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("moderation:scholarshipModeration.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("moderation:scholarshipModeration.subtitle")}
        </p>
      </div>

      <select
        value={status}
        onChange={(e) => {
          setPage(1);
          setStatus(e.target.value as ScholarshipStatus);
        }}
        className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm text-text-primary"
      >
        {STATUSES.map((s) => (
          <option key={s} value={s}>
            {t(`moderation:scholarshipStatus.${s}`)}
          </option>
        ))}
      </select>

      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipModeration.headers.title")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipModeration.headers.status")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipModeration.headers.deadline")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipModeration.headers.applicants")}
              </th>
              <th className="px-4 py-3 text-end">
                {t("moderation:scholarshipModeration.headers.actions")}
              </th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:scholarshipModeration.loading")}
                </td>
              </tr>
            )}
            {isError && !isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:scholarshipModeration.loadError")}{" "}
                  <button
                    type="button"
                    onClick={() => void refetch()}
                    className="text-brand-500 underline"
                  >
                    {t("moderation:common.retry")}
                  </button>
                </td>
              </tr>
            )}
            {!isLoading && !isError && data?.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:scholarshipModeration.empty")}
                </td>
              </tr>
            )}
            {data?.items.map((s) => (
              <tr
                key={s.id}
                className="border-t border-border-subtle hover:bg-bg-subtle/40"
              >
                <td className="px-4 py-3 font-medium text-text-primary">
                  {isAr ? s.titleAr || s.titleEn : s.titleEn || s.titleAr}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadgeClass(s.status)}`}
                  >
                    {t(`moderation:scholarshipStatus.${s.status}`)}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs text-text-tertiary">
                  {format(new Date(s.deadline), "yyyy-MM-dd")}
                </td>
                <td className="px-4 py-3 text-text-secondary">{s.applicantCount}</td>
                <td className="px-4 py-3 text-end">
                  {canModerate && (
                    <div className="inline-flex gap-2">
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => approveMut.mutate(s.id)}
                        className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-success-500 hover:text-success-600 disabled:opacity-50"
                      >
                        {t("moderation:scholarshipModeration.approve")}
                      </button>
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => confirmReject(s.id)}
                        className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500 disabled:opacity-50"
                      >
                        {t("moderation:scholarshipModeration.reject")}
                      </button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-text-secondary">
          <span>
            {t("moderation:common.pageOf", { page: currentPage, total: totalPages })}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={currentPage <= 1}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("moderation:common.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => p + 1)}
              disabled={currentPage >= totalPages}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("moderation:common.next")}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
