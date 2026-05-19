import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { Star } from "lucide-react";
import { useConsultantsQuery } from "@/hooks/useConsultantsQuery";
import type { ConsultantSummary } from "@/services/api/consultants";
import { durationLabel, formatUsd } from "@/lib/bookingFormat";
import { UserAvatar } from "@/components/common/UserAvatar";

type BrowseFilter = "all" | "available" | "unavailable";

type PriceSelectFilter = "any" | "under30" | "30to35" | "above35";
type RatingSelectFilter = "any" | "4plus" | "4_5plus" | "4_8plus";
type AvailabilitySelectFilter = "all" | "available" | "unavailable";
type SortOption = "ratingDesc" | "feeAsc" | "feeDesc" | "experienceDesc";

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
 * the nearest 0.5), and the rest are outlined. Falls back to a neutral row of
 * empty stars when the consultant has no reviews yet.
 */
function StarRating({ value, ariaLabel }: { value: number | null; ariaLabel?: string }) {
  const rounded = value == null ? 0 : Math.round(value * 2) / 2;
  return (
    <div className="flex items-center gap-0.5" aria-label={ariaLabel} role="img">
      {Array.from({ length: 5 }, (_, i) => {
        const pos = i + 1;
        const isFull = rounded >= pos;
        const isHalf = !isFull && rounded >= pos - 0.5;
        return (
          <span key={i} className="relative inline-flex h-4 w-4">
            <Star size={16} className="text-text-tertiary/40" strokeWidth={1.5} />
            {(isFull || isHalf) && (
              <span
                className="absolute inset-0 overflow-hidden"
                style={{ width: isHalf ? "50%" : "100%" }}
              >
                <Star
                  size={16}
                  className="text-amber-400 fill-amber-400"
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
  // Case-insensitive match — consultants self-tag with arbitrary capitalisation,
  // and the dropdown value is one of the canonical-cased tags we built from
  // the union of all consultants' tags.
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
      // Consultants with no published fee sink to the bottom — a "no fee" card
      // doesn't help when the student is filtering by cheapest.
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
    case "ratingDesc":
    default:
      return (b.averageRating ?? 0) - (a.averageRating ?? 0);
  }
}

export function ConsultantsBrowse() {
  const { t } = useTranslation("consultants");
  const { data, isLoading, isError } = useConsultantsQuery();

  const [quickFilter, setQuickFilter] = useState<BrowseFilter>("all");
  const [searchForm, setSearchForm] = useState<SearchFormState>(defaultSearchForm);
  const [appliedSearch, setAppliedSearch] = useState<SearchFormState>(defaultSearchForm);

  // Live search: debounce the form into the applied filters so results update
  // ~250ms after the user stops typing — consistent, no submit press needed
  // (UAT TC-004). The Search button still applies immediately.
  useEffect(() => {
    const timer = setTimeout(() => setAppliedSearch(searchForm), 250);
    return () => clearTimeout(timer);
  }, [searchForm]);

  const consultants = useMemo<ConsultantSummary[]>(() => data ?? [], [data]);

  // Union of every expertise tag any consultant has self-declared — feeds the
  // Specialization dropdown so it always reflects what's actually available
  // (no empty buckets). Sorted alphabetically for predictable ordering.
  const specializationOptions = useMemo(() => {
    const seen = new Map<string, string>();
    for (const consultant of consultants) {
      for (const tag of consultant.expertiseTags) {
        const key = tag.trim().toLowerCase();
        if (!key) continue;
        if (!seen.has(key)) seen.set(key, tag.trim());
      }
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

    // Stable sort: by the chosen criterion first, then alphabetically by name
    // so two rows that tie on the sort key always render in a deterministic order.
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
  const unavailableCount = totalConsultants - availableCount;

  const handleSearchSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setAppliedSearch(searchForm);
  };

  const handleResetFilters = () => {
    setSearchForm(defaultSearchForm);
    setAppliedSearch(defaultSearchForm);
    setQuickFilter("all");
  };

  return (
    <main className="min-h-screen bg-bg-subtle">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
            {t("browse.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-text-secondary">{t("browse.subtitle")}</p>
        </div>

        <form
          onSubmit={handleSearchSubmit}
          className="mt-8 rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-sm"
        >
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-12">
            <div className="xl:col-span-6">
              <label
                htmlFor="consultant-search"
                className="mb-2 block text-xs font-medium text-text-secondary"
              >
                {t("search.label")}
              </label>
              <input
                id="consultant-search"
                type="text"
                value={searchForm.query}
                onChange={(event) =>
                  setSearchForm((current) => ({ ...current, query: event.target.value }))
                }
                placeholder={t("search.placeholder")}
                className="h-12 w-full rounded-xl border border-border-default bg-bg-elevated px-4 text-sm text-text-primary transition outline-none placeholder:text-text-tertiary focus:border-brand-300 focus:ring-2 focus:ring-brand-100"
              />
            </div>

            <div className="xl:col-span-2">
              <label htmlFor="price-filter" className="mb-2 block text-xs font-medium text-text-secondary">
                {t("filters.price")}
              </label>
              <select
                id="price-filter"
                value={searchForm.price}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    price: event.target.value as PriceSelectFilter,
                  }))
                }
                className="h-12 w-full rounded-xl border border-border-default bg-bg-elevated px-4 text-sm text-text-primary transition outline-none focus:border-brand-300 focus:ring-2 focus:ring-brand-100"
              >
                <option value="any">{t("priceOptions.any")}</option>
                <option value="under30">{t("priceOptions.under30")}</option>
                <option value="30to35">{t("priceOptions.30to35")}</option>
                <option value="above35">{t("priceOptions.above35")}</option>
              </select>
            </div>

            <div className="xl:col-span-2">
              <label
                htmlFor="rating-filter"
                className="mb-2 block text-xs font-medium text-text-secondary"
              >
                {t("filters.rating")}
              </label>
              <select
                id="rating-filter"
                value={searchForm.rating}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    rating: event.target.value as RatingSelectFilter,
                  }))
                }
                className="h-12 w-full rounded-xl border border-border-default bg-bg-elevated px-4 text-sm text-text-primary transition outline-none focus:border-brand-300 focus:ring-2 focus:ring-brand-100"
              >
                <option value="any">{t("ratingOptions.any")}</option>
                <option value="4plus">{t("ratingOptions.4plus")}</option>
                <option value="4_5plus">{t("ratingOptions.4_5plus")}</option>
                <option value="4_8plus">{t("ratingOptions.4_8plus")}</option>
              </select>
            </div>

            <div className="xl:col-span-2">
              <label
                htmlFor="availability-filter"
                className="mb-2 block text-xs font-medium text-text-secondary"
              >
                {t("filters.availability")}
              </label>
              <select
                id="availability-filter"
                value={searchForm.availability}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    availability: event.target.value as AvailabilitySelectFilter,
                  }))
                }
                className="h-12 w-full rounded-xl border border-border-default bg-bg-elevated px-4 text-sm text-text-primary transition outline-none focus:border-brand-300 focus:ring-2 focus:ring-brand-100"
              >
                <option value="all">{t("availabilityOptions.all")}</option>
                <option value="available">{t("availabilityOptions.available")}</option>
                <option value="unavailable">{t("availabilityOptions.unavailable")}</option>
              </select>
            </div>

            <div className="xl:col-span-6">
              <label
                htmlFor="specialization-filter"
                className="mb-2 block text-xs font-medium text-text-secondary"
              >
                {t("filters.specialization")}
              </label>
              <select
                id="specialization-filter"
                value={searchForm.specialization}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    specialization: event.target.value,
                  }))
                }
                disabled={specializationOptions.length === 0}
                className="h-12 w-full rounded-xl border border-border-default bg-bg-elevated px-4 text-sm text-text-primary transition outline-none focus:border-brand-300 focus:ring-2 focus:ring-brand-100 disabled:opacity-60 disabled:cursor-not-allowed"
              >
                <option value="any">{t("specializationOptions.any")}</option>
                {specializationOptions.map((tag) => (
                  <option key={tag} value={tag}>
                    {tag}
                  </option>
                ))}
              </select>
            </div>

            <div className="xl:col-span-6">
              <label
                htmlFor="sort-filter"
                className="mb-2 block text-xs font-medium text-text-secondary"
              >
                {t("filters.sortBy")}
              </label>
              <select
                id="sort-filter"
                value={searchForm.sort}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    sort: event.target.value as SortOption,
                  }))
                }
                className="h-12 w-full rounded-xl border border-border-default bg-bg-elevated px-4 text-sm text-text-primary transition outline-none focus:border-brand-300 focus:ring-2 focus:ring-brand-100"
              >
                <option value="ratingDesc">{t("sortOptions.ratingDesc")}</option>
                <option value="experienceDesc">{t("sortOptions.experienceDesc")}</option>
                <option value="feeAsc">{t("sortOptions.feeAsc")}</option>
                <option value="feeDesc">{t("sortOptions.feeDesc")}</option>
              </select>
            </div>
          </div>

          <div className="mt-5 flex flex-wrap gap-3">
            <button
              type="submit"
              className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
            >
              {t("search.submit")}
            </button>

            <button
              type="button"
              onClick={handleResetFilters}
              className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-brand-500 bg-transparent px-5 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
            >
              {t("search.reset")}
            </button>
          </div>
        </form>

        <div className="mt-6 rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap gap-3">
              {(["all", "available", "unavailable"] as const).map((key) => (
                <button
                  key={key}
                  type="button"
                  onClick={() => setQuickFilter(key)}
                  className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                    quickFilter === key
                      ? "bg-brand-500 text-white"
                      : "bg-bg-subtle text-text-secondary hover:bg-border-subtle"
                  }`}
                >
                  {t(`quickFilters.${key}`)}
                </button>
              ))}
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div className="rounded-xl bg-bg-muted px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("stats.total")}
                </p>
                <p className="mt-1 text-lg font-semibold text-text-primary">{totalConsultants}</p>
              </div>

              <div className="rounded-xl bg-bg-muted px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("stats.available")}
                </p>
                <p className="mt-1 text-lg font-semibold text-text-primary">{availableCount}</p>
              </div>

              <div className="rounded-xl bg-bg-muted px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("stats.unavailable")}
                </p>
                <p className="mt-1 text-lg font-semibold text-text-primary">{unavailableCount}</p>
              </div>
            </div>
          </div>
        </div>

        <div className="mt-8 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold tracking-[-0.01em] text-text-primary">
              {t("results.heading")}
            </h2>
            <p className="mt-2 text-sm leading-6 text-text-secondary">{t("results.subtitle")}</p>
          </div>

          {!isLoading && !isError ? (
            <p className="shrink-0 text-sm font-medium text-brand-500">
              {t("results.count", { count: filteredConsultants.length })}
            </p>
          ) : null}
        </div>

        {isError ? (
          <div className="mt-6 rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm font-medium text-danger-500">
            {t("states.error")}
          </div>
        ) : isLoading ? (
          <div className="mt-6 grid gap-6 lg:grid-cols-2 xl:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="flex flex-col gap-4 rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
                <div className="flex items-center gap-4">
                  <div className="skeleton size-14 rounded-full" />
                  <div className="flex-1 space-y-2">
                    <div className="skeleton h-4 w-2/3" />
                    <div className="skeleton h-3 w-1/2" />
                  </div>
                </div>
                <div className="skeleton h-3 w-full" />
                <div className="skeleton h-3 w-4/5" />
                <div className="mt-2 flex gap-2">
                  <div className="skeleton h-6 w-20 rounded-full" />
                  <div className="skeleton h-6 w-16 rounded-full" />
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="mt-6 grid gap-6 lg:grid-cols-2 xl:grid-cols-3">
            {filteredConsultants.length > 0 ? (
              filteredConsultants.map((consultant) => (
                <article
                  key={consultant.id}
                  className="flex h-full flex-col rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex min-w-0 items-start gap-3">
                      <UserAvatar
                        userId={consultant.id}
                        name={consultant.name}
                        className="size-12 shrink-0"
                        initialsClassName="text-lg"
                      />
                      <div className="min-w-0">
                        <h3 className="text-2xl font-semibold tracking-[-0.01em] text-text-primary">
                          {consultant.name}
                        </h3>
                        {consultant.biography ? (
                          <p className="mt-2 text-sm leading-6 text-text-secondary">
                            {consultant.biography}
                          </p>
                        ) : null}
                      </div>
                    </div>

                    <span
                      className={`shrink-0 rounded-full px-3 py-1 text-xs font-medium ${
                        consultant.hasAvailability
                          ? "bg-success-50 text-success-600"
                          : "bg-bg-subtle text-text-secondary"
                      }`}
                    >
                      {t(
                        consultant.hasAvailability
                          ? "badge.available"
                          : "badge.noAvailability",
                      )}
                    </span>
                  </div>

                  <div className="mt-4 flex flex-wrap gap-2">
                    {consultant.expertiseTags.length > 0 ? (
                      consultant.expertiseTags.map((tag) => (
                        <span
                          key={tag}
                          className="rounded-full bg-brand-50 px-3 py-1 text-xs font-medium text-brand-700"
                        >
                          {tag}
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-text-tertiary">{t("card.noExpertiseTags")}</span>
                    )}
                  </div>

                  <div className="mt-6 grid gap-4 rounded-xl bg-bg-muted p-4 sm:grid-cols-2">
                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                        {t("card.rating")}
                      </p>
                      {consultant.averageRating != null ? (
                        <div className="mt-1 flex items-center gap-2">
                          <StarRating
                            value={consultant.averageRating}
                            ariaLabel={t("card.ratingValue", {
                              rating: consultant.averageRating.toFixed(1),
                              count: consultant.reviewCount,
                            })}
                          />
                          <span className="text-sm font-medium text-text-primary">
                            {t("card.ratingValue", {
                              rating: consultant.averageRating.toFixed(1),
                              count: consultant.reviewCount,
                            })}
                          </span>
                        </div>
                      ) : (
                        <div className="mt-1 flex items-center gap-2">
                          <StarRating value={null} />
                          <span className="text-sm font-medium text-text-tertiary">
                            {t("card.noRating")}
                          </span>
                        </div>
                      )}
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                        {t("card.sessions")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-text-primary">
                        {consultant.completedSessionCount}
                      </p>
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                        {t("card.fee")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-text-primary">
                        {consultant.sessionFeeUsd != null
                          ? formatUsd(consultant.sessionFeeUsd)
                          : t("card.feeUnset")}
                      </p>
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                        {t("card.baseDuration")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-text-primary">
                        {consultant.sessionDurationMinutes != null
                          ? durationLabel(consultant.sessionDurationMinutes, t)
                          : t("card.durationUnset")}
                      </p>
                    </div>
                  </div>

                  <div className="mt-6 flex flex-1 flex-col justify-end gap-3">
                    <Link
                      to={`/student/consultants/${consultant.id}`}
                      className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                    >
                      {t("card.viewConsultant")}
                    </Link>
                  </div>
                </article>
              ))
            ) : (
              <div className="lg:col-span-2 xl:col-span-3">
                <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-8 shadow-sm">
                  <h3 className="text-2xl font-semibold tracking-[-0.01em] text-text-primary">
                    {consultants.length === 0 ? t("empty.noConsultants") : t("empty.title")}
                  </h3>
                  <p className="mt-3 max-w-2xl text-sm leading-7 text-text-secondary">
                    {consultants.length === 0 ? t("empty.noConsultantsBody") : t("empty.body")}
                  </p>

                  {consultants.length > 0 ? (
                    <div className="mt-6">
                      <button
                        type="button"
                        onClick={handleResetFilters}
                        className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                      >
                        {t("empty.reset")}
                      </button>
                    </div>
                  ) : null}
                </div>
              </div>
            )}
          </div>
        )}
      </section>
    </main>
  );
}
