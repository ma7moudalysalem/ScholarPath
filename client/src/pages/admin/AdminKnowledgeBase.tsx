import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import {
  Database,
  RefreshCw,
  DownloadCloud,
  Import,
  GraduationCap,
  CircleHelp,
  CircleCheck,
  Sparkles,
  CheckCircle2,
  XCircle,
  Clock,
  Zap,
} from "lucide-react";
import {
  adminApi,
  type KnowledgeBaseStatus,
  type FineTuningStatusResult,
} from "@/services/api/admin";

const KB_KEY = ["admin", "knowledge-base"] as const;
const FT_STATUS_KEY = ["admin", "fine-tuning-status"] as const;

function Stat({
  label,
  value,
  hint,
}: {
  label: string;
  value: string | number;
  hint?: string;
}) {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-elevated p-4">
      <div className="text-xs font-medium text-text-secondary">{label}</div>
      <div className="mt-2 text-2xl font-semibold tabular-nums tracking-tight">{value}</div>
      {hint && <div className="mt-1 text-xs text-text-tertiary">{hint}</div>}
    </div>
  );
}

function JobStatusBadge({ status }: { status: string }) {
  const cfg: Record<string, { icon: React.ReactNode; cls: string }> = {
    succeeded: {
      icon: <CheckCircle2 className="size-4" />,
      cls: "bg-success-50 text-success-700 border-success-200",
    },
    failed: {
      icon: <XCircle className="size-4" />,
      cls: "bg-danger-50 text-danger-700 border-danger-200",
    },
    cancelled: {
      icon: <XCircle className="size-4" />,
      cls: "bg-warning-50 text-warning-700 border-warning-200",
    },
    running: {
      icon: <RefreshCw className="size-4 animate-spin" />,
      cls: "bg-brand-50 text-brand-700 border-brand-200",
    },
    none: {
      icon: <Clock className="size-4" />,
      cls: "bg-bg-subtle text-text-secondary border-border-subtle",
    },
  };
  const { icon, cls } = cfg[status] ?? {
    icon: <Clock className="size-4" />,
    cls: "bg-bg-subtle text-text-secondary border-border-subtle",
  };
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium ${cls}`}>
      {icon}
      {status}
    </span>
  );
}

/**
 * SRS PB-016 — admin control panel for the RAG pipeline. Shows the knowledge-base
 * index status, maintenance actions, and the in-app fine-tuning job wizard.
 */
export function AdminKnowledgeBase() {
  const { t, i18n } = useTranslation(["admin", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const qc = useQueryClient();
  const [deploymentInput, setDeploymentInput] = useState("");

  const status = useQuery<KnowledgeBaseStatus>({
    queryKey: KB_KEY,
    queryFn: () => adminApi.knowledgeBaseStatus(),
  });

  const ftStatus = useQuery<FineTuningStatusResult>({
    queryKey: FT_STATUS_KEY,
    queryFn: () => adminApi.fineTuningStatus(),
    // Don't auto-refetch — admin explicitly clicks "Check status"
    enabled: false,
  });

  const rebuild = useMutation({
    mutationFn: (force: boolean) => adminApi.rebuildKnowledgeBase(force),
    onSuccess: (r) => {
      qc.setQueryData(KB_KEY, r.status);
      toast.success(
        t("admin:knowledgeBase.rebuilt", {
          reembedded: r.reembedded,
          upserted: r.upserted,
          removed: r.removed,
        }),
      );
    },
    onError: () => toast.error(t("common:status.error", { defaultValue: "Something went wrong." })),
  });

  const importMut = useMutation({
    mutationFn: () => adminApi.importExternalDataset(),
    onSuccess: (r) => {
      qc.setQueryData(KB_KEY, r.knowledgeBase.status);
      toast.success(
        t("admin:knowledgeBase.imported", {
          created: r.import.created,
          updated: r.import.updated,
        }),
      );
    },
    onError: () => toast.error(t("common:status.error", { defaultValue: "Something went wrong." })),
  });

  const exportMut = useMutation({
    mutationFn: () => adminApi.fineTuningDataset(),
    onSuccess: (d) => {
      const blob = new Blob([d.jsonl], { type: "application/jsonl" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = d.fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      toast.success(t("admin:knowledgeBase.exported", { n: d.exampleCount }));
    },
    onError: () => toast.error(t("common:status.error", { defaultValue: "Something went wrong." })),
  });

  const startFtMut = useMutation({
    mutationFn: () => adminApi.startFineTuningJob(),
    onSuccess: (r) => {
      toast.success(t("admin:knowledgeBase.ftStarted", { jobId: r.jobId }));
      // The status query is enabled:false, so invalidate is a no-op (it never
      // ran). Force a fetch so the status panel populates without the admin
      // having to click "Check status".
      void qc.refetchQueries({ queryKey: FT_STATUS_KEY });
    },
    onError: () => toast.error(t("admin:knowledgeBase.ftStartError")),
  });

  const checkStatusMut = useMutation({
    mutationFn: () => adminApi.fineTuningStatus(),
    onSuccess: (data) => {
      qc.setQueryData(FT_STATUS_KEY, data);
    },
    onError: () => toast.error(t("common:status.error", { defaultValue: "Something went wrong." })),
  });

  const activateMut = useMutation({
    mutationFn: (name: string) => adminApi.activateFineTunedModel(name),
    onSuccess: (r) => {
      toast.success(t("admin:knowledgeBase.ftActivated", { name: r.deploymentName }));
      qc.invalidateQueries({ queryKey: FT_STATUS_KEY });
      setDeploymentInput("");
    },
    onError: () => toast.error(t("admin:knowledgeBase.ftActivateError")),
  });

  const deactivateMut = useMutation({
    mutationFn: () => adminApi.deactivateFineTunedModel(),
    onSuccess: () => {
      toast.success(t("admin:knowledgeBase.ftDeactivated"));
      qc.invalidateQueries({ queryKey: FT_STATUS_KEY });
    },
    onError: () => toast.error(t("admin:knowledgeBase.ftDeactivateError")),
  });

  const busy = rebuild.isPending || importMut.isPending || exportMut.isPending;
  const s = status.data;
  const ft = checkStatusMut.data ?? ftStatus.data;

  return (
    <div className="space-y-6">
      <header>
        <h1 className="flex items-center gap-2 text-2xl font-semibold tracking-tight">
          <Database aria-hidden className="size-6 text-brand-500" />
          {t("admin:knowledgeBase.title")}
        </h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">
          {t("admin:knowledgeBase.subtitle")}
        </p>
      </header>

      {/* ── Index status ── */}
      {status.isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-24 animate-pulse rounded-lg bg-bg-subtle" />
          ))}
        </div>
      )}

      {s && (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Stat label={t("admin:knowledgeBase.totalDocs")} value={s.totalDocuments} />
            <Stat label={t("admin:knowledgeBase.scholarshipDocs")} value={s.scholarshipDocuments} />
            <Stat label={t("admin:knowledgeBase.faqDocs")} value={s.faqDocuments} />
            <Stat
              label={t("admin:knowledgeBase.embedded")}
              value={`${s.embeddedDocuments}/${s.totalDocuments}`}
              hint={
                s.pendingDocuments > 0
                  ? t("admin:knowledgeBase.pending", { n: s.pendingDocuments })
                  : t("admin:knowledgeBase.allEmbedded")
              }
            />
          </div>

          <div className="flex flex-wrap items-center gap-x-6 gap-y-1 rounded-lg border border-border-subtle bg-bg-elevated p-4 text-sm">
            <span className="text-text-secondary">
              {t("admin:knowledgeBase.model")}:{" "}
              <span className="font-medium text-text-primary">{s.activeEmbeddingModel}</span>
            </span>
            <span className="text-text-secondary">
              {t("admin:knowledgeBase.lastIndexed")}:{" "}
              <span className="font-medium text-text-primary">
                {s.lastIndexedAt
                  ? format(new Date(s.lastIndexedAt), "dd MMM yyyy, HH:mm", { locale: dateLocale })
                  : t("admin:knowledgeBase.never")}
              </span>
            </span>
          </div>
        </>
      )}

      {status.isError && (
        <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
          {t("common:status.error", { defaultValue: "Something went wrong." })}
        </div>
      )}

      {/* ── Maintenance actions ── */}
      <section className="grid gap-4 lg:grid-cols-3">
        {/* Rebuild */}
        <div className="flex flex-col rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <div className="flex items-center gap-2">
            <RefreshCw aria-hidden className="size-5 text-brand-500" />
            <h2 className="font-semibold">{t("admin:knowledgeBase.rebuildTitle")}</h2>
          </div>
          <p className="mt-1 flex-1 text-sm text-text-secondary">
            {t("admin:knowledgeBase.rebuildBody")}
          </p>
          <button
            type="button"
            onClick={() => rebuild.mutate(false)}
            disabled={busy}
            className="mt-4 inline-flex items-center justify-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
          >
            <RefreshCw aria-hidden className={`size-4 ${rebuild.isPending ? "animate-spin" : ""}`} />
            {t("admin:knowledgeBase.rebuildAction")}
          </button>
        </div>

        {/* Import dataset */}
        <div className="flex flex-col rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <div className="flex items-center gap-2">
            <Import aria-hidden className="size-5 text-brand-500" />
            <h2 className="font-semibold">{t("admin:knowledgeBase.importTitle")}</h2>
          </div>
          <p className="mt-1 flex-1 text-sm text-text-secondary">
            {t("admin:knowledgeBase.importBody")}
          </p>
          <button
            type="button"
            onClick={() => importMut.mutate()}
            disabled={busy}
            className="mt-4 inline-flex items-center justify-center gap-2 rounded-lg border border-border-subtle px-4 py-2 text-sm font-medium transition hover:border-brand-500 hover:text-brand-500 disabled:opacity-50"
          >
            <Import aria-hidden className="size-4" />
            {t("admin:knowledgeBase.importAction")}
          </button>
        </div>

        {/* Export fine-tuning dataset */}
        <div className="flex flex-col rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <div className="flex items-center gap-2">
            <DownloadCloud aria-hidden className="size-5 text-brand-500" />
            <h2 className="font-semibold">{t("admin:knowledgeBase.exportTitle")}</h2>
          </div>
          <p className="mt-1 flex-1 text-sm text-text-secondary">
            {t("admin:knowledgeBase.exportBody")}
          </p>
          <button
            type="button"
            onClick={() => exportMut.mutate()}
            disabled={busy}
            className="mt-4 inline-flex items-center justify-center gap-2 rounded-lg border border-border-subtle px-4 py-2 text-sm font-medium transition hover:border-brand-500 hover:text-brand-500 disabled:opacity-50"
          >
            <DownloadCloud aria-hidden className="size-4" />
            {t("admin:knowledgeBase.exportAction")}
          </button>
        </div>
      </section>

      {/* ── Fine-tuning job management ── */}
      <section className="space-y-4 rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <div className="flex items-center gap-2">
          <Sparkles aria-hidden className="size-5 text-brand-500" />
          <h2 className="font-semibold">{t("admin:knowledgeBase.ftTitle")}</h2>
        </div>
        <p className="text-sm text-text-secondary">{t("admin:knowledgeBase.ftBody")}</p>

        {/* Active deployment banner */}
        <div className="flex flex-wrap items-center gap-2 rounded-lg border border-border-subtle bg-bg-canvas p-3 text-sm">
          <Zap aria-hidden className="size-4 shrink-0 text-brand-500" />
          <span className="text-text-secondary">{t("admin:knowledgeBase.ftActiveDeployment")}:</span>
          <span className="font-medium text-text-primary">
            {ft?.activeDeploymentName ?? t("admin:knowledgeBase.ftNoneActive")}
          </span>
          {ft?.hasActiveDeployment && (
            <button
              type="button"
              onClick={() => deactivateMut.mutate()}
              disabled={deactivateMut.isPending}
              className="ms-auto text-xs text-danger-600 hover:underline disabled:opacity-50"
            >
              {deactivateMut.isPending
                ? t("admin:knowledgeBase.ftDeactivating")
                : t("admin:knowledgeBase.ftDeactivate")}
            </button>
          )}
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          {/* Start job */}
          <div className="rounded-lg border border-border-subtle bg-bg-canvas p-4">
            <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
              1. {t("admin:knowledgeBase.ftTitle")}
            </p>
            <button
              type="button"
              onClick={() => startFtMut.mutate()}
              disabled={startFtMut.isPending || checkStatusMut.isPending}
              className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-medium text-white transition hover:bg-brand-700 disabled:opacity-50"
            >
              <Sparkles aria-hidden className="size-4" />
              {startFtMut.isPending
                ? t("admin:knowledgeBase.ftStarting")
                : t("admin:knowledgeBase.ftStart")}
            </button>

            {/* Check status */}
            <button
              type="button"
              onClick={() => checkStatusMut.mutate()}
              disabled={checkStatusMut.isPending || startFtMut.isPending}
              className="mt-2 inline-flex w-full items-center justify-center gap-2 rounded-lg border border-border-subtle px-4 py-2 text-sm font-medium text-text-secondary transition hover:border-brand-500 hover:text-brand-600 disabled:opacity-50"
            >
              <RefreshCw
                aria-hidden
                className={`size-4 ${checkStatusMut.isPending ? "animate-spin" : ""}`}
              />
              {checkStatusMut.isPending
                ? t("admin:knowledgeBase.ftChecking")
                : t("admin:knowledgeBase.ftStatus")}
            </button>

            {/* Status result */}
            {ft && (
              <div className="mt-3 space-y-1.5 text-xs">
                {ft.jobId ? (
                  <>
                    <div className="flex items-center gap-2">
                      <span className="text-text-tertiary">{t("admin:knowledgeBase.ftStatusLabel")}:</span>
                      <JobStatusBadge status={ft.status} />
                    </div>
                    <div className="flex gap-2">
                      <span className="text-text-tertiary">{t("admin:knowledgeBase.ftJobId")}:</span>
                      <code className="font-mono text-text-secondary">{ft.jobId}</code>
                    </div>
                    {ft.fineTunedModel && (
                      <div className="flex gap-2">
                        <span className="text-text-tertiary">{t("admin:knowledgeBase.ftFinishedModel")}:</span>
                        <code className="font-mono text-text-secondary">{ft.fineTunedModel}</code>
                      </div>
                    )}
                    {ft.error && (
                      <div className="flex gap-2 text-danger-600">
                        <span>{t("admin:knowledgeBase.ftError")}:</span>
                        <span>{ft.error}</span>
                      </div>
                    )}
                  </>
                ) : (
                  <p className="text-text-tertiary">{t("admin:knowledgeBase.ftStatusNone")}</p>
                )}
              </div>
            )}
          </div>

          {/* Activate deployment */}
          <div className="rounded-lg border border-border-subtle bg-bg-canvas p-4">
            <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
              2. {t("admin:knowledgeBase.ftActivateTitle")}
            </p>
            <p className="mb-3 text-xs text-text-secondary">
              {t("admin:knowledgeBase.ftActivateBody")}
            </p>
            <label htmlFor="ft-deployment" className="mb-1 block text-xs font-medium text-text-secondary">
              {t("admin:knowledgeBase.ftDeploymentName")}
            </label>
            <input
              id="ft-deployment"
              type="text"
              value={deploymentInput}
              onChange={(e) => setDeploymentInput(e.target.value)}
              placeholder={t("admin:knowledgeBase.ftDeploymentPlaceholder")}
              className="mb-3 w-full rounded-lg border border-border-subtle bg-bg-elevated px-3 py-2 text-sm placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none"
            />
            <button
              type="button"
              onClick={() => deploymentInput.trim() && activateMut.mutate(deploymentInput.trim())}
              disabled={!deploymentInput.trim() || activateMut.isPending}
              className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-success-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-success-700 disabled:opacity-50"
            >
              <CheckCircle2 aria-hidden className="size-4" />
              {activateMut.isPending
                ? t("admin:knowledgeBase.ftActivating")
                : t("admin:knowledgeBase.ftActivate")}
            </button>
          </div>
        </div>
      </section>

      {/* ── How RAG works ── */}
      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-3 text-sm font-semibold">
          {t("admin:knowledgeBase.howTitle")}
        </h2>
        <ol className="space-y-2 text-sm text-text-secondary">
          <li className="flex gap-2">
            <GraduationCap aria-hidden className="mt-0.5 size-4 shrink-0 text-brand-500" />
            {t("admin:knowledgeBase.how1")}
          </li>
          <li className="flex gap-2">
            <CircleHelp aria-hidden className="mt-0.5 size-4 shrink-0 text-brand-500" />
            {t("admin:knowledgeBase.how2")}
          </li>
          <li className="flex gap-2">
            <CircleCheck aria-hidden className="mt-0.5 size-4 shrink-0 text-brand-500" />
            {t("admin:knowledgeBase.how3")}
          </li>
        </ol>
      </section>
    </div>
  );
}
