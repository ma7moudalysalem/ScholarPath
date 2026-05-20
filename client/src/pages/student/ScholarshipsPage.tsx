import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Search, Bookmark, Filter, X } from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  useScholarshipsQuery,
  useToggleBookmarkMutation,
  useFeaturedScholarshipsQuery,
} from "@/hooks/useScholarshipsQuery";
import { SCHOLARSHIP_FIELDS_OF_STUDY } from "@/constants/scholarshipFields";
import type { FundingType, AcademicLevel } from "@/types/domain";
import type { SearchScholarshipsRequest } from "@/services/api/scholarships";
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

function FundingBadge({ type }: { type: FundingType }) {
  const { t } = useTranslation("scholarships");
  const colors: Record<FundingType, string> = {
    FullyFunded:     "bg-success-100 text-success-600",
    PartiallyFunded: "bg-brand-100 text-brand-600",
    TuitionOnly:     "bg-info-50 text-brand-700",
    StipendOnly:     "bg-warning-50 text-warning-600",
    Other:           "bg-bg-subtle text-text-tertiary",
  };
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        colors[type],
      )}
    >
      {t(`fundingType.${type}`)}
    </span>
  );
}

/**
 * Bookmark toggle shown on every scholarship card. A filled icon means the
 * scholarship is saved, so the student can tell at a glance — while browsing —
 * which scholarships they have already bookmarked.
 */
function BookmarkButton({
  scholarshipId,
  isBookmarked,
  onToggle,
}: {
  scholarshipId: string;
  isBookmarked: boolean;
  onToggle: (e: React.MouseEvent, id: string) => void;
}) {
  const { t } = useTranslation("scholarships");
  return (
    <button
      type="button"
      onClick={(e) => onToggle(e, scholarshipId)}
      aria-label={t("bookmark.toggle")}
      aria-pressed={isBookmarked}
      className={cn(
        "absolute inset-e-3 top-3 rounded-md p-1.5 transition",
        isBookmarked
          ? "text-brand-500 hover:bg-brand-500/10"
          : "text-text-tertiary hover:bg-bg-subtle hover:text-brand-500",
      )}
    >
      <Bookmark aria-hidden className="size-4" fill={isBookmarked ? "currentColor" : "none"} />
    </button>
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

      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("scholarships:page.title")}
        </h1>
        {data && (
          <span className="text-sm text-text-tertiary">
            {t("scholarships:page.total", { count: data.total })}
          </span>
        )}
      </div>

      {/* ── Search bar ── */}
      <div className="flex flex-wrap items-center gap-3">
        <label className="relative min-w-55 flex-1">
          <Search
            aria-hidden
            className="pointer-events-none absolute inset-s-3 top-1/2 size-4 -translate-y-1/2 text-text-tertiary"
          />
          <input
            type="search"
            value={query}
            onChange={(e) => { setQuery(e.target.value); setPage(1); }}
            placeholder={t("scholarships:search.placeholder")}
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-elevated ps-10 pe-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>

        <button
          type="button"
          onClick={() => setShowFilters((v) => !v)}
          className={cn(
            "inline-flex h-10 items-center gap-2 rounded-md border px-3 text-sm font-medium transition",
            showFilters
              ? "border-brand-500 bg-brand-500/10 text-brand-500"
              : "border-border-subtle bg-bg-elevated text-text-secondary hover:border-border-default",
          )}
        >
          <Filter aria-hidden className="size-4" />
          {t("scholarships:filters.label")}
          {hasActiveFilters && (
            <span className="flex size-5 items-center justify-center rounded-full bg-brand-500 text-[10px] font-semibold text-text-on-brand">
              {activeFilterCount}
            </span>
          )}
        </button>

        {hasActiveFilters && (
          <button
            type="button"
            onClick={clearFilters}
            className="inline-flex h-10 items-center gap-1.5 rounded-md px-3 text-sm text-text-tertiary hover:text-text-secondary"
          >
            <X aria-hidden className="size-3.5" />
            {t("scholarships:filters.clear")}
          </button>
        )}
      </div>

      {/* ── Filter panel ── */}
      {showFilters && (
        <div className="space-y-4 rounded-lg border border-border-subtle bg-bg-elevated p-4">

          <div>
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
              {t("scholarships:filters.fundingType")}
            </p>
            <div className="flex flex-wrap gap-2">
              {FUNDING_TYPES.map((ft) => (
                <button
                  key={ft}
                  type="button"
                  onClick={() => toggleFilter(ft, fundingTypes, setFundingTypes)}
                  className={cn(
                    "rounded-full border px-3 py-1 text-xs font-medium transition",
                    fundingTypes.includes(ft)
                      ? "border-brand-500 bg-brand-500/10 text-brand-500"
                      : "border-border-subtle bg-bg-canvas text-text-secondary hover:border-border-default",
                  )}
                >
                  {t(`scholarships:fundingType.${ft}`)}
                </button>
              ))}
            </div>
          </div>

          <div>
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
              {t("scholarships:filters.academicLevel")}
            </p>
            <div className="flex flex-wrap gap-2">
              {ACADEMIC_LEVELS.map((lvl) => (
                <button
                  key={lvl}
                  type="button"
                  onClick={() => toggleFilter(lvl, academicLevels, setAcademicLevels)}
                  className={cn(
                    "rounded-full border px-3 py-1 text-xs font-medium transition",
                    academicLevels.includes(lvl)
                      ? "border-brand-500 bg-brand-500/10 text-brand-500"
                      : "border-border-subtle bg-bg-canvas text-text-secondary hover:border-border-default",
                  )}
                >
                  {t(`scholarships:level.${lvl}`)}
                </button>
              ))}
            </div>
          </div>

          <div>
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
              {t("scholarships:filters.fieldOfStudy")}
            </p>
            <select
              value={fieldOfStudy}
              onChange={(e) => { setFieldOfStudy(e.target.value); setPage(1); }}
              className="w-full rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2 text-sm focus:border-brand-500 focus:outline-none sm:w-64"
            >
              <option value="">{t("scholarships:filters.fieldOfStudyAll")}</option>
              {SCHOLARSHIP_FIELDS_OF_STUDY.map((f) => (
                <option key={f} value={f}>{f}</option>
              ))}
            </select>
          </div>

          <div className="flex flex-wrap gap-3">
            <div className="w-full sm:w-56">
              <label className="mb-1 block text-xs font-medium text-text-secondary">
                {t("scholarships:filters.deadlineFrom")}
              </label>
              <DatePicker
                value={deadlineFrom}
                onChange={(v) => { setDeadlineFrom(v); setPage(1); }}
              />
            </div>
            <div className="w-full sm:w-56">
              <label className="mb-1 block text-xs font-medium text-text-secondary">
                {t("scholarships:filters.deadlineTo")}
              </label>
              <DatePicker
                value={deadlineTo}
                onChange={(v) => { setDeadlineTo(v); setPage(1); }}
                min={deadlineFrom || undefined}
              />
            </div>
          </div>
        </div>
      )}

      {/* ── Featured strip (discovery view only — hidden once searching/filtering) ── */}
      {!query && !hasActiveFilters && featured && featured.length > 0 && (
        <section className="space-y-3">
          <div>
            <h2 className="text-lg font-semibold tracking-tight text-text-primary">
              {t("scholarships:featured.title")}
            </h2>
            <p className="text-sm text-text-secondary">
              {t("scholarships:featured.subtitle")}
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {featured.map((s) => {
              const title        = isRtl ? s.titleAr       : s.titleEn;
              const desc         = isRtl ? s.descriptionAr : s.descriptionEn;
              const deadlineDate = new Date(s.deadline);
              const daysLeft     = differenceInCalendarDays(deadlineDate, new Date());
              const isUrgent     = daysLeft <= 7 && daysLeft >= 0;

              return (
                <Link
                  key={s.id}
                  to={`/student/scholarships/${s.id}`}
                  className="group relative flex flex-col rounded-xl border border-brand-500/30 bg-brand-500/5 p-5 shadow-xs transition hover:border-brand-500/60 hover:shadow-sm"
                >
                  <BookmarkButton
                    scholarshipId={s.id}
                    isBookmarked={s.isBookmarked}
                    onToggle={handleBookmark}
                  />

                  <span className="mb-3 inline-flex w-fit items-center rounded-full bg-brand-500/10 px-2 py-0.5 text-xs font-medium text-brand-500">
                    ★ {t("scholarships:card.featured")}
                  </span>

                  <h3 className="mb-1 line-clamp-2 text-sm font-semibold text-text-primary group-hover:text-brand-500">
                    {title}
                  </h3>

                  <p className="mb-3 line-clamp-2 flex-1 text-xs text-text-secondary">
                    {desc}
                  </p>

                  <div className="mt-auto space-y-2">
                    <FundingBadge type={s.fundingType} />

                    {s.fieldsOfStudy.length > 0 && (
                      <div className="flex flex-wrap gap-1">
                        {s.fieldsOfStudy.slice(0, 2).map((f) => (
                          <span
                            key={f}
                            className="inline-flex rounded-full border border-border-subtle bg-bg-subtle px-2 py-0.5 text-[10px] font-medium text-text-tertiary"
                          >
                            {f}
                          </span>
                        ))}
                        {s.fieldsOfStudy.length > 2 && (
                          <span className="inline-flex rounded-full border border-border-subtle bg-bg-subtle px-2 py-0.5 text-[10px] font-medium text-text-tertiary">
                            +{s.fieldsOfStudy.length - 2}
                          </span>
                        )}
                      </div>
                    )}

                    <div className="flex items-center justify-between text-xs text-text-tertiary">
                      <span>{t(`scholarships:level.${s.targetLevel}`)}</span>
                      <span
                        className={cn(
                          "font-medium",
                          isUrgent ? "text-danger-500" : "text-text-tertiary",
                        )}
                      >
                        {daysLeft < 0
                          ? t("scholarships:card.closed")
                          : isUrgent
                            ? t("scholarships:card.daysLeft", { count: daysLeft })
                            : format(deadlineDate, "dd MMM yyyy", { locale: dateLocale })}
                      </span>
                    </div>
                  </div>
                </Link>
              );
            })}
          </div>
        </section>
      )}

      {/* ── Loading skeletons ── */}
      {isLoading && <SkeletonCardGrid count={6} />}

      {/* ── Error state ── */}
      {isError && (
        <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
          {t("common:status.error")}
        </div>
      )}

      {/* ── Empty state ── */}
      {!isLoading && !isError && (data?.items.length ?? 0) === 0 && (
        <div className="flex flex-col items-center justify-center rounded-xl border border-border-subtle bg-bg-elevated py-16 text-center">
          <Search aria-hidden className="mb-3 size-10 text-text-tertiary" />
          <p className="text-base font-medium text-text-primary">
            {t("scholarships:empty.title")}
          </p>
          <p className="mt-1 text-sm text-text-secondary">
            {t("scholarships:empty.body")}
          </p>
        </div>
      )}

      {/* ── Results grid ── */}
      {!isLoading && (data?.items.length ?? 0) > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {data!.items.map((s) => {
            const title        = isRtl ? s.titleAr       : s.titleEn;
            const desc         = isRtl ? s.descriptionAr : s.descriptionEn;
            const deadlineDate = new Date(s.deadline);
            const daysLeft     = differenceInCalendarDays(deadlineDate, new Date());
            const isUrgent     = daysLeft <= 7 && daysLeft >= 0;

            return (
              <Link
                key={s.id}
                to={`/student/scholarships/${s.id}`}
                className="group relative flex flex-col rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition hover:border-brand-500/40 hover:shadow-sm"
              >
                <BookmarkButton
                  scholarshipId={s.id}
                  isBookmarked={s.isBookmarked}
                  onToggle={handleBookmark}
                />

                {/* Featured badge */}
                {s.isFeatured && (
                  <span className="mb-3 inline-flex w-fit items-center rounded-full bg-brand-500/10 px-2 py-0.5 text-xs font-medium text-brand-500">
                    ★ {t("scholarships:card.featured")}
                  </span>
                )}

                <h2 className="mb-1 line-clamp-2 text-sm font-semibold text-text-primary group-hover:text-brand-500">
                  {title}
                </h2>

                <p className="mb-3 line-clamp-2 flex-1 text-xs text-text-secondary">
                  {desc}
                </p>

                <div className="mt-auto space-y-2">
                  <FundingBadge type={s.fundingType} />

                  {/* Fields of study — show first 2, then "+N more" */}
                  {s.fieldsOfStudy.length > 0 && (
                    <div className="flex flex-wrap gap-1">
                      {s.fieldsOfStudy.slice(0, 2).map((f) => (
                        <span
                          key={f}
                          className="inline-flex rounded-full border border-border-subtle bg-bg-subtle px-2 py-0.5 text-[10px] font-medium text-text-tertiary"
                        >
                          {f}
                        </span>
                      ))}
                      {s.fieldsOfStudy.length > 2 && (
                        <span className="inline-flex rounded-full border border-border-subtle bg-bg-subtle px-2 py-0.5 text-[10px] font-medium text-text-tertiary">
                          +{s.fieldsOfStudy.length - 2}
                        </span>
                      )}
                    </div>
                  )}

                  <div className="flex items-center justify-between text-xs text-text-tertiary">
                    <span>{t(`scholarships:level.${s.targetLevel}`)}</span>
                    <span
                      className={cn(
                        "font-medium",
                        isUrgent ? "text-danger-500" : "text-text-tertiary",
                      )}
                    >
                      {daysLeft < 0
                        ? t("scholarships:card.closed")
                        : isUrgent
                          ? t("scholarships:card.daysLeft", { count: daysLeft })
                          : format(deadlineDate, "dd MMM yyyy", { locale: dateLocale })}
                    </span>
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      )}

      {/* ── Pagination ── */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-text-secondary">
          <span>
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
              className="rounded-md border border-border-subtle px-3 py-1.5 disabled:opacity-40 hover:border-border-default"
            >
              {t("scholarships:page.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
              disabled={data.page >= data.totalPages}
              className="rounded-md border border-border-subtle px-3 py-1.5 disabled:opacity-40 hover:border-border-default"
            >
              {t("scholarships:page.next")}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}