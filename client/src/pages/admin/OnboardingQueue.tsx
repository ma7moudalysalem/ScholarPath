import { Fragment, useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { FileText } from "lucide-react";
import { adminApi, type OnboardingRequestRow, type PagedResult } from "@/services/api/admin";
import { apiErrorMessage } from "@/services/api/client";
import { documentsApi } from "@/services/api/documents";
import { PromptDialog } from "@/components/ui/PromptDialog";
import { expertiseTagLabelByLang, languageNameByLang } from "@/lib/expertiseTagLabel";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";

function parseJsonArray(raw: string | null): string[] {
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

/** Renders the submitted onboarding profile snapshot — ScholarshipProvider or Consultant. */
function OnboardingProfilePanel({ row }: { row: OnboardingRequestRow }) {
  const { t, i18n } = useTranslation(["admin"]);
  const paymentsEnabled = usePaymentsEnabled();
  const isScholarshipProvider = row.requestedRole === "ScholarshipProvider";
  const isConsultant = row.requestedRole === "Consultant";

  return (
    <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2 lg:grid-cols-3">
      {isScholarshipProvider && (
        <>
          <Row label={t("admin:onboarding.profile.legalName")} value={row.organizationLegalName} />
          <Row label={t("admin:onboarding.profile.website")} value={
            row.organizationWebsite ? (
              <a href={row.organizationWebsite} target="_blank" rel="noreferrer"
                className="text-brand-500 hover:underline">{row.organizationWebsite}</a>
            ) : null
          } />
          <Row label={t("admin:onboarding.profile.orgEmail")} value={row.organizationEmail} />
          <Row label={t("admin:onboarding.profile.country")} value={row.organizationCountry} />
          <Row label={t("admin:onboarding.profile.scholarshipProviderType")} value={row.scholarshipProviderType} />
          <Row label={t("admin:onboarding.profile.registrationNumber")} value={row.organizationRegistrationNumber} />
          <Row label={t("admin:onboarding.profile.taxNumber")} value={row.organizationTaxNumber} />
          <Row label={t("admin:onboarding.profile.contactName")} value={row.contactPersonFullName} />
          <Row label={t("admin:onboarding.profile.contactPosition")} value={row.contactPersonPosition} />
          <Row label={t("admin:onboarding.profile.phone")} value={row.contactPhoneNumber} />
          {row.scholarshipProviderDescription && (
            <div className="sm:col-span-2 lg:col-span-3">
              <Row label={t("admin:onboarding.profile.description")} value={
                <p className="whitespace-pre-wrap">{row.scholarshipProviderDescription}</p>
              } />
            </div>
          )}
        </>
      )}
      {isConsultant && (
        <>
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
              <a href={row.linkedInUrl} target="_blank" rel="noreferrer"
                className="text-brand-500 hover:underline">{row.linkedInUrl}</a>
            ) : null
          } />
          <Row label={t("admin:onboarding.profile.portfolio")} value={
            row.portfolioUrl ? (
              <a href={row.portfolioUrl} target="_blank" rel="noreferrer"
                className="text-brand-500 hover:underline">{row.portfolioUrl}</a>
            ) : null
          } />
          {row.biography && (
            <div className="sm:col-span-2 lg:col-span-3">
              <Row label={t("admin:onboarding.profile.bio")} value={<p className="whitespace-pre-wrap">{row.biography}</p>} />
            </div>
          )}
          {parseJsonArray(row.expertiseTagsJson).length > 0 && (
            <div className="sm:col-span-2 lg:col-span-3">
              <Row label={t("admin:onboarding.profile.expertiseTags")} value={
                <div className="flex flex-wrap gap-1.5">
                  {parseJsonArray(row.expertiseTagsJson).map((tag) => (
                    <span key={tag} className="rounded-full bg-brand-500/10 px-2 py-0.5 text-xs text-brand-500">
                      {expertiseTagLabelByLang(tag, i18n.language)}
                    </span>
                  ))}
                </div>
              } />
            </div>
          )}
          {parseJsonArray(row.languagesJson).length > 0 && (
            <div className="sm:col-span-2 lg:col-span-3">
              <Row label={t("admin:onboarding.profile.languages")} value={
                <div className="flex flex-wrap gap-1.5">
                  {parseJsonArray(row.languagesJson).map((lang) => (
                    <span key={lang} className="rounded-full bg-bg-subtle px-2 py-0.5 text-xs">
                      {languageNameByLang(lang, i18n.language)}
                    </span>
                  ))}
                </div>
              } />
            </div>
          )}
        </>
      )}
    </dl>
  );
}

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
          <button
            type="button"
            onClick={() => void download(d.id, d.fileName)}
            className="text-brand-500 hover:underline"
          >
            {d.fileName}
          </button>
          {/* FR-ONB-14 — surface which verification document this is so the admin
              reviews by type, not just by file name. */}
          {d.onboardingType && (
            <span className="rounded-full bg-brand-50 px-2 py-0.5 text-[11px] font-medium text-brand-600">
              {t(`admin:onboarding.docTypes.${d.onboardingType}`, d.onboardingType)}
            </span>
          )}
          <span className="text-xs text-text-tertiary">
            {(d.sizeBytes / 1024).toFixed(0)} KB
          </span>
        </li>
      ))}
    </ul>
  );
}

export function OnboardingQueue() {
  const { t, i18n } = useTranslation(["admin", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
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
    // A 409 here means the applicant was already decided — surface that
    // specific message instead of a generic "something went wrong".
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const [rejectTargetUserId, setRejectTargetUserId] = useState<string | null>(null);

  const approve = (u: OnboardingRequestRow) => reviewMut.mutate({ userId: u.userId, approve: true });
  const reject = (u: OnboardingRequestRow) => {
    setRejectTargetUserId(u.userId);
  };

  const submitReject = (notes: string) => {
    if (!rejectTargetUserId) return;
    if (!notes) return; // Notes are required when rejecting
    reviewMut.mutate(
      { userId: rejectTargetUserId, approve: false, notes },
      { onSettled: () => setRejectTargetUserId(null) },
    );
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
                    <td className="px-4 py-3 text-text-secondary">{u.requestedRole ? t(`common:roles.${u.requestedRole}`, { defaultValue: u.requestedRole }) : "—"}</td>
                    <td className="px-4 py-3 text-xs text-text-tertiary">{format(new Date(u.createdAt), "yyyy-MM-dd", { locale: dateLocale })}</td>
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
                          disabled={reviewMut.isPending}
                          className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-success-500 hover:text-success-600 disabled:opacity-50"
                        >
                          {t("admin:onboarding.actions.approve")}
                        </button>
                        <button
                          type="button"
                          onClick={() => reject(u)}
                          disabled={reviewMut.isPending}
                          className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500 disabled:opacity-50"
                        >
                          {t("admin:onboarding.actions.reject")}
                        </button>
                      </div>
                    </td>
                  </tr>
                  {expanded && (
                    <tr className="border-t border-border-subtle bg-bg-subtle/30">
                      <td colSpan={5} className="px-4 py-3">
                        <div className="space-y-4">
                          <OnboardingProfilePanel row={u} />
                          <div>
                            <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                              {t("admin:onboarding.documents.heading")}
                            </h3>
                            <OnboardingDocuments userId={u.userId} />
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
        open={rejectTargetUserId !== null}
        onOpenChange={(open) => {
          if (!open) setRejectTargetUserId(null);
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
