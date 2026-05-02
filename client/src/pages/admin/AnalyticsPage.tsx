import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  adminApi,
  type ApplicationStatusPoint,
  type GrowthPoint,
} from "@/services/api/admin";

const WINDOWS = [7, 30, 90, 180] as const;
type Window = (typeof WINDOWS)[number];

function AreaChart({ points }: { points: GrowthPoint[] }) {
  if (points.length < 2) return <div className="h-48 rounded bg-bg-subtle/50" />;
  const max = Math.max(...points.map((p) => p.count), 1);
  const w = 800;
  const h = 180;
  const step = w / (points.length - 1);

  const linePath = points
    .map((p, i) => `${i === 0 ? "M" : "L"} ${(i * step).toFixed(2)} ${(h - (p.count / max) * (h - 10) - 5).toFixed(2)}`)
    .join(" ");
  const areaPath = `${linePath} L ${w} ${h} L 0 ${h} Z`;

  // 5 horizontal gridlines
  const gridY = [0.25, 0.5, 0.75, 1.0].map((r) => ((h - 10) * r + 5).toFixed(2));

  return (
    <svg viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none" className="h-48 w-full text-brand-500">
      {gridY.map((y) => (
        <line key={y} x1="0" y1={y} x2={w} y2={y} stroke="currentColor" strokeOpacity="0.08" strokeWidth="0.5" />
      ))}
      <path d={areaPath} fill="currentColor" fillOpacity="0.12" />
      <path d={linePath} fill="none" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  );
}

function FunnelBars({ points }: { points: ApplicationStatusPoint[] }) {
  if (points.length === 0) {
    return <div className="h-48 rounded bg-bg-subtle/50" />;
  }
  const max = Math.max(...points.map((p) => p.count), 1);
  return (
    <div className="space-y-2">
      {points.map((p) => (
        <div key={p.status} className="flex items-center gap-3">
          <span className="w-28 text-xs text-text-secondary">{p.status}</span>
          <div className="h-3 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div className="h-full bg-brand-500/70" style={{ width: `${(p.count / max) * 100}%` }} />
          </div>
          <span className="w-10 text-end text-xs font-medium tabular-nums">{p.count}</span>
        </div>
      ))}
    </div>
  );
}

export function AnalyticsPage() {
  const { t } = useTranslation(["admin"]);
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

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("admin:analytics.title")}</h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">{t("admin:analytics.subtitle")}</p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <span className="text-sm text-text-secondary">{t("admin:analytics.window")}:</span>
        <div className="inline-flex rounded-md border border-border-subtle bg-bg-elevated p-0.5">
          {WINDOWS.map((d) => (
            <button
              key={d}
              type="button"
              onClick={() => setWindowDays(d)}
              className={`rounded px-3 py-1 text-xs font-medium transition ${
                windowDays === d ? "bg-brand-500 text-text-on-brand" : "text-text-secondary hover:text-text-primary"
              }`}
            >
              {t(`admin:analytics.windows.${d}`)}
            </button>
          ))}
        </div>
      </div>

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <div className="mb-4 flex items-baseline justify-between">
          <h2 className="text-sm font-semibold">{t("admin:analytics.growth")}</h2>
          <div className="text-xs text-text-tertiary">
            {t("admin:analytics.totalInWindow")}:{" "}
            <span className="font-semibold tabular-nums text-text-primary">{totalInWindow}</span>
          </div>
        </div>
        {growth.isLoading ? (
          <div className="h-48 animate-pulse rounded bg-bg-subtle" />
        ) : (
          <AreaChart points={growth.data ?? []} />
        )}
      </section>

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-4 text-sm font-semibold">{t("admin:analytics.funnel")}</h2>
        {funnel.isLoading ? (
          <div className="h-48 animate-pulse rounded bg-bg-subtle" />
        ) : (
          <FunnelBars points={funnel.data ?? []} />
        )}
      </section>
    </div>
  );
}
