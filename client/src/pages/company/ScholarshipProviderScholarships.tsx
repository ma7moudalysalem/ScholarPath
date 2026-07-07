import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { FileText, Pencil, Plus, RotateCcw, Send, Trash2 } from "lucide-react";
import {
  scholarshipsApi,
  type MyScholarship,
  type ScholarshipStatus,
} from "@/services/api/scholarships";
import { apiErrorMessage } from "@/services/api/client";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { DeadlineHint } from "@/components/scholarships/DeadlineHint";
import { formatCalendarDate } from "@/lib/dates";

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

export function ScholarshipProviderScholarships() {
  const { t, i18n } = useTranslation(["moderation", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const dateLocale = isAr ? ar : undefined;
  const queryClient = useQueryClient();

  const { data, isLoading, isError, refetch } = useQuery<MyScholarship[]>({
    queryKey: ["company", "scholarships", "mine"],
    queryFn: () => scholarshipsApi.getMine(),
  });

  const [archiveTargetId, setArchiveTargetId] = useState<string | null>(null);

  const archiveMut = useMutation({
    mutationFn: (id: string) => scholarshipsApi.archiveScholarship(id),
    onSuccess: () => {
      toast.success(t("moderation:scholarshipProviderScholarships.actions.archiveSuccess"));
      void queryClient.invalidateQueries({
        queryKey: ["company", "scholarships", "mine"],
      });
      setArchiveTargetId(null);
    },
    onError: (err) => {
      toast.error(
        apiErrorMessage(
          err,
          t("moderation:scholarshipProviderScholarships.form.error"),
        ),
      );
      setArchiveTargetId(null);
    },
  });

  const reopenMut = useMutation({
    mutationFn: (id: string) => scholarshipsApi.reopen(id),
    onSuccess: () => {
      toast.success(t("moderation:scholarshipProviderScholarships.actions.reopenSuccess"));
      void queryClient.invalidateQueries({ queryKey: ["company", "scholarships", "mine"] });
    },
    onError: (err) =>
      toast.error(
        apiErrorMessage(err, t("moderation:scholarshipProviderScholarships.form.error")),
      ),
  });

  const submitMut = useMutation({
    mutationFn: (id: string) => scholarshipsApi.submitForReview(id),
    onSuccess: () => {
      toast.success(t("moderation:scholarshipProviderScholarships.actions.submitSuccess"));
      void queryClient.invalidateQueries({ queryKey: ["company", "scholarships", "mine"] });
    },
    onError: (err) =>
      toast.error(
        apiErrorMessage(err, t("moderation:scholarshipProviderScholarships.form.error")),
      ),
  });

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {t("moderation:scholarshipProviderScholarships.title")}
          </h1>
          <p className="mt-1 text-sm text-text-secondary">
            {t("moderation:scholarshipProviderScholarships.subtitle")}
          </p>
        </div>
        <Link
          to="/company/scholarships/new"
          className="inline-flex h-10 shrink-0 items-center justify-center gap-2 rounded-lg bg-brand-500 px-4 text-sm font-medium text-white transition hover:bg-brand-600"
        >
          <Plus aria-hidden className="size-4" />
          {t("moderation:scholarshipProviderScholarships.actions.createNew")}
        </Link>
      </div>

      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipProviderScholarships.headers.title")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipProviderScholarships.headers.status")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipProviderScholarships.headers.deadline")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:scholarshipProviderScholarships.headers.applicants")}
              </th>
              <th className="px-4 py-3 text-end">
                {t("moderation:scholarshipProviderScholarships.headers.actions")}
              </th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:scholarshipProviderScholarships.loading")}
                </td>
              </tr>
            )}
            {isError && !isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:scholarshipProviderScholarships.loadError")}{" "}
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
            {!isLoading && !isError && data?.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:scholarshipProviderScholarships.empty")}
                </td>
              </tr>
            )}
            {data?.map((s) => (
              <tr
                key={s.id}
                className="border-t border-border-subtle hover:bg-bg-subtle/40"
              >
                <td className="px-4 py-3 font-medium text-text-primary">
                  {isAr ? s.titleAr || s.titleEn : s.titleEn || s.titleAr}
                  <p className="mt-0.5 text-xs font-normal text-text-tertiary">
                    {t("moderation:scholarshipProviderScholarships.createdOn", {
                      date: formatCalendarDate(s.createdAt, "dd MMM yyyy", dateLocale),
                    })}
                  </p>
                  {s.status === "Draft" && s.rejectionReason && (
                    <p className="mt-1 max-w-xs text-xs font-normal text-danger-500">
                      {t("moderation:scholarshipProviderScholarships.rejectedReason", {
                        reason: s.rejectionReason,
                      })}
                    </p>
                  )}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadgeClass(s.status)}`}
                  >
                    {t(`moderation:scholarshipStatus.${s.status}`)}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs">
                  <div className="text-text-secondary">
                    {formatCalendarDate(s.deadline, "dd MMM yyyy", dateLocale)}
                  </div>
                  <DeadlineHint deadline={s.deadline} />
                </td>
                <td className="px-4 py-3 text-text-secondary">{s.applicantCount}</td>
                <td className="px-4 py-3">
                  <div className="flex items-center justify-end gap-1.5">
                    <Link
                      to={`/company/applications-review?scholarshipId=${s.id}`}
                      title={t("moderation:scholarshipProviderScholarships.actions.reviewApplications")}
                      aria-label={t("moderation:scholarshipProviderScholarships.actions.reviewApplications")}
                      className="inline-flex size-8 items-center justify-center rounded-md text-text-secondary transition hover:bg-bg-subtle hover:text-brand-500"
                    >
                      <FileText aria-hidden className="size-4" />
                    </Link>
                    <Link
                      to={`/company/scholarships/${s.id}/edit`}
                      title={t("moderation:scholarshipProviderScholarships.actions.edit")}
                      aria-label={t("moderation:scholarshipProviderScholarships.actions.edit")}
                      className="inline-flex size-8 items-center justify-center rounded-md text-text-secondary transition hover:bg-bg-subtle hover:text-brand-500"
                    >
                      <Pencil aria-hidden className="size-4" />
                    </Link>
                    {s.status === "Draft" && (
                      <button
                        type="button"
                        onClick={() => submitMut.mutate(s.id)}
                        disabled={submitMut.isPending}
                        title={t("moderation:scholarshipProviderScholarships.actions.submitForReview")}
                        aria-label={t("moderation:scholarshipProviderScholarships.actions.submitForReview")}
                        className="inline-flex size-8 items-center justify-center rounded-md text-text-secondary transition hover:bg-brand-50 hover:text-brand-500 disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        <Send aria-hidden className="size-4" />
                      </button>
                    )}
                    {s.status === "Closed" && (
                      <button
                        type="button"
                        onClick={() => reopenMut.mutate(s.id)}
                        disabled={reopenMut.isPending}
                        title={t("moderation:scholarshipProviderScholarships.actions.reopen")}
                        aria-label={t("moderation:scholarshipProviderScholarships.actions.reopen")}
                        className="inline-flex size-8 items-center justify-center rounded-md text-text-secondary transition hover:bg-brand-50 hover:text-brand-500 disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        <RotateCcw aria-hidden className="size-4" />
                      </button>
                    )}
                    {s.status !== "Archived" && (
                      <button
                        type="button"
                        onClick={() => setArchiveTargetId(s.id)}
                        disabled={archiveMut.isPending}
                        title={t("moderation:scholarshipProviderScholarships.actions.archive")}
                        aria-label={t("moderation:scholarshipProviderScholarships.actions.archive")}
                        className="inline-flex size-8 items-center justify-center rounded-md text-text-secondary transition hover:bg-danger-50 hover:text-danger-500 disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        <Trash2 aria-hidden className="size-4" />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ConfirmDialog
        open={archiveTargetId !== null}
        onOpenChange={(open) => {
          if (!open) setArchiveTargetId(null);
        }}
        title={t("moderation:scholarshipProviderScholarships.actions.archive")}
        description={t("moderation:scholarshipProviderScholarships.actions.archiveConfirm")}
        confirmLabel={t("moderation:scholarshipProviderScholarships.actions.archive")}
        variant="destructive"
        loading={archiveMut.isPending}
        onConfirm={() => {
          if (archiveTargetId) archiveMut.mutate(archiveTargetId);
        }}
      />
    </div>
  );
}
