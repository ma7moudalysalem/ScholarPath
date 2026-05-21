import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { RefreshCw } from "lucide-react";
import { analyticsApi, type ConsultantKpisDto } from "@/services/api/analytics";

// ── Skeleton helpers ────────────────────────────────────────────────────────

function SkeletonCard() {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-elevated p-5 space-y-3">
      <div className="h-3 w-24 animate-pulse rounded bg-bg-subtle" />
      <div className="h-7 w-16 animate-pulse rounded bg-bg-subtle" />
      <div className="h-2 w-32 animate-pulse rounded bg-bg-subtle" />
    </div>
  );
}

// ── KPI Card ────────────────────────────────────────────────────────────────

interface KpiCardProps {
  label: string;
  value: string | number;
  sub?: string;
}

function KpiCard({ label, value, sub }: KpiCardProps) {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <p className="text-sm font-semibold text-text-secondary">{label}</p>
      <p className="mt-1 text-2xl font-bold tabular-nums text-text-primary">{value}</p>
      {sub && <p className="mt-0.5 text-xs text-text-secondary">{sub}</p>}
    </div>
  );
}

// ── Horizontal bar chart for booking outcomes ───────────────────────────────

interface OutcomeRow {
  label: string;
  count: number;
  pct: number;
  color: string;
}

function BookingOutcomesChart({ rows }: { rows: OutcomeRow[] }) {
  return (
    <div className="space-y-3">
      {rows.map((row) => (
        <div key={row.label} className="flex items-center gap-3">
          <span className="w-36 shrink-0 text-xs text-text-secondary">{row.label}</span>
          <div className="h-3 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className="h-full rounded-full transition-all"
              style={{ width: `${row.pct}%`, backgroundColor: row.color }}
            />
          </div>
          <span className="w-10 shrink-0 text-end text-xs font-medium tabular-nums text-text-primary">
            {row.count}
          </span>
          <span className="w-10 shrink-0 text-end text-xs tabular-nums text-text-secondary">
            {row.pct.toFixed(0)}%
          </span>
        </div>
      ))}
    </div>
  );
}

// ── Star rating display ─────────────────────────────────────────────────────

function StarRating({ rating }: { rating: number }) {
  const full = Math.floor(rating);
  const frac = rating - full;
  const empty = 5 - Math.ceil(rating);
  const id = "star-grad";

  return (
    <svg viewBox="0 0 120 24" className="h-6 w-[120px]" aria-hidden>
      <defs>
        <linearGradient id={id}>
          <stop offset={`${frac * 100}%`} stopColor="#f59e0b" />
          <stop offset={`${frac * 100}%`} stopColor="#d1d5db" />
        </linearGradient>
      </defs>
      {Array.from({ length: full }).map((_, i) => (
        <text key={`f${i}`} x={i * 24} y={20} fontSize={20} fill="#f59e0b">★</text>
      ))}
      {frac > 0 && (
        <text x={full * 24} y={20} fontSize={20} fill={`url(#${id})`}>★</text>
      )}
      {Array.from({ length: empty }).map((_, i) => (
        <text key={`e${i}`} x={(full + (frac > 0 ? 1 : 0) + i) * 24} y={20} fontSize={20} fill="#d1d5db">★</text>
      ))}
    </svg>
  );
}

// ── Main page ───────────────────────────────────────────────────────────────

/** PB-015 T-010 — Consultant Self-Analytics dashboard (custom SVG charts). */
export function ConsultantAnalytics() {
  const { t } = useTranslation(["analytics"]);

  const { data, isLoading, isError, refetch } = useQuery<ConsultantKpisDto>({
    queryKey: ["analytics", "consultant-kpis"],
    queryFn: () => analyticsApi.getConsultantKpis(),
  });

  // Build outcome rows once data is ready
  const outcomeRows: OutcomeRow[] = data
    ? (() => {
        const total = Math.max(data.totalBookings, 1);
        return [
          { label: t("analytics:consultantKpis.completed"), count: data.completedBookings, pct: (data.completedBookings / total) * 100, color: "#22c55e" },
          { label: t("analytics:consultantKpis.cancelled"), count: data.cancelledBookings, pct: (data.cancelledBookings / total) * 100, color: "#f59e0b" },
          { label: t("analytics:consultantKpis.rejected"), count: data.rejectedBookings, pct: (data.rejectedBookings / total) * 100, color: "#ef4444" },
          {
            label: t("analytics:consultantKpis.noShows"),
            count: data.consultantNoShows + data.studentNoShows,
            pct: ((data.consultantNoShows + data.studentNoShows) / total) * 100,
            color: "#8b5cf6",
          },
        ];
      })()
    : [];

  return (
    <div className="space-y-6">
      {/* Heading */}
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("analytics:consultantKpis.title")}
        </h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">
          {t("analytics:consultantKpis.subtitle")}
        </p>
      </div>

      {/* Error state */}
      {isError && !isLoading && (
        <div className="flex flex-col items-center justify-center gap-4 rounded-lg border border-danger-200 bg-danger-50 p-12 text-center">
          <p className="text-sm text-danger-600">{t("analytics:loadError")}</p>
          <button
            type="button"
            onClick={() => refetch()}
            className="flex items-center gap-2 rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand hover:bg-brand-600"
          >
            <RefreshCw className="size-4" />
            {t("analytics:retry")}
          </button>
        </div>
      )}

      {/* KPI cards row */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        {isLoading ? (
          <>
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
            <SkeletonCard />
          </>
        ) : data ? (
          <>
            <KpiCard label={t("analytics:consultantKpis.totalBookings")} value={data.totalBookings} />
            <KpiCard label={t("analytics:consultantKpis.completed")} value={data.completedBookings} />
            <KpiCard
              label={t("analytics:consultantKpis.revenue")}
              value={`$${data.completedRevenueUsd.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
            />
            <KpiCard
              label={t("analytics:consultantKpis.avgRating")}
              value={data.averageRating != null ? data.averageRating.toFixed(1) : "—"}
              sub={data.reviewCount > 0 ? `${data.reviewCount} ${t("analytics:consultantKpis.reviews")}` : undefined}
            />
          </>
        ) : null}
      </div>

      {/* Booking outcomes chart */}
      {!isError && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-4 text-sm font-semibold">{t("analytics:consultantKpis.bookingOutcomes")}</h2>
          {isLoading ? (
            <div className="space-y-3">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="flex items-center gap-3">
                  <div className="h-3 w-36 animate-pulse rounded bg-bg-subtle" />
                  <div className="h-3 flex-1 animate-pulse rounded bg-bg-subtle" />
                  <div className="h-3 w-10 animate-pulse rounded bg-bg-subtle" />
                </div>
              ))}
            </div>
          ) : (
            <BookingOutcomesChart rows={outcomeRows} />
          )}
        </section>
      )}

      {/* Rating summary */}
      {!isError && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-4 text-sm font-semibold">{t("analytics:consultantKpis.ratingSummary")}</h2>
          {isLoading ? (
            <div className="space-y-2">
              <div className="h-6 w-32 animate-pulse rounded bg-bg-subtle" />
              <div className="h-4 w-48 animate-pulse rounded bg-bg-subtle" />
            </div>
          ) : data ? (
            <div className="flex flex-col gap-2">
              {data.averageRating != null ? (
                <>
                  <StarRating rating={data.averageRating} />
                  <p className="text-2xl font-bold tabular-nums text-text-primary">
                    {data.averageRating.toFixed(1)}
                    <span className="ml-2 text-sm font-normal text-text-secondary">
                      / 5 &nbsp;&middot;&nbsp; {data.reviewCount} {t("analytics:consultantKpis.reviews")}
                    </span>
                  </p>
                </>
              ) : (
                <p className="text-sm text-text-secondary">—</p>
              )}
            </div>
          ) : null}
        </section>
      )}
    </div>
  );
}
