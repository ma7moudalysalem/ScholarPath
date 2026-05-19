import { Fragment, useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { FileText } from "lucide-react";
import { adminApi, type OnboardingRequestRow, type PagedResult } from "@/services/api/admin";
import { documentsApi } from "@/services/api/documents";

/** Lists the verification documents a pending applicant uploaded, with download links. */
function OnboardingDocuments({ userId }: { userId: string }) {
  const { t } = useTranslation(["admin", "common"]);
  const { data: docs = [], isLoading } = useQuery({
    queryKey: ["admin", "onboarding-documents", userId],
    queryFn: () => adminApi.getOnboardingDocuments(userId),
  });

  const download = async (id: string, fileName: string) => {
    try {
      const blob = await documentsApi.download(id);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      toast.error(t("common:status.error"));
    }
  };

  if (isLoading) {
    return <p className="text-sm text-text-tertiary">{t("admin:common.loading")}</p>;
  }
  if (docs.length === 0) {
    return <p className="text-sm text-text-tertiary">{t("admin:onboarding.documents.empty")}</p>;
  }
  return (
    <ul className="space-y-1.5">
      {docs.map((d) => (
        <li key={d.id} className="flex items-center gap-2 text-sm">
          <FileText aria-hidden className="size-4 shrink-0 text-text-tertiary" />
          <button
            type="button"
            onClick={() => void download(d.id, d.fileName)}
            className="text-brand-500 hover:underline"
          >
            {d.fileName}
          </button>
          <span className="text-xs text-text-tertiary">
            {(d.sizeBytes / 1024).toFixed(0)} KB
          </span>
        </li>
      ))}
    </ul>
  );
}

export function OnboardingQueue() {
  const { t } = useTranslation(["admin", "common"]);
  const qc = useQueryClient();
  const [expandedUserId, setExpandedUserId] = useState<string | null>(null);

  // For now the queue renders the first page; pagination will come with filters
  // when the volume warrants it.
  const { data, isLoading } = useQuery<PagedResult<OnboardingRequestRow>>({
    queryKey: ["admin", "onboarding-queue", 1],
    queryFn: () => adminApi.getOnboardingQueue(1),
    placeholderData: keepPreviousData,
  });

  const reviewMut = useMutation({
    mutationFn: ({ userId, approve, notes }: { userId: string; approve: boolean; notes?: string }) =>
      adminApi.reviewOnboarding(userId, { approve, notes }),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "onboarding-queue"] });
      void qc.invalidateQueries({ queryKey: ["admin", "analytics", "overview"] });
    },
    onError: () => toast.error(t("common:status.error")),
  });

  const approve = (u: OnboardingRequestRow) => reviewMut.mutate({ userId: u.userId, approve: true });
  const reject = (u: OnboardingRequestRow) => {
    const notes = window.prompt(t("admin:onboarding.reviewDialog.notesLabel"));
    if (!notes) return;
    reviewMut.mutate({ userId: u.userId, approve: false, notes });
  };

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight">{t("admin:onboarding.title")}</h1>

      <div className="overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.name")}</th>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.email")}</th>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.role")}</th>
              <th className="px-4 py-3 text-start">{t("admin:onboarding.headers.createdAt")}</th>
              <th className="px-4 py-3 text-end"></th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">{t("admin:common.loading")}</td></tr>
            )}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">{t("admin:onboarding.empty")}</td></tr>
            )}
            {data?.items.map((u: OnboardingRequestRow) => {
              const expanded = expandedUserId === u.userId;
              return (
                <Fragment key={u.userId}>
                  <tr className="border-t border-border-subtle hover:bg-bg-subtle/40">
                    <td className="px-4 py-3 font-medium">{u.fullName}</td>
                    <td className="px-4 py-3">{u.email}</td>
                    <td className="px-4 py-3 text-text-secondary">{u.requestedRole ?? "—"}</td>
                    <td className="px-4 py-3 text-xs text-text-tertiary">{format(new Date(u.createdAt), "yyyy-MM-dd")}</td>
                    <td className="px-4 py-3 text-end">
                      <div className="inline-flex gap-1.5">
                        <button
                          type="button"
                          onClick={() => setExpandedUserId(expanded ? null : u.userId)}
                          aria-expanded={expanded}
                          className={`rounded-md border px-2 py-1 text-xs ${
                            expanded
                              ? "border-brand-500 text-brand-500"
                              : "border-border-subtle hover:border-brand-500 hover:text-brand-500"
                          }`}
                        >
                          {t("admin:onboarding.actions.documents")}
                        </button>
                        <button
                          type="button"
                          onClick={() => approve(u)}
                          className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-success-500 hover:text-success-600"
                        >
                          {t("admin:onboarding.actions.approve")}
                        </button>
                        <button
                          type="button"
                          onClick={() => reject(u)}
                          className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500"
                        >
                          {t("admin:onboarding.actions.reject")}
                        </button>
                      </div>
                    </td>
                  </tr>
                  {expanded && (
                    <tr className="border-t border-border-subtle bg-bg-subtle/30">
                      <td colSpan={5} className="px-4 py-3">
                        <OnboardingDocuments userId={u.userId} />
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
