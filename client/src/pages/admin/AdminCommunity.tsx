import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { EyeOff } from "lucide-react";
import {
  communityApi,
  type CommunityPagedResult,
  type FlaggedPost,
} from "@/services/api/community";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";

const PAGE_SIZE = 20;

export function AdminCommunity() {
  const { t, i18n } = useTranslation(["moderation", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const qc = useQueryClient();

  const [page, setPage] = useState(1);

  const { data, isLoading, isError, refetch } = useQuery<
    CommunityPagedResult<FlaggedPost>
  >({
    queryKey: ["admin", "community", "flagged", page],
    queryFn: () => communityApi.getFlaggedPosts(page, PAGE_SIZE),
    placeholderData: keepPreviousData,
  });

  const [removeTargetId, setRemoveTargetId] = useState<string | null>(null);

  const removeMut = useMutation({
    mutationFn: (id: string) => communityApi.removePost(id),
    onSuccess: () => {
      toast.success(t("moderation:communityModeration.removeSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "community", "flagged"] });
      setRemoveTargetId(null);
    },
    onError: () => {
      toast.error(t("moderation:communityModeration.removeError"));
      setRemoveTargetId(null);
    },
  });

  const keepMut = useMutation({
    mutationFn: (id: string) => communityApi.dismissFlags(id),
    onSuccess: () => {
      toast.success(t("moderation:communityModeration.keepSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "community", "flagged"] });
    },
    onError: () => toast.error(t("moderation:communityModeration.keepError")),
  });

  const confirmRemove = (id: string) => {
    setRemoveTargetId(id);
  };

  const busy = removeMut.isPending || keepMut.isPending;
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const currentPage = data?.page ?? page;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("moderation:communityModeration.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("moderation:communityModeration.subtitle")}
        </p>
      </div>

      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">
                {t("moderation:communityModeration.headers.post")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:communityModeration.headers.author")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:communityModeration.headers.flags")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:communityModeration.headers.status")}
              </th>
              <th className="px-4 py-3 text-end">
                {t("moderation:communityModeration.headers.actions")}
              </th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:communityModeration.loading")}
                </td>
              </tr>
            )}
            {isError && !isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:communityModeration.loadError")}{" "}
                  <button
                    type="button"
                    onClick={() => void refetch()}
                    className="text-brand-500 underline"
                  >
                    {t("moderation:common.retry")}
                  </button>
                </td>
              </tr>
            )}
            {!isLoading && !isError && data?.items.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:communityModeration.empty")}
                </td>
              </tr>
            )}
            {data?.items.map((p) => (
              <tr
                key={p.id}
                className="border-t border-border-subtle align-top hover:bg-bg-subtle/40"
              >
                <td className="px-4 py-3">
                  <p className="font-medium text-text-primary">
                    {p.title || t("moderation:communityModeration.untitled")}
                  </p>
                  <p className="mt-0.5 line-clamp-2 max-w-md text-xs text-text-secondary">
                    {p.bodyPreview}
                  </p>
                  <p className="mt-1 text-xs text-text-tertiary">
                    {format(new Date(p.createdAt), "yyyy-MM-dd", { locale: dateLocale })}
                  </p>
                </td>
                <td className="px-4 py-3 text-text-secondary">{p.authorName}</td>
                <td className="px-4 py-3">
                  <span className="font-medium text-danger-500">
                    {t("moderation:communityModeration.flags", {
                      count: p.validFlagCount,
                    })}
                  </span>
                  {p.topFlagReason && (
                    <p className="mt-0.5 text-xs text-text-tertiary">
                      {t("moderation:communityModeration.topReason")}: {p.topFlagReason}
                    </p>
                  )}
                </td>
                <td className="px-4 py-3">
                  {p.isAutoHidden && (
                    <span className="inline-flex items-center gap-1 rounded-full bg-warning-50 px-2 py-0.5 text-xs font-medium text-warning-600">
                      <EyeOff className="size-3" />
                      {t("moderation:communityModeration.autoHidden")}
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-end">
                  <div className="inline-flex gap-2">
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => keepMut.mutate(p.id)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-success-500 hover:text-success-600 disabled:opacity-50"
                    >
                      {t("moderation:communityModeration.keep")}
                    </button>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => confirmRemove(p.id)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500 disabled:opacity-50"
                    >
                      {t("moderation:communityModeration.remove")}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-text-secondary">
          <span>
            {t("moderation:common.pageOf", { page: currentPage, total: totalPages })}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={currentPage <= 1}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("moderation:common.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => p + 1)}
              disabled={currentPage >= totalPages}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("moderation:common.next")}
            </button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={removeTargetId !== null}
        onOpenChange={(open) => {
          if (!open) setRemoveTargetId(null);
        }}
        title={t("moderation:communityModeration.remove")}
        description={t("moderation:communityModeration.removeConfirm")}
        confirmLabel={t("moderation:communityModeration.remove")}
        variant="destructive"
        loading={removeMut.isPending}
        onConfirm={() => {
          if (removeTargetId) removeMut.mutate(removeTargetId);
        }}
      />
    </div>
  );
}
