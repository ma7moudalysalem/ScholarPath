import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  RefreshCw,
  Globe,
  GraduationCap,
  ListChecks,
  CheckCircle2,
  XCircle,
  Clock,
  TrendingUp,
  RotateCcw,
  CalendarDays,
  BarChart2,
} from "lucide-react";
import { analyticsApi, type CompanyInsightsDto } from "@/services/api/analytics";
import {
  ChartCard,
  StatCard,
  LegendRow,
  SmoothAreaChart,
} from "@/components/dashboard/primitives";
import { cn } from "@/lib/utils";

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

// ── Horizontal bar list (top 10) ──────────────────────────────────────────────

interface BarRow {
  label: string;
  count: number;
  rate: number;
}

function HorizontalBars({ rows }: { rows: BarRow[] }) {
  if (rows.length === 0) {
    return <div className="h-48 rounded-lg bg-bg-subtle/40" />;
  }
  const max = Math.max(...rows.map((r) => r.count), 1);
  return (
    <div className="space-y-2">
      {rows.map((row) => (
        <div key={row.label} className="flex items-center gap-3">
          <span className="w-32 shrink-0 truncate text-xs font-medium text-text-secondary">
            {row.label}
          </span>
          <div className="relative h-2.5 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className="h-full rounded-full bg-brand-500 transition-all duration-500"
              style={{ width: `${(row.count / max) * 100}%` }}
            />
          </div>
          <span className="w-10 shrink-0 text-end text-xs font-semibold tabular-nums text-text-primary">
            {row.count}
          </span>
          <span className="w-12 shrink-0 text-end text-xs tabular-nums text-text-tertiary">
            {row.rate.toFixed(0)}%
          </span>
        </div>
      ))}
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function CompanyInsights() {
  const { t, i18n } = useTranslation(["analytics"]);
  const isAr = i18n.language === "ar";
  const locale = isAr ? "ar-EG" : "en-US";

  const initial = useMemo(() => defaultRange(), []);
  const [from, setFrom] = useState(initial.from);
  const [to, setTo] = useState(initial.to);

  type SortKey = "applications" | "title";
  const [sortKey, setSortKey] = useState<SortKey>("applications");

  const { data, isLoading, isError, refetch } = useQuery<CompanyInsightsDto>({
    queryKey: ["analytics", "company", "insights", from, to],
    queryFn: () => analyticsApi.getCompanyInsights({ from, to }),
  });

  const onReset = () => {
    const d = defaultRange();
    setFrom(d.from);
    setTo(d.to);
  };

  const countryRows = useMemo<BarRow[]>(
    () =>
      (data?.byCountry ?? []).map((c) => ({
        label: c.country === "Unknown" ? t("analytics:companyInsights.byCountry.unknown") : c.country,
        count: c.count,
        rate: c.acceptanceRate,
      })),
    [data, t],
  );

  const fieldRows = useMemo<BarRow[]>(
    () =>
      (data?.byField ?? []).map((f) => ({
        label: isAr ? f.fieldAr : f.fieldEn,
        count: f.count,
        rate: f.acceptanceRate,
      })),
    [data, isAr],
  );

  const sortedScholarships = useMemo(() => {
    const arr = [...(data?.topScholarships ?? [])];
    arr.sort((a, b) =>
      sortKey === "applications"
        ? b.applications - a.applications
        : a.title.localeCompare(b.title),
    );
    return arr;
  }, [data, sortKey]);

  const monthlyLabels = (data?.monthlyFunnel ?? []).map((f) => {
    const [y, m] = f.month.split("-");
    const date = new Date(Number(y), Number(m) - 1, 1);
    return date.toLocaleDateString(locale, { month: "short", year: "2-digit" });
  });

  const deltaLabel = data
    ? data.comparisonToPlatformAvg >= 0
      ? t("analytics:companyInsights.deltaUp", {
          value: data.comparisonToPlatformAvg.toFixed(1),
        })
      : t("analytics:companyInsights.deltaDown", {
          value: data.comparisonToPlatformAvg.toFixed(1),
        })
    : "";

  return (
    <div className="space-y-6">
      {/* Header */}
      <section className="relative overflow-hidden rounded-3xl border border-border-subtle bg-bg-elevated p-6 sm:p-8">
        <div className="orb orb-brand orb-animated -end-24 -top-24 size-72 opacity-30" />
        <div className="relative z-10 flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="mb-2 flex size-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
              <BarChart2 aria-hidden className="size-4" />
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
              {t("analytics:companyInsights.title")}
            </h1>
            <p className="mt-1 max-w-2xl text-sm text-text-secondary">
              {t("analytics:companyInsights.subtitle")}
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
              <div className="h-8 w-16 animate-pulse rounded bg-bg-subtle" />
              <div className="h-3 w-24 animate-pulse rounded bg-bg-subtle" />
            </div>
          ))
        ) : (
          <>
            <StatCard
              label={t("analytics:companyInsights.kpi.totalApplications")}
              value={data.totalApplications}
              icon={ListChecks}
              accent="brand"
            />
            <StatCard
              label={t("analytics:companyInsights.kpi.accepted")}
              value={data.acceptedCount}
              icon={CheckCircle2}
              accent="success"
            />
            <StatCard
              label={t("analytics:companyInsights.kpi.acceptanceRate")}
              value={`${data.acceptanceRate.toFixed(1)}%`}
              icon={TrendingUp}
              accent="success"
              delta={
                data.totalApplications > 0
                  ? {
                      // Keep the sign so the arrow icon flips for "below
                      // platform average" — Math.abs would have wrongly shown
                      // an upward green arrow for negative deltas.
                      value: Math.round(data.comparisonToPlatformAvg * 10) / 10,
                      label: `${deltaLabel} ${t("analytics:reports.vsPlatformAvg")}`,
                    }
                  : null
              }
            />
            <StatCard
              label={t("analytics:companyInsights.kpi.avgDaysToDecision")}
              value={data.averageDaysToDecision.toFixed(1)}
              icon={Clock}
              accent="warning"
            />
          </>
        )}
      </section>

      {/* Country + Field breakdowns side-by-side */}
      <div className="grid gap-6 lg:grid-cols-2">
        <ChartCard
          title={t("analytics:companyInsights.byCountry.title")}
          subtitle={t("analytics:companyInsights.byCountry.subtitle")}
          trailing={<Globe aria-hidden className="size-4 text-text-tertiary" />}
        >
          {isLoading || !data ? (
            <div className="space-y-2">
              {[0, 1, 2, 3, 4].map((i) => (
                <div key={i} className="h-3 animate-pulse rounded bg-bg-subtle" />
              ))}
            </div>
          ) : countryRows.length === 0 ? (
            <p className="py-8 text-center text-sm text-text-tertiary">
              {t("analytics:reports.noData")}
            </p>
          ) : (
            <HorizontalBars rows={countryRows} />
          )}
        </ChartCard>

        <ChartCard
          title={t("analytics:companyInsights.byField.title")}
          subtitle={t("analytics:companyInsights.byField.subtitle")}
          trailing={<GraduationCap aria-hidden className="size-4 text-text-tertiary" />}
        >
          {isLoading || !data ? (
            <div className="space-y-2">
              {[0, 1, 2, 3, 4].map((i) => (
                <div key={i} className="h-3 animate-pulse rounded bg-bg-subtle" />
              ))}
            </div>
          ) : fieldRows.length === 0 ? (
            <p className="py-8 text-center text-sm text-text-tertiary">
              {t("analytics:reports.noData")}
            </p>
          ) : (
            <HorizontalBars rows={fieldRows} />
          )}
        </ChartCard>
      </div>

      {/* Top scholarships table */}
      <ChartCard
        title={t("analytics:companyInsights.topScholarships.title")}
        subtitle={t("analytics:companyInsights.topScholarships.subtitle")}
        trailing={<XCircle aria-hidden className="hidden" />}
      >
        {isLoading || !data ? (
          <div className="space-y-2">
            {[0, 1, 2, 3].map((i) => (
              <div key={i} className="h-9 animate-pulse rounded bg-bg-subtle" />
            ))}
          </div>
        ) : sortedScholarships.length === 0 ? (
          <p className="py-8 text-center text-sm text-text-tertiary">
            {t("analytics:reports.noData")}
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="text-xs uppercase tracking-wide text-text-tertiary">
                <tr className="border-b border-border-subtle">
                  <th className="px-2 py-2 text-start font-medium">
                    <button
                      type="button"
                      onClick={() => setSortKey("title")}
                      className={cn(
                        "hover:text-text-primary",
                        sortKey === "title" && "text-brand-600",
                      )}
                    >
                      {t("analytics:companyInsights.topScholarships.name")}
                    </button>
                  </th>
                  <th className="px-2 py-2 text-end font-medium">
                    <button
                      type="button"
                      onClick={() => setSortKey("applications")}
                      className={cn(
                        "hover:text-text-primary",
                        sortKey === "applications" && "text-brand-600",
                      )}
                    >
                      {t("analytics:companyInsights.topScholarships.applications")}
                    </button>
                  </th>
                </tr>
              </thead>
              <tbody>
                {sortedScholarships.map((s) => (
                  <tr key={s.id} className="border-b border-border-subtle last:border-b-0">
                    <td className="px-2 py-2 font-medium text-text-primary">{s.title}</td>
                    <td className="px-2 py-2 text-end font-semibold tabular-nums text-text-primary">
                      {s.applications}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </ChartCard>

      {/* Monthly funnel line chart */}
      <ChartCard
        title={t("analytics:companyInsights.funnel.title")}
        subtitle={t("analytics:companyInsights.funnel.subtitle")}
        trailing={
          <LegendRow
            items={[
              {
                label: t("analytics:companyInsights.funnel.views"),
                colorClass: "text-text-tertiary",
              },
              {
                label: t("analytics:companyInsights.funnel.applied"),
                colorClass: "text-brand-500",
              },
              {
                label: t("analytics:companyInsights.funnel.accepted"),
                colorClass: "text-success-500",
              },
            ]}
          />
        }
      >
        {isLoading || !data ? (
          <div className="h-48 animate-pulse rounded-lg bg-bg-subtle sm:h-56" />
        ) : data.monthlyFunnel.length < 2 ? (
          <p className="py-8 text-center text-sm text-text-tertiary">
            {t("analytics:reports.noData")}
          </p>
        ) : (
          <div className="space-y-4">
            <div className="text-text-tertiary">
              <SmoothAreaChart
                values={data.monthlyFunnel.map((f) => f.views)}
                labels={monthlyLabels}
                ariaLabel={t("analytics:companyInsights.funnel.views")}
              />
            </div>
            <div className="text-brand-500">
              <SmoothAreaChart
                values={data.monthlyFunnel.map((f) => f.applied)}
                labels={monthlyLabels}
                ariaLabel={t("analytics:companyInsights.funnel.applied")}
              />
            </div>
            <div className="text-success-500">
              <SmoothAreaChart
                values={data.monthlyFunnel.map((f) => f.accepted)}
                labels={monthlyLabels}
                ariaLabel={t("analytics:companyInsights.funnel.accepted")}
              />
            </div>
          </div>
        )}
      </ChartCard>
    </div>
  );
}
