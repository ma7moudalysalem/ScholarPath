import { Fragment, useState } from "react";
import { Link } from "react-router";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { PlusCircle, ChevronDown, ChevronRight, ExternalLink } from "lucide-react";
import { ar } from "date-fns/locale";
import {
  scholarshipsApi,
  type PaginatedMyScholarships,
  type ScholarshipStatus,
  type ScholarshipDetail,
} from "@/services/api/scholarships";
import { PromptDialog } from "@/components/ui/PromptDialog";
import { SegmentedFilter } from "@/components/ui/SegmentedFilter";
import { DeadlineHint } from "@/components/scholarships/DeadlineHint";
import { formatCalendarDate } from "@/lib/dates";

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

// An admin must READ the listing before approving it — title + deadline alone is
// a blind approval. This lazily loads the full detail (the server lets an admin
// read an UnderReview listing) and lays out every field that matters to the call.
function ScholarshipModerationPanel({ id }: { id: string }) {
  const { t, i18n } = useTranslation(["moderation", "scholarships", "common"]);
  const isAr = i18n.language.startsWith("ar");

  const { data, isLoading, isError } = useQuery<ScholarshipDetail>({
    queryKey: ["scholarship", "detail", id],
    queryFn: () => scholarshipsApi.getById(id),
  });

  if (isLoading) {
    return (
      <p className="text-sm text-text-tertiary">
        {t("moderation:scholarshipModeration.loading")}
      </p>
    );
  }
  if (isError || !data) {
    return (
      <p className="text-sm text-danger-500">
        {t("moderation:scholarshipModeration.loadError")}
      </p>
    );
  }

  const p = "moderation:scholarshipModeration.preview";
  const description = isAr
    ? data.descriptionAr || data.descriptionEn
    : data.descriptionEn || data.descriptionAr;
  const docs = data.requiredDocuments ?? [];
  const fields = data.fieldsOfStudy ?? [];

  return (
    <div className="max-w-3xl space-y-4">
      <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.provider`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{data.ownerScholarshipProviderName || "—"}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.funding`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{t(`scholarships:fundingType.${data.fundingType}`)}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.level`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{t(`scholarships:level.${data.targetLevel}`)}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.country`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{data.country || "—"}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.mode`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">
            {data.mode === "ExternalUrl" ? t(`${p}.external`) : t(`${p}.inApp`)}
          </dd>
        </div>
        {data.reviewFeeUsd != null && (
          <div>
            <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.reviewFee`)}</dt>
            <dd className="mt-0.5 text-sm text-text-secondary">${data.reviewFeeUsd}</dd>
          </div>
        )}
      </dl>

      {description && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.description`)}</p>
          <p className="whitespace-pre-wrap text-sm text-text-secondary">{description}</p>
        </div>
      )}

      {data.eligibilityCriteria && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.eligibility`)}</p>
          <p className="whitespace-pre-wrap text-sm text-text-secondary">{data.eligibilityCriteria}</p>
        </div>
      )}

      {fields.length > 0 && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.fields`)}</p>
          <p className="text-sm text-text-secondary">{fields.join("، ")}</p>
        </div>
      )}

      {docs.length > 0 && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.requiredDocs`)}</p>
          <ul className="list-disc space-y-0.5 ps-5 text-sm text-text-secondary">
            {docs.map((d, i) => (
              <li key={i}>{d}</li>
            ))}
          </ul>
        </div>
      )}

      {data.externalUrl && (
        <a
          href={data.externalUrl}
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-1.5 text-sm text-brand-600 underline"
        >
          <ExternalLink aria-hidden className="size-3.5" />
          {t(`${p}.externalLink`)}
        </a>
      )}
    </div>
  );
}

export function AdminScholarships() {
  const { t, i18n } = useTranslation(["moderation", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const dateLocale = isAr ? ar : undefined;
  const qc = useQueryClient();

  const [status, setStatus] = useState<ScholarshipStatus>("UnderReview");
  const [page, setPage] = useState(1);
  // Which row is expanded to preview the full listing before deciding.
  const [expandedId, setExpandedId] = useState<string | null>(null);

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
      // An approved scholarship becomes publicly visible — refresh the
      // student/public scholarship lists too so it shows without a manual reload.
      void qc.invalidateQueries({ queryKey: ["scholarships"] });
    },
    onError: () => toast.error(t("moderation:scholarshipModeration.approveError")),
  });

  const [rejectTargetId, setRejectTargetId] = useState<string | null>(null);

  const rejectMut = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      scholarshipsApi.reject(id, reason),
    onSuccess: () => {
      toast.success(t("moderation:scholarshipModeration.rejectSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "scholarships"] });
      // A rejected scholarship must disappear from any public list it slipped into.
      void qc.invalidateQueries({ queryKey: ["scholarships"] });
      setRejectTargetId(null);
    },
    onError: () => {
      toast.error(t("moderation:scholarshipModeration.rejectError"));
      setRejectTargetId(null);
    },
  });

  const confirmReject = (id: string) => {
    setRejectTargetId(id);
  };

  const submitReject = (reason: string) => {
    if (!rejectTargetId) return;
    if (!reason) return; // Reason is required for rejection
    rejectMut.mutate({ id: rejectTargetId, reason });
  };

  const busy = approveMut.isPending || rejectMut.isPending;
  const totalPages = data?.totalPages ?? 1;
  const currentPage = data?.pageNumber ?? page;
  const canModerate = status === "UnderReview";

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {t("moderation:scholarshipModeration.title")}
          </h1>
          <p className="mt-1 text-sm text-text-secondary">
            {t("moderation:scholarshipModeration.subtitle")}
          </p>
        </div>
        <Link
          to="/admin/scholarships/new"
          className="inline-flex items-center gap-2 rounded-lg bg-brand-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-brand-700"
        >
          <PlusCircle aria-hidden className="size-4" />
          {t("moderation:scholarshipModeration.create")}
        </Link>
      </div>

      <SegmentedFilter
        ariaLabel={t("moderation:scholarshipModeration.headers.status")}
        value={status}
        onChange={(v) => { setPage(1); setStatus(v); }}
        options={STATUSES.map((s) => ({ value: s, label: t(`moderation:scholarshipStatus.${s}`) }))}
      />

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
            {data?.items.map((s) => {
              const isExpanded = expandedId === s.id;
              const title = isAr ? s.titleAr || s.titleEn : s.titleEn || s.titleAr;
              return (
                <Fragment key={s.id}>
                  <tr className="border-t border-border-subtle hover:bg-bg-subtle/40">
                    <td className="px-4 py-3 font-medium text-text-primary">
                      <button
                        type="button"
                        onClick={() => setExpandedId(isExpanded ? null : s.id)}
                        aria-expanded={isExpanded}
                        className="inline-flex items-start gap-1.5 text-start transition-colors hover:text-brand-600"
                      >
                        {isExpanded ? (
                          <ChevronDown aria-hidden className="mt-0.5 size-4 shrink-0 text-text-tertiary" />
                        ) : (
                          <ChevronRight aria-hidden className="mt-0.5 size-4 shrink-0 text-text-tertiary rtl:rotate-180" />
                        )}
                        <span>{title}</span>
                      </button>
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
                    <td className="px-4 py-3 text-end">
                      <div className="inline-flex gap-2">
                        <button
                          type="button"
                          onClick={() => setExpandedId(isExpanded ? null : s.id)}
                          className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-brand-400 hover:text-brand-600"
                        >
                          {isExpanded
                            ? t("moderation:scholarshipModeration.preview.hide")
                            : t("moderation:scholarshipModeration.preview.review")}
                        </button>
                        {canModerate && (
                          <>
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
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                  {isExpanded && (
                    <tr className="border-t border-border-subtle bg-bg-subtle/30">
                      <td colSpan={5} className="px-4 py-4">
                        <ScholarshipModerationPanel id={s.id} />
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
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

      <PromptDialog
        open={rejectTargetId !== null}
        onOpenChange={(open) => {
          if (!open) setRejectTargetId(null);
        }}
        title={t("moderation:scholarshipModeration.reject")}
        inputLabel={t("moderation:scholarshipModeration.rejectPrompt")}
        inputMultiline
        requireInput
        variant="destructive"
        confirmLabel={t("moderation:scholarshipModeration.reject")}
        loading={rejectMut.isPending}
        onConfirm={submitReject}
      />
    </div>
  );
}
