import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";
import { Calendar, Building2, ExternalLink, ArrowRight } from "lucide-react";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import type { StudentApplicationRow } from "@/services/api/applications";
import { cn } from "@/lib/utils";

interface ApplicationCardProps {
  application: StudentApplicationRow;
}

export function ApplicationCard({ application }: ApplicationCardProps) {
  const { t, i18n } = useTranslation("applications");
  const navigate = useNavigate();
  const dateLocale = i18n.dir() === "rtl" ? ar : undefined;

  const openDetails = () => navigate(`/student/applications/${application.applicationId}`);

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
      className="group relative flex flex-col space-y-3 rounded-xl border border-border-subtle bg-bg-elevated p-4 shadow-sm transition-all hover:border-brand-500/40 hover:shadow-md cursor-pointer"
    >
      <div className="flex items-start justify-between">
        <h3 className="line-clamp-2 text-sm font-semibold text-text-primary group-hover:text-brand-600 transition-colors">
          {application.scholarshipTitle}
        </h3>
        {application.mode === "External" && (
          <ExternalLink size={14} className="text-text-tertiary shrink-0 mt-0.5" />
        )}
      </div>

      <div className="flex flex-col space-y-1.5">
        <div className="flex items-center text-xs text-text-secondary">
          <Building2 size={14} className="me-1.5 shrink-0" />
          <span className="truncate">{application.companyName || t("companyFallback")}</span>
        </div>
        <div className="flex items-center text-xs text-text-tertiary">
          <Calendar size={14} className="me-1.5 shrink-0" />
          <span>
            {t("card.updated")}:{" "}
            {format(new Date(application.updatedAt), "MMM d, yyyy", { locale: dateLocale })}
          </span>
        </div>
      </div>

      <div className="flex items-center justify-between pt-2">
        <span
          className={cn(
            "rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wider",
            application.mode === "External"
              ? "bg-warning-50 text-warning-600"
              : "bg-brand-50 text-brand-600",
          )}
        >
          {application.mode === "External" ? t("card.external") : t("card.inApp")}
        </span>

        <span className="flex items-center text-xs font-medium text-brand-600 opacity-0 group-hover:opacity-100 transition-opacity">
          {t("card.viewDetails")}
          <ArrowRight size={12} className="ms-1 rtl:rotate-180" />
        </span>
      </div>
    </div>
  );
}
