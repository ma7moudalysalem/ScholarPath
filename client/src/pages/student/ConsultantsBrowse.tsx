import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { Star, Search, X, ArrowRight, Filter, MessageSquare } from "lucide-react";
import { motion } from "motion/react";
import { cn } from "@/lib/utils";
import { useConsultantsQuery } from "@/hooks/useConsultantsQuery";
import type { ConsultantSummary } from "@/services/api/consultants";
import { durationLabel, formatUsd } from "@/lib/bookingFormat";
import { UserAvatar } from "@/components/common/UserAvatar";

type BrowseFilter = "all" | "available" | "unavailable";

type PriceSelectFilter = "any" | "under30" | "30to35" | "above35";
type RatingSelectFilter = "any" | "4plus" | "4_5plus" | "4_8plus";
type AvailabilitySelectFilter = "all" | "available" | "unavailable";
type SortOption =
  | "ratingDesc"
  | "feeAsc"
  | "feeDesc"
  | "experienceDesc"
  | "reviewsDesc";

/**
 * Fallback specialization list — the 8 canonical seeded ExpertiseTag rows
 * (DbSeeder.Scholarships.cs) plus 7 common adjacencies. Used when no
 * consultant has tagged themselves yet so the dropdown isn't empty on a
 * cold-start database. Kept in EN here; the labels are user-data so they
 * aren't translated.
 */
const FALLBACK_SPECIALIZATIONS = [
  "Statement of Purpose",
  "Interview Preparation",
  "University Selection",
  "CV Review",
  "Research Proposals",
  "Funding Strategy",
  "PhD Applications",
  "Scholarship Search",
  "Master's Applications",
  "Undergraduate Applications",
  "Test Preparation",
  "Language Proficiency",
  "Visa Guidance",
  "Career Counseling",
  "Letter of Recommendation",
];

type SearchFormState = {
  query: string;
  price: PriceSelectFilter;
  rating: RatingSelectFilter;
  availability: AvailabilitySelectFilter;
  specialization: string;
  sort: SortOption;
};

const defaultSearchForm: SearchFormState = {
  query: "",
  price: "any",
  rating: "any",
  availability: "all",
  specialization: "any",
  sort: "ratingDesc",
};

/**
 * Five-star rating display used on the browse card. Whole stars are filled,
 * a half-star approximation is drawn for the fractional remainder (rounded to
 * the nearest 0.5), and the rest are outlined.
 */
function StarRating({
  value,
  ariaLabel,
  size = 14,
}: {
  value: number | null;
  ariaLabel?: string;
  size?: number;
}) {
  const rounded = value == null ? 0 : Math.round(value * 2) / 2;
  return (
    <div className="flex items-center gap-0.5" aria-label={ariaLabel} role="img">
      {Array.from({ length: 5 }, (_, i) => {
        const pos = i + 1;
        const isFull = rounded >= pos;
        const isHalf = !isFull && rounded >= pos - 0.5;
        return (
          <span
            key={i}
            className="relative inline-flex"
            style={{ width: size, height: size }}
          >
            <Star size={size} className="text-text-tertiary/40" strokeWidth={1.5} />
            {(isFull || isHalf) && (
              <span
                className="absolute inset-0 overflow-hidden"
                style={{ width: isHalf ? "50%" : "100%" }}
              >
                <Star
                  size={size}
                  className="fill-amber-400 text-amber-400"
                  strokeWidth={1.5}
                />
              </span>
            )}
          </span>
        );
      })}
    </div>
  );
}

function matchesPriceFilter(consultant: ConsultantSummary, value: PriceSelectFilter) {
  if (value === "any") return true;
  const fee = consultant.sessionFeeUsd;
  if (fee == null) return false;
  if (value === "under30") return fee < 30;
  if (value === "30to35") return fee >= 30 && fee <= 35;
  return fee > 35;
}

function matchesRatingFilter(consultant: ConsultantSummary, value: RatingSelectFilter) {
  if (value === "any") return true;
  const rating = consultant.averageRating;
  if (rating == null) return false;
  if (value === "4plus") return rating >= 4;
  if (value === "4_5plus") return rating >= 4.5;
  return rating >= 4.8;
}

function matchesAvailabilityFilter(
  consultant: ConsultantSummary,
  value: AvailabilitySelectFilter,
) {
  if (value === "all") return true;
  return value === "available" ? consultant.hasAvailability : !consultant.hasAvailability;
}

function matchesSpecializationFilter(consultant: ConsultantSummary, value: string) {
  if (value === "any") return true;
  return consultant.expertiseTags.some(
    (tag) => tag.toLowerCase() === value.toLowerCase(),
  );
}

function compareBySort(
  a: ConsultantSummary,
  b: ConsultantSummary,
  sort: SortOption,
): number {
  switch (sort) {
    case "feeAsc": {
      const af = a.sessionFeeUsd ?? Number.POSITIVE_INFINITY;
      const bf = b.sessionFeeUsd ?? Number.POSITIVE_INFINITY;
      return af - bf;
    }
    case "feeDesc": {
      const af = a.sessionFeeUsd ?? -1;
      const bf = b.sessionFeeUsd ?? -1;
      return bf - af;
    }
    case "experienceDesc":
      return b.completedSessionCount - a.completedSessionCount;
    case "reviewsDesc":
      return b.reviewCount - a.reviewCount;
    case "ratingDesc":
    default:
      return (b.averageRating ?? 0) - (a.averageRating ?? 0);
  }
}

// ── Consultant card ──────────────────────────────────────────────────────────

function ConsultantCard({
  consultant,
  index,
}: {
  consultant: ConsultantSummary;
  index: number;
}) {
  const { t } = useTranslation("consultants");

  return (
    <motion.article
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25, delay: Math.min(index, 8) * 0.04 }}
      className="group flex h-full flex-col overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated transition-all duration-200 hover:-translate-y-1 hover:border-brand-200 hover:shadow-lg"
    >
      {/* Decorative gradient strip */}
      <div className="h-1.5 w-full bg-gradient-to-r from-brand-500 via-brand-400 to-brand-600 opacity-60 group-hover:opacity-100" />

      <div className="flex flex-1 flex-col p-5">
        {/* Header — avatar + name + availability */}
        <div className="flex items-start gap-4">
          <div className="relative shrink-0">
            <UserAvatar
              userId={consultant.id}
              name={consultant.name}
              className="size-14"
              initialsClassName="text-lg"
            />
            {consultant.hasAvailability && (
              <span
                className="absolute bottom-0 end-0 size-3.5 rounded-full bg-success-500 ring-2 ring-bg-elevated"
                aria-label={t("badge.available")}
              />
            )}
          </div>

          <div className="min-w-0 flex-1">
            <h3 className="truncate text-base font-semibold text-text-primary">
              {consultant.name}
            </h3>

            {/* Rating + sessions */}
            <div className="mt-1.5 flex items-center gap-2 text-xs">
              {consultant.averageRating != null ? (
                <span className="inline-flex items-center gap-1">
                  <StarRating value={consultant.averageRating} size={12} />
                  <span className="font-semibold text-text-primary">
                    {consultant.averageRating.toFixed(1)}
                  </span>
                  <span className="text-text-tertiary">({consultant.reviewCount})</span>
                </span>
              ) : (
                <span className="text-text-tertiary">{t("card.noRating")}</span>
              )}
              <span aria-hidden className="text-text-tertiary">·</span>
              <span className="text-text-tertiary">
                {t("card.sessionsShort", { count: consultant.completedSessionCount })}
              </span>
            </div>
          </div>
        </div>

        {/* Expertise tags */}
        <div className="mt-4 flex flex-wrap gap-1.5">
          {consultant.expertiseTags.length > 0 ? (
            <>
              {consultant.expertiseTags.slice(0, 3).map((tag) => (
                <span key={tag} className="badge badge-brand text-[10.5px]">
                  {tag}
                </span>
              ))}
              {consultant.expertiseTags.length > 3 && (
                <span className="badge badge-neutral text-[10.5px]">
                  +{consultant.expertiseTags.length - 3}
                </span>
              )}
            </>
          ) : (
            <span className="text-xs text-text-tertiary">{t("card.noExpertiseTags")}</span>
          )}
        </div>

        {/* Bio */}
        {consultant.biography ? (
          <p className="mt-4 line-clamp-2 flex-1 text-sm leading-relaxed text-text-secondary">
            {consultant.biography}
          </p>
        ) : (
          <div className="mt-4 flex-1" />
        )}

        {/* Footer — rate + CTA */}
        <div className="mt-5 flex items-center justify-between border-t border-border-subtle pt-4">
          <div>
            {consultant.sessionFeeUsd != null ? (
              <>
                <p className="text-lg font-bold text-brand-600">
                  {formatUsd(consultant.sessionFeeUsd)}
                </p>
                <p className="text-[10px] font-medium uppercase tracking-wider text-text-tertiary">
                  {consultant.sessionDurationMinutes != null
                    ? durationLabel(consultant.sessionDurationMinutes, t)
                    : t("card.perSession")}
                </p>
              </>
            ) : (
              <p className="text-sm text-text-tertiary">{t("card.feeUnset")}</p>
            )}
          </div>

          <Link
            to={`/student/consultants/${consultant.id}`}
            className="btn btn-primary btn-sm"
          >
            {t("card.bookSession")}
            <ArrowRight aria-hidden className="size-3.5 rtl:rotate-180" />
          </Link>
        </div>
      </div>
    </motion.article>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function ConsultantsBrowse() {
  const { t } = useTranslation("consultants");
  const { data, isLoading, isError } = useConsultantsQuery();

  const [quickFilter, setQuickFilter] = useState<BrowseFilter>("all");
  const [searchForm, setSearchForm] = useState<SearchFormState>(defaultSearchForm);
  const [appliedSearch, setAppliedSearch] = useState<SearchFormState>(defaultSearchForm);
  const [showFilters, setShowFilters] = useState(false);

  // Live search: 250 ms debounce
  useEffect(() => {
    const timer = setTimeout(() => setAppliedSearch(searchForm), 250);
    return () => clearTimeout(timer);
  }, [searchForm]);

  const consultants = useMemo<ConsultantSummary[]>(() => data ?? [], [data]);

  const specializationOptions = useMemo(() => {
    const seen = new Map<string, string>();
    for (const consultant of consultants) {
      for (const tag of consultant.expertiseTags) {
        const key = tag.trim().toLowerCase();
        if (!key) continue;
        if (!seen.has(key)) seen.set(key, tag.trim());
      }
    }
    // Fall back to the canonical seeded list (+ common adjacencies) when no
    // consultant has tagged themselves yet — keeps the dropdown useful on a
    // cold-start database. Once consultants exist we use only their tags so
    // we never offer a filter that returns zero results.
    if (seen.size === 0) {
      return [...FALLBACK_SPECIALIZATIONS].sort((a, b) => a.localeCompare(b));
    }
    return Array.from(seen.values()).sort((a, b) => a.localeCompare(b));
  }, [consultants]);

  const searchedConsultants = useMemo(() => {
    const normalizedQuery = appliedSearch.query.trim().toLowerCase();
    const matched = consultants.filter((consultant) => {
      const searchableText = [
        consultant.name,
        consultant.biography ?? "",
        ...consultant.expertiseTags,
        ...consultant.languages,
      ]
        .join(" ")
        .toLowerCase();

      const queryMatch =
        normalizedQuery.length === 0 || searchableText.includes(normalizedQuery);
      const priceMatch = matchesPriceFilter(consultant, appliedSearch.price);
      const ratingMatch = matchesRatingFilter(consultant, appliedSearch.rating);
      const availabilityMatch = matchesAvailabilityFilter(consultant, appliedSearch.availability);
      const specMatch = matchesSpecializationFilter(consultant, appliedSearch.specialization);

      return queryMatch && priceMatch && ratingMatch && availabilityMatch && specMatch;
    });

    return [...matched].sort((a, b) => {
      const primary = compareBySort(a, b, appliedSearch.sort);
      return primary !== 0 ? primary : a.name.localeCompare(b.name);
    });
  }, [appliedSearch, consultants]);

  const filteredConsultants = useMemo(() => {
    return searchedConsultants.filter((consultant) => {
      if (quickFilter === "available") return consultant.hasAvailability;
      if (quickFilter === "unavailable") return !consultant.hasAvailability;
      return true;
    });
  }, [quickFilter, searchedConsultants]);

  const totalConsultants = consultants.length;
  const availableCount = consultants.filter((item) => item.hasAvailability).length;

  const handleResetFilters = () => {
    setSearchForm(defaultSearchForm);
    setAppliedSearch(defaultSearchForm);
    setQuickFilter("all");
  };

  const hasActiveFilters =
    appliedSearch.query.length > 0 ||
    appliedSearch.price !== "any" ||
    appliedSearch.rating !== "any" ||
    appliedSearch.availability !== "all" ||
    appliedSearch.specialization !== "any" ||
    quickFilter !== "all";

  return (
    <div className="space-y-6">

      {/* ── Page header ── */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary">
            {t("browse.title")}
          </h1>
          <p className="mt-2 max-w-xl text-text-secondary">{t("browse.subtitle")}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <span className="badge badge-brand">
            {t("stats.total")} {totalConsultants}
          </span>
          <span className="badge badge-success">
            <span className="me-1 inline-block size-1.5 rounded-full bg-success-500" />
            {availableCount} {t("stats.available")}
          </span>
        </div>
      </div>

      {/* ── Sticky filter bar ── */}
      <div className="sticky top-14 z-20 -mx-4 mb-6 border-y border-border-subtle bg-bg-canvas/85 px-4 py-3 backdrop-blur-xl sm:-mx-6 sm:px-6">
        <div className="flex flex-wrap items-center gap-2">
          {/* Search input */}
          <div className="relative min-w-[220px] max-w-md flex-1">
            <Search
              aria-hidden
              className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-text-tertiary"
            />
            <input
              type="search"
              value={searchForm.query}
              onChange={(e) =>
                setSearchForm((cur) => ({ ...cur, query: e.target.value }))
              }
              placeholder={t("search.placeholder")}
              className="h-10 w-full rounded-lg border border-border-default bg-bg-elevated ps-9 pe-3 text-sm transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            />
          </div>

          {/* Quick filters as pills */}
          {(["all", "available", "unavailable"] as const).map((key) => (
            <button
              key={key}
              type="button"
              onClick={() => setQuickFilter(key)}
              className={cn(
                "h-10 rounded-full border px-3.5 text-xs font-medium transition",
                quickFilter === key
                  ? "border-brand-500 bg-brand-500 text-white shadow-sm"
                  : "border-border-default bg-bg-elevated text-text-secondary hover:border-brand-300",
              )}
            >
              {t(`quickFilters.${key}`)}
            </button>
          ))}

          {/* Always-visible Sort by dropdown — promoted out of the
              expandable panel because it's the most-used filter on browse. */}
          <div className="relative inline-flex h-10 items-center">
            <label
              htmlFor="sort-quick"
              className="pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 text-[11px] font-semibold uppercase tracking-wide text-text-tertiary"
            >
              {t("filters.sortBy")}
            </label>
            <select
              id="sort-quick"
              value={searchForm.sort}
              onChange={(e) =>
                setSearchForm((cur) => ({
                  ...cur,
                  sort: e.target.value as SortOption,
                }))
              }
              className="h-10 appearance-none rounded-full border border-border-default bg-bg-elevated ps-[88px] pe-3.5 text-xs font-medium text-text-secondary transition hover:border-brand-300 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              aria-label={t("filters.sortBy")}
            >
              <option value="ratingDesc">{t("sortOptions.ratingDesc")}</option>
              <option value="feeAsc">{t("sortOptions.feeAsc")}</option>
              <option value="feeDesc">{t("sortOptions.feeDesc")}</option>
              <option value="reviewsDesc">{t("sortOptions.reviewsDesc")}</option>
              <option value="experienceDesc">{t("sortOptions.experienceDesc")}</option>
            </select>
          </div>

          {/* More filters */}
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
            {t("filters.more")}
          </button>

          {hasActiveFilters && (
            <button
              type="button"
              onClick={handleResetFilters}
              className="inline-flex h-10 items-center gap-1 rounded-full px-3 text-xs font-medium text-text-tertiary transition hover:text-danger-500"
            >
              <X aria-hidden className="size-3.5" />
              {t("search.reset")}
            </button>
          )}
        </div>

        {showFilters && (
          <div className="mt-3 grid gap-4 rounded-xl border border-border-subtle bg-bg-elevated p-4 shadow-sm sm:grid-cols-2 lg:grid-cols-3">
            <div>
              <label
                htmlFor="price-filter"
                className="mb-1.5 block text-[11px] font-semibold uppercase tracking-wide text-text-tertiary"
              >
                {t("filters.price")}
              </label>
              <select
                id="price-filter"
                value={searchForm.price}
                onChange={(e) =>
                  setSearchForm((cur) => ({
                    ...cur,
                    price: e.target.value as PriceSelectFilter,
                  }))
                }
                className="h-10 w-full rounded-lg border border-border-default bg-bg-canvas px-3 text-sm transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              >
                <option value="any">{t("priceOptions.any")}</option>
                <option value="under30">{t("priceOptions.under30")}</option>
                <option value="30to35">{t("priceOptions.30to35")}</option>
                <option value="above35">{t("priceOptions.above35")}</option>
              </select>
            </div>

            <div>
              <label
                htmlFor="rating-filter"
                className="mb-1.5 block text-[11px] font-semibold uppercase tracking-wide text-text-tertiary"
              >
                {t("filters.rating")}
              </label>
              <select
                id="rating-filter"
                value={searchForm.rating}
                onChange={(e) =>
                  setSearchForm((cur) => ({
                    ...cur,
                    rating: e.target.value as RatingSelectFilter,
                  }))
                }
                className="h-10 w-full rounded-lg border border-border-default bg-bg-canvas px-3 text-sm transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              >
                <option value="any">{t("ratingOptions.any")}</option>
                <option value="4plus">{t("ratingOptions.4plus")}</option>
                <option value="4_5plus">{t("ratingOptions.4_5plus")}</option>
                <option value="4_8plus">{t("ratingOptions.4_8plus")}</option>
              </select>
            </div>

            <div>
              <label
                htmlFor="specialization-filter"
                className="mb-1.5 block text-[11px] font-semibold uppercase tracking-wide text-text-tertiary"
              >
                {t("filters.specialization")}
              </label>
              <select
                id="specialization-filter"
                value={searchForm.specialization}
                onChange={(e) =>
                  setSearchForm((cur) => ({ ...cur, specialization: e.target.value }))
                }
                className="h-10 w-full rounded-lg border border-border-default bg-bg-canvas px-3 text-sm transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              >
                <option value="any">{t("specializationOptions.any")}</option>
                {specializationOptions.map((tag) => (
                  <option key={tag} value={tag}>{tag}</option>
                ))}
              </select>
            </div>
          </div>
        )}
      </div>

      {/* ── Results header ── */}
      <div className="flex items-end justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight text-text-primary">
            {t("results.heading")}
          </h2>
          <p className="mt-1 text-sm text-text-secondary">{t("results.subtitle")}</p>
        </div>
        {!isLoading && !isError && (
          <span className="shrink-0 text-sm font-semibold text-brand-600">
            {t("results.count", { count: filteredConsultants.length })}
          </span>
        )}
      </div>

      {/* ── States ── */}
      {isError ? (
        <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm font-medium text-danger-500">
          {t("states.error")}
        </div>
      ) : isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div
              key={i}
              className="flex flex-col gap-4 rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-sm"
            >
              <div className="flex items-center gap-3">
                <div className="skeleton size-14 rounded-full" />
                <div className="flex-1 space-y-2">
                  <div className="skeleton h-4 w-2/3" />
                  <div className="skeleton h-3 w-1/2" />
                </div>
              </div>
              <div className="skeleton h-3 w-full" />
              <div className="skeleton h-3 w-4/5" />
              <div className="flex gap-2">
                <div className="skeleton h-6 w-20 rounded-full" />
                <div className="skeleton h-6 w-16 rounded-full" />
              </div>
            </div>
          ))}
        </div>
      ) : filteredConsultants.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {filteredConsultants.map((c, i) => (
            <ConsultantCard key={c.id} consultant={c} index={i} />
          ))}
        </div>
      ) : (
        /* Premium empty state */
        <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated p-12 text-center">
          <div className="mb-5 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-100 to-brand-50 text-brand-600">
            <MessageSquare aria-hidden className="size-7" />
          </div>
          <h3 className="text-lg font-semibold text-text-primary">
            {consultants.length === 0 ? t("empty.noConsultants") : t("empty.title")}
          </h3>
          <p className="mt-2 max-w-md text-sm text-text-secondary">
            {consultants.length === 0 ? t("empty.noConsultantsBody") : t("empty.body")}
          </p>
          {consultants.length > 0 && (
            <button
              type="button"
              onClick={handleResetFilters}
              className="btn btn-primary btn-sm mt-6"
            >
              {t("empty.reset")}
            </button>
          )}
        </div>
      )}
    </div>
  );
}
