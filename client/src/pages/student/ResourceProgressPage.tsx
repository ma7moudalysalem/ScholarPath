import { Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import {
  TrendingUp,
  ArrowRight,
  ArrowLeft,
  CheckCircle,
  BookOpen,
  Clock,
} from "lucide-react";
import { motion } from "motion/react";
import { resourcesApi, type ResourceProgressItem } from "@/services/api/resources";

export function ResourceProgressPage() {
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isAr ? ar : undefined;
  const ContinueIcon = isRtl ? ArrowLeft : ArrowRight;

  const { data, isLoading, isError, refetch } = useQuery<ResourceProgressItem[]>({
    queryKey: ["resources", "progress", "mine"],
    queryFn: () => resourcesApi.getMyProgress(),
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight text-text-primary">
          {t("resources:progress.title")}
        </h1>
        <p className="mt-2 max-w-xl text-text-secondary">
          {t("resources:progress.subtitle")}
        </p>
      </div>

      {isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="skeleton h-36 rounded-2xl" />
          ))}
        </div>
      )}

      {isError && !isLoading && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("resources:progress.loadError")}{" "}
          <button
            type="button"
            onClick={() => void refetch()}
            className="text-brand-500 underline font-medium"
          >
            {t("resources:progress.retry")}
          </button>
        </p>
      )}

      {!isLoading && !isError && data?.length === 0 && (
        <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated p-12 text-center">
          <div className="mb-5 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-100 to-brand-50 text-brand-600">
            <TrendingUp aria-hidden className="size-7" />
          </div>
          <h3 className="text-lg font-semibold text-text-primary">
            {t("resources:progress.empty")}
          </h3>
          <p className="mt-2 max-w-md text-sm text-text-secondary">
            {t("resources:progress.emptyBody")}
          </p>
          <Link to="/student/resources" className="btn btn-primary btn-sm mt-6">
            <BookOpen aria-hidden className="size-4" />
            {t("resources:progress.browse")}
          </Link>
        </div>
      )}

      {!isLoading && !isError && (data?.length ?? 0) > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {data?.map((item, i) => {
            const title = isAr
              ? item.titleAr || item.titleEn
              : item.titleEn || item.titleAr;
            const total = item.totalChapters;
            const done = item.chaptersCompletedCount;
            const pct = total > 0 ? Math.round((done / total) * 100) : 0;
            const isComplete = total > 0 && done >= total;

            return (
              <motion.div
                key={item.resourceId}
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.25, delay: Math.min(i, 8) * 0.04 }}
              >
                <Link
                  to={`/student/resources/${item.slug}`}
                  className="group flex h-full flex-col rounded-2xl border border-border-subtle bg-bg-elevated p-5 transition-all hover:-translate-y-1 hover:border-brand-200 hover:shadow-lg"
                >
                  <div className="flex items-start justify-between gap-3">
                    {isComplete ? (
                      <span className="badge badge-success">
                        <CheckCircle aria-hidden className="size-3" />
                        {t("resources:progress.completed")}
                      </span>
                    ) : (
                      <span className="badge badge-brand">
                        <TrendingUp aria-hidden className="size-3" />
                        {t("resources:progress.inProgress")}
                      </span>
                    )}
                    <span className="text-sm font-bold text-brand-600">
                      {t("resources:progress.percentComplete", { percent: pct })}
                    </span>
                  </div>

                  <h2 className="mt-3 line-clamp-2 text-base font-semibold leading-snug text-text-primary transition-colors group-hover:text-brand-600">
                    {title}
                  </h2>

                  <div
                    className="mt-4 h-2 w-full overflow-hidden rounded-full bg-bg-subtle"
                    role="progressbar"
                    aria-valuenow={pct}
                    aria-valuemin={0}
                    aria-valuemax={100}
                  >
                    <div
                      className={`h-full rounded-full transition-[width] duration-500 ${
                        isComplete
                          ? "bg-gradient-to-r from-success-500 to-success-600"
                          : "bg-gradient-to-r from-brand-500 to-brand-600"
                      }`}
                      style={{ width: `${pct}%` }}
                    />
                  </div>

                  <p className="mt-2 text-xs font-medium text-text-secondary">
                    {t("resources:progress.chaptersDone", { done, total })}
                  </p>

                  <div className="mt-auto flex items-center justify-between pt-4 text-xs text-text-tertiary">
                    <span className="inline-flex items-center gap-1">
                      <Clock aria-hidden className="size-3" />
                      {t("resources:progress.lastAccessed", {
                        date: format(new Date(item.lastAccessedAt), "dd MMM yyyy", {
                          locale: dateLocale,
                        }),
                      })}
                    </span>
                    <span className="inline-flex items-center gap-1 font-semibold text-brand-600">
                      {isComplete
                        ? t("resources:progress.review")
                        : t("resources:progress.continue")}
                      <ContinueIcon aria-hidden className="size-3.5" />
                    </span>
                  </div>
                </Link>
              </motion.div>
            );
          })}
        </div>
      )}
    </div>
  );
}
