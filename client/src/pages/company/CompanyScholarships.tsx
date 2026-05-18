import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import {
  scholarshipsApi,
  type MyScholarship,
  type ScholarshipStatus,
} from "@/services/api/scholarships";

function statusBadgeClass(s: ScholarshipStatus): string {
  switch (s) {
    case "Open":
      return "bg-success-100 text-success-600";
    case "UnderReview":
      return "bg-warning-50 text-warning-600";
    case "Closed":
    case "Archived":
      return "bg-danger-50 text-danger-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

export function CompanyScholarships() {
  const { t, i18n } = useTranslation(["moderation", "common"]);
  const isAr = i18n.language.startsWith("ar");

  const { data, isLoading, isError, refetch } = useQuery<MyScholarship[]>({
    queryKey: ["company", "scholarships", "mine"],
    queryFn: () => scholarshipsApi.getMine(),
  });

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("moderation:companyScholarships.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("moderation:companyScholarships.subtitle")}
        </p>
      </div>

      <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">
                {t("moderation:companyScholarships.headers.title")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:companyScholarships.headers.status")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:companyScholarships.headers.deadline")}
              </th>
              <th className="px-4 py-3 text-start">
                {t("moderation:companyScholarships.headers.applicants")}
              </th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:companyScholarships.loading")}
                </td>
              </tr>
            )}
            {isError && !isLoading && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:companyScholarships.loadError")}{" "}
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
            {!isLoading && !isError && data?.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-6 text-center text-text-tertiary">
                  {t("moderation:companyScholarships.empty")}
                </td>
              </tr>
            )}
            {data?.map((s) => (
              <tr
                key={s.id}
                className="border-t border-border-subtle hover:bg-bg-subtle/40"
              >
                <td className="px-4 py-3 font-medium text-text-primary">
                  {isAr ? s.titleAr || s.titleEn : s.titleEn || s.titleAr}
                </td>
                <td className="px-4 py-3">
                  <span
                    className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadgeClass(s.status)}`}
                  >
                    {t(`moderation:scholarshipStatus.${s.status}`)}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs text-text-tertiary">
                  {format(new Date(s.deadline), "yyyy-MM-dd")}
                </td>
                <td className="px-4 py-3 text-text-secondary">{s.applicantCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
