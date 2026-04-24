import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import {
  adminApi,
  type PagedResult,
  type UpgradeRequestRow,
  type UpgradeRequestStatus,
} from "@/services/api/admin";

const FILTERS: { value: UpgradeRequestStatus | null; key: string }[] = [
  { value: "Pending", key: "pending" },
  { value: "Approved", key: "approved" },
  { value: "Rejected", key: "rejected" },
  { value: null, key: "all" },
];

export function UpgradeQueue() {
  const { t } = useTranslation(["admin", "common"]);
  const qc = useQueryClient();
  const [filter, setFilter] = useState<UpgradeRequestStatus | null>("Pending");
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery<PagedResult<UpgradeRequestRow>>({
    queryKey: ["admin", "upgrade-queue", filter, page],
    queryFn: () => adminApi.getUpgradeQueue(filter, page),
    placeholderData: keepPreviousData,
  });

  const reviewMut = useMutation({
    mutationFn: ({ id, approve, notes }: { id: string; approve: boolean; notes?: string }) =>
      adminApi.reviewUpgrade(id, { approve, notes }),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "upgrade-queue"] });
    },
    onError: () => toast.error(t("common:status.error")),
  });

  const approve = (r: UpgradeRequestRow) => reviewMut.mutate({ id: r.id, approve: true });
  const reject = (r: UpgradeRequestRow) => {
    const notes = window.prompt(t("admin:onboarding.reviewDialog.notesLabel"));
    if (!notes) return;
    reviewMut.mutate({ id: r.id, approve: false, notes });
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t("admin:upgrades.title")}</h1>
        <div className="inline-flex rounded-md border border-border-subtle bg-bg-elevated p-0.5">
          {FILTERS.map((f) => (
            <button
              key={f.key}
              type="button"
              onClick={() => { setFilter(f.value); setPage(1); }}
              className={`rounded px-3 py-1 text-xs font-medium transition ${filter === f.value ? "bg-brand-500 text-text-on-brand" : "text-text-secondary hover:text-text-primary"}`}
            >
              {t(`admin:upgrades.filters.${f.key}`)}
            </button>
          ))}
        </div>
      </div>

      <div className="overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.email")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.target")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.status")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.reason")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.createdAt")}</th>
              <th className="px-4 py-3 text-end"></th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={6} className="px-4 py-6 text-center text-text-tertiary">{t("admin:common.loading")}</td></tr>
            )}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={6} className="px-4 py-6 text-center text-text-tertiary">{t("admin:upgrades.empty")}</td></tr>
            )}
            {data?.items.map((r: UpgradeRequestRow) => (
              <tr key={r.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                <td className="px-4 py-3 font-medium">{r.userEmail}</td>
                <td className="px-4 py-3">{r.target}</td>
                <td className="px-4 py-3">{r.status}</td>
                <td className="px-4 py-3 text-text-secondary">{r.reason ?? "—"}</td>
                <td className="px-4 py-3 text-xs text-text-tertiary">{format(new Date(r.createdAt), "yyyy-MM-dd")}</td>
                <td className="px-4 py-3 text-end">
                  {r.status === "Pending" && (
                    <div className="inline-flex gap-1.5">
                      <button type="button" onClick={() => approve(r)} className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-emerald-500 hover:text-emerald-500">
                        {t("admin:onboarding.actions.approve")}
                      </button>
                      <button type="button" onClick={() => reject(r)} className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-rose-500 hover:text-rose-500">
                        {t("admin:onboarding.actions.reject")}
                      </button>
                    </div>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
