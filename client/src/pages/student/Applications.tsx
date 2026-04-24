import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { LayoutGrid, ListFilter, Plus } from "lucide-react";
import { applicationsApi } from "@/services/api/applications";
import type { ApplicationStatus } from "@/services/api/applications";
import { queryKeys } from "@/lib/queryClient";
import { KanbanBoard } from "@/components/application/KanbanBoard";
import { RatingModal } from "@/components/company/RatingModal";

export function Applications() {
  const { t } = useTranslation("applications");
  const queryClient = useQueryClient();
  const [selectedAppForReview, setSelectedAppForReview] = useState<{
    id: string;
    companyId: string;
    companyName: string;
  } | null>(null);

  const { data: applications = [], isLoading } = useQuery({
    queryKey: queryKeys.applications.mine,
    queryFn: applicationsApi.getMyApplications,
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ApplicationStatus }) =>
      applicationsApi.updateExternalStatus(id, status),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.applications.mine });
      toast.success(t("kanban.moveSuccess"));

      // Trigger rating modal if moved to a final state
      if (variables.status === "Accepted" || variables.status === "Rejected") {
        const app = applications.find((a) => a.applicationId === variables.id);
        if (app && app.companyId) {
          setSelectedAppForReview({
            id: app.applicationId,
            companyId: app.companyId,
            companyName: app.companyName || "Company",
          });
        }
      }
    },
    onError: () => {
      toast.error(t("kanban.moveError"));
    },
  });

  const handleStatusChange = (id: string, newStatus: ApplicationStatus) => {
    updateStatusMutation.mutate({ id, status: newStatus });
  };

  const handleSubmitRating = async (
    applicationId: string,
    companyId: string,
    rating: number,
    comment: string
  ) => {
    await applicationsApi.submitReview(applicationId, companyId, rating, comment);
  };

  return (
    <div className="flex h-full flex-col space-y-6 p-6 lg:p-8">
      <div className="flex flex-col space-y-2 md:flex-row md:items-center md:justify-between md:space-y-0">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary">
            {t("title")}
          </h1>
          <p className="text-sm text-text-secondary">
            {t("subtitle")}
          </p>
        </div>

        <div className="flex items-center space-x-3">
          <div className="flex rounded-lg border border-border-subtle bg-bg-elevated p-1 shadow-sm dark:bg-slate-900 dark:border-slate-800">
            <button className="rounded-md bg-bg-subtle px-2 py-1 text-brand-600 dark:bg-slate-800">
              <LayoutGrid size={18} />
            </button>
            <button className="rounded-md px-2 py-1 text-text-tertiary hover:bg-bg-subtle transition-colors dark:hover:bg-slate-800">
              <ListFilter size={18} />
            </button>
          </div>
          <button className="cta-pill bg-brand-600 text-white hover:bg-brand-700 shadow-sm flex items-center">
            <Plus size={18} className="mr-1.5" />
            Add External
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-hidden">
        {isLoading ? (
          <div className="flex h-full items-center justify-center">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-brand-500 border-t-transparent" />
          </div>
        ) : (
          <KanbanBoard
            applications={applications}
            onStatusChange={handleStatusChange}
          />
        )}
      </div>

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
