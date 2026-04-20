import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useState } from "react";
import { toast } from "sonner";
import { Download, Trash2, Undo2 } from "lucide-react";
import { dataPrivacyApi, type DataRequestDto } from "@/services/api/dataPrivacy";
import { format } from "date-fns";

const KEY = ["me", "data-requests"] as const;

export function DataPrivacy() {
  const { t, i18n } = useTranslation(["privacy", "common"]);
  const qc = useQueryClient();

  const { data: requests = [], isLoading } = useQuery<DataRequestDto[]>({
    queryKey: KEY,
    queryFn: () => dataPrivacyApi.listMine(),
  });

  const exportMut = useMutation({
    mutationFn: () => dataPrivacyApi.requestExport(),
    onSuccess: () => {
      toast.success(t("privacy:export.pending"));
      void qc.invalidateQueries({ queryKey: KEY });
    },
    onError: (err: { status?: number }) => {
      if (err.status === 409) toast.info(t("privacy:export.pending"));
      else toast.error(t("common:status.error"));
    },
  });

  const [deleteReason, setDeleteReason] = useState("");
  const deleteMut = useMutation({
    mutationFn: (reason: string) => dataPrivacyApi.requestDelete(reason || undefined),
    onSuccess: () => {
      setDeleteReason("");
      void qc.invalidateQueries({ queryKey: KEY });
    },
    onError: (err: { status?: number }) => {
      if (err.status === 409) toast.info(t("privacy:delete.pending", { date: "" }));
      else toast.error(t("common:status.error"));
    },
  });

  const cancelMut = useMutation({
    mutationFn: () => dataPrivacyApi.cancelDelete(),
    onSuccess: () => void qc.invalidateQueries({ queryKey: KEY }),
  });

  const activeExport = requests.find(
    (r) => r.type === "Export" && (r.status === "Pending" || r.status === "Processing"),
  );
  const completedExport = requests.find(
    (r) => r.type === "Export" && r.status === "Completed" && r.downloadUrl,
  );
  const pendingDelete = requests.find(
    (r) => r.type === "Delete" && r.status === "Pending",
  );

  const fmtDate = (iso: string | null) => {
    if (!iso) return "—";
    try {
      return format(new Date(iso), i18n.language === "ar" ? "d MMM yyyy" : "MMM d, yyyy");
    } catch {
      return iso;
    }
  };

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="mb-2 text-3xl">{t("privacy:title")}</h1>
      <p className="mb-10 text-text-secondary">{t("privacy:subtitle")}</p>

      {/* Export card */}
      <section className="mb-6 rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
        <h2 className="mb-2 flex items-center gap-2 text-xl font-semibold">
          <Download aria-hidden className="size-5 text-brand-500" />
          {t("privacy:export.heading")}
        </h2>
        <p className="mb-4 text-sm text-text-secondary">{t("privacy:export.body")}</p>

        {completedExport && (
          <div className="mb-3 flex items-center justify-between rounded-md bg-success-50 p-3 text-sm">
            <span>
              {t("privacy:export.ready", {
                expires: fmtDate(completedExport.downloadExpiresAt),
              })}
            </span>
            <a
              href={completedExport.downloadUrl!}
              className="cta-pill bg-success-500 px-4 py-1.5 text-xs text-white hover:bg-success-500/90"
              target="_blank"
              rel="noreferrer"
            >
              {t("privacy:export.download")}
            </a>
          </div>
        )}

        <button
          type="button"
          disabled={!!activeExport || exportMut.isPending}
          onClick={() => exportMut.mutate()}
          className="cta-pill bg-brand-500 px-5 py-2 text-sm text-white hover:bg-brand-600 disabled:opacity-50"
        >
          {activeExport ? t("privacy:export.pending") : t("privacy:export.cta")}
        </button>
      </section>

      {/* Delete card */}
      <section className="mb-10 rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
        <h2 className="mb-2 flex items-center gap-2 text-xl font-semibold text-danger-500">
          <Trash2 aria-hidden className="size-5" />
          {t("privacy:delete.heading")}
        </h2>
        <p className="mb-3 text-sm text-text-secondary">{t("privacy:delete.body")}</p>
        <p className="mb-4 rounded-md bg-danger-50 p-3 text-sm text-danger-500">
          {t("privacy:delete.warning")}
        </p>

        {pendingDelete ? (
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm text-text-primary">
              {t("privacy:delete.pending", { date: fmtDate(pendingDelete.scheduledProcessAt) })}
            </p>
            <button
              type="button"
              onClick={() => cancelMut.mutate()}
              disabled={cancelMut.isPending}
              className="cta-pill inline-flex items-center gap-2 border border-border-default px-4 py-2 text-sm"
            >
              <Undo2 aria-hidden className="size-4" />
              {t("privacy:delete.cancel")}
            </button>
          </div>
        ) : (
          <div className="space-y-3">
            <label className="block text-sm">
              <span className="mb-1 block font-medium">{t("privacy:delete.reasonLabel")}</span>
              <textarea
                value={deleteReason}
                onChange={(e) => setDeleteReason(e.target.value)}
                rows={3}
                className="w-full rounded-md border border-border-default bg-bg-elevated p-2 text-sm focus:border-brand-500 focus:outline-none"
              />
            </label>
            <button
              type="button"
              onClick={() => deleteMut.mutate(deleteReason)}
              disabled={deleteMut.isPending}
              className="cta-pill bg-danger-500 px-5 py-2 text-sm text-white hover:bg-danger-500/90 disabled:opacity-50"
            >
              {t("privacy:delete.cta")}
            </button>
          </div>
        )}
      </section>

      {/* History */}
      <section>
        <h3 className="mb-3 text-lg font-semibold">{t("privacy:history.heading")}</h3>
        {isLoading ? (
          <p className="text-sm text-text-tertiary">{t("common:status.loading")}</p>
        ) : requests.length === 0 ? (
          <p className="text-sm text-text-tertiary">{t("privacy:history.empty")}</p>
        ) : (
          <div className="overflow-hidden rounded-lg border border-border-subtle">
            <table className="w-full text-sm">
              <thead className="bg-bg-subtle text-start text-text-secondary">
                <tr>
                  <th className="px-3 py-2 text-start">{t("privacy:history.type")}</th>
                  <th className="px-3 py-2 text-start">{t("privacy:history.requestedAt")}</th>
                  <th className="px-3 py-2 text-start">{t("privacy:history.status")}</th>
                  <th className="px-3 py-2 text-start">{t("privacy:history.scheduledAt")}</th>
                </tr>
              </thead>
              <tbody>
                {requests.map((r) => (
                  <tr key={r.id} className="border-t border-border-subtle">
                    <td className="px-3 py-2">{r.type}</td>
                    <td className="px-3 py-2 text-text-secondary">{fmtDate(r.requestedAt)}</td>
                    <td className="px-3 py-2">{t(`privacy:status.${r.status}`)}</td>
                    <td className="px-3 py-2 text-text-secondary">{fmtDate(r.scheduledProcessAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
