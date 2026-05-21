import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Plus, ListChecks, Clock, CheckCircle2, FileText } from "lucide-react";
import { applicationsApi } from "@/services/api/applications";
import { apiErrorMessage } from "@/services/api/client";
import type { ApplicationStatus } from "@/services/api/applications";
import { queryKeys } from "@/lib/queryClient";
import { KanbanBoard } from "@/components/application/KanbanBoard";
import { AddExternalApplicationModal } from "@/components/application/AddExternalApplicationModal";
import { RatingModal } from "@/components/company/RatingModal";

/** Small stat tile for the applications header dashboard. */
function StatTile({
  icon: Icon,
  label,
  value,
  tone = "neutral",
}: {
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean }>;
  label: string;
  value: number;
  tone?: "neutral" | "brand" | "success" | "warning";
}) {
  const toneClasses: Record<string, string> = {
    neutral: "text-text-primary",
    brand:   "text-brand-600",
    success: "text-success-600",
    warning: "text-warning-600",
  };
  const iconBgClasses: Record<string, string> = {
    neutral: "bg-bg-subtle text-text-secondary",
    brand:   "bg-brand-50 text-brand-600",
    success: "bg-success-50 text-success-600",
    warning: "bg-warning-50 text-warning-600",
  };
  return (
    <div className="flex items-center gap-3 rounded-2xl border border-border-subtle bg-bg-elevated p-4 shadow-xs">
      <div
        className={`flex size-10 items-center justify-center rounded-xl ${iconBgClasses[tone]}`}
      >
        <Icon aria-hidden className="size-5" />
      </div>
      <div>
        <p className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
          {label}
        </p>
        <p className={`text-xl font-bold ${toneClasses[tone]}`}>{value}</p>
      </div>
    </div>
  );
}

export function Applications() {
  const { t } = useTranslation("applications");
  const queryClient = useQueryClient();
  const [isAddExternalOpen, setIsAddExternalOpen] = useState(false);
  const [selectedAppForReview, setSelectedAppForReview] = useState<{
    id: string;
    companyId: string;
    companyName: string;
  } | null>(null);

  const { data: applications = [], isLoading } = useQuery({
    queryKey: queryKeys.applications.mine,
    queryFn: applicationsApi.getMyApplications,
  });

  // Counters MUST stay in sync with the Kanban groupings in `KanbanBoard.tsx`:
  //   • Pending = anything in the {Pending+Applied} OR {UnderReview+Shortlisted+
  //     WaitingResult} columns — i.e. submitted but no final decision yet.
  //   • Accepted = the single Accepted column.
  //   • Active = not in a terminal column (Accepted / Rejected / Withdrawn).
  //   • Total = every non-deleted tracker (Drafts and Intending included).
  const stats = useMemo(() => {
    const total = applications.length;
    const pending = applications.filter(
      (a) =>
        a.status === "Pending" ||
        a.status === "UnderReview" ||
        a.status === "Shortlisted" ||
        a.status === "Applied" ||
        a.status === "WaitingResult",
    ).length;
    const accepted = applications.filter((a) => a.status === "Accepted").length;
    const active = applications.filter(
      (a) =>
        a.status !== "Rejected" &&
        a.status !== "Withdrawn" &&
        a.status !== "Accepted",
    ).length;
    return { total, pending, accepted, active };
  }, [applications]);

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ApplicationStatus }) =>
      applicationsApi.updateExternalStatus(id, status),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.applications.mine });
      toast.success(t("kanban.moveSuccess"));

      if (variables.status === "Accepted" || variables.status === "Rejected") {
        const app = applications.find((a) => a.applicationId === variables.id);
        if (app && app.companyId) {
          setSelectedAppForReview({
            id: app.applicationId,
            companyId: app.companyId,
            companyName: app.companyName || t("companyFallback"),
          });
        }
      }
    },
    onError: (err) => {
      toast.error(apiErrorMessage(err, t("kanban.moveError")));
    },
  });

  const handleStatusChange = (id: string, newStatus: ApplicationStatus) => {
    updateStatusMutation.mutate({ id, status: newStatus });
  };

  const handleSubmitRating = async (
    applicationId: string,
    companyId: string,
    rating: number,
    comment: string,
  ) => {
    await applicationsApi.submitReview(applicationId, companyId, rating, comment);
  };

  return (
    <div className="flex h-full flex-col gap-6">

      {/* ── Page header — rich layout ── */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary">
            {t("title")}
          </h1>
          <p className="mt-2 max-w-xl text-text-secondary">{t("subtitle")}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <span className="badge badge-brand">
            {stats.total} {t("stats.total")}
          </span>
          <button
            type="button"
            onClick={() => setIsAddExternalOpen(true)}
            className="btn btn-primary"
          >
            <Plus aria-hidden className="size-4" />
            {t("addExternal")}
          </button>
        </div>
      </div>

      {/* ── Stat tiles ── */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <StatTile
          icon={ListChecks}
          label={t("stats.total")}
          value={stats.total}
          tone="brand"
        />
        <StatTile
          icon={FileText}
          label={t("stats.active")}
          value={stats.active}
        />
        <StatTile
          icon={Clock}
          label={t("stats.pending")}
          value={stats.pending}
          tone="warning"
        />
        <StatTile
          icon={CheckCircle2}
          label={t("stats.accepted")}
          value={stats.accepted}
          tone="success"
        />
      </div>

      {/* ── Kanban ── */}
      <div className="flex-1 overflow-hidden">
        {isLoading ? (
          <div className="flex h-full min-h-[40vh] items-center justify-center">
            <div className="size-8 animate-spin rounded-full border-4 border-brand-500 border-t-transparent" />
          </div>
        ) : applications.length === 0 ? (
          /* Premium empty state */
          <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated p-12 text-center">
            <div className="mb-5 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-100 to-brand-50 text-brand-600">
              <ListChecks aria-hidden className="size-7" />
            </div>
            <h3 className="text-lg font-semibold text-text-primary">
              {t("kanban.emptyColumn")}
            </h3>
            <p className="mt-2 max-w-md text-sm text-text-secondary">
              {t("subtitle")}
            </p>
            <button
              type="button"
              onClick={() => setIsAddExternalOpen(true)}
              className="btn btn-primary btn-sm mt-6"
            >
              <Plus aria-hidden className="size-4" />
              {t("addExternal")}
            </button>
          </div>
        ) : (
          <KanbanBoard
            applications={applications}
            onStatusChange={handleStatusChange}
          />
        )}
      </div>

      <AddExternalApplicationModal
        isOpen={isAddExternalOpen}
        onOpenChange={setIsAddExternalOpen}
      />

      {selectedAppForReview && (
        <RatingModal
          isOpen={!!selectedAppForReview}
          onOpenChange={(open) => !open && setSelectedAppForReview(null)}
          applicationId={selectedAppForReview.id}
          companyId={selectedAppForReview.companyId}
          companyName={selectedAppForReview.companyName}
          onSubmitRating={handleSubmitRating}
        />
      )}
    </div>
  );
}
