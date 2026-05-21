import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";
import { Calendar, Building2, ExternalLink, ArrowRight } from "lucide-react";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import type { StudentApplicationRow, ApplicationStatus } from "@/services/api/applications";
import { cn } from "@/lib/utils";

interface ApplicationCardProps {
  application: StudentApplicationRow;
}

/**
 * Maps the application status to a small progress indicator (0..1).
 * Used to show how far along the application is in the lifecycle.
 */
function statusProgress(status: ApplicationStatus): number {
  switch (status) {
    case "Draft":
    case "Intending":
      return 0.15;
    case "Applied":
    case "Pending":
    case "WaitingResult":
      return 0.5;
    case "UnderReview":
    case "Shortlisted":
      return 0.75;
    case "Accepted":
      return 1;
    case "Rejected":
    case "Withdrawn":
      return 1;
    default:
      return 0.25;
  }
}

function statusTone(status: ApplicationStatus): {
  bar: string;
  badge: string;
} {
  switch (status) {
    case "Accepted":
      return { bar: "bg-success-500", badge: "bg-success-50 text-success-600 border border-success-200" };
    case "Rejected":
      return { bar: "bg-danger-500", badge: "bg-danger-50 text-danger-500 border border-danger-200" };
    case "Withdrawn":
      return { bar: "bg-text-tertiary", badge: "bg-bg-subtle text-text-secondary border border-border-subtle" };
    case "UnderReview":
    case "Shortlisted":
      return { bar: "bg-brand-500", badge: "bg-brand-50 text-brand-700 border border-brand-200" };
    case "Applied":
    case "Pending":
    case "WaitingResult":
      return { bar: "bg-warning-500", badge: "bg-warning-50 text-warning-600 border border-warning-50" };
    case "Draft":
    case "Intending":
    default:
      return { bar: "bg-text-tertiary", badge: "bg-bg-subtle text-text-secondary border border-border-subtle" };
  }
}

export function ApplicationCard({ application }: ApplicationCardProps) {
  const { t, i18n } = useTranslation("applications");
  const navigate = useNavigate();
  const dateLocale = i18n.dir() === "rtl" ? ar : undefined;

  const openDetails = () => navigate(`/student/applications/${application.applicationId}`);
  const progress = statusProgress(application.status);
  const tone = statusTone(application.status);

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={openDetails}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          openDetails();
        }
      }}
      className="group relative flex cursor-pointer flex-col gap-3 overflow-hidden rounded-xl border border-border-subtle bg-bg-elevated p-4 shadow-xs transition-all hover:-translate-y-0.5 hover:border-brand-300 hover:shadow-md"
    >
      {/* Status badge at top */}
      <div className="flex items-start justify-between gap-2">
        <span
          className={cn(
            "rounded-full px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider",
            tone.badge,
          )}
        >
          {t(`kanban.columns.${application.status}`)}
        </span>
        {application.mode === "External" && (
          <ExternalLink
            size={14}
            className="shrink-0 text-text-tertiary"
            aria-label={t("card.external")}
          />
        )}
      </div>

      {/* Title */}
      <h3 className="line-clamp-2 text-sm font-semibold text-text-primary transition-colors group-hover:text-brand-600">
        {application.scholarshipTitle}
      </h3>

      {/* Provider + updated */}
      <div className="flex flex-col gap-1.5">
        <div className="flex items-center gap-1.5 text-xs text-text-secondary">
          <Building2 size={13} className="shrink-0" />
          <span className="truncate">{application.companyName || t("companyFallback")}</span>
        </div>
        <div className="flex items-center gap-1.5 text-xs text-text-tertiary">
          <Calendar size={13} className="shrink-0" />
          <span>
            {t("card.updated")}:{" "}
            {format(new Date(application.updatedAt), "MMM d, yyyy", { locale: dateLocale })}
          </span>
        </div>
      </div>

      {/* Progress indicator */}
      <div className="space-y-1.5">
        <div className="h-1 w-full overflow-hidden rounded-full bg-bg-subtle">
          <div
            className={cn("h-full rounded-full transition-all", tone.bar)}
            style={{ width: `${Math.round(progress * 100)}%` }}
            aria-hidden
          />
        </div>
      </div>

      {/* Footer hint */}
      <div className="flex items-center justify-between pt-1">
        <span
          className={cn(
            "rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider",
            application.mode === "External"
              ? "bg-warning-50 text-warning-600"
              : "bg-brand-50 text-brand-600",
          )}
        >
          {application.mode === "External" ? t("card.external") : t("card.inApp")}
        </span>
        <span className="flex items-center gap-1 text-xs font-semibold text-brand-600 opacity-0 transition-opacity group-hover:opacity-100">
          {t("card.viewDetails")}
          <ArrowRight size={12} className="rtl:rotate-180" />
        </span>
      </div>
    </div>
  );
}
