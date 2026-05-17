import { useParams, Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ArrowLeft, ExternalLink } from "lucide-react";
import {
  applicationsApi,
  type ApplicationDetail,
  type ApplicationStatus,
} from "@/services/api/applications";

function statusBadgeClass(s: ApplicationStatus): string {
  switch (s) {
    case "Accepted":
      return "bg-emerald-500/10 text-emerald-500";
    case "Rejected":
    case "Withdrawn":
      return "bg-rose-500/10 text-rose-500";
    case "UnderReview":
    case "Shortlisted":
    case "WaitingResult":
      return "bg-sky-500/10 text-sky-500";
    case "Pending":
    case "Applied":
    case "Intending":
      return "bg-amber-500/10 text-amber-600";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

/** Pretty-prints a JSON blob, falling back to the raw string if it is not JSON. */
function prettyJson(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export function StudentApplicationDetail() {
  const { t } = useTranslation(["moderation", "common"]);
  const { id } = useParams<{ id: string }>();

  const { data, isLoading, isError } = useQuery<ApplicationDetail>({
    queryKey: ["application", "detail", id],
    queryFn: () => applicationsApi.getById(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <p className="py-12 text-center text-sm text-text-tertiary">
        {t("moderation:common.loading")}
      </p>
    );
  }

  if (isError || !data) {
    return (
      <div className="space-y-4 py-12 text-center">
        <p className="text-sm text-text-tertiary">{t("moderation:appDetail.notFound")}</p>
        <Link
          to="/student/applications"
          className="inline-flex items-center gap-1 text-sm text-brand-500 underline"
        >
          <ArrowLeft className="size-4" />
          {t("moderation:appDetail.back")}
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <Link
        to="/student/applications"
        className="inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary"
      >
        <ArrowLeft className="size-4" />
        {t("moderation:appDetail.back")}
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-text-tertiary">
            {t("moderation:appDetail.scholarship")}
          </p>
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {data.scholarshipTitleEn}
          </h1>
          {data.companyName && (
            <p className="mt-1 text-sm text-text-secondary">
              {t("moderation:appDetail.company")}: {data.companyName}
            </p>
          )}
        </div>
        <span
          className={`rounded-full px-3 py-1 text-sm font-medium ${statusBadgeClass(data.status)}`}
        >
          {t(`moderation:applicationStatus.${data.status}`)}
        </span>
      </div>

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-3 text-lg font-semibold text-text-primary">
          {t("moderation:appDetail.timeline")}
        </h2>
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          <Field label={t("moderation:appDetail.mode")}>
            {t(`moderation:applicationMode.${data.mode}`)}
          </Field>
          <Field label={t("moderation:appDetail.created")}>
            {format(new Date(data.createdAt), "yyyy-MM-dd")}
          </Field>
          <Field label={t("moderation:appDetail.submitted")}>
            {data.submittedAt
              ? format(new Date(data.submittedAt), "yyyy-MM-dd")
              : t("moderation:appDetail.notProvided")}
          </Field>
          <Field label={t("moderation:appDetail.reviewStarted")}>
            {data.reviewStartedAt
              ? format(new Date(data.reviewStartedAt), "yyyy-MM-dd")
              : t("moderation:appDetail.notProvided")}
          </Field>
          <Field label={t("moderation:appDetail.decision")}>
            {data.decisionAt
              ? format(new Date(data.decisionAt), "yyyy-MM-dd")
              : t("moderation:appDetail.notProvided")}
          </Field>
          <Field label={t("moderation:appDetail.deadline")}>
            {data.deadline
              ? format(new Date(data.deadline), "yyyy-MM-dd")
              : t("moderation:appDetail.notProvided")}
          </Field>
        </dl>
      </section>

      {data.decisionReason && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-2 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.decisionReason")}
          </h2>
          <p className="whitespace-pre-wrap text-sm text-text-secondary">
            {data.decisionReason}
          </p>
        </section>
      )}

      {data.mode === "External" && (data.externalTrackingUrl || data.externalReferenceId) && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-2 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.externalTracking")}
          </h2>
          {data.externalReferenceId && (
            <p className="text-sm text-text-secondary">
              {t("moderation:appDetail.externalRef")}: {data.externalReferenceId}
            </p>
          )}
          {data.externalTrackingUrl && (
            <a
              href={data.externalTrackingUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-2 inline-flex items-center gap-1 text-sm text-brand-500 underline"
            >
              <ExternalLink className="size-4" />
              {t("moderation:appDetail.openExternal")}
            </a>
          )}
        </section>
      )}

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-2 text-lg font-semibold text-text-primary">
          {t("moderation:appDetail.formData")}
        </h2>
        {data.formDataJson ? (
          <pre className="overflow-x-auto rounded-md bg-bg-subtle p-3 text-xs text-text-secondary">
            {prettyJson(data.formDataJson)}
          </pre>
        ) : (
          <p className="text-sm text-text-tertiary">{t("moderation:appDetail.noFormData")}</p>
        )}
      </section>

      {data.attachedDocumentsJson && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-2 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.documents")}
          </h2>
          <pre className="overflow-x-auto rounded-md bg-bg-subtle p-3 text-xs text-text-secondary">
            {prettyJson(data.attachedDocumentsJson)}
          </pre>
        </section>
      )}

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-2 text-lg font-semibold text-text-primary">
          {t("moderation:appDetail.personalNotes")}
        </h2>
        {data.personalNotes ? (
          <p className="whitespace-pre-wrap text-sm text-text-secondary">
            {data.personalNotes}
          </p>
        ) : (
          <p className="text-sm text-text-tertiary">{t("moderation:appDetail.noNotes")}</p>
        )}
      </section>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs uppercase tracking-wide text-text-tertiary">{label}</dt>
      <dd className="mt-0.5 text-sm text-text-primary">{children}</dd>
    </div>
  );
}
