import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useMyBookingsQuery } from "@/hooks/useBookingsQuery";
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

type StudentFilter = "all" | BookingStatusBucket;

export function StudentBookings() {
  const { t, i18n } = useTranslation("bookings");
  const lang = i18n.language;
  const { data, isLoading, isError } = useMyBookingsQuery();
  const [filter, setFilter] = useState<StudentFilter>("all");

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
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            {t("list.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("list.subtitle")}</p>
        </div>

        {isError ? (
          <div className="mt-8 rounded-2xl border border-[#fecaca] bg-[#fef2f2] p-6 text-sm font-medium text-[#dc2626]">
            {t("states.error")}
          </div>
        ) : isLoading ? (
          <div className="mt-8 space-y-6">
            {Array.from({ length: 3 }).map((_, index) => (
              <div
                key={index}
                className="h-48 animate-pulse rounded-2xl border border-[#e5e7eb] bg-white shadow-sm"
              />
            ))}
          </div>
        ) : (
          <>
            <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.total")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.total}</p>
              </div>

              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.pending")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.pending}</p>
              </div>

              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.accepted")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.confirmed}</p>
              </div>

              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.completed")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.completed}</p>
              </div>

              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("stats.closed")}
                </p>
                <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.closed}</p>
              </div>
            </div>

            <div className="mt-8 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
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
                            ? "bg-[#2563eb] text-white"
                            : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                        }`}
                      >
                        {t(
                          key === "all"
                            ? "filters.all"
                            : key === "confirmed"
                              ? "filters.accepted"
                              : `filters.${key}`,
                        )}
                      </button>
                    ),
                  )}
                </div>

                <Link
                  to="/student/consultants"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  {t("list.bookAnother")}
                </Link>
              </div>
            </div>

            <div className="mt-8 space-y-6">
              {filteredBookings.length > 0 ? (
                filteredBookings.map((booking) => (
                  <article
                    key={booking.id}
                    className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm"
                  >
                    <div className="flex flex-col gap-6 xl:flex-row xl:items-start xl:justify-between">
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-3">
                          <UserAvatar
                            userId={booking.consultantId}
                            name={booking.consultantName}
                            className="size-10 shrink-0"
                          />
                          <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                            {booking.consultantName}
                          </h2>

                          <span
                            className={`rounded-full px-3 py-1 text-xs font-medium ${statusBadgeClass(
                              booking.status,
                            )}`}
                          >
                            {t(statusLabelKey(booking.status))}
                          </span>
                        </div>

                        <div className="mt-6 grid gap-4 rounded-xl bg-[#f9fafb] p-4 sm:grid-cols-2 lg:grid-cols-4">
                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("fields.session")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                              {t("sessionType")}
                            </p>
                          </div>

                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("fields.dateTime")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                              {formatDateTime(booking.scheduledStartAt, lang)}
                            </p>
                          </div>

                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("fields.duration")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                              {durationLabel(booking.durationMinutes, t)}
                            </p>
                          </div>

                          <div>
                            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("fields.fee")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                              {formatUsd(booking.priceUsd)}
                            </p>
                          </div>

                          <div className="sm:col-span-2 lg:col-span-2">
                            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("fields.bookingNote")}
                            </p>
                            <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                              {t(`notes.${statusBucket(booking.status)}`)}
                            </p>
                          </div>
                        </div>
                      </div>

                      <aside className="w-full rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4 xl:max-w-[280px]">
                        <h3 className="text-base font-semibold text-[#1d1d1f]">
                          {t("card.quickActions")}
                        </h3>

                        <div className="mt-4 flex flex-col gap-3">
                          <Link
                            to={`/student/bookings/${booking.id}`}
                            className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                          >
                            {t("card.viewDetails")}
                          </Link>

                          <Link
                            to={`/student/consultants/${booking.consultantId}`}
                            className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                          >
                            {t("card.viewConsultant")}
                          </Link>
                        </div>
                      </aside>
                    </div>
                  </article>
                ))
              ) : (
                <div className="rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
                  <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                    {t("empty.title")}
                  </h2>

                  <p className="mt-3 max-w-2xl text-sm leading-7 text-[#4b5563]">
                    {t("empty.description")}
                  </p>

                  <div className="mt-6">
                    <Link
                      to="/student/consultants"
                      className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                    >
                      {t("empty.browseConsultants")}
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
