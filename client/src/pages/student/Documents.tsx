import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import {
  Download,
  FileText,
  Trash2,
  Upload,
  FilePlus,
  Image as ImageIcon,
  FileSpreadsheet,
  FileArchive,
  FileType,
  LayoutGrid,
  Rows3,
} from "lucide-react";
import { motion } from "motion/react";
import {
  documentCategories,
  documentsApi,
  type DocumentCategory,
  type DocumentItem,
} from "@/services/api/documents";
import { ApiError, apiErrorMessage } from "@/services/api/client";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { EmptyState } from "@/components/ui/EmptyState";

function getFileTypeIcon(fileName: string): { icon: typeof FileText; theme: string } {
  const ext = fileName.split(".").pop()?.toLowerCase() ?? "";
  if (["pdf"].includes(ext)) return { icon: FileType, theme: "from-danger-500 to-danger-600" };
  if (["doc", "docx"].includes(ext)) return { icon: FileText, theme: "from-brand-500 to-brand-700" };
  if (["xls", "xlsx", "csv"].includes(ext)) return { icon: FileSpreadsheet, theme: "from-success-500 to-success-600" };
  if (["png", "jpg", "jpeg", "webp", "gif", "svg"].includes(ext)) return { icon: ImageIcon, theme: "from-warning-500 to-warning-600" };
  if (["zip", "rar", "7z"].includes(ext)) return { icon: FileArchive, theme: "from-text-secondary to-text-primary" };
  return { icon: FileText, theme: "from-text-secondary to-text-primary" };
}

const MAX_BYTES = 25 * 1024 * 1024;

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function Documents() {
  const { t, i18n } = useTranslation(["documents", "common"]);
  const dateLocale = i18n.dir() === "rtl" ? ar : undefined;
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<DocumentCategory | "">("");
  const [uploadCategory, setUploadCategory] = useState<DocumentCategory>("Transcript");
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);
  const [view, setView] = useState<"grid" | "list">("grid");
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data, isLoading, isError, refetch } = useQuery<DocumentItem[]>({
    queryKey: ["documents", filter || "all"],
    queryFn: () => documentsApi.list(filter || undefined),
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) =>
      documentsApi.upload({ file, category: uploadCategory }),
    onSuccess: () => {
      toast.success(t("documents:upload.success"));
      setPendingFile(null);
      if (fileInputRef.current) fileInputRef.current.value = "";
      void queryClient.invalidateQueries({ queryKey: ["documents"] });
    },
    onError: (err) => {
      const message =
        err instanceof ApiError && err.payload.detail
          ? err.payload.detail
          : t("documents:upload.error");
      toast.error(message);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => documentsApi.remove(id),
    onSuccess: () => {
      toast.success(t("documents:actions.deleteSuccess"));
      void queryClient.invalidateQueries({ queryKey: ["documents"] });
      setDeleteTargetId(null);
    },
    // Surface the server's actual reason ("Document is attached to a
    // submitted application — withdraw the application first", etc.) — the
    // generic "deleteError" used to leave the user with no actionable hint.
    onError: (err) => {
      toast.error(apiErrorMessage(err, t("documents:actions.deleteError")));
      setDeleteTargetId(null);
    },
  });

  const handleFilePick = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    if (file && file.size > MAX_BYTES) {
      toast.error(t("documents:upload.tooLarge"));
      setPendingFile(null);
      e.target.value = "";
      return;
    }
    setPendingFile(file);
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(false);
    const file = e.dataTransfer.files?.[0] ?? null;
    if (!file) return;
    if (file.size > MAX_BYTES) {
      toast.error(t("documents:upload.tooLarge"));
      return;
    }
    setPendingFile(file);
  };

  const handleUpload = () => {
    if (pendingFile) uploadMutation.mutate(pendingFile);
  };

  const handleDownload = async (doc: DocumentItem) => {
    try {
      const blob = await documentsApi.download(doc.id);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = doc.fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      toast.error(apiErrorMessage(err, t("documents:actions.downloadError")));
    }
  };

  const handleDelete = (id: string) => {
    setDeleteTargetId(id);
  };

  return (
    <div className="space-y-6">
      {/* ── Header ── */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary">
            {t("documents:title")}
          </h1>
          <p className="mt-2 max-w-xl text-text-secondary">{t("documents:subtitle")}</p>
        </div>
      </div>

      {/* ── Upload panel with drag-zone ── */}
      <div
        className={`relative rounded-2xl border-2 border-dashed transition-all p-6 ${
          isDragOver
            ? "border-brand-500 bg-brand-50/40 shadow-brand-sm"
            : "border-border-default bg-bg-elevated hover:border-brand-300"
        }`}
        onDragOver={(e) => {
          e.preventDefault();
          setIsDragOver(true);
        }}
        onDragLeave={() => setIsDragOver(false)}
        onDrop={handleDrop}
      >
        <input
          ref={fileInputRef}
          type="file"
          onChange={handleFilePick}
          className="hidden"
          id="document-file-input"
        />

        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="flex items-center gap-3 flex-1 min-w-0">
            <div className={`flex size-12 shrink-0 items-center justify-center rounded-2xl transition-colors ${
              isDragOver ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm" : "bg-brand-50 text-brand-600 border border-brand-200/60"
            }`}>
              {isDragOver ? <FilePlus className="size-6" aria-hidden /> : <Upload className="size-5" aria-hidden />}
            </div>
            <div className="min-w-0">
              <h2 className="text-sm font-bold text-text-primary tracking-tight">
                {t("documents:upload.heading")}
              </h2>
              <p className="text-xs text-text-tertiary mt-0.5">
                {pendingFile ? (
                  <span className="font-medium text-text-secondary">{pendingFile.name}</span>
                ) : (
                  t("documents:upload.hint")
                )}
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2 w-full sm:w-auto">
            <select
              id="document-category-select"
              aria-label={t("documents:upload.category")}
              value={uploadCategory}
              onChange={(e) => setUploadCategory(e.target.value as DocumentCategory)}
              className="input-premium h-10 w-full sm:w-auto"
            >
              {documentCategories.map((c) => (
                <option key={c} value={c}>
                  {t(`documents:category.${c}`)}
                </option>
              ))}
            </select>
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              className="btn btn-secondary"
            >
              <Upload aria-hidden className="size-4" />
              {t("documents:upload.pickFile")}
            </button>
            <button
              type="button"
              onClick={handleUpload}
              disabled={!pendingFile || uploadMutation.isPending}
              className="btn btn-primary"
            >
              {uploadMutation.isPending
                ? t("documents:upload.uploading")
                : t("documents:upload.submit")}
            </button>
          </div>
        </div>
      </div>

      {/* ── Filter chips + view switcher ── */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-1.5 max-w-full overflow-x-auto scrollbar-premium">
          <button
            type="button"
            onClick={() => setFilter("")}
            className={`px-3 py-1.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all border ${
              filter === ""
                ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white border-transparent shadow-brand-sm"
                : "bg-bg-elevated text-text-secondary border-border-subtle hover:border-brand-300 hover:text-text-primary"
            }`}
          >
            {t("documents:list.filterAll")}
            {data && data.length > 0 && filter === "" && (
              <span className="ms-1.5 inline-flex items-center justify-center min-w-[18px] px-1 h-[18px] rounded-full bg-white/20 text-[10px] font-bold">
                {data.length}
              </span>
            )}
          </button>
          {documentCategories.map((c) => {
            const isActive = filter === c;
            const count = (data ?? []).filter((d) => d.category === c).length;
            if (count === 0 && !isActive) return null;
            return (
              <button
                key={c}
                type="button"
                onClick={() => setFilter(c)}
                className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all border ${
                  isActive
                    ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white border-transparent shadow-brand-sm"
                    : "bg-bg-elevated text-text-secondary border-border-subtle hover:border-brand-300 hover:text-text-primary"
                }`}
              >
                {t(`documents:category.${c}`)}
                {count > 0 && (
                  <span className={`inline-flex items-center justify-center min-w-[18px] px-1 h-[18px] rounded-full text-[10px] font-bold ${
                    isActive ? "bg-white/20" : "bg-bg-subtle text-text-tertiary"
                  }`}>
                    {count}
                  </span>
                )}
              </button>
            );
          })}
        </div>

        {data && data.length > 0 && (
          <div className="inline-flex items-center gap-1 bg-bg-elevated p-1 rounded-lg border border-border-subtle shadow-elevation-1">
            <button
              type="button"
              onClick={() => setView("grid")}
              aria-label={t("list.gridView", "Grid view")}
              className={`p-1.5 rounded-md transition-colors ${
                view === "grid" ? "bg-brand-50 text-brand-600" : "text-text-tertiary hover:text-text-secondary"
              }`}
            >
              <LayoutGrid size={14} />
            </button>
            <button
              type="button"
              onClick={() => setView("list")}
              aria-label={t("list.listView", "List view")}
              className={`p-1.5 rounded-md transition-colors ${
                view === "list" ? "bg-brand-50 text-brand-600" : "text-text-tertiary hover:text-text-secondary"
              }`}
            >
              <Rows3 size={14} />
            </button>
          </div>
        )}
      </div>

      {/* ── List states ── */}
      {isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="card-premium p-4 h-32 skeleton" />
          ))}
        </div>
      )}

      {isError && !isLoading && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("documents:list.loadError")}{" "}
          <button
            type="button"
            onClick={() => void refetch()}
            className="text-brand-500 underline font-medium"
          >
            {t("documents:list.retry")}
          </button>
        </p>
      )}

      {!isLoading && !isError && data && data.length === 0 && (
        <EmptyState
          icon={FileText}
          title={t("documents:list.empty")}
          description={t("documents:upload.hint")}
          action={{
            label: t("documents:upload.pickFile"),
            onClick: () => fileInputRef.current?.click(),
            leadingIcon: <Upload size={14} />,
          }}
        />
      )}

      {/* ── Documents grid / list ── */}
      {!isLoading && !isError && data && data.length > 0 && view === "grid" && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {data.map((doc, idx) => {
            const { icon: FileIcon, theme } = getFileTypeIcon(doc.fileName);
            return (
              <motion.div
                key={doc.id}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.22, delay: Math.min(idx * 0.03, 0.2) }}
                className="card-premium p-4 group flex flex-col"
              >
                <div className="flex items-start gap-3">
                  <div className={`flex size-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br ${theme} text-white shadow-elevation-2`}>
                    <FileIcon size={20} aria-hidden />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h3 dir="auto" className="font-bold text-sm text-text-primary truncate tracking-tight" title={doc.fileName}>
                      {doc.fileName}
                    </h3>
                    <p className="text-xs text-text-tertiary mt-0.5">
                      {t(`documents:category.${doc.category}`)}
                    </p>
                  </div>
                </div>
                <div className="mt-4 flex items-center justify-between text-xs text-text-tertiary border-t border-border-subtle pt-3">
                  <span className="tabular-nums">{formatBytes(doc.sizeBytes)}</span>
                  <span className="tabular-nums">
                    {format(new Date(doc.uploadedAt), "dd MMM yyyy", { locale: dateLocale })}
                  </span>
                </div>
                <div className="mt-3 flex items-center justify-end gap-1.5 opacity-0 group-hover:opacity-100 transition-opacity focus-within:opacity-100">
                  <button
                    type="button"
                    onClick={() => void handleDownload(doc)}
                    aria-label={t("documents:actions.download")}
                    className="btn btn-secondary btn-sm"
                  >
                    <Download aria-hidden className="size-3.5" />
                    {t("documents:actions.download")}
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDelete(doc.id)}
                    disabled={deleteMutation.isPending}
                    aria-label={t("documents:actions.delete")}
                    className="inline-flex items-center gap-1 rounded-lg border border-border-subtle bg-bg-elevated px-2.5 py-1.5 text-xs font-semibold text-text-secondary transition hover:border-danger-400 hover:text-danger-500 hover:bg-danger-50 disabled:opacity-50"
                  >
                    <Trash2 aria-hidden className="size-3.5" />
                  </button>
                </div>
              </motion.div>
            );
          })}
        </div>
      )}

      {!isLoading && !isError && data && data.length > 0 && view === "list" && (
        <div className="overflow-x-auto rounded-xl border border-border-subtle bg-bg-elevated shadow-elevation-1">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border-subtle text-start text-xs uppercase text-text-tertiary bg-bg-subtle/40">
                <th className="px-4 py-3 text-start font-bold tracking-wider">
                  {t("documents:headers.name")}
                </th>
                <th className="px-4 py-3 text-start font-bold tracking-wider">
                  {t("documents:headers.category")}
                </th>
                <th className="px-4 py-3 text-start font-bold tracking-wider">
                  {t("documents:headers.size")}
                </th>
                <th className="px-4 py-3 text-start font-bold tracking-wider">
                  {t("documents:headers.uploaded")}
                </th>
                <th className="px-4 py-3 text-end font-bold tracking-wider">
                  {t("documents:headers.actions")}
                </th>
              </tr>
            </thead>
            <tbody>
              {data.map((doc) => {
                const { icon: FileIcon, theme } = getFileTypeIcon(doc.fileName);
                return (
                  <tr
                    key={doc.id}
                    className="border-b border-border-subtle last:border-0 hover:bg-bg-subtle/40 transition-colors"
                  >
                    <td className="px-4 py-3">
                      <span className="flex items-center gap-3 text-text-primary">
                        <span className={`inline-flex size-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br ${theme} text-white shadow-elevation-1`}>
                          <FileIcon size={14} aria-hidden />
                        </span>
                        <span dir="auto" className="truncate font-medium">{doc.fileName}</span>
                      </span>
                    </td>
                    <td className="px-4 py-3 text-text-secondary">
                      {t(`documents:category.${doc.category}`)}
                    </td>
                    <td className="px-4 py-3 text-text-secondary tabular-nums">
                      {formatBytes(doc.sizeBytes)}
                    </td>
                    <td className="px-4 py-3 text-text-secondary tabular-nums">
                      {format(new Date(doc.uploadedAt), "dd MMM yyyy", { locale: dateLocale })}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => void handleDownload(doc)}
                          aria-label={t("documents:actions.download")}
                          className="inline-flex items-center gap-1 rounded-md border border-border-subtle bg-bg-elevated px-2 py-1 text-xs font-medium text-text-secondary transition hover:border-brand-500 hover:text-brand-500"
                        >
                          <Download aria-hidden className="size-3.5" />
                          {t("documents:actions.download")}
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(doc.id)}
                          disabled={deleteMutation.isPending}
                          aria-label={t("documents:actions.delete")}
                          className="inline-flex items-center gap-1 rounded-md border border-border-subtle bg-bg-elevated px-2 py-1 text-xs font-medium text-text-secondary transition hover:border-danger-400 hover:text-danger-500 disabled:opacity-50"
                        >
                          <Trash2 aria-hidden className="size-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmDialog
        open={deleteTargetId !== null}
        onOpenChange={(open) => {
          if (!open) setDeleteTargetId(null);
        }}
        title={t("documents:actions.delete")}
        description={t("documents:actions.deleteConfirm")}
        confirmLabel={t("documents:actions.delete")}
        variant="destructive"
        loading={deleteMutation.isPending}
        onConfirm={() => {
          if (deleteTargetId) deleteMutation.mutate(deleteTargetId);
        }}
      />
    </div>
  );
}
