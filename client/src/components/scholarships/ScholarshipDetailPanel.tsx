import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { ExternalLink } from "lucide-react";
import { scholarshipsApi, type ScholarshipDetail } from "@/services/api/scholarships";

/**
 * Full read-only preview of a scholarship listing — provider, funding, level,
 * country, mode, review fee, description, eligibility, fields of study, required
 * documents and any external link. Lazily loads the detail (the server lets an
 * admin read a non-public listing) so an admin never approves / features a
 * listing sight-unseen. Shared by the moderation queue and the featured page.
 */
export function ScholarshipDetailPanel({ id }: { id: string }) {
  const { t, i18n } = useTranslation(["moderation", "scholarships", "common"]);
  const isAr = i18n.language.startsWith("ar");

  const { data, isLoading, isError } = useQuery<ScholarshipDetail>({
    queryKey: ["scholarship", "detail", id],
    queryFn: () => scholarshipsApi.getById(id),
  });

  if (isLoading) {
    return <p className="text-sm text-text-tertiary">{t("moderation:scholarshipModeration.loading")}</p>;
  }
  if (isError || !data) {
    return <p className="text-sm text-danger-500">{t("moderation:scholarshipModeration.loadError")}</p>;
  }

  const p = "moderation:scholarshipModeration.preview";
  const description = isAr
    ? data.descriptionAr || data.descriptionEn
    : data.descriptionEn || data.descriptionAr;
  const docs = data.requiredDocuments ?? [];
  const fields = data.fieldsOfStudy ?? [];

  return (
    <div className="max-w-3xl space-y-4">
      <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.provider`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{data.ownerScholarshipProviderName || "—"}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.funding`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{t(`scholarships:fundingType.${data.fundingType}`)}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.level`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{t(`scholarships:level.${data.targetLevel}`)}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.country`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">{data.country || "—"}</dd>
        </div>
        <div>
          <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.mode`)}</dt>
          <dd className="mt-0.5 text-sm text-text-secondary">
            {data.mode === "ExternalUrl" ? t(`${p}.external`) : t(`${p}.inApp`)}
          </dd>
        </div>
        {data.reviewFeeUsd != null && (
          <div>
            <dt className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.reviewFee`)}</dt>
            <dd className="mt-0.5 text-sm text-text-secondary">${data.reviewFeeUsd}</dd>
          </div>
        )}
      </dl>

      {description && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.description`)}</p>
          <p className="whitespace-pre-wrap text-sm text-text-secondary">{description}</p>
        </div>
      )}

      {data.eligibilityCriteria && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.eligibility`)}</p>
          <p className="whitespace-pre-wrap text-sm text-text-secondary">{data.eligibilityCriteria}</p>
        </div>
      )}

      {fields.length > 0 && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.fields`)}</p>
          <p className="text-sm text-text-secondary">{fields.join("، ")}</p>
        </div>
      )}

      {docs.length > 0 && (
        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-text-tertiary">{t(`${p}.requiredDocs`)}</p>
          <ul className="list-disc space-y-0.5 ps-5 text-sm text-text-secondary">
            {docs.map((d, i) => (
              <li key={i}>{d}</li>
            ))}
          </ul>
        </div>
      )}

      {data.externalUrl && (
        <a
          href={data.externalUrl}
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-1.5 text-sm text-brand-600 underline"
        >
          <ExternalLink aria-hidden className="size-3.5" />
          {t(`${p}.externalLink`)}
        </a>
      )}
    </div>
  );
}
