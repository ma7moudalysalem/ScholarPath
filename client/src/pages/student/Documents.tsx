import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { Download, FileText, Trash2, Upload } from "lucide-react";
import {
  documentCategories,
  documentsApi,
  type DocumentCategory,
  type DocumentItem,
} from "@/services/api/documents";
import { ApiError, apiErrorMessage } from "@/services/api/client";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";

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
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("documents:title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">{t("documents:subtitle")}</p>
      </div>

      {/* ── Upload panel ── */}
      <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-3 text-sm font-semibold text-text-primary">
          {t("documents:upload.heading")}
        </h2>
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <input
              ref={fileInputRef}
              type="file"
              onChange={handleFilePick}
              className="hidden"
              id="document-file-input"
            />
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              className="inline-flex h-10 items-center gap-2 rounded-md border border-border-subtle bg-bg-base px-3 text-sm text-text-primary transition hover:border-brand-500"
            >
              <Upload aria-hidden className="size-4" />
              {t("documents:upload.pickFile")}
            </button>
          </div>

          <div className="flex flex-col gap-1">
            <label
              htmlFor="document-category-select"
              className="text-xs text-text-tertiary"
            >
              {t("documents:upload.category")}
            </label>
            <select
              id="document-category-select"
              value={uploadCategory}
              onChange={(e) => setUploadCategory(e.target.value as DocumentCategory)}
              className="h-10 rounded-md border border-border-subtle bg-bg-base px-3 text-sm text-text-primary"
            >
              {documentCategories.map((c) => (
                <option key={c} value={c}>
                  {t(`documents:category.${c}`)}
                </option>
              ))}
            </select>
          </div>

          <button
            type="button"
            onClick={handleUpload}
            disabled={!pendingFile || uploadMutation.isPending}
            className="inline-flex h-10 items-center gap-2 rounded-md bg-brand-500 px-4 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
          >
            {uploadMutation.isPending
              ? t("documents:upload.uploading")
              : t("documents:upload.submit")}
          </button>
        </div>

        <p className="mt-2 text-xs text-text-tertiary">
          {pendingFile ? pendingFile.name : t("documents:upload.noFile")}
          {" — "}
          {t("documents:upload.hint")}
        </p>
      </div>

      {/* ── Filter + count ── */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <select
          value={filter}
          onChange={(e) => setFilter(e.target.value as DocumentCategory | "")}
          className="h-9 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm text-text-primary"
        >
          <option value="">{t("documents:list.filterAll")}</option>
          {documentCategories.map((c) => (
            <option key={c} value={c}>
              {t(`documents:category.${c}`)}
            </option>
          ))}
        </select>
        {data && data.length > 0 && (
          <span className="text-sm text-text-tertiary">
            {t("documents:list.count", { count: data.length })}
          </span>
        )}
      </div>

      {/* ── List states ── */}
      {isLoading && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("documents:list.loading")}
        </p>
      )}

      {isError && !isLoading && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("documents:list.loadError")}{" "}
          <button
            type="button"
            onClick={() => void refetch()}
            className="text-brand-500 underline"
          >
            {t("documents:list.retry")}
          </button>
        </p>
      )}

      {!isLoading && !isError && data && data.length === 0 && (
        <div className="flex flex-col items-center justify-center rounded-xl border border-border-subtle bg-bg-elevated py-16 text-center">
          <FileText aria-hidden className="mb-3 size-10 text-text-tertiary" />
          <p className="text-sm text-text-secondary">{t("documents:list.empty")}</p>
        </div>
      )}

      {/* ── Documents table ── */}
      {!isLoading && !isError && data && data.length > 0 && (
        <div className="overflow-x-auto rounded-xl border border-border-subtle bg-bg-elevated">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border-subtle text-start text-xs uppercase text-text-tertiary">
                <th className="px-4 py-3 text-start font-medium">
                  {t("documents:headers.name")}
                </th>
                <th className="px-4 py-3 text-start font-medium">
                  {t("documents:headers.category")}
                </th>
                <th className="px-4 py-3 text-start font-medium">
                  {t("documents:headers.size")}
                </th>
                <th className="px-4 py-3 text-start font-medium">
                  {t("documents:headers.uploaded")}
                </th>
                <th className="px-4 py-3 text-end font-medium">
                  {t("documents:headers.actions")}
                </th>
              </tr>
            </thead>
            <tbody>
              {data.map((doc) => (
                <tr
                  key={doc.id}
                  className="border-b border-border-subtle last:border-0"
                >
                  <td className="px-4 py-3">
                    <span className="flex items-center gap-2 text-text-primary">
                      <FileText
                        aria-hidden
                        className="size-4 shrink-0 text-text-tertiary"
                      />
                      <span className="truncate">{doc.fileName}</span>
                    </span>
                  </td>
                  <td className="px-4 py-3 text-text-secondary">
                    {t(`documents:category.${doc.category}`)}
                  </td>
                  <td className="px-4 py-3 text-text-secondary">
                    {formatBytes(doc.sizeBytes)}
                  </td>
                  <td className="px-4 py-3 text-text-secondary">
                    {format(new Date(doc.uploadedAt), "dd MMM yyyy", { locale: dateLocale })}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => void handleDownload(doc)}
                        aria-label={t("documents:actions.download")}
                        className="inline-flex items-center gap-1 rounded-md border border-border-subtle px-2 py-1 text-xs text-text-secondary transition hover:border-brand-500 hover:text-brand-500"
                      >
                        <Download aria-hidden className="size-3.5" />
                        {t("documents:actions.download")}
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDelete(doc.id)}
                        disabled={deleteMutation.isPending}
                        aria-label={t("documents:actions.delete")}
                        className="inline-flex items-center gap-1 rounded-md border border-border-subtle px-2 py-1 text-xs text-text-secondary transition hover:border-danger-400 hover:text-danger-500 disabled:opacity-50"
                      >
                        <Trash2 aria-hidden className="size-3.5" />
                        {t("documents:actions.delete")}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
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
