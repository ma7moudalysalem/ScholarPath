import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { motion } from "motion/react";
import {
  Calendar,
  Clock,
  DollarSign,
  ArrowRight,
  Plus,
  CheckCircle2,
  XCircle,
  Hourglass,
} from "lucide-react";
import { cn } from "@/lib/utils";
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

function StatTile({
  icon: Icon,
  label,
  value,
  tone = "neutral",
}: {
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean }>;
  label: string;
  value: number;
  tone?: "neutral" | "brand" | "success" | "warning" | "danger";
}) {
  const toneClasses: Record<string, string> = {
    neutral: "text-text-primary",
    brand:   "text-brand-600",
    success: "text-success-600",
    warning: "text-warning-600",
    danger:  "text-danger-500",
  };
  const iconBgClasses: Record<string, string> = {
    neutral: "bg-bg-subtle text-text-secondary",
    brand:   "bg-brand-50 text-brand-600",
    success: "bg-success-50 text-success-600",
    warning: "bg-warning-50 text-warning-600",
    danger:  "bg-danger-50 text-danger-500",
  };
  return (
    <div className="flex items-center gap-3 rounded-2xl border border-border-subtle bg-bg-elevated p-4 shadow-xs">
      <div className={`flex size-10 items-center justify-center rounded-xl ${iconBgClasses[tone]}`}>
        <Icon aria-hidden className="size-5" />
      </div>
      <div>
        <p className="text-[10px] font-semibold uppercase tracking-wider text-text-tertiary">
          {label}
        </p>
        <p className={`text-xl font-bold ${toneClasses[tone]}`}>{value}</p>
      </div>
    </div>
  );
}

export function StudentBookings() {
  const { t, i18n } = useTranslation("bookings");
  const lang = i18n.language;
  const isRtl = i18n.dir() === "rtl";
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
    <div className="space-y-6">

      {/* ── Page header ── */}
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary">
            {t("list.title")}
          </h1>
          <p className="mt-2 max-w-xl text-text-secondary">{t("list.subtitle")}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <span className="badge badge-brand">{summary.total} {t("stats.total")}</span>
          <Link to="/student/consultants" className="btn btn-primary btn-sm">
            <Plus aria-hidden className="size-4" />
            {t("list.bookAnother")}
          </Link>
        </div>
      </div>

      {/* ── States ── */}
      {isError ? (
        <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm font-medium text-danger-500">
          {t("states.error")}
        </div>
      ) : isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <div
              key={i}
              className="h-32 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm"
            />
          ))}
        </div>
      ) : (
        <>
          {/* ── Stat tiles ── */}
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
            <StatTile
              icon={Calendar}
              label={t("stats.total")}
              value={summary.total}
              tone="brand"
            />
            <StatTile
              icon={Hourglass}
              label={t("stats.pending")}
              value={summary.pending}
              tone="warning"
            />
            <StatTile
              icon={CheckCircle2}
              label={t("stats.accepted")}
              value={summary.confirmed}
              tone="success"
            />
            <StatTile
              icon={CheckCircle2}
              label={t("stats.completed")}
              value={summary.completed}
            />
            <StatTile
              icon={XCircle}
              label={t("stats.closed")}
              value={summary.closed}
            />
          </div>

          {/* ── Sticky filter bar ── */}
          <div className="sticky top-14 z-20 -mx-4 border-y border-border-subtle bg-bg-canvas/85 px-4 py-3 backdrop-blur-xl sm:-mx-6 sm:px-6">
            <div className="flex flex-wrap items-center gap-2">
              {(["all", "pending", "confirmed", "completed", "closed"] as const).map((key) => {
                const count =
                  key === "all"
                    ? summary.total
                    : summary[key];
                return (
                  <button
                    key={key}
                    type="button"
                    onClick={() => setFilter(key)}
                    className={cn(
                      "inline-flex h-10 items-center gap-2 rounded-full border px-3.5 text-xs font-medium transition",
                      filter === key
                        ? "border-brand-500 bg-brand-500 text-white shadow-sm"
                        : "border-border-default bg-bg-elevated text-text-secondary hover:border-brand-300",
                    )}
                  >
                    {t(
                      key === "all"
                        ? "filters.all"
                        : key === "confirmed"
                          ? "filters.accepted"
                          : `filters.${key}`,
                    )}
                    <span
                      className={cn(
                        "rounded-full px-1.5 text-[10px] font-bold",
                        filter === key
                          ? "bg-white/25 text-white"
                          : "bg-bg-subtle text-text-secondary",
                      )}
                    >
                      {count}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* ── Booking list ── */}
          {filteredBookings.length > 0 ? (
            <div className="space-y-3">
              {filteredBookings.map((booking, i) => (
                <motion.article
                  key={booking.id}
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.2, delay: Math.min(i, 6) * 0.04 }}
                  className="group overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated shadow-xs transition-all hover:border-brand-200 hover:shadow-md"
                >
                  <div className="flex flex-col gap-5 p-5 lg:flex-row lg:items-center">
                    {/* Left: avatar + identity */}
                    <div className="flex min-w-0 flex-1 items-start gap-4">
                      <UserAvatar
                        userId={booking.consultantId}
                        name={booking.consultantName}
                        className="size-12 shrink-0"
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <h2 className="truncate text-base font-semibold text-text-primary">
                            {booking.consultantName}
                          </h2>
                          <span
                            className={cn(
                              "rounded-full px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider",
                              statusBadgeClass(booking.status),
                            )}
                          >
                            {t(statusLabelKey(booking.status))}
                          </span>
                        </div>
                        <p className="mt-1 text-xs text-text-tertiary">{t("sessionType")}</p>

                        {/* Compact metadata row */}
                        <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs">
                          <span className="inline-flex items-center gap-1.5 text-text-secondary">
                            <Calendar aria-hidden className="size-3.5 text-text-tertiary" />
                            <span className="font-medium">
                              {formatDateTime(booking.scheduledStartAt, lang)}
                            </span>
                          </span>
                          <span className="inline-flex items-center gap-1.5 text-text-secondary">
                            <Clock aria-hidden className="size-3.5 text-text-tertiary" />
                            {durationLabel(booking.durationMinutes, t)}
                          </span>
                          <span className="inline-flex items-center gap-1.5 text-text-secondary">
                            <DollarSign aria-hidden className="size-3.5 text-text-tertiary" />
                            <span className="font-semibold text-text-primary">
                              {booking.priceUsd === 0
                                ? t("scholarships:freeListing")
                                : formatUsd(booking.priceUsd)}
                            </span>
                          </span>
                        </div>

                        <p className="mt-3 line-clamp-2 text-xs leading-relaxed text-text-secondary">
                          {t(`notes.${statusBucket(booking.status)}`)}
                        </p>
                      </div>
                    </div>

                    {/* Right: actions */}
                    <div className="flex shrink-0 flex-col gap-2 lg:items-end">
                      <Link
                        to={`/student/bookings/${booking.id}`}
                        className="btn btn-primary btn-sm w-full lg:w-auto"
                      >
                        {t("card.viewDetails")}
                        <ArrowRight aria-hidden className={cn("size-3.5", isRtl && "rotate-180")} />
                      </Link>
                      <Link
                        to={`/student/consultants/${booking.consultantId}`}
                        className="btn btn-secondary btn-sm w-full lg:w-auto"
                      >
                        {t("card.viewConsultant")}
                      </Link>
                    </div>
                  </div>
                </motion.article>
              ))}
            </div>
          ) : (
            /* Premium empty state */
            <div className="flex min-h-[50vh] flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated p-12 text-center">
              <div className="mb-5 flex size-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-100 to-brand-50 text-brand-600">
                <Calendar aria-hidden className="size-7" />
              </div>
              <h3 className="text-lg font-semibold text-text-primary">{t("empty.title")}</h3>
              <p className="mt-2 max-w-md text-sm text-text-secondary">
                {t("empty.description")}
              </p>
              <Link to="/student/consultants" className="btn btn-primary btn-sm mt-6">
                {t("empty.browseConsultants")}
              </Link>
            </div>
          )}
        </>
      )}
    </div>
  );
}
