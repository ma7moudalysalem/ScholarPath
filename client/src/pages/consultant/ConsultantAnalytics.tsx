import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  RefreshCw,
  Star,
  CalendarCheck,
  CheckCircle2,
  Wallet,
  TrendingUp,
} from "lucide-react";
import { analyticsApi, type ConsultantKpisDto } from "@/services/api/analytics";
import {
  ChartCard,
  LegendRow,
  StatCard,
} from "@/components/dashboard/primitives";
import { cn } from "@/lib/utils";

// ─── Skeletons ───────────────────────────────────────────────────────────────

function SkeletonStatCard() {
  return (
    <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 space-y-3">
      <div className="h-9 w-9 animate-pulse rounded-xl bg-bg-subtle" />
      <div className="h-8 w-16 animate-pulse rounded bg-bg-subtle" />
      <div className="h-3 w-24 animate-pulse rounded bg-bg-subtle" />
    </div>
  );
}

// ─── Outcomes (horizontal bars with colored chips) ───────────────────────────

interface OutcomeRow {
  label: string;
  count: number;
  pct: number;
  /** Tailwind bg color class for the bar fill. */
  barClass: string;
  /** Tailwind text color class for the dot in the legend. */
  dotClass: string;
}

function OutcomeBars({ rows }: { rows: OutcomeRow[] }) {
  return (
    <div className="space-y-3">
      {rows.map((row) => (
        <div key={row.label} className="flex items-center gap-3">
          <span className={cn("size-2 shrink-0 rounded-full bg-current", row.dotClass)} aria-hidden />
          <span className="w-32 shrink-0 text-xs font-medium text-text-secondary">{row.label}</span>
          <div className="relative h-2.5 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className={cn("h-full rounded-full transition-all duration-500", row.barClass)}
              style={{ width: `${row.pct}%` }}
            />
          </div>
          <span className="w-10 shrink-0 text-end text-xs font-semibold tabular-nums text-text-primary">
            {row.count}
          </span>
          <span className="w-10 shrink-0 text-end text-xs tabular-nums text-text-tertiary">
            {row.pct.toFixed(0)}%
          </span>
        </div>
      ))}
    </div>
  );
}

// ─── Star rating display ─────────────────────────────────────────────────────

function StarRating({ rating }: { rating: number }) {
  const full = Math.floor(rating);
  const frac = rating - full;
  const empty = 5 - Math.ceil(rating);
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: full }).map((_, i) => (
        <Star key={`f${i}`} aria-hidden className="size-5 fill-amber-400 text-amber-400" />
      ))}
      {frac > 0 && (
        <div className="relative size-5">
          <Star aria-hidden className="absolute size-5 text-bg-subtle" />
          <div className="absolute overflow-hidden" style={{ width: `${frac * 100}%` }}>
            <Star aria-hidden className="size-5 fill-amber-400 text-amber-400" />
          </div>
        </div>
      )}
      {Array.from({ length: empty }).map((_, i) => (
        <Star key={`e${i}`} aria-hidden className="size-5 text-bg-subtle" />
      ))}
    </div>
  );
}

// ─── Page ────────────────────────────────────────────────────────────────────

/** PB-015 T-010 — Consultant Self-Analytics dashboard (custom SVG charts). */
export function ConsultantAnalytics() {
  const { t, i18n } = useTranslation(["analytics"]);

  const { data, isLoading, isError, refetch } = useQuery<ConsultantKpisDto>({
    queryKey: ["analytics", "consultant-kpis"],
    queryFn: () => analyticsApi.getConsultantKpis(),
  });

  const outcomeRows = useMemo<OutcomeRow[]>(() => {
    if (!data) return [];
    const total = Math.max(data.totalBookings, 1);
    return [
      {
        label: t("analytics:consultantKpis.completed"),
        count: data.completedBookings,
        pct: (data.completedBookings / total) * 100,
        barClass: "bg-success-500",
        dotClass: "text-success-500",
      },
      {
        label: t("analytics:consultantKpis.cancelled"),
        count: data.cancelledBookings,
        pct: (data.cancelledBookings / total) * 100,
        barClass: "bg-warning-500",
        dotClass: "text-warning-500",
      },
      {
        label: t("analytics:consultantKpis.rejected"),
        count: data.rejectedBookings,
        pct: (data.rejectedBookings / total) * 100,
        barClass: "bg-danger-500",
        dotClass: "text-danger-500",
      },
      {
        label: t("analytics:consultantKpis.noShows"),
        count: data.consultantNoShows + data.studentNoShows,
        pct: ((data.consultantNoShows + data.studentNoShows) / total) * 100,
        barClass: "bg-purple-500",
        dotClass: "text-purple-500",
      },
    ];
  }, [data, t]);

  // Currency-style format so the locale (en-US / ar-EG) drives the digit
  // script + currency-glyph placement; the `$` prefix on the StatCard is
  // removed because Intl already includes the symbol.
  const formattedRevenue = data
    ? data.completedRevenueUsd.toLocaleString(i18n.language === "ar" ? "ar-EG" : "en-US", {
        style: "currency",
        currency: "USD",
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      })
    : "—";

  return (
    <div className="space-y-6">
      {/* Header banner */}
      <section className="relative overflow-hidden rounded-3xl border border-border-subtle bg-bg-elevated p-6 sm:p-8">
        <div className="orb orb-brand orb-animated -end-24 -top-24 size-72 opacity-30" />
        <div className="orb orb-aurora -start-32 -bottom-32 size-80 opacity-20" />
        <div className="relative z-10 flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="mb-2 flex size-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
              <TrendingUp aria-hidden className="size-4" />
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
              {t("analytics:consultantKpis.title")}
            </h1>
            <p className="mt-1 max-w-2xl text-sm text-text-secondary">
              {t("analytics:consultantKpis.subtitle")}
            </p>
          </div>
          {data && data.averageRating != null && (
            <div className="flex flex-col items-end gap-1">
              <StarRating rating={data.averageRating} />
              <p className="text-xs text-text-tertiary">
                {data.averageRating.toFixed(1)} / 5 ·{" "}
                {data.reviewCount} {t("analytics:consultantKpis.reviews")}
              </p>
            </div>
          )}
        </div>
      </section>

      {/* Error state */}
      {isError && !isLoading && (
        <div className="flex flex-col items-center justify-center gap-4 rounded-2xl border border-danger-200 bg-danger-50 p-12 text-center">
          <p className="text-sm text-danger-600">{t("analytics:loadError")}</p>
          <button
            type="button"
            onClick={() => refetch()}
            className="btn btn-primary"
          >
            <RefreshCw className="size-4" />
            {t("analytics:retry")}
          </button>
        </div>
      )}

      {/* KPI grid */}
      <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        {isLoading ? (
          <>
            <SkeletonStatCard />
            <SkeletonStatCard />
            <SkeletonStatCard />
            <SkeletonStatCard />
          </>
        ) : data ? (
          <>
            <StatCard
              label={t("analytics:consultantKpis.totalBookings")}
              value={data.totalBookings}
              icon={CalendarCheck}
              accent="brand"
              delay={0.02}
            />
            <StatCard
              label={t("analytics:consultantKpis.completed")}
              value={data.completedBookings}
              icon={CheckCircle2}
              accent="success"
              delta={
                data.totalBookings > 0
                  ? {
                      value: Math.round((data.completedBookings / data.totalBookings) * 100),
                      label: `${data.totalBookings} ${t("analytics:consultantKpis.totalBookings").toLowerCase()}`,
                    }
                  : null
              }
              delay={0.06}
            />
            <StatCard
              label={t("analytics:consultantKpis.revenue")}
              value={formattedRevenue}
              icon={Wallet}
              accent="warning"
              delay={0.1}
            />
            {/* AvgRating: don't pass reviewCount through `delta.value` — StatCard
                renders that field with a trailing %, which would mis-label "5"
                reviews as "5%". The count is shown inline in the rating summary
                card below the grid. */}
            <StatCard
              label={t("analytics:consultantKpis.avgRating")}
              value={data.averageRating != null ? data.averageRating.toFixed(1) : "—"}
              icon={Star}
              accent="brand"
              delay={0.14}
            />
          </>
        ) : null}
      </section>

      {/* Booking outcomes chart */}
      {!isError && (
        <ChartCard
          title={t("analytics:consultantKpis.bookingOutcomes")}
          subtitle={t("analytics:legend.outcomes")}
          trailing={
            <LegendRow
              items={[
                { label: t("analytics:consultantKpis.completed"), colorClass: "text-success-500" },
                { label: t("analytics:consultantKpis.cancelled"), colorClass: "text-warning-500" },
                { label: t("analytics:consultantKpis.rejected"), colorClass: "text-danger-500" },
                { label: t("analytics:consultantKpis.noShows"), colorClass: "text-purple-500" },
              ]}
            />
          }
        >
          {isLoading ? (
            <div className="space-y-3">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="flex items-center gap-3">
                  <div className="h-3 w-32 animate-pulse rounded bg-bg-subtle" />
                  <div className="h-2.5 flex-1 animate-pulse rounded-full bg-bg-subtle" />
                  <div className="h-3 w-10 animate-pulse rounded bg-bg-subtle" />
                </div>
              ))}
            </div>
          ) : (
            <OutcomeBars rows={outcomeRows} />
          )}
        </ChartCard>
      )}

      {/* Rating summary card */}
      {!isError && data && (
        <ChartCard
          title={t("analytics:consultantKpis.ratingSummary")}
          subtitle={
            data.reviewCount > 0
              ? `${data.reviewCount} ${t("analytics:consultantKpis.reviews")}`
              : undefined
          }
        >
          {data.averageRating != null ? (
            <div className="flex flex-wrap items-center gap-6">
              <div className="flex items-baseline gap-3">
                <span className="text-5xl font-bold tabular-nums tracking-tight text-text-primary">
                  {data.averageRating.toFixed(1)}
                </span>
                <span className="text-lg font-normal text-text-tertiary">/ 5</span>
              </div>
              <div className="flex flex-col gap-1.5">
                <StarRating rating={data.averageRating} />
                <p className="text-xs text-text-secondary">
                  {data.reviewCount} {t("analytics:consultantKpis.reviews")}
                </p>
              </div>
            </div>
          ) : (
            <p className="text-sm text-text-secondary">—</p>
          )}
        </ChartCard>
      )}
    </div>
  );
}
