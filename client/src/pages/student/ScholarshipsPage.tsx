import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import {
  Search,
  Bookmark,
  Filter,
  X,
  Calendar,
  ArrowRight,
  Sparkles,
  GraduationCap,
} from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { parseCalendarDate } from "@/lib/dates";
import { ar, type Locale } from "date-fns/locale";
import { toast } from "sonner";
import { motion } from "motion/react";
import { cn } from "@/lib/utils";
import {
  useScholarshipsQuery,
  useToggleBookmarkMutation,
  useFeaturedScholarshipsQuery,
} from "@/hooks/useScholarshipsQuery";
import { SCHOLARSHIP_FIELDS_OF_STUDY } from "@/constants/scholarshipFields";
import type { FundingType, AcademicLevel } from "@/types/domain";
import type {
  SearchScholarshipsRequest,
  ScholarshipListItem,
} from "@/services/api/scholarships";
import { SkeletonCardGrid } from "@/components/common/Skeleton";
import { DatePicker } from "@/components/ui/DatePicker";
import { apiErrorMessage } from "@/services/api/client";

// ── Constants ─────────────────────────────────────────────────────────────────

const FUNDING_TYPES: FundingType[] = [
  "FullyFunded",
  "PartiallyFunded",
  "TuitionOnly",
  "StipendOnly",
  "Other",
];

const ACADEMIC_LEVELS: AcademicLevel[] = [
  "HighSchool",
  "Undergrad",
  "Masters",
  "PhD",
  "PostDoc",
  "Other",
];

// ── Sub-components ────────────────────────────────────────────────────────────

/**
 * Premium scholarship card — used on the main browse grid + the featured strip.
 * Hero gradient banner at top, content body, footer with deadline + view CTA.
 * Bookmark icon is overlaid on the banner so the user can save without leaving
 * the grid.
 */
function ScholarshipCard({
  scholarship,
  isRtl,
  dateLocale,
  onBookmark,
  variant = "default",
  index = 0,
}: {
  scholarship: ScholarshipListItem;
  isRtl: boolean;
  dateLocale: Locale | undefined;
  onBookmark: (e: React.MouseEvent, id: string) => void;
  variant?: "default" | "featured";
  index?: number;
}) {
  const { t } = useTranslation("scholarships");

  const title        = isRtl ? scholarship.titleAr       : scholarship.titleEn;
  const desc         = isRtl ? scholarship.descriptionAr : scholarship.descriptionEn;
  const deadlineDate = parseCalendarDate(scholarship.deadline) ?? new Date(scholarship.deadline);
  const daysLeft     = differenceInCalendarDays(deadlineDate, new Date());
  const isUrgent     = daysLeft <= 7 && daysLeft >= 0;
  const isClosed     = daysLeft < 0;

  const fundingColors: Record<FundingType, string> = {
    FullyFunded:     "badge-success",
    PartiallyFunded: "badge-brand",
    TuitionOnly:     "badge-brand",
    StipendOnly:     "badge-warning",
    Other:           "badge-neutral",
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25, delay: Math.min(index, 8) * 0.04 }}
    >
      <Link
        to={`/student/scholarships/${scholarship.id}`}
        className={cn(
          "group relative flex h-full flex-col overflow-hidden rounded-2xl border bg-bg-elevated transition-all duration-200",
          "hover:-translate-y-1 hover:shadow-lg",
          variant === "featured"
            ? "border-brand-200 hover:border-brand-300"
            : "border-border-subtle hover:border-brand-200",
        )}
      >
        {/* Hero gradient banner */}
        <div
          className={cn(
            "relative h-28 overflow-hidden",
            variant === "featured"
              ? "bg-gradient-to-br from-brand-500 to-brand-700"
              : "bg-gradient-to-br from-brand-100 to-brand-50",
          )}
        >
          {/* Decorative mesh dot pattern */}
          <div
            className="pointer-events-none absolute inset-0 opacity-30"
            style={{
              backgroundImage:
                "radial-gradient(circle at 20% 20%, rgba(255,255,255,0.4) 0px, transparent 50%), radial-gradient(circle at 80% 80%, rgba(255,255,255,0.25) 0px, transparent 50%)",
            }}
          />

          {/* Top-left: featured chip */}
          {scholarship.isFeatured && (
            <span
              className={cn(
                "absolute start-3 top-3 inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-[11px] font-semibold backdrop-blur-md",
                variant === "featured"
                  ? "bg-white/90 text-brand-700"
                  : "bg-brand-600/90 text-white",
              )}
            >
              <Sparkles aria-hidden className="size-3" />
              {t("card.featured")}
            </span>
          )}

          {/* Top-right: bookmark */}
          <button
            type="button"
            onClick={(e) => onBookmark(e, scholarship.id)}
            aria-label={t("bookmark.toggle")}
            aria-pressed={scholarship.isBookmarked}
            className={cn(
              "absolute end-3 top-3 inline-flex size-8 items-center justify-center rounded-full backdrop-blur-md transition",
              scholarship.isBookmarked
                ? "bg-brand-600 text-white shadow-md"
                : "bg-white/85 text-text-secondary hover:bg-white hover:text-brand-600",
            )}
          >
            <Bookmark
              aria-hidden
              className="size-4"
              fill={scholarship.isBookmarked ? "currentColor" : "none"}
            />
          </button>

          {/* Bottom-left: graduation icon — only on default cards */}
          {variant === "default" && (
            <div className="absolute bottom-3 start-3 flex size-9 items-center justify-center rounded-xl bg-white/85 text-brand-600 shadow-sm backdrop-blur-md">
              <GraduationCap aria-hidden className="size-5" />
            </div>
          )}
        </div>

        {/* Body */}
        <div className="flex flex-1 flex-col p-5">
          {/* Title */}
          <h2 className="line-clamp-2 text-base font-semibold leading-snug text-text-primary transition-colors group-hover:text-brand-600">
            {title}
          </h2>

          {/* Provider + level row */}
          <div className="mt-1 flex items-center gap-2 text-xs text-text-tertiary">
            {scholarship.ownerCompanyName && (
              <>
                <span className="truncate">{scholarship.ownerCompanyName}</span>
                <span aria-hidden>·</span>
              </>
            )}
            <span>{t(`level.${scholarship.targetLevel}`)}</span>
          </div>

          {/* Description */}
          <p className="mt-3 line-clamp-2 flex-1 text-sm leading-relaxed text-text-secondary">
            {desc}
          </p>

          {/* Chips row */}
          <div className="mt-4 flex flex-wrap items-center gap-1.5">
            <span className={cn("badge text-[10.5px]", fundingColors[scholarship.fundingType])}>
              {t(`fundingType.${scholarship.fundingType}`)}
            </span>
            {scholarship.fieldsOfStudy.slice(0, 1).map((f) => (
              <span key={f} className="badge badge-neutral max-w-[10rem] truncate text-[10.5px]">
                {f}
              </span>
            ))}
            {scholarship.fieldsOfStudy.length > 1 && (
              <span className="badge badge-neutral text-[10.5px]">
                +{scholarship.fieldsOfStudy.length - 1}
              </span>
            )}
          </div>

          {/* Footer */}
          <div className="mt-5 flex items-center justify-between border-t border-border-subtle pt-4">
            <div className="flex items-center gap-1.5 text-xs">
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
                  ? t("card.closed")
                  : isUrgent
                    ? t("card.daysLeft", { count: daysLeft })
                    : format(deadlineDate, "dd MMM yyyy", { locale: dateLocale })}
              </span>
            </div>

            <span className="inline-flex items-center gap-1 text-xs font-semibold text-brand-600 opacity-0 transition-opacity group-hover:opacity-100">
              {t("card.viewDetails")}
              <ArrowRight aria-hidden className="size-3.5 rtl:rotate-180" />
            </span>
          </div>
        </div>
      </Link>
    </motion.div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function ScholarshipsPage() {
  const { t, i18n } = useTranslation(["scholarships", "common"]);
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;

  // ── Filters state ─────────────────────────────────────────────────────────
  const [query, setQuery]                   = useState("");
  const [fundingTypes, setFundingTypes]     = useState<FundingType[]>([]);
  const [academicLevels, setAcademicLevels] = useState<AcademicLevel[]>([]);
  const [deadlineFrom, setDeadlineFrom]     = useState("");
  const [deadlineTo, setDeadlineTo]         = useState("");
  const [fieldOfStudy, setFieldOfStudy]     = useState("");
  const [showFilters, setShowFilters]       = useState(false);
  const [page, setPage]                     = useState(1);

  const req: SearchScholarshipsRequest = {
    query:          query          || undefined,
    fundingTypes:   fundingTypes.length   > 0 ? fundingTypes   : undefined,
    academicLevels: academicLevels.length > 0 ? academicLevels : undefined,
    deadlineFrom:   deadlineFrom   || undefined,
    deadlineTo:     deadlineTo     || undefined,
    fieldOfStudy:   fieldOfStudy   || undefined,
    page,
    pageSize: 12,
  };

  const { data, isLoading, isError } = useScholarshipsQuery(req);
  const bookmarkMut = useToggleBookmarkMutation();
  const { data: featured } = useFeaturedScholarshipsQuery(6);

  // ── Handlers ──────────────────────────────────────────────────────────────

  const handleBookmark = (e: React.MouseEvent, id: string) => {
    e.preventDefault();
    e.stopPropagation();
    bookmarkMut.mutate(id, {
      onSuccess: (res) =>
        toast.success(
          res.bookmarked
            ? t("scholarships:bookmark.saved")
            : t("scholarships:bookmark.removed"),
        ),
      onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
    });
  };

  const toggleFilter = <T,>(
    value: T,
    list: T[],
    setter: (v: T[]) => void,
  ) => {
    setter(
      list.includes(value)
        ? list.filter((x) => x !== value)
        : [...list, value],
    );
    setPage(1);
  };

  const clearFilters = () => {
    setQuery("");
    setFundingTypes([]);
    setAcademicLevels([]);
    setDeadlineFrom("");
    setDeadlineTo("");
    setFieldOfStudy("");
    setPage(1);
  };

  const hasActiveFilters =
    fundingTypes.length   > 0 ||
    academicLevels.length > 0 ||
    !!deadlineFrom             ||
    !!deadlineTo               ||
    !!fieldOfStudy;

  const activeFilterCount =
    fundingTypes.length +
    academicLevels.length +
    (deadlineFrom ? 1 : 0) +
    (deadlineTo   ? 1 : 0) +
    (fieldOfStudy ? 1 : 0);

  // ── Render ────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">

      {/* ── Page header — rich layout with stats + subtitle ── */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary">
            {t("scholarships:page.title")}
          </h1>
          <p className="mt-2 max-w-xl text-text-secondary">
            {t("scholarships:page.subtitle")}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {data && (
            <span className="badge badge-brand">
              {t("scholarships:page.total", { count: data.total })}
            </span>
          )}
          <Link to="/student/bookmarks" className="btn btn-secondary btn-sm">
            <Bookmark aria-hidden className="size-4" />
            {t("scholarships:bookmarks.title")}
          </Link>
        </div>
      </div>

      {/* ── Sticky filter bar ── */}
      <div className="sticky top-14 z-20 -mx-4 mb-6 border-y border-border-subtle bg-bg-canvas/85 px-4 py-3 backdrop-blur-xl sm:-mx-6 sm:px-6">
        <div className="flex flex-wrap items-center gap-2">
          {/* Search input */}
          <div className="relative min-w-[200px] max-w-xs flex-1">
            <Search
              aria-hidden
              className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-text-tertiary"
            />
            <input
              type="search"
              value={query}
              onChange={(e) => { setQuery(e.target.value); setPage(1); }}
              placeholder={t("scholarships:search.placeholder")}
              className="h-10 w-full rounded-lg border border-border-default bg-bg-elevated ps-9 pe-3 text-sm transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            />
          </div>

          {/* Pill filters — funding types */}
          {FUNDING_TYPES.slice(0, 3).map((ft) => (
            <button
              key={ft}
              type="button"
              onClick={() => toggleFilter(ft, fundingTypes, setFundingTypes)}
              className={cn(
                "h-10 rounded-full border px-3.5 text-xs font-medium transition",
                fundingTypes.includes(ft)
                  ? "border-brand-500 bg-brand-500 text-white shadow-sm"
                  : "border-border-default bg-bg-elevated text-text-secondary hover:border-brand-300 hover:text-brand-600",
              )}
            >
              {t(`scholarships:fundingType.${ft}`)}
            </button>
          ))}

          {/* More filters button */}
          <button
            type="button"
            onClick={() => setShowFilters((v) => !v)}
            className={cn(
              "inline-flex h-10 items-center gap-1.5 rounded-full border px-3.5 text-xs font-medium transition",
              showFilters
                ? "border-brand-500 bg-brand-50 text-brand-600"
                : "border-border-default bg-bg-elevated text-text-secondary hover:border-brand-300",
            )}
          >
            <Filter aria-hidden className="size-3.5" />
            {t("scholarships:filters.label")}
            {activeFilterCount > 0 && (
              <span className="ms-1 flex size-4 items-center justify-center rounded-full bg-brand-500 text-[9px] font-bold text-white">
                {activeFilterCount}
              </span>
            )}
          </button>

          {/* Clear */}
          {hasActiveFilters && (
            <button
              type="button"
              onClick={clearFilters}
              className="inline-flex h-10 items-center gap-1 rounded-full px-3 text-xs font-medium text-text-tertiary transition hover:text-danger-500"
            >
              <X aria-hidden className="size-3.5" />
              {t("scholarships:filters.clear")}
            </button>
          )}
        </div>

        {/* Expanded filter panel */}
        {showFilters && (
          <div className="mt-3 grid gap-4 rounded-xl border border-border-subtle bg-bg-elevated p-4 shadow-sm sm:grid-cols-2 lg:grid-cols-4">

            <div className="lg:col-span-2">
              <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-text-tertiary">
                {t("scholarships:filters.academicLevel")}
              </p>
              <div className="flex flex-wrap gap-1.5">
                {ACADEMIC_LEVELS.map((lvl) => (
                  <button
                    key={lvl}
                    type="button"
                    onClick={() => toggleFilter(lvl, academicLevels, setAcademicLevels)}
                    className={cn(
                      "rounded-full border px-3 py-1 text-xs font-medium transition",
                      academicLevels.includes(lvl)
                        ? "border-brand-500 bg-brand-500 text-white"
                        : "border-border-subtle bg-bg-canvas text-text-secondary hover:border-brand-300",
                    )}
                  >
                    {t(`scholarships:level.${lvl}`)}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-text-tertiary">
                {t("scholarships:filters.fieldOfStudy")}
              </p>
              <select
                value={fieldOfStudy}
                onChange={(e) => { setFieldOfStudy(e.target.value); setPage(1); }}
                className="w-full rounded-lg border border-border-default bg-bg-canvas px-3 py-2 text-sm transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              >
                <option value="">{t("scholarships:filters.fieldOfStudyAll")}</option>
                {SCHOLARSHIP_FIELDS_OF_STUDY.map((f) => (
                  <option key={f} value={f}>{f}</option>
                ))}
              </select>
            </div>

            <div>
              <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-text-tertiary">
                {t("scholarships:filters.deadlineFrom")}
              </p>
              <DatePicker
                value={deadlineFrom}
                onChange={(v) => { setDeadlineFrom(v); setPage(1); }}
              />
            </div>

            <div className="lg:col-start-4">
              <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-text-tertiary">
                {t("scholarships:filters.deadlineTo")}
              </p>
              <DatePicker
                value={deadlineTo}
                onChange={(v) => { setDeadlineTo(v); setPage(1); }}
                min={deadlineFrom || undefined}
              />
            </div>
          </div>
        )}
      </div>

      {/* ── Featured strip (discovery view — hidden once searching/filtering) ── */}
      {!query && !hasActiveFilters && featured && featured.length > 0 && (
        <section className="space-y-4">
          <div className="flex items-end justify-between">
            <div>
              <h2 className="text-xl font-semibold tracking-tight text-text-primary">
                {t("scholarships:featured.title")}
              </h2>
              <p className="mt-1 text-sm text-text-secondary">
                {t("scholarships:featured.subtitle")}
              </p>
            </div>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {featured.slice(0, 3).map((s, i) => (
              <ScholarshipCard
                key={s.id}
                scholarship={s}
                isRtl={isRtl}
                dateLocale={dateLocale}
                onBookmark={handleBookmark}
                variant="featured"
                index={i}
              />
            ))}
          </div>
        </section>
      )}

      {/* ── Loading skeletons ── */}
      {isLoading && <SkeletonCardGrid count={6} />}

      {/* ── Error state ── */}
      {isError && (
        <div className="rounded-xl border border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
          {t("common:status.error")}
        </div>
      )}

      {/* ── Premium empty state ── */}
      {!isLoading && !isError && (data?.items.length ?? 0) === 0 && (
        <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated p-12 text-center">
          <div className="mb-5 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-100 to-brand-50 text-brand-600">
            <Search aria-hidden className="size-7" />
          </div>
          <h3 className="text-lg font-semibold text-text-primary">
            {t("scholarships:empty.title")}
          </h3>
          <p className="mt-2 max-w-sm text-sm text-text-secondary">
            {t("scholarships:empty.body")}
          </p>
          {hasActiveFilters && (
            <button
              type="button"
              onClick={clearFilters}
              className="btn btn-primary btn-sm mt-6"
            >
              {t("scholarships:empty.clearFilters")}
            </button>
          )}
        </div>
      )}

      {/* ── Results section ── */}
      {!isLoading && (data?.items.length ?? 0) > 0 && (
        <section className="space-y-4">
          {(query || hasActiveFilters) && (
            <h2 className="text-base font-semibold text-text-primary">
              {t("scholarships:page.total", { count: data?.total ?? 0 })}
            </h2>
          )}
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {data!.items.map((s, i) => (
              <ScholarshipCard
                key={s.id}
                scholarship={s}
                isRtl={isRtl}
                dateLocale={dateLocale}
                onBookmark={handleBookmark}
                index={i}
              />
            ))}
          </div>
        </section>
      )}

      {/* ── Pagination ── */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between rounded-xl border border-border-subtle bg-bg-elevated p-4 text-sm">
          <span className="text-text-secondary">
            {t("scholarships:page.pagination", {
              page:  data.page,
              total: data.totalPages,
            })}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={data.page <= 1}
              className="btn btn-secondary btn-sm"
            >
              {t("scholarships:page.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
              disabled={data.page >= data.totalPages}
              className="btn btn-secondary btn-sm"
            >
              {t("scholarships:page.next")}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
