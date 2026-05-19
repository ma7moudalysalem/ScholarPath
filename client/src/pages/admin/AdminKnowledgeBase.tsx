import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { toast } from "sonner";
import {
  Database,
  RefreshCw,
  DownloadCloud,
  Import,
  GraduationCap,
  CircleHelp,
  CircleCheck,
} from "lucide-react";
import { adminApi, type KnowledgeBaseStatus } from "@/services/api/admin";

const KB_KEY = ["admin", "knowledge-base"] as const;

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

/**
 * SRS PB-016 — admin control panel for the RAG pipeline. Shows the knowledge-base
 * index status and drives the three maintenance actions: rebuild/re-embed the
 * index, import the curated external scholarships dataset, and export a
 * fine-tuning dataset built from the platform's own data.
 */
export function AdminKnowledgeBase() {
  const { t } = useTranslation(["admin", "common"]);
  const qc = useQueryClient();

  const status = useQuery<KnowledgeBaseStatus>({
    queryKey: KB_KEY,
    queryFn: () => adminApi.knowledgeBaseStatus(),
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
      toast.success(
        t("admin:knowledgeBase.exported", { n: d.exampleCount }),
      );
    },
    onError: () => toast.error(t("common:status.error", { defaultValue: "Something went wrong." })),
  });

  const busy = rebuild.isPending || importMut.isPending || exportMut.isPending;
  const s = status.data;

  return (
    <div className="space-y-6">
      <header>
        <h1 className="flex items-center gap-2 text-2xl font-semibold tracking-tight">
          <Database aria-hidden className="size-6 text-brand-500" />
          {t("admin:knowledgeBase.title", { defaultValue: "AI knowledge base (RAG)" })}
        </h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">
          {t("admin:knowledgeBase.subtitle", {
            defaultValue:
              "The retrieval-augmented-generation index behind the chatbot. Scholarships and FAQ entries are embedded into a searchable vector store that grounds every AI answer in real platform data.",
          })}
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
            <Stat
              label={t("admin:knowledgeBase.totalDocs", { defaultValue: "Documents" })}
              value={s.totalDocuments}
            />
            <Stat
              label={t("admin:knowledgeBase.scholarshipDocs", { defaultValue: "Scholarships" })}
              value={s.scholarshipDocuments}
            />
            <Stat
              label={t("admin:knowledgeBase.faqDocs", { defaultValue: "FAQ entries" })}
              value={s.faqDocuments}
            />
            <Stat
              label={t("admin:knowledgeBase.embedded", { defaultValue: "Embedded" })}
              value={`${s.embeddedDocuments}/${s.totalDocuments}`}
              hint={
                s.pendingDocuments > 0
                  ? t("admin:knowledgeBase.pending", { n: s.pendingDocuments })
                  : t("admin:knowledgeBase.allEmbedded", { defaultValue: "fully indexed" })
              }
            />
          </div>

          <div className="flex flex-wrap items-center gap-x-6 gap-y-1 rounded-lg border border-border-subtle bg-bg-elevated p-4 text-sm">
            <span className="text-text-secondary">
              {t("admin:knowledgeBase.model", { defaultValue: "Embedding model" })}:{" "}
              <span className="font-medium text-text-primary">{s.activeEmbeddingModel}</span>
            </span>
            <span className="text-text-secondary">
              {t("admin:knowledgeBase.lastIndexed", { defaultValue: "Last indexed" })}:{" "}
              <span className="font-medium text-text-primary">
                {s.lastIndexedAt
                  ? format(new Date(s.lastIndexedAt), "dd MMM yyyy, HH:mm")
                  : t("admin:knowledgeBase.never", { defaultValue: "never" })}
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
            <h2 className="font-semibold">
              {t("admin:knowledgeBase.rebuildTitle", { defaultValue: "Rebuild index" })}
            </h2>
          </div>
          <p className="mt-1 flex-1 text-sm text-text-secondary">
            {t("admin:knowledgeBase.rebuildBody", {
              defaultValue:
                "Re-scan scholarships and the FAQ dataset, then embed any new or changed documents.",
            })}
          </p>
          <button
            type="button"
            onClick={() => rebuild.mutate(false)}
            disabled={busy}
            className="mt-4 inline-flex items-center justify-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
          >
            <RefreshCw aria-hidden className={`size-4 ${rebuild.isPending ? "animate-spin" : ""}`} />
            {t("admin:knowledgeBase.rebuildAction", { defaultValue: "Rebuild" })}
          </button>
        </div>

        {/* Import dataset */}
        <div className="flex flex-col rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <div className="flex items-center gap-2">
            <Import aria-hidden className="size-5 text-brand-500" />
            <h2 className="font-semibold">
              {t("admin:knowledgeBase.importTitle", { defaultValue: "Import dataset" })}
            </h2>
          </div>
          <p className="mt-1 flex-1 text-sm text-text-secondary">
            {t("admin:knowledgeBase.importBody", {
              defaultValue:
                "Import the curated external scholarships dataset into the catalogue, then re-index.",
            })}
          </p>
          <button
            type="button"
            onClick={() => importMut.mutate()}
            disabled={busy}
            className="mt-4 inline-flex items-center justify-center gap-2 rounded-lg border border-border-subtle px-4 py-2 text-sm font-medium transition hover:border-brand-500 hover:text-brand-500 disabled:opacity-50"
          >
            <Import aria-hidden className="size-4" />
            {t("admin:knowledgeBase.importAction", { defaultValue: "Import + re-index" })}
          </button>
        </div>

        {/* Export fine-tuning dataset */}
        <div className="flex flex-col rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <div className="flex items-center gap-2">
            <DownloadCloud aria-hidden className="size-5 text-brand-500" />
            <h2 className="font-semibold">
              {t("admin:knowledgeBase.exportTitle", { defaultValue: "Fine-tuning dataset" })}
            </h2>
          </div>
          <p className="mt-1 flex-1 text-sm text-text-secondary">
            {t("admin:knowledgeBase.exportBody", {
              defaultValue:
                "Export a chat JSONL dataset built from the FAQ and catalogue for an Azure OpenAI fine-tuning job.",
            })}
          </p>
          <button
            type="button"
            onClick={() => exportMut.mutate()}
            disabled={busy}
            className="mt-4 inline-flex items-center justify-center gap-2 rounded-lg border border-border-subtle px-4 py-2 text-sm font-medium transition hover:border-brand-500 hover:text-brand-500 disabled:opacity-50"
          >
            <DownloadCloud aria-hidden className="size-4" />
            {t("admin:knowledgeBase.exportAction", { defaultValue: "Export .jsonl" })}
          </button>
        </div>
      </section>

      {/* ── How RAG works ── */}
      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-3 text-sm font-semibold">
          {t("admin:knowledgeBase.howTitle", { defaultValue: "How retrieval-augmented generation works here" })}
        </h2>
        <ol className="space-y-2 text-sm text-text-secondary">
          <li className="flex gap-2">
            <GraduationCap aria-hidden className="mt-0.5 size-4 shrink-0 text-brand-500" />
            {t("admin:knowledgeBase.how1", {
              defaultValue:
                "Every open scholarship and FAQ entry is turned into a document and embedded into a vector.",
            })}
          </li>
          <li className="flex gap-2">
            <CircleHelp aria-hidden className="mt-0.5 size-4 shrink-0 text-brand-500" />
            {t("admin:knowledgeBase.how2", {
              defaultValue:
                "When a student asks the chatbot, the question is embedded and the closest documents are retrieved.",
            })}
          </li>
          <li className="flex gap-2">
            <CircleCheck aria-hidden className="mt-0.5 size-4 shrink-0 text-brand-500" />
            {t("admin:knowledgeBase.how3", {
              defaultValue:
                "Those documents are injected into the prompt as context, so answers stay grounded — and are shown as citations.",
            })}
          </li>
        </ol>
      </section>
    </div>
  );
}
