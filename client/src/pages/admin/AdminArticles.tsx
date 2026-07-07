import { Fragment, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Star, ChevronDown, ChevronRight, ExternalLink } from "lucide-react";
import {
  resourcesApi,
  type ResourceListItem,
  type PaginatedResources,
  type ResourceDetail,
} from "@/services/api/resources";
import { PromptDialog } from "@/components/ui/PromptDialog";
import { SegmentedFilter } from "@/components/ui/SegmentedFilter";
import { Markdown } from "@/components/resources/ResourceMarkdown";
import { cn } from "@/lib/utils";

type Tab = "pending" | "published";

// ─── Pending-review detail preview ────────────────────────────────────────────
// An admin must be able to READ the exact content they are about to publish —
// the queue row alone (title/type/author/tags) is a blind approval. This lazily
// loads the full ResourceDetail (server allows an admin to fetch a PendingReview
// resource) and renders the body with the same Markdown renderer students see.

function PendingDetailPanel({ id }: { id: string }) {
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");

  const { data, isLoading, isError } = useQuery<ResourceDetail>({
    queryKey: ["resources", "detail", id],
    queryFn: () => resourcesApi.getDetail(id),
  });

  if (isLoading) {
    return (
      <p className="text-sm text-text-tertiary">{t("resources:moderation.loading")}</p>
    );
  }
  if (isError || !data) {
    return (
      <p className="text-sm text-danger-500">{t("resources:moderation.loadError")}</p>
    );
  }

  const description = isAr
    ? data.descriptionAr ?? data.descriptionEn
    : data.descriptionEn ?? data.descriptionAr;
  const content = isAr
    ? data.contentMarkdownAr ?? data.contentMarkdownEn
    : data.contentMarkdownEn ?? data.contentMarkdownAr;
  const chapters = [...(data.chapters ?? [])].sort((a, b) => a.sortOrder - b.sortOrder);

  return (
    <div className="max-w-3xl space-y-4">
      {/* Meta line — who wrote it + type + tags */}
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-text-tertiary">
        {data.authorName && (
          <span>
            {t("resources:moderation.preview.author")}: {data.authorName} ({data.authorRole})
          </span>
        )}
        <span>{t(`resources:resourceType.${data.type}`)}</span>
        {(data.tags ?? []).length > 0 && <span>{data.tags.join(", ")}</span>}
      </div>

      {data.coverImageUrl && (
        <img
          src={data.coverImageUrl}
          alt=""
          className="max-h-48 w-full rounded-lg object-cover"
        />
      )}

      {description && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
            {t("resources:moderation.preview.description")}
          </p>
          <p className="text-sm text-text-secondary">{description}</p>
        </div>
      )}

      {data.externalLinkUrl && (
        <a
          href={data.externalLinkUrl}
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-1.5 text-sm text-brand-600 underline"
        >
          <ExternalLink aria-hidden className="size-3.5" />
          {t("resources:moderation.preview.externalLink")}
        </a>
      )}

      {content ? (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
            {t("resources:moderation.preview.content")}
          </p>
          <div className="rounded-lg border border-border-subtle bg-bg-elevated p-4">
            <Markdown source={content} />
          </div>
        </div>
      ) : (
        !data.externalLinkUrl &&
        chapters.length === 0 && (
          <p className="text-sm italic text-text-tertiary">
            {t("resources:moderation.preview.noContent")}
          </p>
        )
      )}

      {chapters.length > 0 && (
        <div className="space-y-3">
          <p className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">
            {t("resources:detail.chapters")}
          </p>
          {chapters.map((c) => {
            const chTitle = isAr ? c.titleAr || c.titleEn : c.titleEn || c.titleAr;
            const chContent = isAr
              ? c.contentMarkdownAr ?? c.contentMarkdownEn
              : c.contentMarkdownEn ?? c.contentMarkdownAr;
            return (
              <div
                key={c.id}
                className="rounded-lg border border-border-subtle bg-bg-elevated p-4"
              >
                <p className="mb-2 text-sm font-semibold text-text-primary">{chTitle}</p>
                {chContent && <Markdown source={chContent} />}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ─── Pending-review tab ───────────────────────────────────────────────────────

function PendingTab() {
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  // Which pending row is expanded to preview its full content before deciding.
  const [expandedId, setExpandedId] = useState<string | null>(null);

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
            {data?.map((r) => {
              const isExpanded = expandedId === r.id;
              const title = isAr ? r.titleAr || r.titleEn : r.titleEn || r.titleAr;
              return (
                <Fragment key={r.id}>
                  <tr className="border-t border-border-subtle hover:bg-bg-subtle/40">
                    <td className="px-4 py-3 font-medium text-text-primary">
                      <button
                        type="button"
                        onClick={() => setExpandedId(isExpanded ? null : r.id)}
                        aria-expanded={isExpanded}
                        className="inline-flex items-start gap-1.5 text-start transition-colors hover:text-brand-600"
                      >
                        {isExpanded ? (
                          <ChevronDown aria-hidden className="mt-0.5 size-4 shrink-0 text-text-tertiary" />
                        ) : (
                          <ChevronRight aria-hidden className="mt-0.5 size-4 shrink-0 text-text-tertiary rtl:rotate-180" />
                        )}
                        <span>{title}</span>
                      </button>
                    </td>
                    <td className="px-4 py-3 text-text-secondary">
                      {t(`resources:resourceType.${r.type}`)}
                    </td>
                    <td className="px-4 py-3 text-text-secondary">{r.authorRole}</td>
                    <td className="px-4 py-3 text-xs text-text-tertiary">
                      {(r.tags ?? []).slice(0, 3).join(", ") || "—"}
                    </td>
                    <td className="px-4 py-3 text-end">
                      <div className="inline-flex gap-2">
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() => setExpandedId(isExpanded ? null : r.id)}
                          className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-brand-400 hover:text-brand-600 disabled:opacity-50"
                        >
                          {isExpanded
                            ? t("resources:moderation.preview.hide")
                            : t("resources:moderation.preview.review")}
                        </button>
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
                  {isExpanded && (
                    <tr className="border-t border-border-subtle bg-bg-subtle/30">
                      <td colSpan={5} className="px-4 py-4">
                        <PendingDetailPanel id={r.id} />
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>

      <PromptDialog
        open={rejectTargetId !== null}
        onOpenChange={(open) => { if (!open) setRejectTargetId(null); }}
        title={t("resources:moderation.reject")}
        inputLabel={t("resources:moderation.rejectPrompt")}
        inputMultiline
        requireInput
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
                  {(r.tags ?? []).slice(0, 3).join(", ") || "—"}
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

      {/* Tabs — the same segmented control admin status filters use, for consistency. */}
      <SegmentedFilter
        ariaLabel={t("resources:moderation.title")}
        value={tab}
        onChange={setTab}
        options={(["pending", "published"] as const).map((key) => ({
          value: key,
          label: t(`resources:moderation.tabs.${key}`),
        }))}
      />

      {tab === "pending" ? <PendingTab /> : <PublishedTab />}
    </div>
  );
}
