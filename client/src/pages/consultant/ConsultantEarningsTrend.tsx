import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  RefreshCw,
  TrendingUp,
  Wallet,
  Trophy,
  Calendar,
  Sparkles,
  RotateCcw,
  CalendarDays,
} from "lucide-react";
import {
  analyticsApi,
  type ConsultantEarningsTrendDto,
} from "@/services/api/analytics";
import {
  ChartCard,
  StatCard,
  LegendRow,
} from "@/components/dashboard/primitives";

function isoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function defaultRange(): { from: string; to: string } {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 364);
  return { from: isoDate(from), to: isoDate(to) };
}

// ── Line chart with dashed projection segment ─────────────────────────────────

interface ProjectedLineProps {
  values: number[];
  labels: string[];
  projectionValue: number | null;
  projectionLabel: string;
  netLabel: string;
}

function ProjectedLineChart({
  values,
  labels,
  projectionValue,
  projectionLabel,
  netLabel,
}: ProjectedLineProps) {
  const all = projectionValue != null ? [...values, projectionValue] : values;
  if (all.length < 2) {
    return <div className="h-48 rounded-lg bg-bg-subtle/40" />;
  }
  const w = 800;
  const h = 220;
  const padX = 8;
  const padY = 20;
  const innerW = w - padX * 2;
  const innerH = h - padY * 2;
  const max = Math.max(...all);
  const min = Math.min(...all, 0);
  const range = Math.max(max - min, 1);
  const step = innerW / (all.length - 1);

  const pts = all.map((v, i) => {
    const x = padX + i * step;
    const y = padY + innerH - ((v - min) / range) * innerH;
    return [x, y] as const;
  });

  const buildPath = (pp: readonly (readonly [number, number])[]): string => {
    if (pp.length < 2) return "";
    let d = `M ${pp[0][0].toFixed(2)} ${pp[0][1].toFixed(2)}`;
    for (let i = 1; i < pp.length; i++) {
      const [px, py] = pp[i - 1];
      const [x, y] = pp[i];
      const cx = (px + x) / 2;
      const cy = (py + y) / 2;
      d += ` Q ${px.toFixed(2)} ${py.toFixed(2)} ${cx.toFixed(2)} ${cy.toFixed(2)}`;
      if (i === pp.length - 1) d += ` T ${x.toFixed(2)} ${y.toFixed(2)}`;
    }
    return d;
  };

  const actualPts = pts.slice(0, values.length);
  const projectionPts =
    projectionValue != null
      ? [pts[values.length - 1], pts[values.length]] as const
      : null;

  const solidPath = buildPath(actualPts);
  const dashedPath = projectionPts ? buildPath(projectionPts) : "";

  // Gridlines
  const grid = [0.25, 0.5, 0.75, 1].map((r) => padY + innerH * r);

  return (
    <svg
      viewBox={`0 0 ${w} ${h}`}
      preserveAspectRatio="none"
      role="img"
      aria-label={netLabel}
      className="h-56 w-full text-brand-500"
    >
      <defs>
        <linearGradient id="net-area" x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.25" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
        </linearGradient>
      </defs>
      {grid.map((y, i) => (
        <line
          key={i}
          x1={padX}
          y1={y}
          x2={padX + innerW}
          y2={y}
          stroke="currentColor"
          strokeOpacity="0.08"
          strokeWidth="0.5"
        />
      ))}
      {/* Area under solid line */}
      <path
        d={`${solidPath} L ${actualPts[actualPts.length - 1][0].toFixed(2)} ${(padY + innerH).toFixed(2)} L ${actualPts[0][0].toFixed(2)} ${(padY + innerH).toFixed(2)} Z`}
        fill="url(#net-area)"
      />
      {/* Solid line */}
      <path
        d={solidPath}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
      />
      {/* Dashed projection line */}
      {dashedPath && (
        <path
          d={dashedPath}
          fill="none"
          stroke="currentColor"
          strokeWidth="1.75"
          strokeDasharray="6 4"
          strokeLinecap="round"
          strokeOpacity="0.65"
        />
      )}
      {/* Data points */}
      {actualPts.map(([x, y], i) => (
        <circle key={`a${i}`} cx={x} cy={y} r="3" fill="currentColor" />
      ))}
      {/* Projection endpoint */}
      {projectionPts && (
        <>
          <circle
            cx={projectionPts[1][0]}
            cy={projectionPts[1][1]}
            r="5"
            fill="currentColor"
            fillOpacity="0.2"
          />
          <circle
            cx={projectionPts[1][0]}
            cy={projectionPts[1][1]}
            r="3"
            fill="currentColor"
            stroke="white"
            strokeWidth="1"
          />
          <text
            x={projectionPts[1][0]}
            y={projectionPts[1][1] - 8}
            fontSize="9"
            fontWeight="600"
            textAnchor="middle"
            fill="currentColor"
          >
            {projectionLabel}
          </text>
        </>
      )}
      {/* X labels */}
      {labels.map((label, i) => {
        if (i % Math.ceil(labels.length / 6) !== 0 && i !== labels.length - 1) return null;
        const x = pts[i][0];
        return (
          <text
            key={i}
            x={x}
            y={h - 4}
            fontSize="9"
            textAnchor="middle"
            fill="currentColor"
            fillOpacity="0.55"
          >
            {label}
          </text>
        );
      })}
    </svg>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function ConsultantEarningsTrend() {
  const { t, i18n } = useTranslation(["analytics"]);
  const locale = i18n.language === "ar" ? "ar-EG" : "en-US";

  const initial = useMemo(() => defaultRange(), []);
  const [from, setFrom] = useState(initial.from);
  const [to, setTo] = useState(initial.to);

  const { data, isLoading, isError, refetch } = useQuery<ConsultantEarningsTrendDto>({
    queryKey: ["analytics", "consultant", "earnings-trend", from, to],
    queryFn: () => analyticsApi.getConsultantEarningsTrend({ from, to }),
  });

  const onReset = () => {
    const d = defaultRange();
    setFrom(d.from);
    setTo(d.to);
  };

  // Currency-style format so the locale (en-US / ar-EG) drives both digit
  // script and the placement of the $ glyph (AR readers expect ٢٥٠ ر.س. style
  // placement). Whole-dollar precision keeps the KPI cards dense.
  const fmtUsd = (n: number) =>
    n.toLocaleString(locale, {
      style: "currency",
      currency: "USD",
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    });

  const monthlyLabels = (data?.monthlyEarnings ?? []).map((m) => {
    const [y, mo] = m.month.split("-");
    const date = new Date(Number(y), Number(mo) - 1, 1);
    return date.toLocaleDateString(locale, { month: "short", year: "2-digit" });
  });

  // Add the projected next month to label list
  const projectedLabel = useMemo(() => {
    if (!data || data.monthlyEarnings.length === 0) return "";
    const last = data.monthlyEarnings[data.monthlyEarnings.length - 1];
    const [y, mo] = last.month.split("-");
    const next = new Date(Number(y), Number(mo), 1); // +1 month
    return next.toLocaleDateString(locale, { month: "short", year: "2-digit" });
  }, [data, locale]);

  const projectionLabel = data ? fmtUsd(data.projectedNextMonth) : "";

  // Choose ranking message
  const rankingText = useMemo(() => {
    if (!data) return "";
    if (data.peerAvgNetUsd <= 0) return t("analytics:consultantEarnings.ranking.noPeers");
    if (data.yourPercentile >= 50) {
      return t("analytics:consultantEarnings.ranking.topX", {
        percentile: Math.max(1, 100 - data.yourPercentile),
      });
    }
    if (data.yourPercentile >= 40)
      return t("analytics:consultantEarnings.ranking.average");
    return t("analytics:consultantEarnings.ranking.below");
  }, [data, t]);

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-3xl border border-border-subtle bg-bg-elevated p-6 sm:p-8">
        <div className="orb orb-brand orb-animated -end-24 -top-24 size-72 opacity-30" />
        <div className="orb orb-aurora -start-32 -bottom-32 size-80 opacity-20" />
        <div className="relative z-10 flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="mb-2 flex size-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
              <TrendingUp aria-hidden className="size-4" />
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
              {t("analytics:consultantEarnings.title")}
            </h1>
            <p className="mt-1 max-w-2xl text-sm text-text-secondary">
              {t("analytics:consultantEarnings.subtitle")}
            </p>
          </div>
        </div>
      </section>

      {/* Date range */}
      <section className="card-premium p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-text-tertiary">
            <CalendarDays aria-hidden className="size-4" />
            {t("analytics:reports.dateRange.label")}
          </div>
          <label className="flex flex-col gap-1 text-xs">
            <span className="text-text-secondary">{t("analytics:reports.dateRange.from")}</span>
            <input
              type="date"
              value={from}
              max={to}
              onChange={(e) => setFrom(e.target.value)}
              className="rounded-md border border-border-subtle bg-bg-elevated px-3 py-1.5 text-sm text-text-primary focus:border-brand-400 focus:outline-none"
            />
          </label>
          <label className="flex flex-col gap-1 text-xs">
            <span className="text-text-secondary">{t("analytics:reports.dateRange.to")}</span>
            <input
              type="date"
              value={to}
              min={from}
              onChange={(e) => setTo(e.target.value)}
              className="rounded-md border border-border-subtle bg-bg-elevated px-3 py-1.5 text-sm text-text-primary focus:border-brand-400 focus:outline-none"
            />
          </label>
          <button
            type="button"
            onClick={onReset}
            className="inline-flex items-center gap-1.5 rounded-md border border-border-subtle bg-bg-elevated px-3 py-2 text-xs font-medium text-text-secondary transition hover:bg-bg-subtle"
          >
            <RotateCcw aria-hidden className="size-3.5" />
            {t("analytics:reports.dateRange.reset")}
          </button>
        </div>
      </section>

      {isError && (
        <div className="flex flex-col items-center justify-center gap-4 rounded-2xl border border-danger-200 bg-danger-50 p-12 text-center">
          <p className="text-sm text-danger-600">{t("analytics:reports.loadError")}</p>
          <button type="button" onClick={() => refetch()} className="btn btn-primary">
            <RefreshCw className="size-4" />
            {t("analytics:reports.retry")}
          </button>
        </div>
      )}

      {/* KPI row */}
      <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        {isLoading || !data ? (
          [0, 1, 2, 3].map((i) => (
            <div
              key={i}
              className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 space-y-3"
            >
              <div className="h-9 w-9 animate-pulse rounded-xl bg-bg-subtle" />
              <div className="h-8 w-20 animate-pulse rounded bg-bg-subtle" />
              <div className="h-3 w-24 animate-pulse rounded bg-bg-subtle" />
            </div>
          ))
        ) : (
          <>
            <StatCard
              label={t("analytics:consultantEarnings.kpi.totalNet")}
              value={fmtUsd(data.totalNetUsd)}
              icon={Wallet}
              accent="success"
            />
            <StatCard
              label={t("analytics:consultantEarnings.kpi.projected")}
              value={fmtUsd(data.projectedNextMonth)}
              icon={Sparkles}
              accent="brand"
            />
            <StatCard
              label={t("analytics:consultantEarnings.kpi.percentile")}
              value={`${Math.round(data.yourPercentile)}%`}
              icon={Trophy}
              accent={data.yourPercentile >= 50 ? "success" : "neutral"}
            />
            <StatCard
              label={t("analytics:consultantEarnings.kpi.upcoming")}
              value={fmtUsd(data.upcomingBookingRevenue)}
              icon={Calendar}
              accent="warning"
            />
          </>
        )}
      </section>

      {/* Ranking strip */}
      {data && data.peerAvgNetUsd > 0 && (
        <section className="relative overflow-hidden rounded-2xl border border-border-subtle bg-gradient-to-br from-brand-50 to-bg-elevated p-5 sm:p-6">
          <div className="orb orb-brand orb-animated -end-12 -top-16 size-40 opacity-40" />
          <div className="relative z-10 flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <div className="flex size-10 items-center justify-center rounded-xl bg-brand-500 text-white">
                <Trophy aria-hidden className="size-5" />
              </div>
              <div>
                <p className="text-base font-semibold text-text-primary">{rankingText}</p>
                <p className="mt-0.5 text-xs text-text-secondary">
                  {t("analytics:consultantEarnings.kpi.peerAvg")}:{" "}
                  {fmtUsd(data.peerAvgNetUsd)}
                </p>
              </div>
            </div>
            <div className="text-end">
              <div className="text-2xl font-bold tabular-nums text-brand-600">
                {Math.round(data.yourPercentile)}%
              </div>
              <div className="text-xs uppercase tracking-wide text-text-tertiary">
                {t("analytics:consultantEarnings.kpi.percentile")}
              </div>
            </div>
          </div>
        </section>
      )}

      {/* Monthly chart with projection */}
      <ChartCard
        title={t("analytics:consultantEarnings.monthlyChart.title")}
        subtitle={t("analytics:consultantEarnings.monthlyChart.subtitle")}
        trailing={
          <LegendRow
            items={[
              {
                label: t("analytics:consultantEarnings.monthlyChart.net"),
                colorClass: "text-brand-500",
              },
              {
                label: t("analytics:consultantEarnings.monthlyChart.projection"),
                colorClass: "text-brand-400",
              },
            ]}
          />
        }
      >
        {isLoading || !data ? (
          <div className="h-48 animate-pulse rounded-lg bg-bg-subtle sm:h-56" />
        ) : data.monthlyEarnings.length < 2 ? (
          <p className="py-8 text-center text-sm text-text-tertiary">
            {t("analytics:reports.noData")}
          </p>
        ) : (
          <ProjectedLineChart
            values={data.monthlyEarnings.map((m) => m.netUsd)}
            labels={[...monthlyLabels, projectedLabel]}
            projectionValue={data.projectedNextMonth}
            projectionLabel={projectionLabel}
            netLabel={t("analytics:consultantEarnings.monthlyChart.net")}
          />
        )}
      </ChartCard>

      {/* Upcoming revenue tile */}
      {data && data.upcomingBookingRevenue > 0 && (
        <ChartCard
          title={t("analytics:consultantEarnings.upcoming.title")}
          subtitle={t("analytics:consultantEarnings.upcoming.subtitle")}
        >
          <div className="flex flex-wrap items-baseline gap-3">
            <span className="text-4xl font-bold tabular-nums tracking-tight text-text-primary">
              {fmtUsd(data.upcomingBookingRevenue)}
            </span>
            <span className="text-sm text-text-tertiary">
              {t("analytics:consultantEarnings.kpi.upcoming")}
            </span>
          </div>
        </ChartCard>
      )}
    </div>
  );
}
