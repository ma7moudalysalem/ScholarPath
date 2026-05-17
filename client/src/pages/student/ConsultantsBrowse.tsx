import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useConsultantsQuery } from "@/hooks/useConsultantsQuery";
import type { ConsultantSummary } from "@/services/api/consultants";
import { durationLabel, formatUsd } from "@/lib/bookingFormat";
import { UserAvatar } from "@/components/common/UserAvatar";

type BrowseFilter = "all" | "available" | "unavailable";

type PriceSelectFilter = "any" | "under30" | "30to35" | "above35";
type RatingSelectFilter = "any" | "4plus" | "4_5plus" | "4_8plus";
type AvailabilitySelectFilter = "all" | "available" | "unavailable";

type SearchFormState = {
  query: string;
  price: PriceSelectFilter;
  rating: RatingSelectFilter;
  availability: AvailabilitySelectFilter;
};

const defaultSearchForm: SearchFormState = {
  query: "",
  price: "any",
  rating: "any",
  availability: "all",
};

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

export function ConsultantsBrowse() {
  const { t } = useTranslation("consultants");
  const { data, isLoading, isError } = useConsultantsQuery();

  const [quickFilter, setQuickFilter] = useState<BrowseFilter>("all");
  const [searchForm, setSearchForm] = useState<SearchFormState>(defaultSearchForm);
  const [appliedSearch, setAppliedSearch] = useState<SearchFormState>(defaultSearchForm);

  const consultants = useMemo<ConsultantSummary[]>(() => data ?? [], [data]);

  const searchedConsultants = useMemo(() => {
    const normalizedQuery = appliedSearch.query.trim().toLowerCase();

    return consultants.filter((consultant) => {
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

      return queryMatch && priceMatch && ratingMatch && availabilityMatch;
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
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            {t("browse.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("browse.subtitle")}</p>
        </div>

        <form
          onSubmit={handleSearchSubmit}
          className="mt-8 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm"
        >
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-12">
            <div className="xl:col-span-6">
              <label
                htmlFor="consultant-search"
                className="mb-2 block text-xs font-medium text-[#6b7280]"
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
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
              />
            </div>

            <div className="xl:col-span-2">
              <label htmlFor="price-filter" className="mb-2 block text-xs font-medium text-[#6b7280]">
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
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
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
                className="mb-2 block text-xs font-medium text-[#6b7280]"
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
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
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
                className="mb-2 block text-xs font-medium text-[#6b7280]"
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
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
              >
                <option value="all">{t("availabilityOptions.all")}</option>
                <option value="available">{t("availabilityOptions.available")}</option>
                <option value="unavailable">{t("availabilityOptions.unavailable")}</option>
              </select>
            </div>
          </div>

          <div className="mt-5 flex flex-wrap gap-3">
            <button
              type="submit"
              className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
            >
              {t("search.submit")}
            </button>

            <button
              type="button"
              onClick={handleResetFilters}
              className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
            >
              {t("search.reset")}
            </button>
          </div>
        </form>

        <div className="mt-6 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap gap-3">
              {(["all", "available", "unavailable"] as const).map((key) => (
                <button
                  key={key}
                  type="button"
                  onClick={() => setQuickFilter(key)}
                  className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                    quickFilter === key
                      ? "bg-[#2563eb] text-white"
                      : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                  }`}
                >
                  {t(`quickFilters.${key}`)}
                </button>
              ))}
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div className="rounded-xl bg-[#f9fafb] px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.total")}
                </p>
                <p className="mt-1 text-lg font-semibold text-[#1d1d1f]">{totalConsultants}</p>
              </div>

              <div className="rounded-xl bg-[#f9fafb] px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.available")}
                </p>
                <p className="mt-1 text-lg font-semibold text-[#1d1d1f]">{availableCount}</p>
              </div>

              <div className="rounded-xl bg-[#f9fafb] px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.unavailable")}
                </p>
                <p className="mt-1 text-lg font-semibold text-[#1d1d1f]">{unavailableCount}</p>
              </div>
            </div>
          </div>
        </div>

        <div className="mt-8 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              {t("results.heading")}
            </h2>
            <p className="mt-2 text-sm leading-6 text-[#4b5563]">{t("results.subtitle")}</p>
          </div>

          {!isLoading && !isError ? (
            <p className="shrink-0 text-sm font-medium text-[#2563eb]">
              {t("results.count", { count: filteredConsultants.length })}
            </p>
          ) : null}
        </div>

        {isError ? (
          <div className="mt-6 rounded-2xl border border-[#fecaca] bg-[#fef2f2] p-6 text-sm font-medium text-[#dc2626]">
            {t("states.error")}
          </div>
        ) : isLoading ? (
          <div className="mt-6 grid gap-6 lg:grid-cols-2 xl:grid-cols-3">
            {Array.from({ length: 6 }).map((_, index) => (
              <div
                key={index}
                className="h-80 animate-pulse rounded-2xl border border-[#e5e7eb] bg-white shadow-sm"
              />
            ))}
          </div>
        ) : (
          <div className="mt-6 grid gap-6 lg:grid-cols-2 xl:grid-cols-3">
            {filteredConsultants.length > 0 ? (
              filteredConsultants.map((consultant) => (
                <article
                  key={consultant.id}
                  className="flex h-full flex-col rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm"
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
                        <h3 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                          {consultant.name}
                        </h3>
                        {consultant.biography ? (
                          <p className="mt-2 text-sm leading-6 text-[#4b5563]">
                            {consultant.biography}
                          </p>
                        ) : null}
                      </div>
                    </div>

                    <span
                      className={`shrink-0 rounded-full px-3 py-1 text-xs font-medium ${
                        consultant.hasAvailability
                          ? "bg-[#f0fdf4] text-[#15803d]"
                          : "bg-[#f3f4f6] text-[#4b5563]"
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
                          className="rounded-full bg-[#eef2ff] px-3 py-1 text-xs font-medium text-[#4338ca]"
                        >
                          {tag}
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-[#9ca3af]">{t("card.noExpertiseTags")}</span>
                    )}
                  </div>

                  <div className="mt-6 grid gap-4 rounded-xl bg-[#f9fafb] p-4 sm:grid-cols-2">
                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        {t("card.rating")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                        {consultant.averageRating != null
                          ? t("card.ratingValue", {
                              rating: consultant.averageRating.toFixed(1),
                              count: consultant.reviewCount,
                            })
                          : t("card.noRating")}
                      </p>
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        {t("card.sessions")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                        {consultant.completedSessionCount}
                      </p>
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        {t("card.fee")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                        {consultant.sessionFeeUsd != null
                          ? formatUsd(consultant.sessionFeeUsd)
                          : t("card.feeUnset")}
                      </p>
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        {t("card.baseDuration")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                        {consultant.sessionDurationMinutes != null
                          ? durationLabel(consultant.sessionDurationMinutes, t)
                          : t("card.durationUnset")}
                      </p>
                    </div>
                  </div>

                  <div className="mt-6 flex flex-1 flex-col justify-end gap-3">
                    <Link
                      to={`/student/consultants/${consultant.id}`}
                      className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                    >
                      {t("card.viewConsultant")}
                    </Link>
                  </div>
                </article>
              ))
            ) : (
              <div className="lg:col-span-2 xl:col-span-3">
                <div className="rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
                  <h3 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                    {consultants.length === 0 ? t("empty.noConsultants") : t("empty.title")}
                  </h3>
                  <p className="mt-3 max-w-2xl text-sm leading-7 text-[#4b5563]">
                    {consultants.length === 0 ? t("empty.noConsultantsBody") : t("empty.body")}
                  </p>

                  {consultants.length > 0 ? (
                    <div className="mt-6">
                      <button
                        type="button"
                        onClick={handleResetFilters}
                        className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
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
