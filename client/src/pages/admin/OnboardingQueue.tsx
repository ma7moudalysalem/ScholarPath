import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { adminApi, type OnboardingRequestRow, type PagedResult } from "@/services/api/admin";

export function OnboardingQueue() {
  const { t } = useTranslation(["admin", "common"]);
  const qc = useQueryClient();

  // For now the queue renders the first page; pagination will come with filters
  // when the volume warrants it.
  const { data, isLoading } = useQuery<PagedResult<OnboardingRequestRow>>({
    queryKey: ["admin", "onboarding-queue", 1],
    queryFn: () => adminApi.getOnboardingQueue(1),
    placeholderData: keepPreviousData,
  });

  const reviewMut = useMutation({
    mutationFn: ({ userId, approve, notes }: { userId: string; approve: boolean; notes?: string }) =>
      adminApi.reviewOnboarding(userId, { approve, notes }),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "onboarding-queue"] });
      void qc.invalidateQueries({ queryKey: ["admin", "analytics", "overview"] });
    },
    onError: () => toast.error(t("common:status.error")),
  });

  const approve = (u: OnboardingRequestRow) => reviewMut.mutate({ userId: u.userId, approve: true });
  const reject = (u: OnboardingRequestRow) => {
    const notes = window.prompt(t("admin:onboarding.reviewDialog.notesLabel"));
    if (!notes) return;
    reviewMut.mutate({ userId: u.userId, approve: false, notes });
  };

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight">{t("admin:onboarding.title")}</h1>

      <div className="overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.name")}</th>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.email")}</th>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.role")}</th>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.createdAt")}</th>
              <th className="px-4 py-3 text-end"></th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">{t("admin:common.loading")}</td></tr>
            )}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">{t("admin:onboarding.empty")}</td></tr>
            )}
            {data?.items.map((u: OnboardingRequestRow) => (
              <tr key={u.userId} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                <td className="px-4 py-3 font-medium">{u.fullName}</td>
                <td className="px-4 py-3">{u.email}</td>
                <td className="px-4 py-3 text-text-secondary">{u.requestedRole ?? "—"}</td>
                <td className="px-4 py-3 text-xs text-text-tertiary">{format(new Date(u.createdAt), "yyyy-MM-dd")}</td>
                <td className="px-4 py-3 text-end">
                  <div className="inline-flex gap-1.5">
                    <button
                      type="button"
                      onClick={() => approve(u)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-emerald-500 hover:text-emerald-500"
                    >
                      {t("admin:onboarding.actions.approve")}
                    </button>
                    <button
                      type="button"
                      onClick={() => reject(u)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-rose-500 hover:text-rose-500"
                    >
                      {t("admin:onboarding.actions.reject")}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
