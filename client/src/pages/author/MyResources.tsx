import { Link } from "react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Plus, Edit, Send, Eye, Loader2 } from "lucide-react";
import { resourcesApi, type ResourceListItem, type ResourceStatus } from "@/services/api/resources";
import { apiErrorMessage } from "@/services/api/client";
import { cn } from "@/lib/utils";

const STATUS_CLASSES: Record<ResourceStatus, string> = {
  Draft:         "bg-bg-subtle text-text-secondary border-border-subtle",
  PendingReview: "bg-warning-50 text-warning-700 border-warning-200",
  Published:     "bg-success-50 text-success-700 border-success-200",
  Hidden:        "bg-danger-50 text-danger-700 border-danger-200",
  Removed:       "bg-danger-50 text-danger-700 border-danger-200",
};

function StatusBadge({ status }: { status: ResourceStatus }) {
  const { t } = useTranslation("resources");
  return (
    <span
      className={cn(
        "inline-flex rounded-full border px-2 py-0.5 text-xs font-medium",
        STATUS_CLASSES[status],
      )}
    >
      {t(`author.status.${status}`)}
    </span>
  );
}

export function MyResources() {
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  const { data, isLoading, isError, refetch } = useQuery<ResourceListItem[]>({
    queryKey: ["resources", "mine"],
    queryFn: () => resourcesApi.getMine(),
  });

  const submitMut = useMutation({
    mutationFn: (id: string) => resourcesApi.submit(id),
    onSuccess: (newStatus, id) => {
      const msg =
        newStatus === "Published"
          ? t("resources:author.publishedDirectly")
          : t("resources:author.submittedForReview");
      toast.success(msg);
      void qc.invalidateQueries({ queryKey: ["resources", "mine"] });
      void qc.invalidateQueries({ queryKey: ["resources", "detail", id] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  if (isLoading) {
    return (
      <div className="flex min-h-[40vh] items-center justify-center">
        <Loader2 className="size-6 animate-spin text-brand-500" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
        {t("resources:author.loadError")}{" "}
        <button type="button" onClick={() => void refetch()} className="underline">
          {t("common:cta.retry")}
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      {/* ── Header ── */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {t("resources:author.title")}
          </h1>
          <p className="mt-0.5 text-sm text-text-secondary">
            {t("resources:author.subtitle")}
          </p>
        </div>
        <Link
          to="/author/resources/new"
          className="inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600"
        >
          <Plus aria-hidden className="size-4" />
          {t("resources:author.create")}
        </Link>
      </div>

      {/* ── Empty state ── */}
      {(!data || data.length === 0) && (
        <div className="rounded-xl border border-dashed border-border-default bg-bg-elevated p-10 text-center">
          <p className="text-sm text-text-secondary">{t("resources:author.empty")}</p>
          <Link
            to="/author/resources/new"
            className="mt-4 inline-flex items-center gap-1.5 text-sm font-medium text-brand-500 hover:underline"
          >
            <Plus aria-hidden className="size-4" />
            {t("resources:author.createFirst")}
          </Link>
        </div>
      )}

      {/* ── Table ── */}
      {data && data.length > 0 && (
        <div className="overflow-hidden rounded-xl border border-border-subtle bg-bg-elevated shadow-xs">
          <table className="min-w-full divide-y divide-border-subtle text-sm">
            <thead className="bg-bg-subtle">
              <tr>
                {[
                  t("resources:author.col.title"),
                  t("resources:author.col.type"),
                  t("resources:author.col.status"),
                  t("resources:author.col.actions"),
                ].map((h) => (
                  <th
                    key={h}
                    className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-text-tertiary"
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-border-subtle">
              {data.map((r) => {
                const title = isAr ? r.titleAr || r.titleEn : r.titleEn || r.titleAr;
                const canEdit   = r.status === "Draft";
                const canSubmit = r.status === "Draft";
                const canView   = r.status === "Published";

                return (
                  <tr key={r.id} className="transition hover:bg-bg-subtle/40">
                    <td className="max-w-xs px-4 py-3">
                      <p className="truncate font-medium text-text-primary">{title}</p>
                      {r.tags.length > 0 && (
                        <p className="mt-0.5 truncate text-xs text-text-tertiary">
                          {r.tags.slice(0, 3).join(" · ")}
                        </p>
                      )}
                    </td>
                    <td className="px-4 py-3 text-text-secondary">
                      {t(`resources:resourceType.${r.type}`)}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={r.status} />
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        {canEdit && (
                          <Link
                            to={`/author/resources/${r.id}/edit`}
                            className="inline-flex items-center gap-1.5 rounded-md border border-border-subtle bg-bg-canvas px-2.5 py-1.5 text-xs font-medium text-text-secondary transition hover:border-border-default hover:text-text-primary"
                          >
                            <Edit aria-hidden className="size-3.5" />
                            {t("resources:author.edit")}
                          </Link>
                        )}
                        {canSubmit && (
                          <button
                            type="button"
                            disabled={submitMut.isPending}
                            onClick={() => submitMut.mutate(r.id)}
                            className="inline-flex items-center gap-1.5 rounded-md border border-brand-300 bg-brand-50 px-2.5 py-1.5 text-xs font-medium text-brand-600 transition hover:bg-brand-100 disabled:opacity-60"
                          >
                            {submitMut.isPending && submitMut.variables === r.id ? (
                              <Loader2 aria-hidden className="size-3.5 animate-spin" />
                            ) : (
                              <Send aria-hidden className="size-3.5" />
                            )}
                            {t("resources:author.submit")}
                          </button>
                        )}
                        {canView && (
                          <Link
                            to={`/student/resources/${r.slug}`}
                            className="inline-flex items-center gap-1.5 rounded-md border border-border-subtle bg-bg-canvas px-2.5 py-1.5 text-xs font-medium text-text-secondary transition hover:border-border-default hover:text-text-primary"
                          >
                            <Eye aria-hidden className="size-3.5" />
                            {t("resources:author.view")}
                          </Link>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
