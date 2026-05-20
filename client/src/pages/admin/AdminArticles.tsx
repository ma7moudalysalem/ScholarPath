import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Star } from "lucide-react";
import {
  resourcesApi,
  type ResourceListItem,
  type PaginatedResources,
} from "@/services/api/resources";
import { PromptDialog } from "@/components/ui/PromptDialog";
import { cn } from "@/lib/utils";

type Tab = "pending" | "published";

// ─── Pending-review tab ───────────────────────────────────────────────────────

function PendingTab() {
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  const { data, isLoading, isError, refetch } = useQuery<ResourceListItem[]>({
    queryKey: ["admin", "resources", "pending"],
    queryFn: () => resourcesApi.getPendingReview(),
  });

  const approveMut = useMutation({
    mutationFn: (id: string) => resourcesApi.approve(id),
    onSuccess: () => {
      toast.success(t("resources:moderation.approveSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "resources", "pending"] });
      // Also refresh published list so the newly-approved resource appears
      void qc.invalidateQueries({ queryKey: ["admin", "resources", "published"] });
    },
    onError: () => toast.error(t("resources:moderation.approveError")),
  });

  const [rejectTargetId, setRejectTargetId] = useState<string | null>(null);

  const rejectMut = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      resourcesApi.reject(id, reason),
    onSuccess: () => {
      toast.success(t("resources:moderation.rejectSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "resources", "pending"] });
      setRejectTargetId(null);
    },
    onError: () => {
      toast.error(t("resources:moderation.rejectError"));
      setRejectTargetId(null);
    },
  });

  const busy = approveMut.isPending || rejectMut.isPending;

  return (
    <>
      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("resources:moderation.headers.title")}</th>
              <th className="px-4 py-3 text-start">{t("resources:moderation.headers.type")}</th>
              <th className="px-4 py-3 text-start">{t("resources:moderation.headers.author")}</th>
              <th className="px-4 py-3 text-start">{t("resources:moderation.headers.tags")}</th>
              <th className="px-4 py-3 text-end">{t("resources:moderation.headers.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("resources:moderation.loading")}
                </td>
              </tr>
            )}
            {isError && !isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("resources:moderation.loadError")}{" "}
                  <button
                    type="button"
                    onClick={() => void refetch()}
                    className="text-brand-500 underline"
                  >
                    {t("resources:moderation.retry")}
                  </button>
                </td>
              </tr>
            )}
            {!isLoading && !isError && data?.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("resources:moderation.empty")}
                </td>
              </tr>
            )}
            {data?.map((r) => (
              <tr
                key={r.id}
                className="border-t border-border-subtle hover:bg-bg-subtle/40"
              >
                <td className="px-4 py-3 font-medium text-text-primary">
                  {isAr ? r.titleAr || r.titleEn : r.titleEn || r.titleAr}
                </td>
                <td className="px-4 py-3 text-text-secondary">
                  {t(`resources:resourceType.${r.type}`)}
                </td>
                <td className="px-4 py-3 text-text-secondary">{r.authorRole}</td>
                <td className="px-4 py-3 text-xs text-text-tertiary">
                  {r.tags.slice(0, 3).join(", ") || "—"}
                </td>
                <td className="px-4 py-3 text-end">
                  <div className="inline-flex gap-2">
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => approveMut.mutate(r.id)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-success-500 hover:text-success-600 disabled:opacity-50"
                    >
                      {t("resources:moderation.approve")}
                    </button>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => setRejectTargetId(r.id)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500 disabled:opacity-50"
                    >
                      {t("resources:moderation.reject")}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PromptDialog
        open={rejectTargetId !== null}
        onOpenChange={(open) => { if (!open) setRejectTargetId(null); }}
        title={t("resources:moderation.reject")}
        inputLabel={t("resources:moderation.rejectPrompt")}
        inputMultiline
        variant="destructive"
        confirmLabel={t("resources:moderation.reject")}
        loading={rejectMut.isPending}
        onConfirm={(reason) => {
          if (!rejectTargetId || !reason) return;
          rejectMut.mutate({ id: rejectTargetId, reason });
        }}
      />
    </>
  );
}

// ─── Published tab ────────────────────────────────────────────────────────────

function PublishedTab() {
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const { data, isLoading, isError, refetch } = useQuery<PaginatedResources>({
    queryKey: ["admin", "resources", "published", page],
    queryFn: () => resourcesApi.search({ page, pageSize: PAGE_SIZE }),
  });

  const featureMut = useMutation({
    mutationFn: ({ id, featured }: { id: string; featured: boolean }) =>
      resourcesApi.setFeatured(id, featured),
    onSuccess: () => {
      toast.success(t("resources:moderation.featureSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "resources", "published"] });
      void qc.invalidateQueries({ queryKey: ["resources", "featured"] });
    },
    onError: () => toast.error(t("resources:moderation.featureError")),
  });

  const hideMut = useMutation({
    mutationFn: ({ id, status }: { id: string; status: "Published" | "Hidden" }) =>
      resourcesApi.setVisibility(id, status),
    onSuccess: () => {
      toast.success(t("resources:moderation.hideSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "resources", "published"] });
    },
    onError: () => toast.error(t("resources:moderation.hideError")),
  });

  const busy = featureMut.isPending || hideMut.isPending;
  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  return (
    <div className="space-y-4">
      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("resources:moderation.headers.title")}</th>
              <th className="px-4 py-3 text-start">{t("resources:moderation.headers.type")}</th>
              <th className="px-4 py-3 text-start">{t("resources:moderation.headers.tags")}</th>
              <th className="px-4 py-3 text-center">{t("resources:moderation.headers.featured")}</th>
              <th className="px-4 py-3 text-end">{t("resources:moderation.headers.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("resources:moderation.loading")}
                </td>
              </tr>
            )}
            {isError && !isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("resources:moderation.loadErrorPublished")}{" "}
                  <button
                    type="button"
                    onClick={() => void refetch()}
                    className="text-brand-500 underline"
                  >
                    {t("resources:moderation.retry")}
                  </button>
                </td>
              </tr>
            )}
            {!isLoading && !isError && items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("resources:moderation.emptyPublished")}
                </td>
              </tr>
            )}
            {items.map((r) => (
              <tr
                key={r.id}
                className="border-t border-border-subtle hover:bg-bg-subtle/40"
              >
                <td className="max-w-xs px-4 py-3 font-medium text-text-primary">
                  <span className="block truncate">
                    {isAr ? r.titleAr || r.titleEn : r.titleEn || r.titleAr}
                  </span>
                </td>
                <td className="px-4 py-3 text-text-secondary">
                  {t(`resources:resourceType.${r.type}`)}
                </td>
                <td className="px-4 py-3 text-xs text-text-tertiary">
                  {r.tags.slice(0, 3).join(", ") || "—"}
                </td>
                <td className="px-4 py-3 text-center">
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => featureMut.mutate({ id: r.id, featured: !r.isFeatured })}
                    aria-label={
                      r.isFeatured
                        ? t("resources:moderation.unfeature")
                        : t("resources:moderation.feature")
                    }
                    title={
                      r.isFeatured
                        ? t("resources:moderation.unfeature")
                        : t("resources:moderation.feature")
                    }
                    className={cn(
                      "inline-flex size-8 items-center justify-center rounded-md transition disabled:opacity-50",
                      r.isFeatured
                        ? "text-amber-500 hover:text-amber-400"
                        : "text-text-tertiary hover:text-amber-400",
                    )}
                  >
                    <Star
                      aria-hidden
                      className="size-4"
                      fill={r.isFeatured ? "currentColor" : "none"}
                    />
                  </button>
                </td>
                <td className="px-4 py-3 text-end">
                  {r.status === "Published" ? (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => hideMut.mutate({ id: r.id, status: "Hidden" })}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-warning-400 hover:text-warning-500 disabled:opacity-50"
                    >
                      {t("resources:moderation.hide")}
                    </button>
                  ) : (
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => hideMut.mutate({ id: r.id, status: "Published" })}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-success-500 hover:text-success-600 disabled:opacity-50"
                    >
                      {t("resources:moderation.show")}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-3">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className="rounded-md border border-border-subtle px-3 py-1.5 text-xs disabled:opacity-40 hover:bg-bg-subtle"
          >
            {t("resources:pagination.prev")}
          </button>
          <span className="text-xs text-text-tertiary">
            {t("resources:pagination.pageOf", { page, total: totalPages })}
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className="rounded-md border border-border-subtle px-3 py-1.5 text-xs disabled:opacity-40 hover:bg-bg-subtle"
          >
            {t("resources:pagination.next")}
          </button>
        </div>
      )}
    </div>
  );
}

// ─── Page shell ───────────────────────────────────────────────────────────────

export function AdminArticles() {
  const { t } = useTranslation(["resources"]);
  const [tab, setTab] = useState<Tab>("pending");

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("resources:moderation.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("resources:moderation.subtitle")}
        </p>
      </div>

      {/* Tab strip */}
      <div className="flex gap-1 rounded-lg border border-border-subtle bg-bg-subtle p-1">
        {(["pending", "published"] as const).map((key) => (
          <button
            key={key}
            type="button"
            onClick={() => setTab(key)}
            className={cn(
              "rounded-md px-4 py-1.5 text-sm font-medium transition",
              tab === key
                ? "bg-bg-elevated text-text-primary shadow-sm"
                : "text-text-secondary hover:text-text-primary",
            )}
          >
            {t(`resources:moderation.tabs.${key}`)}
          </button>
        ))}
      </div>

      {tab === "pending" ? <PendingTab /> : <PublishedTab />}
    </div>
  );
}
