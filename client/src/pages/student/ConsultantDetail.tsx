import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";
import {
  useConsultantAvailabilityQuery,
  useConsultantDetailQuery,
} from "@/hooks/useConsultantsQuery";
import type { BookableSlot } from "@/services/api/consultants";
import { durationLabel, formatDate, formatTime, formatUsd } from "@/lib/bookingFormat";

/** Builds the `/student/checkout` link for a concrete bookable slot. */
function buildCheckoutLink(consultantId: string, slot: BookableSlot) {
  const params = new URLSearchParams({
    consultantId,
    availabilityId: slot.availabilityId,
    start: slot.startAt,
    end: slot.endAt,
  });
  return `/student/checkout?${params.toString()}`;
}

export function ConsultantDetail() {
  const { t, i18n } = useTranslation("consultants");
  const lang = i18n.language;
  const { id } = useParams();

  const {
    data: consultant,
    isLoading: isConsultantLoading,
    isError: isConsultantError,
  } = useConsultantDetailQuery(id);

  const {
    data: slotsData,
    isLoading: isSlotsLoading,
    isError: isSlotsError,
  } = useConsultantAvailabilityQuery(id);

  const slots = useMemo<BookableSlot[]>(() => slotsData ?? [], [slotsData]);
  const primarySlot = slots[0];

  if (isConsultantLoading) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-4">
            <div className="h-10 w-64 animate-pulse rounded-lg bg-white" />
            <div className="h-72 animate-pulse rounded-2xl border border-[#e5e7eb] bg-white shadow-sm" />
          </div>
        </section>
      </main>
    );
  }

  if (isConsultantError || !consultant) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-[#fecaca] bg-[#fef2f2] p-6 text-sm font-medium text-[#dc2626]">
            {t("states.error")}
          </div>
          <div className="mt-6">
            <Link
              to="/student/consultants"
              className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
            >
              {t("detail.backToConsultants")}
            </Link>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            {t("detail.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("detail.subtitle")}</p>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8">
            <div className="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
              <div>
                <p className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                  {consultant.name}
                </p>
                {consultant.countryOfResidence ? (
                  <p className="mt-1 text-sm text-[#4b5563]">{consultant.countryOfResidence}</p>
                ) : null}

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
              </div>

              <span
                className={`rounded-full px-3 py-1 text-xs font-medium ${
                  consultant.hasAvailability
                    ? "bg-[#f0fdf4] text-[#15803d]"
                    : "bg-[#f3f4f6] text-[#4b5563]"
                }`}
              >
                {t(consultant.hasAvailability ? "badge.available" : "badge.noAvailability")}
              </span>
            </div>

            {consultant.biography ? (
              <p className="mt-6 text-sm leading-7 text-[#4b5563]">{consultant.biography}</p>
            ) : null}

            <div className="mt-6 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-3">
              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("detail.rating")}
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
                  {t("detail.sessions")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {consultant.completedSessionCount}
                </p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("detail.fee")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {consultant.sessionFeeUsd != null
                    ? consultant.sessionDurationMinutes != null
                      ? t("detail.feeValue", {
                          fee: formatUsd(consultant.sessionFeeUsd),
                          duration: durationLabel(consultant.sessionDurationMinutes, t),
                        })
                      : formatUsd(consultant.sessionFeeUsd)
                    : t("card.feeUnset")}
                </p>
              </div>
            </div>

            {consultant.languages.length > 0 ? (
              <div className="mt-4 rounded-xl bg-[#f9fafb] p-5">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("detail.languages")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {consultant.languages.join(" · ")}
                </p>
              </div>
            ) : null}

            {consultant.recentReviews.length > 0 ? (
              <div className="mt-6">
                <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                  {t("detail.reviewsTitle")}
                </h2>
                <div className="mt-4 space-y-3">
                  {consultant.recentReviews.map((review) => (
                    <div
                      key={review.id}
                      className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4"
                    >
                      <div className="flex items-center justify-between gap-3">
                        <p className="text-sm font-medium text-[#1d1d1f]">{review.studentName}</p>
                        <span className="rounded-full bg-[#fffbeb] px-3 py-1 text-xs font-medium text-[#b45309]">
                          {t("detail.reviewRating", { rating: review.rating })}
                        </span>
                      </div>
                      {review.comment ? (
                        <p className="mt-2 text-sm leading-6 text-[#4b5563]">{review.comment}</p>
                      ) : null}
                      <p className="mt-2 text-xs text-[#9ca3af]">
                        {formatDate(review.createdAt, lang)}
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            ) : null}
          </section>

          <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
            <p className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              {t("detail.bookingOverview")}
            </p>

            {isSlotsLoading ? (
              <div className="mt-5 h-40 animate-pulse rounded-xl bg-[#f9fafb]" />
            ) : isSlotsError ? (
              <div className="mt-5 rounded-xl border border-[#fecaca] bg-[#fef2f2] p-4 text-sm font-medium text-[#dc2626]">
                {t("states.slotsError")}
              </div>
            ) : primarySlot ? (
              <>
                <div className="mt-5 space-y-4">
                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("detail.nextSlot")}
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">
                      {formatDate(primarySlot.startAt, lang)} · {formatTime(primarySlot.startAt, lang)}
                    </p>
                  </div>

                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("detail.sessionFormat")}
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">{t("detail.sessionFormatValue")}</p>
                  </div>

                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("detail.duration")}
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">
                      {durationLabel(primarySlot.durationMinutes, t)}
                    </p>
                  </div>
                </div>

                <div className="mt-6 flex flex-col gap-3">
                  <Link
                    to={buildCheckoutLink(consultant.id, primarySlot)}
                    className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                  >
                    {t("detail.bookConsultant")}
                  </Link>

                  <Link
                    to="/student/consultants"
                    className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                  >
                    {t("detail.backToConsultants")}
                  </Link>
                </div>
              </>
            ) : (
              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
                <p className="text-sm leading-6 text-[#4b5563]">{t("detail.noSlotsBooking")}</p>
              </div>
            )}
          </aside>
        </div>

        <div className="mt-8 rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
            <div>
              <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("detail.upcomingAvailability")}
              </h2>
              <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                {t("detail.upcomingLive")}
              </p>
            </div>

            {!isSlotsLoading && !isSlotsError ? (
              <span className="inline-flex rounded-full bg-[#eff6ff] px-3 py-1 text-xs font-medium text-[#1d4ed8]">
                {t("detail.openSlots", { count: slots.length })}
              </span>
            ) : null}
          </div>

          {isSlotsLoading ? (
            <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {Array.from({ length: 3 }).map((_, index) => (
                <div key={index} className="h-44 animate-pulse rounded-xl bg-[#f9fafb]" />
              ))}
            </div>
          ) : isSlotsError ? (
            <div className="mt-6 rounded-xl border border-[#fecaca] bg-[#fef2f2] p-5 text-sm font-medium text-[#dc2626]">
              {t("states.slotsError")}
            </div>
          ) : slots.length > 0 ? (
            <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {slots.map((slot) => (
                <article
                  key={`${slot.availabilityId}-${slot.startAt}`}
                  className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                        {formatDate(slot.startAt, lang)}
                      </p>
                      <p className="mt-1 text-sm leading-6 text-[#4b5563]">
                        {formatTime(slot.startAt, lang)} – {formatTime(slot.endAt, lang)}
                      </p>
                    </div>

                    <span className="rounded-full bg-[#f0fdf4] px-3 py-1 text-xs font-medium text-[#15803d]">
                      {t("slotTag.available")}
                    </span>
                  </div>

                  <div className="mt-5">
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("detail.slotDuration")}
                    </p>
                    <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                      {durationLabel(slot.durationMinutes, t)}
                    </p>
                  </div>

                  <Link
                    to={buildCheckoutLink(consultant.id, slot)}
                    className="mt-5 inline-flex h-11 w-full items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-4 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                  >
                    {t("detail.selectSlot")}
                  </Link>
                </article>
              ))}
            </div>
          ) : (
            <div className="mt-6 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
              <p className="text-sm leading-6 text-[#4b5563]">{t("detail.noUpcomingSlots")}</p>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
