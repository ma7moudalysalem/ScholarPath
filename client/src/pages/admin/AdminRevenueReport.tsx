import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Download,
  RefreshCw,
  Wallet,
  TrendingUp,
  PieChart as PieIcon,
  RotateCcw,
  CalendarDays,
} from "lucide-react";
import { analyticsApi, type AdminRevenueDto } from "@/services/api/analytics";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { DatePicker } from "@/components/ui/DatePicker";
import {
  ChartCard,
  StatCard,
  LegendRow,
} from "@/components/dashboard/primitives";
import { cn } from "@/lib/utils";

// ── Date helpers ──────────────────────────────────────────────────────────────

// Format a Date as YYYY-MM-DD in UTC. The backend treats the from/to range in
// UTC, so emitting local-zone components here would shift the window by a day
// for callers east/west of UTC.
function isoDate(d: Date): string {
  const y = d.getUTCFullYear();
  const m = String(d.getUTCMonth() + 1).padStart(2, "0");
  const day = String(d.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function defaultRange(): { from: string; to: string } {
  // Anchor at today UTC so we don't miss / double-count edge days.
  const now = new Date();
  const to = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  const from = new Date(to);
  from.setUTCDate(from.getUTCDate() - 89); // last 90 days inclusive
  return { from: isoDate(from), to: isoDate(to) };
}

// ── Stacked monthly bars (gross + refunded) ───────────────────────────────────

interface MonthlyBarsProps {
  months: AdminRevenueDto["byMonth"];
  ariaLabel: string;
  locale: string;
}

function MonthlyBars({ months, ariaLabel, locale }: MonthlyBarsProps) {
  if (months.length === 0) {
    return <div className="h-48 rounded-lg bg-bg-subtle/40" />;
  }
  const max = Math.max(...months.map((m) => m.grossUsd), 1);

  return (
    <div className="flex items-end gap-3 overflow-x-auto pb-2" role="img" aria-label={ariaLabel}>
      {months.map((m) => {
        const grossH = (m.grossUsd / max) * 180;
        const refundH = m.grossUsd > 0
          ? Math.min((m.refundedUsd / max) * 180, grossH)
          : 0;
        const monthLabel = (() => {
          const [y, mo] = m.month.split("-");
          const date = new Date(Number(y), Number(mo) - 1, 1);
          return date.toLocaleDateString(locale, { month: "short", year: "2-digit" });
        })();
        return (
          <div
            key={m.month}
            className="flex shrink-0 flex-col items-center gap-2"
            style={{ width: 56 }}
          >
            <span className="text-[10px] font-semibold tabular-nums text-text-secondary">
              ${Math.round(m.grossUsd).toLocaleString(locale)}
            </span>
            <div className="relative h-[180px] w-7 overflow-hidden rounded-md bg-bg-subtle">
              <div
                className="absolute inset-x-0 bottom-0 bg-brand-500"
                style={{ height: `${grossH}px` }}
              />
              {refundH > 0 && (
                <div
                  className="absolute inset-x-0 bottom-0 bg-danger-500/80"
                  style={{ height: `${refundH}px` }}
                />
              )}
            </div>
            <span className="text-[10px] font-medium uppercase tracking-wide text-text-tertiary">
              {monthLabel}
            </span>
          </div>
        );
      })}
    </div>
  );
}

// ── Pie chart (bookings vs reviews) ───────────────────────────────────────────

interface PieProps {
  bookings: number;
  reviews: number;
  bookingLabel: string;
  reviewLabel: string;
  /** BCP-47 locale used to format the monetary values (e.g. ar-EG / en-US). */
  locale: string;
}

function RevenuePie({ bookings, reviews, bookingLabel, reviewLabel, locale }: PieProps) {
  const total = bookings + reviews;
  if (total <= 0) {
    return <div className="h-48 rounded-lg bg-bg-subtle/40" />;
  }
  const bookingPct = (bookings / total) * 100;

  // Use conic-gradient for a crisp 2-segment pie
  const bg = `conic-gradient(rgb(99 102 241) 0% ${bookingPct}%, rgb(245 158 11) ${bookingPct}% 100%)`;

  return (
    <div className="flex flex-wrap items-center gap-6">
      <div
        className="relative size-40 shrink-0 rounded-full"
        style={{ background: bg }}
        role="img"
        aria-label={`${bookingLabel}: ${bookingPct.toFixed(0)}%`}
      >
        <div className="absolute inset-3 rounded-full bg-bg-elevated" />
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-lg font-bold tabular-nums text-text-primary">
            {bookingPct.toFixed(0)}%
          </span>
          <span className="text-[10px] uppercase tracking-wide text-text-tertiary">
            {bookingLabel}
          </span>
        </div>
      </div>
      <ul className="space-y-2 text-sm">
        <li className="flex items-center gap-2">
          <span className="inline-block size-3 rounded-sm bg-indigo-500" aria-hidden />
          <span className="font-medium">{bookingLabel}</span>
          <span className="ms-2 tabular-nums text-text-secondary">
            ${bookings.toLocaleString(locale)}
          </span>
        </li>
        <li className="flex items-center gap-2">
          <span className="inline-block size-3 rounded-sm bg-amber-500" aria-hidden />
          <span className="font-medium">{reviewLabel}</span>
          <span className="ms-2 tabular-nums text-text-secondary">
            ${reviews.toLocaleString(locale)}
          </span>
        </li>
      </ul>
    </div>
  );
}

// ── CSV export ────────────────────────────────────────────────────────────────

function exportCsv(filename: string, rows: (string | number)[][]): void {
  const csv = rows
    .map((row) =>
      row
        .map((cell) => {
          const s = String(cell ?? "");
          if (s.includes(",") || s.includes('"') || s.includes("\n")) {
            return `"${s.replace(/"/g, '""')}"`;
          }
          return s;
        })
        .join(","),
    )
    .join("\n");
  // Excel needs the UTF-8 BOM at the start of CSV files or it mis-decodes
  // non-ASCII as latin-1. The disable line keeps eslint from flagging the
  // intentional BOM character below.
  // eslint-disable-next-line no-irregular-whitespace
  const blob = new Blob([`﻿${csv}`], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function AdminRevenueReport() {
  const { t, i18n } = useTranslation(["analytics", "common", "payments"]);
  const locale = i18n.language === "ar" ? "ar-EG" : "en-US";
  // Master payments switch — when off, the report still loads but shows a
  // banner so the admin knows new transactions stopped accruing.
  const paymentsEnabled = usePaymentsEnabled();

  const initial = useMemo(() => defaultRange(), []);
  const [from, setFrom] = useState(initial.from);
  const [to, setTo] = useState(initial.to);

  const { data, isLoading, isError, refetch } = useQuery<AdminRevenueDto>({
    queryKey: ["analytics", "admin", "revenue", from, to],
    queryFn: () => analyticsApi.getAdminRevenue({ from, to }),
  });

  const onReset = () => {
    const d = defaultRange();
    setFrom(d.from);
    setTo(d.to);
  };

  const onExport = () => {
    if (!data) return;
    const header = ["Section", "Key", "Value"];
    const rows: (string | number)[][] = [
      header,
      ["totals", "totalGrossUsd", data.totalGrossUsd],
      ["totals", "totalProfitShareUsd", data.totalProfitShareUsd],
      ["totals", "totalPayeeNetUsd", data.totalPayeeNetUsd],
      ["totals", "totalRefundedUsd", data.totalRefundedUsd],
      ["totals", "refundRate", data.refundRate],
      ["totals", "bookingRevenueUsd", data.bookingRevenueUsd],
      ["totals", "reviewRevenueUsd", data.reviewRevenueUsd],
      ["totals", "monthOverMonthGrowthPct", data.monthOverMonthGrowth],
      ["totals", "refundCount", data.refundCount],
      ["totals", "successfulPaymentCount", data.successfulPaymentCount],
      ["", "", ""],
      ["byMonth", "month,grossUsd,netUsd,refundedUsd", ""],
    ];
    for (const m of data.byMonth) {
      rows.push(["byMonth", m.month, `${m.grossUsd},${m.netUsd},${m.refundedUsd}`]);
    }
    rows.push(["", "", ""]);
    rows.push(["topConsultants", "id,name,revenueUsd", ""]);
    for (const c of data.topConsultants) {
      rows.push(["topConsultants", c.id, `${c.name},${c.revenueUsd}`]);
    }
    exportCsv(`scholarpath-revenue-${from}-to-${to}.csv`, rows);
  };

  const fmtUsd = (n: number) =>
    n.toLocaleString(locale, {
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    });
  const fmtPct = (n: number) => `${(n * 100).toLocaleString(locale, { maximumFractionDigits: 1 })}%`;

  return (
    <div className="space-y-6">
      {!paymentsEnabled && (
        <div className="rounded-lg border border-brand-200 bg-brand-50 px-4 py-3 text-sm text-brand-700">
          {t("payments:billing.paymentsDisabledBanner")}
        </div>
      )}
      {/* Header */}
      <section className="relative overflow-hidden rounded-3xl border border-border-subtle bg-bg-elevated p-6 sm:p-8">
        <div className="orb orb-brand orb-animated -end-24 -top-24 size-72 opacity-30" />
        <div className="relative z-10 flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="mb-2 flex size-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
              <Wallet aria-hidden className="size-4" />
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
              {t("analytics:adminRevenue.title")}
            </h1>
            <p className="mt-1 max-w-2xl text-sm text-text-secondary">
              {t("analytics:adminRevenue.subtitle")}
            </p>
          </div>
          <div className="flex flex-wrap items-end gap-2">
            <button
              type="button"
              onClick={onExport}
              disabled={!data}
              className="inline-flex items-center gap-1.5 rounded-lg border border-border-subtle bg-bg-elevated px-3 py-2 text-sm font-medium text-text-primary transition hover:bg-bg-subtle disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Download aria-hidden className="size-4" />
              {t("analytics:reports.exportCsv")}
            </button>
          </div>
        </div>
      </section>

      {/* Date-range card */}
      <section className="card-premium p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-text-tertiary">
            <CalendarDays aria-hidden className="size-4" />
            {t("analytics:reports.dateRange.label")}
          </div>
          <label className="flex flex-col gap-1 text-xs">
            <span className="text-text-secondary">{t("analytics:reports.dateRange.from")}</span>
            <DatePicker
              value={from}
              max={to}
              onChange={setFrom}
              ariaLabel={t("analytics:reports.dateRange.from")}
              className="h-9 w-40 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
            />
          </label>
          <label className="flex flex-col gap-1 text-xs">
            <span className="text-text-secondary">{t("analytics:reports.dateRange.to")}</span>
            <DatePicker
              value={to}
              min={from}
              onChange={setTo}
              ariaLabel={t("analytics:reports.dateRange.to")}
              className="h-9 w-40 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
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

      {/* KPI cards */}
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
              label={t("analytics:adminRevenue.kpi.gross")}
              value={`$${fmtUsd(data.totalGrossUsd)}`}
              icon={Wallet}
              accent="brand"
              delta={{
                value: Math.round(data.monthOverMonthGrowth),
                label: t("analytics:reports.vsPriorPeriod"),
              }}
            />
            <StatCard
              label={t("analytics:adminRevenue.kpi.net")}
              value={`$${fmtUsd(data.totalPayeeNetUsd)}`}
              icon={TrendingUp}
              accent="success"
            />
            {/*
              StatCard's `delta` field appends a "%" suffix to its `value`, so
              passing a raw refund count (e.g. 5) rendered as "5%". Drop the
              delta — the refund-count column already shows the count.
            */}
            <StatCard
              label={t("analytics:adminRevenue.kpi.refundRate")}
              value={fmtPct(data.refundRate)}
              icon={RotateCcw}
              accent={data.refundRate > 0.05 ? "danger" : "neutral"}
            />
            <StatCard
              label={t("analytics:adminRevenue.kpi.growth")}
              value={`${data.monthOverMonthGrowth >= 0 ? "+" : ""}${data.monthOverMonthGrowth.toFixed(1)}%`}
              icon={TrendingUp}
              accent={data.monthOverMonthGrowth >= 0 ? "success" : "danger"}
            />
          </>
        )}
      </section>

      {/* Monthly bars + breakdown pie side-by-side */}
      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <ChartCard
            title={t("analytics:adminRevenue.monthlyChart.title")}
            subtitle={t("analytics:adminRevenue.monthlyChart.subtitle")}
            trailing={
              <LegendRow
                items={[
                  {
                    label: t("analytics:adminRevenue.monthlyChart.gross"),
                    colorClass: "text-brand-500",
                  },
                  {
                    label: t("analytics:adminRevenue.monthlyChart.refunded"),
                    colorClass: "text-danger-500",
                  },
                ]}
              />
            }
          >
            {isLoading || !data ? (
              <div className="h-48 animate-pulse rounded-lg bg-bg-subtle" />
            ) : data.byMonth.length === 0 ? (
              <p className="py-12 text-center text-sm text-text-tertiary">
                {t("analytics:reports.noData")}
              </p>
            ) : (
              <MonthlyBars
                months={data.byMonth}
                ariaLabel={t("analytics:adminRevenue.monthlyChart.title")}
                locale={locale}
              />
            )}
          </ChartCard>
        </div>

        <ChartCard
          title={t("analytics:adminRevenue.breakdownChart.title")}
          subtitle={t("analytics:adminRevenue.breakdownChart.subtitle")}
          trailing={<PieIcon aria-hidden className="size-4 text-text-tertiary" />}
        >
          {isLoading || !data ? (
            <div className="h-40 animate-pulse rounded-full bg-bg-subtle" />
          ) : (
            <RevenuePie
              bookings={data.bookingRevenueUsd}
              reviews={data.reviewRevenueUsd}
              bookingLabel={t("analytics:adminRevenue.breakdownChart.bookings")}
              reviewLabel={t("analytics:adminRevenue.breakdownChart.reviews")}
              locale={locale}
            />
          )}
        </ChartCard>
      </div>

      {/* Top consultants table */}
      <ChartCard
        title={t("analytics:adminRevenue.topConsultants.title")}
        subtitle={t("analytics:adminRevenue.topConsultants.subtitle")}
      >
        {isLoading || !data ? (
          <div className="space-y-2">
            {[0, 1, 2, 3, 4].map((i) => (
              <div key={i} className="h-9 animate-pulse rounded bg-bg-subtle" />
            ))}
          </div>
        ) : data.topConsultants.length === 0 ? (
          <p className="py-8 text-center text-sm text-text-tertiary">
            {t("analytics:reports.noData")}
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead className="text-xs uppercase tracking-wide text-text-tertiary">
                <tr className="border-b border-border-subtle">
                  <th className="px-2 py-2 text-start font-medium w-12">
                    {t("analytics:adminRevenue.topConsultants.rank")}
                  </th>
                  <th className="px-2 py-2 text-start font-medium">
                    {t("analytics:adminRevenue.topConsultants.name")}
                  </th>
                  <th className="px-2 py-2 text-end font-medium">
                    {t("analytics:adminRevenue.topConsultants.revenue")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.topConsultants.map((c, idx) => (
                  <tr
                    key={c.id}
                    className={cn(
                      "border-b border-border-subtle last:border-b-0",
                      idx === 0 && "bg-brand-50/30",
                    )}
                  >
                    <td className="px-2 py-2 font-semibold tabular-nums text-text-secondary">
                      {idx + 1}
                    </td>
                    <td className="px-2 py-2 font-medium text-text-primary">{c.name}</td>
                    <td className="px-2 py-2 text-end font-semibold tabular-nums text-text-primary">
                      ${fmtUsd(c.revenueUsd)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </ChartCard>
    </div>
  );
}
