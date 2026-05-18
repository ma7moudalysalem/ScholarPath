import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useConsultantBookingsQuery } from "@/hooks/useBookingsQuery";
import type { BookingListItem } from "@/services/api/bookings";
import { UserAvatar } from "@/components/common/UserAvatar";
import {
  durationLabel,
  formatDateTime,
  formatUsd,
  statusBadgeClass,
  statusBucket,
  statusLabelKey,
  type BookingStatusBucket,
} from "@/lib/bookingFormat";

type ConsultantFilter = "all" | BookingStatusBucket;

export function ConsultantBookings() {
  const { t, i18n } = useTranslation("consultantPortal");
  const lang = i18n.language;
  const { data, isLoading, isError } = useConsultantBookingsQuery();
  const [filter, setFilter] = useState<ConsultantFilter>("all");

  const bookings = useMemo<BookingListItem[]>(() => data ?? [], [data]);

  const summary = useMemo(() => {
    return {
      total: bookings.length,
      pending: bookings.filter((item) => statusBucket(item.status) === "pending").length,
      confirmed: bookings.filter((item) => statusBucket(item.status) === "confirmed").length,
      completed: bookings.filter((item) => statusBucket(item.status) === "completed").length,
      closed: bookings.filter((item) => statusBucket(item.status) === "closed").length,
    };
  }, [bookings]);

  const filteredBookings = useMemo(() => {
    if (filter === "all") return bookings;
    return bookings.filter((item) => statusBucket(item.status) === filter);
  }, [bookings, filter]);

  return (
    <main className="min-h-screen bg-bg-subtle">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
            {t("bookings.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-text-secondary">{t("bookings.subtitle")}</p>
        </div>

        {isError ? (
          <div className="mt-8 rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm font-medium text-danger-500">
            {t("states.error")}
          </div>
        ) : isLoading ? (
          <div className="mt-8 space-y-6">
            {Array.from({ length: 3 }).map((_, index) => (
              <div
                key={index}
                className="h-48 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm"
              />
            ))}
          </div>
        ) : (
          <>
            <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
              <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("bookings.summary.total")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-text-primary">{summary.total}</p>
              </div>

              <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("bookings.summary.pending")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-text-primary">{summary.pending}</p>
              </div>

              <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("bookings.summary.confirmed")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-text-primary">{summary.confirmed}</p>
              </div>

              <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("bookings.summary.completed")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-text-primary">{summary.completed}</p>
              </div>

              <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                  {t("bookings.summary.closed")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-text-primary">{summary.closed}</p>
              </div>
            </div>

            <div className="mt-8 rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
              <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
                <div className="flex flex-wrap gap-3">
                  {(["all", "pending", "confirmed", "completed", "closed"] as const).map(
                    (key) => (
                      <button
                        key={key}
                        type="button"
                        onClick={() => setFilter(key)}
                        className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                          filter === key
                            ? "bg-brand-500 text-white"
                            : "bg-bg-subtle text-text-secondary hover:bg-border-subtle"
                        }`}
                      >
                        {t(`bookings.filters.${key}`)}
                      </button>
                    ),
                  )}
                </div>

                <Link
                  to="/consultant/availability"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                >
                  {t("bookings.openAvailability")}
                </Link>
              </div>
            </div>

            <div className="mt-8 space-y-6">
              {filteredBookings.length > 0 ? (
                filteredBookings.map((booking) => (
                  <article
                    key={booking.id}
                    className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm"
                  >
                    <div className="flex flex-col gap-6 xl:flex-row xl:items-start xl:justify-between">
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-3">
                          <UserAvatar
                            userId={booking.studentId}
                            name={booking.studentName}
                            className="size-10 shrink-0"
                          />
                          <h2 className="text-2xl font-semibold tracking-[-0.01em] text-text-primary">
                            {booking.studentName}
                          </h2>

                          <span
                            className={`rounded-full px-3 py-1 text-xs font-medium ${statusBadgeClass(
                              booking.status,
                            )}`}
                          >
                            {t(statusLabelKey(booking.status))}
                          </span>
                        </div>

                        <div className="mt-6 grid gap-4 rounded-xl bg-bg-muted p-4 sm:grid-cols-2 lg:grid-cols-4">
                          {booking.studentEmail ? (
                            <div>
                              <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                                {t("bookings.card.studentEmail")}
                              </p>
                              <p className="mt-1 text-sm font-medium text-text-primary">
                                {booking.studentEmail}
                              </p>
                            </div>
                          ) : null}

                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                              {t("bookings.card.session")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-text-primary">
                              {t("sessionType")}
                            </p>
                          </div>

                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                              {t("bookings.card.dateTime")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-text-primary">
                              {formatDateTime(booking.scheduledStartAt, lang)}
                            </p>
                          </div>

                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                              {t("bookings.card.duration")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-text-primary">
                              {durationLabel(booking.durationMinutes, t)}
                            </p>
                          </div>

                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                              {t("bookings.card.fee")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-text-primary">
                              {formatUsd(booking.priceUsd)}
                            </p>
                          </div>

                          <div className="sm:col-span-2 lg:col-span-3">
                            <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                              {t("bookings.card.consultantNote")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-text-primary">
                              {t(`bookings.note.${statusBucket(booking.status)}`)}
                            </p>
                          </div>
                        </div>
                      </div>

                      <aside className="w-full rounded-xl border border-border-subtle bg-bg-muted p-4 xl:max-w-[280px]">
                        <h3 className="text-base font-semibold text-text-primary">
                          {t("bookings.card.quickActions")}
                        </h3>

                        <div className="mt-4 flex flex-col gap-3">
                          <Link
                            to={`/consultant/bookings/${booking.id}`}
                            className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                          >
                            {t("bookings.card.viewDetails")}
                          </Link>
                        </div>
                      </aside>
                    </div>
                  </article>
                ))
              ) : (
                <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-8 shadow-sm">
                  <h2 className="text-2xl font-semibold tracking-[-0.01em] text-text-primary">
                    {t("bookings.empty.title")}
                  </h2>

                  <p className="mt-3 max-w-2xl text-sm leading-7 text-text-secondary">
                    {t("bookings.empty.description")}
                  </p>

                  <div className="mt-6">
                    <Link
                      to="/consultant/availability"
                      className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                    >
                      {t("bookings.openAvailability")}
                    </Link>
                  </div>
                </div>
              )}
            </div>
          </>
        )}
      </section>
    </main>
  );
}
