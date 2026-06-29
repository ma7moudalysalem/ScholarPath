import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useAllBookingsQuery } from "@/hooks/useBookingsQuery";
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
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";

type FilterBucket = "all" | BookingStatusBucket;

export function CompanyBookings() {
  const { t, i18n } = useTranslation(["bookings", "company"]);
  const lang = i18n.language;
  const { data, isLoading, isError } = useAllBookingsQuery();
  const [filter, setFilter] = useState<FilterBucket>("all");
  const paymentsEnabled = usePaymentsEnabled();

  const bookings = useMemo<BookingListItem[]>(() => data ?? [], [data]);

  const summary = useMemo(() => ({
    total: bookings.length,
    pending: bookings.filter((b) => statusBucket(b.status) === "pending").length,
    confirmed: bookings.filter((b) => statusBucket(b.status) === "confirmed").length,
    completed: bookings.filter((b) => statusBucket(b.status) === "completed").length,
    closed: bookings.filter((b) => statusBucket(b.status) === "closed").length,
  }), [bookings]);

  const filtered = useMemo(
    () => filter === "all" ? bookings : bookings.filter((b) => statusBucket(b.status) === filter),
    [bookings, filter],
  );

  const filters: { key: FilterBucket; label: string }[] = [
    { key: "all", label: t("bookings:filters.all") },
    { key: "pending", label: t("bookings:filters.pending") },
    { key: "confirmed", label: t("bookings:filters.accepted") },
    { key: "completed", label: t("bookings:filters.completed") },
    { key: "closed", label: t("bookings:filters.closed") },
  ];

  return (
    <main className="min-h-screen bg-bg-subtle">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
            {t("company:bookings.title")}
          </h1>
          <p className="max-w-3xl text-base leading-7 text-text-secondary">
            {t("company:bookings.subtitle")}
          </p>
        </div>

        {isError ? (
          <div className="mt-8 rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm font-medium text-danger-500">
            {t("bookings:states.error")}
          </div>
        ) : isLoading ? (
          <div className="mt-8 space-y-6">
            {Array.from({ length: 4 }).map((_, i) => (
              <div
                key={i}
                className="h-44 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm"
              />
            ))}
          </div>
        ) : (
          <>
            {/* Summary stats */}
            <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
              {[
                { label: t("bookings:stats.total"), value: summary.total },
                { label: t("bookings:stats.pending"), value: summary.pending },
                { label: t("bookings:stats.accepted"), value: summary.confirmed },
                { label: t("bookings:stats.completed"), value: summary.completed },
                { label: t("bookings:stats.closed"), value: summary.closed },
              ].map(({ label, value }) => (
                <div key={label} className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {label}
                  </p>
                  <p className="mt-2 text-3xl font-semibold text-text-primary">{value}</p>
                </div>
              ))}
            </div>

            {/* Filter tabs */}
            <div className="mt-8 flex flex-wrap gap-2">
              {filters.map(({ key, label }) => (
                <button
                  key={key}
                  type="button"
                  onClick={() => setFilter(key)}
                  className={`rounded-lg border px-4 py-2 text-sm font-medium transition ${
                    filter === key
                      ? "border-brand-500 bg-brand-500 text-white"
                      : "border-border-subtle bg-bg-elevated text-text-secondary hover:border-brand-300 hover:text-text-primary"
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>

            {/* Booking list */}
            {filtered.length === 0 ? (
              <div className="mt-8 rounded-2xl border border-border-subtle bg-bg-elevated p-10 text-center shadow-sm">
                <p className="text-lg font-semibold text-text-primary">{t("bookings:empty.title")}</p>
                <p className="mt-2 text-sm text-text-secondary">{t("bookings:empty.description")}</p>
              </div>
            ) : (
              <div className="mt-6 space-y-4">
                {filtered.map((booking) => (
                  <BookingRow
                    key={booking.id}
                    booking={booking}
                    lang={lang}
                    t={t}
                    paymentsEnabled={paymentsEnabled}
                  />
                ))}
              </div>
            )}
          </>
        )}
      </section>
    </main>
  );
}

function BookingRow({
  booking,
  lang,
  t,
  paymentsEnabled,
}: {
  booking: BookingListItem;
  lang: string;
  t: (key: string, opts?: Record<string, unknown>) => string;
  paymentsEnabled: boolean;
}) {
  const feeAmount = paymentsEnabled ? booking.priceUsd : 0;

  return (
    <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        {/* Left: consultant + student info */}
        <div className="flex items-start gap-4">
          <UserAvatar
            userId={booking.consultantId}
            name={booking.consultantName}
            className="size-10 rounded-full"
          />
          <div>
            <p className="font-semibold text-text-primary">{booking.consultantName}</p>
            <p className="text-sm text-text-secondary">
              {t("company:bookings.studentLabel")}:{" "}
              <span className="font-medium text-text-primary">{booking.studentName}</span>
            </p>
            <p className="mt-1 text-xs text-text-tertiary">
              {formatDateTime(booking.scheduledStartAt, lang)} —{" "}
              {durationLabel(booking.durationMinutes, t)}
            </p>
          </div>
        </div>

        {/* Right: status + fee */}
        <div className="flex flex-col items-start gap-2 sm:items-end">
          <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${statusBadgeClass(booking.status)}`}
          >
            {t(`bookings:${statusLabelKey(booking.status)}`)}
          </span>
          <span className="text-sm font-semibold text-text-primary">
            {feeAmount === 0 ? t("bookings:fields.fee") + ": Free" : formatUsd(feeAmount)}
          </span>
        </div>
      </div>

      <div className="mt-4 flex justify-end">
        <Link
          to={`/bookings/${booking.id}`}
          className="inline-flex h-9 items-center justify-center rounded-lg border border-brand-500 bg-transparent px-4 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
        >
          {t("bookings:card.viewDetails")}
        </Link>
      </div>
    </div>
  );
}
