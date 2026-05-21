import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";
import {
  ArrowLeft,
  ArrowRight,
  Star,
  Calendar,
  Clock,
  Globe,
  MessageSquare,
  Award,
  Languages,
} from "lucide-react";
import {
  useConsultantAvailabilityQuery,
  useConsultantDetailQuery,
} from "@/hooks/useConsultantsQuery";
import type { BookableSlot } from "@/services/api/consultants";
import { durationLabel, formatDate, formatTime, formatUsd } from "@/lib/bookingFormat";
import { expertiseTagLabelByLang } from "@/lib/expertiseTagLabel";
import { UserAvatar } from "@/components/common/UserAvatar";
import { cn } from "@/lib/utils";

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

/** Small inline star + rating for the hero block. */
function StarRating({ value, size = 14 }: { value: number | null; size?: number }) {
  const rounded = value == null ? 0 : Math.round(value * 2) / 2;
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: 5 }, (_, i) => {
        const pos = i + 1;
        const isFull = rounded >= pos;
        const isHalf = !isFull && rounded >= pos - 0.5;
        return (
          <span key={i} className="relative inline-flex" style={{ width: size, height: size }}>
            <Star size={size} className="text-text-tertiary/40" strokeWidth={1.5} />
            {(isFull || isHalf) && (
              <span
                className="absolute inset-0 overflow-hidden"
                style={{ width: isHalf ? "50%" : "100%" }}
              >
                <Star size={size} className="fill-amber-400 text-amber-400" strokeWidth={1.5} />
              </span>
            )}
          </span>
        );
      })}
    </div>
  );
}

function StatChip({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean }>;
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-center gap-2.5 rounded-xl border border-border-subtle bg-bg-muted px-3 py-2.5">
      <Icon aria-hidden className="size-4 shrink-0 text-text-tertiary" />
      <div className="min-w-0">
        <p className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
          {label}
        </p>
        <p className="truncate text-sm font-semibold text-text-primary">{value}</p>
      </div>
    </div>
  );
}

export function ConsultantDetail() {
  const { t, i18n } = useTranslation("consultants");
  const lang = i18n.language;
  const isRtl = i18n.dir() === "rtl";
  const BackIcon = isRtl ? ArrowRight : ArrowLeft;
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
      <div className="mx-auto max-w-6xl space-y-4">
        <div className="h-10 w-64 animate-pulse rounded-lg bg-bg-elevated" />
        <div className="h-72 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm" />
      </div>
    );
  }

  if (isConsultantError || !consultant) {
    return (
      <div className="mx-auto max-w-3xl space-y-6">
        <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm font-medium text-danger-500">
          {t("states.error")}
        </div>
        <Link to="/student/consultants" className="btn btn-primary">
          {t("detail.backToConsultants")}
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">

      {/* ── Back link ── */}
      <Link
        to="/student/consultants"
        className="inline-flex items-center gap-1.5 text-sm font-medium text-text-secondary transition hover:text-brand-600"
      >
        <BackIcon aria-hidden className="size-4" />
        {t("detail.backToConsultants")}
      </Link>

      {/* ── Hero ── */}
      <div className="overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated shadow-elevation-1">
        {/* Banner gradient */}
        <div className="relative h-28 bg-gradient-to-br from-brand-600 via-brand-500 to-brand-700 sm:h-36">
          <div
            aria-hidden
            className="pointer-events-none absolute inset-0 opacity-50"
            style={{
              backgroundImage:
                "radial-gradient(at 20% 30%, rgba(255,255,255,0.15) 0px, transparent 50%), radial-gradient(at 80% 70%, rgba(255,255,255,0.08) 0px, transparent 55%)",
            }}
          />
          <span
            className={cn(
              "absolute end-4 top-4 rounded-full px-3 py-1 text-xs font-semibold backdrop-blur-md sm:end-6 sm:top-6",
              consultant.hasAvailability
                ? "bg-success-500/95 text-white"
                : "bg-bg-elevated/95 text-text-secondary",
            )}
          >
            {consultant.hasAvailability ? (
              <span className="inline-flex items-center gap-1.5">
                <span aria-hidden className="size-1.5 rounded-full bg-white" />
                {t("badge.available")}
              </span>
            ) : (
              t("badge.noAvailability")
            )}
          </span>
        </div>

        {/* Profile block — avatar overlaps the banner, name + meta sit BELOW
            the avatar so the layout stays predictable on every viewport
            (the previous sm:items-end pinned the name to the avatar's
            baseline, which left awkward whitespace under longer names). */}
        <div className="px-5 pb-6 sm:px-6">
          <div className="relative -mt-12 flex flex-col items-start gap-4 sm:-mt-14 sm:flex-row sm:items-start">
            <div className="relative shrink-0">
              <UserAvatar
                userId={consultant.id}
                name={consultant.name}
                className="size-24 ring-4 ring-bg-elevated sm:size-28"
                initialsClassName="text-3xl"
              />
              {consultant.hasAvailability && (
                <span
                  aria-label={t("badge.available")}
                  className="absolute bottom-1 end-1 size-4 rounded-full bg-success-500 ring-2 ring-bg-elevated"
                />
              )}
            </div>

            <div className="min-w-0 flex-1 pt-2 sm:pt-14">
              <h1 className="break-words text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
                {consultant.name}
              </h1>
              <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-sm text-text-secondary">
                {consultant.countryOfResidence && (
                  <span className="inline-flex items-center gap-1.5">
                    <Globe aria-hidden className="size-3.5 text-text-tertiary" />
                    {consultant.countryOfResidence}
                  </span>
                )}
                {consultant.averageRating != null ? (
                  <span className="inline-flex items-center gap-1.5">
                    <StarRating value={consultant.averageRating} size={14} />
                    <span className="font-semibold text-text-primary">
                      {consultant.averageRating.toFixed(1)}
                    </span>
                    <span className="text-text-tertiary">
                      ({consultant.reviewCount})
                    </span>
                  </span>
                ) : (
                  <span className="inline-flex items-center gap-1.5 text-text-tertiary">
                    <Star aria-hidden className="size-3.5" />
                    {t("card.noRating")}
                  </span>
                )}
                <span className="inline-flex items-center gap-1.5">
                  <Award aria-hidden className="size-3.5 text-text-tertiary" />
                  {t("card.sessionsShort", { count: consultant.completedSessionCount })}
                </span>
              </div>
            </div>
          </div>

          {/* Expertise tags */}
          <div className="mt-5 flex flex-wrap gap-1.5">
            {consultant.expertiseTags.length > 0 ? (
              consultant.expertiseTags.map((tag) => (
                <span key={tag} className="badge badge-brand">{expertiseTagLabelByLang(tag, i18n.language)}</span>
              ))
            ) : (
              <span className="text-xs text-text-tertiary">{t("card.noExpertiseTags")}</span>
            )}
          </div>

          {/* Quick-stats chip row */}
          <div className="mt-5 grid gap-2 sm:grid-cols-3">
            <StatChip
              icon={Award}
              label={t("detail.sessions")}
              value={consultant.completedSessionCount}
            />
            <StatChip
              icon={Clock}
              label={t("detail.fee")}
              value={
                consultant.sessionFeeUsd != null
                  ? consultant.sessionDurationMinutes != null
                    ? `${formatUsd(consultant.sessionFeeUsd)} · ${durationLabel(consultant.sessionDurationMinutes, t)}`
                    : formatUsd(consultant.sessionFeeUsd)
                  : t("card.feeUnset")
              }
            />
            <StatChip
              icon={Languages}
              label={t("detail.languages")}
              value={
                consultant.languages.length > 0
                  ? consultant.languages.join(" · ")
                  : "—"
              }
            />
          </div>
        </div>
      </div>

      {/* ── Two-column body ── */}
      <div className="grid gap-6 lg:grid-cols-3">

        {/* Left: bio + reviews ── */}
        <div className="space-y-6 lg:col-span-2">

          {consultant.biography && (
            <section className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
              <h2 className="mb-3 text-base font-semibold text-text-primary">
                {t("detail.title")}
              </h2>
              <p className="whitespace-pre-line text-sm leading-relaxed text-text-secondary">
                {consultant.biography}
              </p>
            </section>
          )}

          {consultant.recentReviews.length > 0 && (
            <section className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
              <h2 className="mb-4 text-base font-semibold text-text-primary">
                {t("detail.reviewsTitle")}
              </h2>
              <div className="space-y-4">
                {consultant.recentReviews.map((review) => (
                  <div
                    key={review.id}
                    className="rounded-xl border border-border-subtle bg-bg-muted p-4"
                  >
                    <div className="flex items-center justify-between gap-3">
                      <p className="text-sm font-semibold text-text-primary">
                        {review.studentName}
                      </p>
                      <span className="inline-flex items-center gap-1 rounded-full bg-amber-50 px-2 py-0.5 text-xs font-semibold text-amber-700">
                        <Star size={12} className="fill-amber-500 text-amber-500" />
                        {review.rating.toFixed(1)}
                      </span>
                    </div>
                    {review.comment && (
                      <p className="mt-2 text-sm leading-relaxed text-text-secondary">
                        {review.comment}
                      </p>
                    )}
                    <p className="mt-2 text-xs text-text-tertiary">
                      {formatDate(review.createdAt, lang)}
                    </p>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Upcoming availability */}
          <section className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
            <div className="mb-4 flex items-end justify-between gap-3">
              <div>
                <h2 className="text-base font-semibold text-text-primary">
                  {t("detail.upcomingAvailability")}
                </h2>
                <p className="mt-1 text-xs text-text-secondary">
                  {t("detail.upcomingLive")}
                </p>
              </div>
              {!isSlotsLoading && !isSlotsError && (
                <span className="badge badge-brand">
                  {t("detail.openSlots", { count: slots.length })}
                </span>
              )}
            </div>

            {isSlotsLoading ? (
              <div className="grid gap-3 sm:grid-cols-2">
                {Array.from({ length: 4 }).map((_, i) => (
                  <div key={i} className="h-24 animate-pulse rounded-xl bg-bg-muted" />
                ))}
              </div>
            ) : isSlotsError ? (
              <div className="rounded-xl border border-danger-200 bg-danger-50 p-4 text-sm font-medium text-danger-500">
                {t("states.slotsError")}
              </div>
            ) : slots.length > 0 ? (
              <div className="grid gap-3 sm:grid-cols-2">
                {slots.map((slot) => (
                  <Link
                    key={`${slot.availabilityId}-${slot.startAt}`}
                    to={buildCheckoutLink(consultant.id, slot)}
                    className="group flex flex-col rounded-xl border border-border-subtle bg-bg-muted p-4 transition hover:border-brand-300 hover:bg-brand-50/40"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <p className="text-sm font-semibold text-text-primary">
                          {formatDate(slot.startAt, lang)}
                        </p>
                        <p className="mt-0.5 text-xs text-text-secondary">
                          {formatTime(slot.startAt, lang)} – {formatTime(slot.endAt, lang)}
                        </p>
                      </div>
                      <span className="badge badge-success text-[10.5px]">
                        {t("slotTag.available")}
                      </span>
                    </div>
                    <div className="mt-3 flex items-center justify-between text-xs">
                      <span className="text-text-tertiary">
                        {durationLabel(slot.durationMinutes, t)}
                      </span>
                      <span className="font-semibold text-brand-600 opacity-0 transition-opacity group-hover:opacity-100">
                        {t("detail.selectSlot")}
                        <ArrowRight aria-hidden className="ms-1 inline size-3 rtl:rotate-180" />
                      </span>
                    </div>
                  </Link>
                ))}
              </div>
            ) : (
              <div className="flex min-h-[160px] flex-col items-center justify-center rounded-xl border border-dashed border-border-subtle bg-bg-muted/50 p-6 text-center">
                <div className="mb-3 flex size-12 items-center justify-center rounded-2xl bg-bg-elevated text-text-tertiary">
                  <Calendar aria-hidden className="size-5" />
                </div>
                <p className="text-sm text-text-secondary">
                  {t("detail.noUpcomingSlots")}
                </p>
              </div>
            )}
          </section>
        </div>

        {/* Right: sticky booking CTA ── */}
        <aside className="lg:col-span-1">
          <div className="sticky top-20 space-y-4">

            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
                {t("detail.bookingOverview")}
              </p>

              {/* Price highlight */}
              {consultant.sessionFeeUsd != null && (
                <div className="mt-3 flex items-baseline gap-1.5">
                  <span className="text-3xl font-bold text-brand-600">
                    {formatUsd(consultant.sessionFeeUsd)}
                  </span>
                  {consultant.sessionDurationMinutes != null && (
                    <span className="text-xs text-text-tertiary">
                      / {durationLabel(consultant.sessionDurationMinutes, t)}
                    </span>
                  )}
                </div>
              )}

              {isSlotsLoading ? (
                <div className="mt-4 h-20 animate-pulse rounded-xl bg-bg-muted" />
              ) : isSlotsError ? (
                <div className="mt-4 rounded-xl border border-danger-200 bg-danger-50 p-3 text-xs font-medium text-danger-500">
                  {t("states.slotsError")}
                </div>
              ) : primarySlot ? (
                <div className="mt-4 space-y-3 rounded-xl bg-bg-muted p-4">
                  <div className="flex items-start gap-2">
                    <Calendar aria-hidden className="mt-0.5 size-4 shrink-0 text-text-tertiary" />
                    <div>
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
                        {t("detail.nextSlot")}
                      </p>
                      <p className="mt-0.5 text-sm font-semibold text-text-primary">
                        {formatDate(primarySlot.startAt, lang)}
                      </p>
                      <p className="text-xs text-text-secondary">
                        {formatTime(primarySlot.startAt, lang)}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start gap-2">
                    <MessageSquare aria-hidden className="mt-0.5 size-4 shrink-0 text-text-tertiary" />
                    <div>
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
                        {t("detail.sessionFormat")}
                      </p>
                      <p className="mt-0.5 text-sm font-semibold text-text-primary">
                        {t("detail.sessionFormatValue")}
                      </p>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="mt-4 rounded-xl border border-dashed border-border-subtle bg-bg-muted/60 p-4 text-xs text-text-secondary">
                  {t("detail.noSlotsBooking")}
                </div>
              )}

              <div className="mt-4 flex flex-col gap-2">
                {primarySlot ? (
                  <Link
                    to={buildCheckoutLink(consultant.id, primarySlot)}
                    className="btn btn-primary w-full"
                  >
                    {t("detail.bookConsultant")}
                    <ArrowRight aria-hidden className="size-4 rtl:rotate-180" />
                  </Link>
                ) : (
                  <button
                    type="button"
                    disabled
                    className="btn btn-primary w-full"
                  >
                    {t("detail.bookConsultant")}
                  </button>
                )}
                <Link to="/student/consultants" className="btn btn-secondary w-full">
                  {t("detail.backToConsultants")}
                </Link>
              </div>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
