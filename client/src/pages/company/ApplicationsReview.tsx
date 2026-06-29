import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { CheckCircle, XCircle, Eye, Clock, Search, Filter, Download, FileText, Loader2 } from "lucide-react";
import {
  applicationsApi,
  type CompanyApplicationRow,
  type ApplicationStatus,
} from "@/services/api/applications";
import { documentsApi } from "@/services/api/documents";
import { apiErrorMessage } from "@/services/api/client";
import { PromptDialog } from "@/components/ui/PromptDialog";

export function ApplicationsReview() {
  const { t, i18n } = useTranslation("applications");
  const lang = i18n.language;
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState("");
  // Status filter chip-row, toggled by the Filters button.
  const [showFilters, setShowFilters] = useState(false);
  const [statusFilter, setStatusFilter] = useState<ApplicationStatus | "all">("all");
  // Row whose details are open in the view drawer.
  const [viewTarget, setViewTarget] = useState<CompanyApplicationRow | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ["company", "applications", statusFilter],
    queryFn: () =>
      applicationsApi.getCompanyApplications(
        undefined,
        1,
        100,
        statusFilter === "all" ? undefined : statusFilter,
      ),
  });
  const applications = data?.items ?? [];

  /**
   * When set, a {@link PromptDialog} is open for the selected application and
   * decision. Tracks both the row id and the requested status so the same
   * dialog backs both "Accept" and "Reject" actions.
   */
  const [decisionTarget, setDecisionTarget] = useState<
    | { id: string; status: "Accepted" | "Rejected" }
    | null
  >(null);

  const reviewMutation = useMutation({
    mutationFn: ({
      id,
      status,
      reason,
    }: {
      id: string;
      status: ApplicationStatus;
      reason?: string;
    }) => applicationsApi.reviewApplication(id, status, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["company", "applications"] });
      toast.success(t("companyReview.decision.success"));
      setDecisionTarget(null);
    },
    onError: (err) => {
      toast.error(apiErrorMessage(err, t("companyReview.decision.error")));
      setDecisionTarget(null);
    },
  });

  const handleDecisionClick = (id: string, status: "Accepted" | "Rejected") => {
    setDecisionTarget({ id, status });
  };

  const submitDecision = (reason: string) => {
    if (!decisionTarget) return;
    reviewMutation.mutate({
      id: decisionTarget.id,
      status: decisionTarget.status,
      reason: reason || undefined,
    });
  };

  // Status is now filtered server-side; only apply the search term here.
  const needle = searchTerm.toLowerCase();
  const filteredApps = needle
    ? applications.filter(
        (app: CompanyApplicationRow) =>
          app.studentName.toLowerCase().includes(needle) ||
          app.scholarshipTitle.toLowerCase().includes(needle),
      )
    : applications;

  // Status values worth filtering on this queue (actionable + terminal).
  const FILTER_STATUSES: (ApplicationStatus | "all")[] = [
    "all", "Pending", "UnderReview", "WaitingResult", "Accepted", "Rejected",
  ];

  return (
    <div className="mx-auto max-w-7xl p-6">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight text-text-primary">
          {t("companyReview.title")}
        </h1>
        <p className="text-sm text-text-secondary">
          {t("companyReview.subtitle")}
        </p>
      </div>

      <div className="mb-6 flex flex-col space-y-4 md:flex-row md:items-center md:justify-between md:space-y-0">
        <div className="relative w-full max-w-sm">
          <Search
            className="absolute start-3 top-1/2 -translate-y-1/2 text-text-tertiary"
            size={18}
          />
          <input
            type="text"
            placeholder={t("companyReview.searchPlaceholder")}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full rounded-lg border border-border-subtle bg-bg-elevated py-2 ps-10 pe-4 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-100"
          />
        </div>

        <button
          type="button"
          onClick={() => setShowFilters((v) => !v)}
          aria-expanded={showFilters}
          className={`flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-colors ${
            showFilters || statusFilter !== "all"
              ? "border-brand-200 bg-brand-50 text-brand-600"
              : "border-border-subtle bg-bg-elevated text-text-secondary hover:bg-bg-subtle"
          }`}
        >
          <Filter size={18} />
          <span>{t("companyReview.filters")}</span>
          {statusFilter !== "all" && (
            <span className="rounded-full bg-brand-500 px-1.5 text-[10px] font-bold text-white">1</span>
          )}
        </button>
      </div>

      {showFilters && (
        <div className="mb-6 flex flex-wrap items-center gap-2 rounded-xl border border-border-subtle bg-bg-elevated p-3">
          {FILTER_STATUSES.map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => setStatusFilter(s)}
              className={`rounded-lg px-3 py-1.5 text-xs font-semibold transition-colors ${
                statusFilter === s
                  ? "bg-brand-500 text-white"
                  : "bg-bg-subtle text-text-secondary hover:bg-bg-muted"
              }`}
            >
              {s === "all"
                ? t("companyReview.filterAll", "All")
                : t(`companyReview.status.${s}`, { defaultValue: s })}
            </button>
          ))}
        </div>
      )}

      <div className="overflow-hidden rounded-xl border border-border-subtle bg-bg-elevated shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-start text-sm">
            <thead className="bg-bg-muted text-xs font-semibold uppercase text-text-tertiary">
              <tr>
                <th className="px-6 py-4">{t("companyReview.table.student")}</th>
                <th className="px-6 py-4">{t("companyReview.table.scholarship")}</th>
                <th className="px-6 py-4">{t("companyReview.table.status")}</th>
                <th className="px-6 py-4">{t("companyReview.table.submitted")}</th>
                <th className="px-6 py-4 text-end">
                  {t("companyReview.table.actions")}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-subtle">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center">
                    <div className="flex justify-center">
                      <div className="h-6 w-6 animate-spin rounded-full border-2 border-brand-500 border-t-transparent" />
                    </div>
                  </td>
                </tr>
              ) : filteredApps.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-text-secondary">
                    {t("companyReview.empty")}
                  </td>
                </tr>
              ) : (
                filteredApps.map((app: CompanyApplicationRow) => {
                  // The CompanyApplicationRow DTO uses `applicationId` and
                  // `submittedAt` (nullable when the row is still a draft) —
                  // earlier callers referenced `app.id` / `app.createdAt`
                  // which don't exist on the shape, so `new Date(undefined)`
                  // rendered "Invalid Date" for every row.
                  const submittedLabel = app.submittedAt
                    ? new Date(app.submittedAt).toLocaleDateString(lang)
                    : "—";
                  // Pending + Applied / UnderReview / WaitingResult are all
                  // "actionable" for the company reviewer — show the Clock
                  // icon and the accept/reject buttons for any of them.
                  const isActionable =
                    app.status === "Pending"
                    || app.status === "Applied"
                    || app.status === "UnderReview"
                    || app.status === "WaitingResult";
                  return (
                  <tr key={app.applicationId} className="transition-colors hover:bg-bg-muted/50">
                    <td className="px-6 py-4">
                      <div className="font-medium text-text-primary">{app.studentName}</div>
                    </td>
                    <td className="px-6 py-4 text-text-secondary">
                      {app.scholarshipTitle}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                          app.status === "Accepted"
                            ? "bg-success-50 text-success-700"
                            : app.status === "Rejected"
                              ? "bg-danger-50 text-danger-500"
                              : "bg-warning-50 text-warning-600"
                        }`}
                      >
                        {isActionable ? (
                          <Clock size={12} className="me-1" />
                        ) : null}
                        {t(`companyReview.status.${app.status}`, { defaultValue: app.status })}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-text-tertiary">
                      {submittedLabel}
                    </td>
                    <td className="px-6 py-4 text-end">
                      <div className="flex justify-end space-x-2">
                        <button
                          type="button"
                          onClick={() => setViewTarget(app)}
                          className="p-1.5 text-text-tertiary transition-colors hover:text-brand-600"
                          aria-label={t("companyReview.actions.view")}
                          title={t("companyReview.actions.view")}
                        >
                          <Eye size={18} />
                        </button>
                        {isActionable && (
                          <>
                            <button
                              type="button"
                              onClick={() => handleDecisionClick(app.applicationId, "Accepted")}
                              className="p-1.5 text-text-tertiary transition-colors hover:text-success-600"
                              title={t("companyReview.actions.accept")}
                            >
                              <CheckCircle size={18} />
                            </button>
                            <button
                              type="button"
                              onClick={() => handleDecisionClick(app.applicationId, "Rejected")}
                              className="p-1.5 text-text-tertiary transition-colors hover:text-danger-500"
                              title={t("companyReview.actions.reject")}
                            >
                              <XCircle size={18} />
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      <PromptDialog
        open={decisionTarget !== null}
        onOpenChange={(open) => {
          if (!open) setDecisionTarget(null);
        }}
        title={
          decisionTarget?.status === "Rejected"
            ? t("companyReview.decision.rejectTitle")
            : t("companyReview.decision.acceptTitle")
        }
        inputLabel={
          decisionTarget?.status === "Rejected"
            ? t("companyReview.decision.rejectPrompt")
            : t("companyReview.decision.acceptPrompt")
        }
        inputMultiline
        variant={decisionTarget?.status === "Rejected" ? "destructive" : "default"}
        confirmLabel={
          decisionTarget?.status === "Rejected"
            ? t("companyReview.actions.reject")
            : t("companyReview.actions.accept")
        }
        loading={reviewMutation.isPending}
        onConfirm={submitDecision}
      />

      {/* View-details drawer — opened by the eye icon on each row */}
      {viewTarget && (
        <ApplicationDetailModal
          row={viewTarget}
          lang={lang}
          t={t}
          onClose={() => setViewTarget(null)}
          onDecision={(id, status) => {
            setViewTarget(null);
            handleDecisionClick(id, status);
          }}
        />
      )}
    </div>
  );
}

// ── Application detail modal ──────────────────────────────────────────────────

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function ApplicationDetailModal({
  row,
  lang,
  t,
  onClose,
  onDecision,
}: {
  row: CompanyApplicationRow;
  lang: string;
  t: ReturnType<typeof useTranslation<"applications">>["t"];
  onClose: () => void;
  onDecision: (id: string, status: "Accepted" | "Rejected") => void;
}) {
  const { data: details, isLoading } = useQuery({
    queryKey: ["company", "application", "detail", row.applicationId],
    queryFn: () => applicationsApi.getCompanyApplicationDetails(row.applicationId),
  });

  const handleDownload = async (docId: string, fileName: string) => {
    try {
      const blob = await documentsApi.download(docId);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch {
      // silent — the download icon just fails to trigger, no toast needed here
    }
  };

  // Parse flat {key: value} form data for display
  const formEntries: [string, string][] = (() => {
    if (!details?.formDataJson) return [];
    try {
      const parsed = JSON.parse(details.formDataJson) as Record<string, unknown>;
      return Object.entries(parsed).map(([k, v]) => [k, v == null ? "" : String(v)]);
    } catch {
      return [];
    }
  })();

  const isTerminal = ["Accepted", "Rejected", "Withdrawn"].includes(row.status);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-text-primary/30 p-4 backdrop-blur-sm"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="flex max-h-[90vh] w-full max-w-lg flex-col overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated shadow-elevation-3"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
      >
        {/* Header */}
        <div className="flex items-start justify-between gap-4 border-b border-border-subtle p-6 pb-4">
          <h2 className="text-lg font-bold text-text-primary">
            {t("companyReview.detail.title", "Application details")}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1 text-text-tertiary hover:bg-bg-subtle hover:text-text-primary"
            aria-label={t("companyReview.detail.close", "Close")}
          >
            <XCircle size={20} />
          </button>
        </div>

        {/* Scrollable body */}
        <div className="flex-1 overflow-y-auto p-6 pt-4 space-y-5">
          {/* Summary */}
          <dl className="space-y-3 text-sm">
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs uppercase tracking-wide text-text-tertiary">{t("companyReview.table.student")}</dt>
              <dd className="font-medium text-text-primary">{row.studentName}</dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs uppercase tracking-wide text-text-tertiary">{t("companyReview.table.scholarship")}</dt>
              <dd className="text-text-primary">{row.scholarshipTitle}</dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs uppercase tracking-wide text-text-tertiary">{t("companyReview.table.status")}</dt>
              <dd className="text-text-primary">
                {t(`companyReview.status.${row.status}`, { defaultValue: row.status })}
              </dd>
            </div>
            <div className="flex flex-col gap-0.5">
              <dt className="text-xs uppercase tracking-wide text-text-tertiary">{t("companyReview.table.submitted")}</dt>
              <dd className="text-text-primary">
                {row.submittedAt ? new Date(row.submittedAt).toLocaleDateString(lang) : "—"}
              </dd>
            </div>
          </dl>

          {isLoading && (
            <div className="flex items-center justify-center py-6">
              <Loader2 size={20} className="animate-spin text-text-tertiary" />
            </div>
          )}

          {/* Form answers */}
          {!isLoading && formEntries.length > 0 && (
            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                {t("companyReview.detail.formAnswers", "Form answers")}
              </h3>
              <dl className="space-y-2 rounded-lg border border-border-subtle bg-bg-canvas p-3 text-sm">
                {formEntries.map(([key, value]) => (
                  <div key={key}>
                    <dt className="text-xs text-text-tertiary capitalize">{key.replace(/_/g, " ")}</dt>
                    <dd className="text-text-primary whitespace-pre-wrap">{value || "—"}</dd>
                  </div>
                ))}
              </dl>
            </div>
          )}

          {/* Uploaded documents */}
          {!isLoading && details && details.documents.length > 0 && (
            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                {t("companyReview.detail.documents", "Documents")}
              </h3>
              <ul className="space-y-2">
                {details.documents.map((doc) => (
                  <li
                    key={doc.id}
                    className="flex items-center gap-3 rounded-lg border border-border-subtle bg-bg-canvas p-3 text-sm"
                  >
                    <FileText size={16} className="shrink-0 text-text-tertiary" />
                    <span className="flex-1 truncate text-text-primary">{doc.fileName}</span>
                    <span className="text-xs text-text-tertiary">{formatBytes(doc.sizeBytes)}</span>
                    <button
                      type="button"
                      onClick={() => void handleDownload(doc.id, doc.fileName)}
                      title={t("companyReview.detail.download", "Download")}
                      className="rounded-md p-1.5 text-text-tertiary transition hover:bg-bg-subtle hover:text-brand-600"
                    >
                      <Download size={15} />
                    </button>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* No documents hint */}
          {!isLoading && details && details.documents.length === 0 && !details.formDataJson && (
            <p className="text-sm text-text-tertiary">
              {t("companyReview.detail.noContent", "No form answers or documents were submitted.")}
            </p>
          )}
        </div>

        {/* Footer actions */}
        {!isTerminal && (
          <div className="flex justify-end gap-2 border-t border-border-subtle p-4">
            <button
              type="button"
              onClick={() => onDecision(row.applicationId, "Rejected")}
              className="rounded-lg border border-danger-200 px-3 py-1.5 text-sm font-medium text-danger-600 transition hover:border-danger-400 hover:bg-danger-50"
            >
              {t("companyReview.actions.reject")}
            </button>
            <button
              type="button"
              onClick={() => onDecision(row.applicationId, "Accepted")}
              className="rounded-lg bg-success-500 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-success-600"
            >
              {t("companyReview.actions.accept")}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
