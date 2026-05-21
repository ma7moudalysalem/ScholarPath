import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { TrendingUp, ListChecks } from "lucide-react";
import {
  adminApi,
  type ApplicationStatusPoint,
  type GrowthPoint,
} from "@/services/api/admin";
import {
  ChartCard,
  TimeRangeTabs,
  SmoothAreaChart,
  LegendRow,
} from "@/components/dashboard/primitives";
import { cn } from "@/lib/utils";

const WINDOWS = [7, 30, 90, 180] as const;
type Window = (typeof WINDOWS)[number];

function FunnelBars({ points }: { points: ApplicationStatusPoint[] }) {
  const { t } = useTranslation(["admin"]);
  if (points.length === 0) {
    return <div className="h-48 rounded-lg bg-bg-subtle/40" />;
  }
  const max = Math.max(...points.map((p) => p.count), 1);
  const colors = ["bg-brand-500", "bg-success-500", "bg-warning-500", "bg-danger-500", "bg-text-tertiary"];
  return (
    <div className="space-y-3">
      {points.map((p, idx) => (
        <div key={p.status} className="flex items-center gap-3">
          <span className="w-28 shrink-0 truncate text-xs font-medium text-text-secondary">
            {t(`admin:applicationStatus.${p.status}`, { defaultValue: p.status })}
          </span>
          <div className="relative h-2.5 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className={cn("h-full rounded-full transition-all", colors[idx % colors.length])}
              style={{ width: `${(p.count / max) * 100}%` }}
            />
          </div>
          <span className="w-12 shrink-0 text-end text-xs font-semibold tabular-nums text-text-primary">
            {p.count}
          </span>
        </div>
      ))}
    </div>
  );
}

export function AnalyticsPage() {
  const { t, i18n } = useTranslation(["admin", "analytics"]);
  const [windowDays, setWindowDays] = useState<Window>(30);

  const growth = useQuery<GrowthPoint[]>({
    queryKey: ["admin", "analytics", "user-growth", windowDays],
    queryFn: () => adminApi.userGrowth(windowDays),
  });

  const funnel = useQuery<ApplicationStatusPoint[]>({
    queryKey: ["admin", "analytics", "application-funnel"],
    queryFn: () => adminApi.applicationFunnel(),
  });

  const totalInWindow = useMemo(
    () => (growth.data ?? []).reduce((acc, p) => acc + p.count, 0),
    [growth.data],
  );

  const growthValues = useMemo(() => (growth.data ?? []).map((p) => p.count), [growth.data]);
  const growthLabels = useMemo(
    () =>
      (growth.data ?? []).map((p) => {
        const d = new Date(p.date);
        return d.toLocaleDateString(i18n.language === "ar" ? "ar-EG" : "en-US", {
          month: "short",
          day: "numeric",
        });
      }),
    [growth.data, i18n.language],
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
            {t("admin:analytics.title")}
          </h1>
          <p className="mt-1 max-w-2xl text-sm text-text-secondary">{t("admin:analytics.subtitle")}</p>
        </div>
        <div className="flex flex-col items-start gap-2 sm:items-end">
          <span className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
            {t("admin:analytics.window")}
          </span>
          <TimeRangeTabs
            value={windowDays}
            onChange={(v) => setWindowDays(v as Window)}
            options={WINDOWS.map((d) => ({ value: d, label: t(`admin:analytics.windows.${d}`) }))}
          />
        </div>
      </div>

      {/* Hero metric */}
      <section className="relative overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated p-6">
        <div className="orb orb-brand orb-animated -end-32 -top-32 size-72 opacity-25" />
        <div className="relative flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="flex items-center gap-2">
              <span className="flex size-8 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
                <TrendingUp aria-hidden className="size-4" />
              </span>
              <p className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
                {t("admin:analytics.totalInWindow")}
              </p>
            </div>
            <p className="mt-2 text-4xl font-bold tabular-nums tracking-tight text-text-primary">
              {totalInWindow.toLocaleString(i18n.language === "ar" ? "ar-EG" : "en-US")}
            </p>
            <p className="mt-1 text-sm text-text-secondary">
              {t(`admin:analytics.windows.${windowDays}`)}
            </p>
          </div>
        </div>
      </section>

      <ChartCard
        title={t("admin:analytics.growth")}
        subtitle={t("analytics:context.growthSubtitle")}
        trailing={<LegendRow items={[{ label: t("analytics:legend.growth"), colorClass: "text-brand-500" }]} />}
      >
        {growth.isLoading ? (
          <div className="h-48 animate-pulse rounded-lg bg-bg-subtle sm:h-56" />
        ) : (
          <SmoothAreaChart
            values={growthValues}
            labels={growthLabels}
            ariaLabel={t("admin:analytics.growth")}
          />
        )}
      </ChartCard>

      <ChartCard
        title={t("admin:analytics.funnel")}
        subtitle={t("analytics:context.funnelSubtitle")}
        trailing={
          <span className="inline-flex items-center gap-1.5 rounded-full bg-bg-subtle px-2.5 py-1 text-[11px] font-medium text-text-secondary">
            <ListChecks aria-hidden className="size-3" />
            {t("analytics:legend.applications")}
          </span>
        }
      >
        {funnel.isLoading ? (
          <div className="space-y-3">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-2.5 animate-pulse rounded-full bg-bg-subtle" />
            ))}
          </div>
        ) : (
          <FunnelBars points={funnel.data ?? []} />
        )}
      </ChartCard>
    </div>
  );
}
