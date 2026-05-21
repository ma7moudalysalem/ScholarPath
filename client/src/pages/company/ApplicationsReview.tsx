import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { CheckCircle, XCircle, Eye, Clock, Search, Filter } from "lucide-react";
import {
  applicationsApi,
  type CompanyApplicationRow,
  type ApplicationStatus,
} from "@/services/api/applications";
import { apiErrorMessage } from "@/services/api/client";
import { PromptDialog } from "@/components/ui/PromptDialog";

export function ApplicationsReview() {
  const { t, i18n } = useTranslation("applications");
  const lang = i18n.language;
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState("");

  const { data, isLoading } = useQuery({
    queryKey: ["company", "applications"],
    queryFn: () => applicationsApi.getCompanyApplications(),
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

  const filteredApps = applications.filter(
    (app: CompanyApplicationRow) =>
      app.studentName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      app.studentEmail.toLowerCase().includes(searchTerm.toLowerCase()) ||
      app.scholarshipTitle.toLowerCase().includes(searchTerm.toLowerCase()),
  );

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
          className="flex items-center space-x-2 rounded-lg border border-border-subtle bg-bg-elevated px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle"
        >
          <Filter size={18} />
          <span>{t("companyReview.filters")}</span>
        </button>
      </div>

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
    </div>
  );
}
