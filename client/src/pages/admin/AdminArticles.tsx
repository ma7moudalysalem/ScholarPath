import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
  resourcesApi,
  type ResourceListItem,
} from "@/services/api/resources";

export function AdminArticles() {
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
    },
    onError: () => toast.error(t("resources:moderation.approveError")),
  });

  const rejectMut = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      resourcesApi.reject(id, reason),
    onSuccess: () => {
      toast.success(t("resources:moderation.rejectSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "resources", "pending"] });
    },
    onError: () => toast.error(t("resources:moderation.rejectError")),
  });

  const confirmReject = (id: string) => {
    const reason = window.prompt(t("resources:moderation.rejectPrompt"));
    if (reason && reason.trim()) rejectMut.mutate({ id, reason: reason.trim() });
  };

  const busy = approveMut.isPending || rejectMut.isPending;

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

      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">
                {t("resources:moderation.headers.title")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("resources:moderation.headers.type")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("resources:moderation.headers.author")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("resources:moderation.headers.tags")}
              </th>
              <th className="px-4 py-3 text-end">
                {t("resources:moderation.headers.actions")}
              </th>
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
                      onClick={() => confirmReject(r.id)}
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
    </div>
  );
}
