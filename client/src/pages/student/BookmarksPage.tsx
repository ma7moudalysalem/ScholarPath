import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Bookmark,
  Search,
  Trash2,
  BookOpen,
  Calendar,
  ArrowRight,
  Sparkles,
  GraduationCap,
} from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { parseCalendarDate } from "@/lib/dates";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { motion } from "motion/react";
import { cn } from "@/lib/utils";
import {
  useBookmarksQuery,
  useToggleBookmarkMutation,
} from "@/hooks/useScholarshipsQuery";
import type { FundingType } from "@/types/domain";
import { SkeletonCardGrid } from "@/components/common/Skeleton";
import { apiErrorMessage } from "@/services/api/client";
import { resourcesApi, type ResourceListItem } from "@/services/api/resources";
import { expertiseTagLabelByLang } from "@/lib/expertiseTagLabel";

type Tab = "scholarships" | "resources";

function fundingBadgeClass(type: FundingType): string {
  switch (type) {
    case "FullyFunded":     return "badge-success";
    case "PartiallyFunded": return "badge-brand";
    case "TuitionOnly":     return "badge-brand";
    case "StipendOnly":     return "badge-warning";
    default:                return "badge-neutral";
  }
}

// ── Scholarship bookmarks tab ─────────────────────────────────────────────────

function ScholarshipsTab() {
  const { t, i18n } = useTranslation(["scholarships", "common"]);
  const isRtl       = i18n.dir() === "rtl";
  const dateLocale  = isRtl ? ar : undefined;

  const { data: bookmarks, isLoading, isError } = useBookmarksQuery();
  const bookmarkMut = useToggleBookmarkMutation();

  const handleRemove = (e: React.MouseEvent, scholarshipId: string) => {
    e.preventDefault();
    e.stopPropagation();
    bookmarkMut.mutate(scholarshipId, {
      onSuccess: () => toast.success(t("scholarships:bookmark.removed")),
      onError:   (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
    });
  };

  if (isLoading) {
    return <SkeletonCardGrid count={6} />;
  }

  if (isError) {
    return (
      <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
        {t("common:status.error")}
      </div>
    );
  }

  if (!bookmarks || bookmarks.length === 0) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated p-12 text-center">
        <div className="mb-5 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-100 to-brand-50 text-brand-600">
          <Bookmark aria-hidden className="size-7" />
        </div>
        <h3 className="text-lg font-semibold text-text-primary">
          {t("scholarships:bookmarks.empty.title")}
        </h3>
        <p className="mt-2 max-w-md text-sm text-text-secondary">
          {t("scholarships:bookmarks.empty.body")}
        </p>
        <Link to="/student/scholarships" className="btn btn-primary btn-sm mt-6">
          <Search aria-hidden className="size-4" />
          {t("scholarships:bookmarks.browse")}
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm font-medium text-text-secondary">
        {t("scholarships:bookmarks.count", { count: bookmarks.length })}
      </p>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {bookmarks.map(({ id, scholarship, savedAt }, i) => {
          const title        = isRtl ? scholarship.titleAr        : scholarship.titleEn;
          const desc         = isRtl ? scholarship.descriptionAr  : scholarship.descriptionEn;
          const deadlineDate = parseCalendarDate(scholarship.deadline) ?? new Date(scholarship.deadline);
          const daysLeft     = differenceInCalendarDays(deadlineDate, new Date());
          const isUrgent     = daysLeft <= 7 && daysLeft >= 0;
          const isClosed     = daysLeft < 0;

          return (
            <motion.div
              key={id}
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.25, delay: Math.min(i, 8) * 0.04 }}
            >
              <Link
                to={`/student/scholarships/${scholarship.id}`}
                className="group relative flex h-full flex-col overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated transition-all duration-200 hover:-translate-y-1 hover:border-brand-200 hover:shadow-lg"
              >
                {/* Hero gradient banner */}
                <div className="relative h-24 overflow-hidden bg-gradient-to-br from-brand-100 to-brand-50">
                  <div
                    className="pointer-events-none absolute inset-0 opacity-30"
                    style={{
                      backgroundImage:
                        "radial-gradient(circle at 20% 20%, rgba(255,255,255,0.4) 0px, transparent 50%), radial-gradient(circle at 80% 80%, rgba(255,255,255,0.25) 0px, transparent 50%)",
                    }}
                  />

                  {/* Featured chip */}
                  {scholarship.isFeatured && (
                    <span className="absolute start-3 top-3 inline-flex items-center gap-1 rounded-full bg-brand-600/90 px-2.5 py-1 text-[11px] font-semibold text-white backdrop-blur-md">
                      <Sparkles aria-hidden className="size-3" />
                      {t("scholarships:card.featured")}
                    </span>
                  )}

                  {/* Remove bookmark */}
                  <button
                    type="button"
                    onClick={(e) => handleRemove(e, scholarship.id)}
                    aria-label={t("scholarships:bookmark.remove")}
                    disabled={bookmarkMut.isPending}
                    className="absolute end-3 top-3 inline-flex size-8 items-center justify-center rounded-full bg-white/85 text-text-secondary backdrop-blur-md transition hover:bg-white hover:text-danger-500 disabled:opacity-50"
                  >
                    <Trash2 aria-hidden className="size-4" />
                  </button>

                  {/* Graduation icon bottom-left */}
                  <div className="absolute bottom-3 start-3 flex size-9 items-center justify-center rounded-xl bg-white/85 text-brand-600 shadow-sm backdrop-blur-md">
                    <GraduationCap aria-hidden className="size-5" />
                  </div>
                </div>

                {/* Body */}
                <div className="flex flex-1 flex-col p-5">
                  <h2 className="line-clamp-2 text-base font-semibold leading-snug text-text-primary transition-colors group-hover:text-brand-600">
                    {title}
                  </h2>

                  <p className="mt-1 text-xs text-text-tertiary">
                    {t(`scholarships:level.${scholarship.targetLevel}`)}
                  </p>

                  <p className="mt-3 line-clamp-2 flex-1 text-sm leading-relaxed text-text-secondary">
                    {desc}
                  </p>

                  <div className="mt-4 flex flex-wrap items-center gap-1.5">
                    <span className={cn("badge text-[10.5px]", fundingBadgeClass(scholarship.fundingType))}>
                      {t(`scholarships:fundingType.${scholarship.fundingType}`)}
                    </span>
                  </div>

                  {/* Footer */}
                  <div className="mt-5 space-y-2 border-t border-border-subtle pt-4">
                    <div className="flex items-center justify-between text-xs">
                      <div className="inline-flex items-center gap-1.5">
                        <Calendar
                          aria-hidden
                          className={cn(
                            "size-3.5",
                            isClosed
                              ? "text-danger-500"
                              : isUrgent
                                ? "text-warning-500"
                                : "text-text-tertiary",
                          )}
                        />
                        <span
                          className={cn(
                            "font-medium",
                            isClosed
                              ? "text-danger-500"
                              : isUrgent
                                ? "text-warning-600"
                                : "text-text-secondary",
                          )}
                        >
                          {isClosed
                            ? t("scholarships:card.closed")
                            : isUrgent
                              ? t("scholarships:card.daysLeft", { count: daysLeft })
                              : format(deadlineDate, "dd MMM yyyy", { locale: dateLocale })}
                        </span>
                      </div>
                      <span className="inline-flex items-center gap-1 text-brand-600 opacity-0 transition-opacity group-hover:opacity-100">
                        <span className="font-semibold">
                          {t("scholarships:card.viewDetails")}
                        </span>
                        <ArrowRight aria-hidden className="size-3.5 rtl:rotate-180" />
                      </span>
                    </div>
                    <p className="text-[10.5px] text-text-tertiary">
                      {t("scholarships:bookmarks.savedAt", {
                        date: format(new Date(savedAt), "dd MMM yyyy", {
                          locale: dateLocale,
                        }),
                      })}
                    </p>
                  </div>
                </div>
              </Link>
            </motion.div>
          );
        })}
      </div>
    </div>
  );
}

// ── Resource bookmarks tab ────────────────────────────────────────────────────

function ResourcesTab() {
  const { t, i18n } = useTranslation(["scholarships", "resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  const { data, isLoading, isError } = useQuery<ResourceListItem[]>({
    queryKey: ["resources", "bookmarks", "mine"],
    queryFn: () => resourcesApi.getMyBookmarks(),
  });

  const toggleMut = useMutation({
    mutationFn: (id: string) => resourcesApi.toggleBookmark(id),
    onSuccess: () => {
      toast.success(t("resources:detail.bookmark"));
      void qc.invalidateQueries({ queryKey: ["resources", "bookmarks", "mine"] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="skeleton h-32 rounded-2xl" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
        {t("common:status.error")}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated p-12 text-center">
        <div className="mb-5 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-100 to-brand-50 text-brand-600">
          <BookOpen aria-hidden className="size-7" />
        </div>
        <h3 className="text-lg font-semibold text-text-primary">
          {t("scholarships:bookmarks.emptyResources.title")}
        </h3>
        <p className="mt-2 max-w-md text-sm text-text-secondary">
          {t("scholarships:bookmarks.emptyResources.body")}
        </p>
        <Link to="/student/resources" className="btn btn-primary btn-sm mt-6">
          <Search aria-hidden className="size-4" />
          {t("scholarships:bookmarks.browseResources")}
        </Link>
      </div>
    );
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {data.map((r, i) => {
        const title = isAr ? r.titleAr || r.titleEn : r.titleEn || r.titleAr;
        const desc  = isAr ? r.descriptionAr || r.descriptionEn : r.descriptionEn || r.descriptionAr;
        return (
          <motion.div
            key={r.id}
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.25, delay: Math.min(i, 6) * 0.04 }}
          >
            <Link
              to={`/student/resources/${r.slug}`}
              className="group relative flex h-full flex-col overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated p-5 transition-all hover:-translate-y-1 hover:border-brand-200 hover:shadow-lg"
            >
              {/* Header */}
              <div className="flex items-start justify-between gap-3">
                <span className="badge badge-brand">
                  <BookOpen aria-hidden className="size-3" />
                  {t(`resources:resourceType.${r.type}`)}
                </span>
                <button
                  type="button"
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    toggleMut.mutate(r.id);
                  }}
                  aria-label={t("scholarships:bookmark.remove")}
                  disabled={toggleMut.isPending}
                  className="text-text-tertiary transition hover:text-danger-500 disabled:opacity-50"
                >
                  <Trash2 aria-hidden className="size-4" />
                </button>
              </div>

              <h2 className="mt-3 line-clamp-2 text-base font-semibold leading-snug text-text-primary transition-colors group-hover:text-brand-600">
                {title}
              </h2>

              {desc && (
                <p className="mt-2 line-clamp-2 flex-1 text-sm leading-relaxed text-text-secondary">
                  {desc}
                </p>
              )}

              {r.tags.length > 0 && (
                <div className="mt-4 flex flex-wrap gap-1.5 border-t border-border-subtle pt-4">
                  {r.tags.slice(0, 3).map((tag) => (
                    <span key={tag} className="badge badge-neutral text-[10.5px]">
                      {expertiseTagLabelByLang(tag, i18n.language)}
                    </span>
                  ))}
                </div>
              )}
            </Link>
          </motion.div>
        );
      })}
    </div>
  );
}

// ── Page shell ────────────────────────────────────────────────────────────────

export function BookmarksPage() {
  const { t } = useTranslation(["scholarships"]);
  const [tab, setTab] = useState<Tab>("scholarships");

  return (
    <div className="space-y-6">
      {/* ── Header ── */}
      <div className="mb-2 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary">
            {t("scholarships:bookmarks.title")}
          </h1>
          <p className="mt-2 max-w-xl text-text-secondary">
            {t("scholarships:bookmarks.empty.body")}
          </p>
        </div>
      </div>

      {/* Premium tab strip */}
      <div className="inline-flex w-fit gap-1 rounded-xl border border-border-subtle bg-bg-subtle p-1">
        {(["scholarships", "resources"] as const).map((key) => (
          <button
            key={key}
            type="button"
            onClick={() => setTab(key)}
            className={cn(
              "rounded-lg px-4 py-2 text-sm font-semibold transition",
              tab === key
                ? "bg-bg-elevated text-text-primary shadow-sm"
                : "text-text-secondary hover:text-text-primary",
            )}
          >
            {t(`scholarships:bookmarks.tabs.${key}`)}
          </button>
        ))}
      </div>

      {tab === "scholarships" ? <ScholarshipsTab /> : <ResourcesTab />}
    </div>
  );
}
