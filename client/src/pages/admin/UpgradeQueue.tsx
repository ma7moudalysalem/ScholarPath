import { Fragment, useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ar } from "date-fns/locale";
import { formatCalendarDate } from "@/lib/dates";
import { FileText } from "lucide-react";
import {
  adminApi,
  type PagedResult,
  type UpgradeRequestRow,
  type UpgradeRequestStatus,
} from "@/services/api/admin";
import { apiErrorMessage } from "@/services/api/client";
import { documentsApi } from "@/services/api/documents";
import { PromptDialog } from "@/components/ui/PromptDialog";
import { SegmentedFilter } from "@/components/ui/SegmentedFilter";
import { expertiseTagLabelByLang, languageNameByLang } from "@/lib/expertiseTagLabel";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";

const FILTERS: { value: UpgradeRequestStatus | null; key: string }[] = [
  { value: "Pending", key: "pending" },
  { value: "Approved", key: "approved" },
  { value: "Rejected", key: "rejected" },
  { value: null, key: "all" },
];

function parseJsonArray(raw: string | null | undefined): string[] {
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw) as unknown;
    return Array.isArray(parsed) ? (parsed as string[]).filter((s) => typeof s === "string") : [];
  } catch {
    return [];
  }
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  if (value === null || value === undefined || value === "") return null;
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs uppercase tracking-wide text-text-tertiary">{label}</dt>
      <dd className="text-sm text-text-primary">{value}</dd>
    </div>
  );
}

/**
 * The proposed consultant profile the applicant submitted with their upgrade
 * request — the same snapshot an admin sees for a fresh Consultant onboarding,
 * so a Student→Consultant upgrade is never approved on a bare reason string.
 */
function UpgradeProfilePanel({ row }: { row: UpgradeRequestRow }) {
  const { t, i18n } = useTranslation(["admin"]);
  const paymentsEnabled = usePaymentsEnabled();
  const tags = parseJsonArray(row.expertiseTagsJson);
  const langs = parseJsonArray(row.languagesJson);

  return (
    <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
      <Row label={t("admin:onboarding.profile.professionalTitle")} value={row.professionalTitle} />
      <Row label={t("admin:onboarding.profile.highestDegree")} value={row.highestDegree} />
      <Row label={t("admin:onboarding.profile.fieldOfExpertise")} value={row.fieldOfExpertise} />
      <Row label={t("admin:onboarding.profile.yearsExperience")} value={row.yearsOfExperience} />
      {paymentsEnabled && (
        <Row label={t("admin:onboarding.profile.sessionFeeUsd")} value={row.sessionFeeUsd} />
      )}
      <Row label={t("admin:onboarding.profile.sessionDurationMinutes")} value={row.sessionDurationMinutes} />
      <Row label={t("admin:onboarding.profile.country")} value={row.consultantCountry} />
      <Row label={t("admin:onboarding.profile.timezone")} value={row.timezone} />
      <Row label={t("admin:onboarding.profile.linkedIn")} value={
        row.linkedInUrl ? (
          <a href={row.linkedInUrl} target="_blank" rel="noreferrer" className="text-brand-500 hover:underline">{row.linkedInUrl}</a>
        ) : null
      } />
      <Row label={t("admin:onboarding.profile.portfolio")} value={
        row.portfolioUrl ? (
          <a href={row.portfolioUrl} target="_blank" rel="noreferrer" className="text-brand-500 hover:underline">{row.portfolioUrl}</a>
        ) : null
      } />
      {row.biography && (
        <div className="sm:col-span-2 lg:col-span-3">
          <Row label={t("admin:onboarding.profile.bio")} value={<p className="whitespace-pre-wrap">{row.biography}</p>} />
        </div>
      )}
      {tags.length > 0 && (
        <div className="sm:col-span-2 lg:col-span-3">
          <Row label={t("admin:onboarding.profile.expertiseTags")} value={
            <div className="flex flex-wrap gap-1.5">
              {tags.map((tag) => (
                <span key={tag} className="rounded-full bg-brand-500/10 px-2 py-0.5 text-xs text-brand-500">
                  {expertiseTagLabelByLang(tag, i18n.language)}
                </span>
              ))}
            </div>
          } />
        </div>
      )}
      {langs.length > 0 && (
        <div className="sm:col-span-2 lg:col-span-3">
          <Row label={t("admin:onboarding.profile.languages")} value={
            <div className="flex flex-wrap gap-1.5">
              {langs.map((lang) => (
                <span key={lang} className="rounded-full bg-bg-subtle px-2 py-0.5 text-xs">
                  {languageNameByLang(lang, i18n.language)}
                </span>
              ))}
            </div>
          } />
        </div>
      )}
    </dl>
  );
}

/** Verification documents the applicant uploaded — same store the onboarding review reads. */
function UpgradeDocuments({ userId }: { userId: string }) {
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
    } catch (err) {
      toast.error(apiErrorMessage(err, t("common:status.error")));
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
          <button type="button" onClick={() => void download(d.id, d.fileName)} className="text-brand-500 hover:underline">
            {d.fileName}
          </button>
          {d.onboardingType && (
            <span className="rounded-full bg-brand-50 px-2 py-0.5 text-[11px] font-medium text-brand-600">
              {t(`admin:onboarding.docTypes.${d.onboardingType}`, d.onboardingType)}
            </span>
          )}
          <span className="text-xs text-text-tertiary">{(d.sizeBytes / 1024).toFixed(0)} KB</span>
        </li>
      ))}
    </ul>
  );
}

export function UpgradeQueue() {
  const { t, i18n } = useTranslation(["admin", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const qc = useQueryClient();
  const [filter, setFilter] = useState<UpgradeRequestStatus | null>("Pending");
  const [page, setPage] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const { data, isLoading } = useQuery<PagedResult<UpgradeRequestRow>>({
    queryKey: ["admin", "upgrade-queue", filter, page],
    queryFn: () => adminApi.getUpgradeQueue(filter, page),
    placeholderData: keepPreviousData,
  });

  const reviewMut = useMutation({
    mutationFn: ({ id, approve, notes }: { id: string; approve: boolean; notes?: string }) =>
      adminApi.reviewUpgrade(id, { approve, notes }),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "upgrade-queue"] });
    },
    // A 409 here means another admin already decided this request — surface
    // that specific message instead of a generic "something went wrong".
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const [rejectTargetId, setRejectTargetId] = useState<string | null>(null);

  const approve = (r: UpgradeRequestRow) => reviewMut.mutate({ id: r.id, approve: true });
  const reject = (r: UpgradeRequestRow) => {
    setRejectTargetId(r.id);
  };

  const submitReject = (notes: string) => {
    if (!rejectTargetId) return;
    if (!notes) return; // Notes are required when rejecting
    reviewMut.mutate(
      { id: rejectTargetId, approve: false, notes },
      { onSettled: () => setRejectTargetId(null) },
    );
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t("admin:upgrades.title")}</h1>
        <SegmentedFilter
          ariaLabel={t("admin:upgrades.title")}
          value={filter}
          onChange={(v) => { setFilter(v); setPage(1); }}
          options={FILTERS.map((f) => ({ value: f.value, label: t(`admin:upgrades.filters.${f.key}`) }))}
        />
      </div>

      <div className="overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.email")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.target")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.status")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.reason")}</th>
              <th className="px-4 py-3 text-start">{t("admin:upgrades.headers.createdAt")}</th>
              <th className="px-4 py-3 text-end"></th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={6} className="px-4 py-6 text-center text-text-tertiary">{t("admin:common.loading")}</td></tr>
            )}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={6} className="px-4 py-6 text-center text-text-tertiary">{t("admin:upgrades.empty")}</td></tr>
            )}
            {data?.items.map((r: UpgradeRequestRow) => {
              const expanded = expandedId === r.id;
              // Consultant upgrades carry a proposed profile worth reviewing.
              const hasEvidence = r.target === "Consultant";
              return (
                <Fragment key={r.id}>
                  <tr className="border-t border-border-subtle hover:bg-bg-subtle/40">
                    <td className="px-4 py-3 font-medium">
                      <div>{r.fullName || r.userEmail}</div>
                      {r.fullName && (
                        <div className="text-xs font-normal text-text-tertiary">{r.userEmail}</div>
                      )}
                    </td>
                    <td className="px-4 py-3">{t(`admin:upgrades.target.${r.target}`, { defaultValue: r.target })}</td>
                    <td className="px-4 py-3">{t(`admin:upgrades.status.${r.status}`, { defaultValue: r.status })}</td>
                    <td className="px-4 py-3 text-text-secondary">{r.reason ?? "—"}</td>
                    <td className="px-4 py-3 text-xs text-text-tertiary">{formatCalendarDate(r.createdAt, "dd MMM yyyy", dateLocale)}</td>
                    <td className="px-4 py-3 text-end">
                      <div className="inline-flex gap-1.5">
                        {hasEvidence && (
                          <button
                            type="button"
                            onClick={() => setExpandedId(expanded ? null : r.id)}
                            aria-expanded={expanded}
                            className={`rounded-md border px-2 py-1 text-xs ${
                              expanded
                                ? "border-brand-500 text-brand-500"
                                : "border-border-subtle hover:border-brand-500 hover:text-brand-500"
                            }`}
                          >
                            {t("admin:upgrades.actions.details", { defaultValue: "Details" })}
                          </button>
                        )}
                        {r.status === "Pending" && (
                          <>
                            <button type="button" onClick={() => approve(r)} disabled={reviewMut.isPending} className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-success-500 hover:text-success-600 disabled:opacity-50">
                              {t("admin:onboarding.actions.approve")}
                            </button>
                            <button type="button" onClick={() => reject(r)} disabled={reviewMut.isPending} className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500 disabled:opacity-50">
                              {t("admin:onboarding.actions.reject")}
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                  {expanded && hasEvidence && (
                    <tr className="border-t border-border-subtle bg-bg-subtle/30">
                      <td colSpan={6} className="px-4 py-3">
                        <div className="space-y-4">
                          <UpgradeProfilePanel row={r} />
                          <div>
                            <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                              {t("admin:onboarding.documents.heading")}
                            </h3>
                            <UpgradeDocuments userId={r.userId} />
                          </div>
                        </div>
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
        onOpenChange={(open) => {
          if (!open) setRejectTargetId(null);
        }}
        title={t("admin:onboarding.reviewDialog.title")}
        inputLabel={t("admin:onboarding.reviewDialog.notesLabel")}
        inputMultiline
        requireInput
        variant="destructive"
        confirmLabel={t("admin:onboarding.reviewDialog.reject")}
        cancelLabel={t("admin:onboarding.reviewDialog.cancel")}
        loading={reviewMut.isPending}
        onConfirm={submitReject}
      />
    </div>
  );
}
