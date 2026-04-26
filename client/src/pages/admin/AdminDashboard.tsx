import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { adminApi, type AnalyticsOverviewDto, type ApplicationStatusPoint, type GrowthPoint } from "@/services/api/admin";

interface KpiProps {
  label: string;
  value: string | number;
  accent?: "brand" | "success" | "warning";
}

function Kpi({ label, value, accent = "brand" }: KpiProps) {
  const color =
    accent === "success" ? "text-emerald-500" : accent === "warning" ? "text-amber-500" : "text-brand-500";
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-elevated p-4">
      <div className="text-xs font-medium uppercase tracking-wide text-text-tertiary">{label}</div>
      <div className={`mt-1 text-2xl font-semibold ${color}`}>{value}</div>
    </div>
  );
}

function formatCents(cents: number): string {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(cents / 100);
}

function SparkLine({ points }: { points: GrowthPoint[] }) {
  if (points.length < 2) return null;
  const max = Math.max(...points.map((p) => p.count), 1);
  const w = 100;
  const h = 40;
  const step = w / (points.length - 1);
  const path = points
    .map((p, i) => `${i === 0 ? "M" : "L"} ${(i * step).toFixed(2)} ${(h - (p.count / max) * h).toFixed(2)}`)
    .join(" ");
  return (
    <svg viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none" className="h-24 w-full">
      <path d={path} fill="none" stroke="currentColor" strokeWidth="1.5" className="text-brand-500" />
    </svg>
  );
}

function FunnelBars({ points }: { points: ApplicationStatusPoint[] }) {
  const max = Math.max(...points.map((p) => p.count), 1);
  return (
    <div className="space-y-2">
      {points.map((p) => (
        <div key={p.status} className="flex items-center gap-3">
          <span className="w-28 text-xs text-text-secondary">{p.status}</span>
          <div className="h-3 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className="h-full bg-brand-500/70"
              style={{ width: `${(p.count / max) * 100}%` }}
            />
          </div>
          <span className="w-10 text-end text-xs font-medium tabular-nums">{p.count}</span>
        </div>
      ))}
    </div>
  );
}

export function AdminDashboard() {
  const { t } = useTranslation(["admin"]);

  const overview = useQuery<AnalyticsOverviewDto>({
    queryKey: ["admin", "analytics", "overview"],
    queryFn: () => adminApi.analyticsOverview(),
  });

  const growth = useQuery<GrowthPoint[]>({
    queryKey: ["admin", "analytics", "user-growth", 30],
    queryFn: () => adminApi.userGrowth(30),
  });

  const funnel = useQuery<ApplicationStatusPoint[]>({
    queryKey: ["admin", "analytics", "application-funnel"],
    queryFn: () => adminApi.applicationFunnel(),
  });

  const o = overview.data;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight">{t("admin:nav.dashboard")}</h1>

      {overview.isLoading || !o ? (
        <div className="text-text-tertiary">{t("admin:common.loading")}</div>
      ) : (
        <>
          <section className="grid grid-cols-2 gap-3 md:grid-cols-4">
            <Kpi label={t("admin:dashboard.totalUsers")} value={o.totalUsers} />
            <Kpi label={t("admin:dashboard.activeUsers")} value={o.activeUsers} accent="success" />
            <Kpi label={t("admin:dashboard.pendingApprovals")} value={o.pendingApprovals} accent="warning" />
            <Kpi label={t("admin:dashboard.aiInteractions24h")} value={o.aiInteractions24h} />
            <Kpi label={t("admin:dashboard.totalScholarships")} value={o.totalScholarships} />
            <Kpi label={t("admin:dashboard.openScholarships")} value={o.openScholarships} />
            <Kpi label={t("admin:dashboard.submittedApplications")} value={o.submittedApplications} />
            <Kpi label={t("admin:dashboard.completedBookings")} value={o.completedBookings} />
            <Kpi label={t("admin:dashboard.revenue")} value={formatCents(o.revenueCentsCaptured)} accent="success" />
            <Kpi label={t("admin:dashboard.profitShare")} value={formatCents(o.profitShareCentsAccumulated)} accent="brand" />
          </section>

          <section className="grid gap-4 lg:grid-cols-2">
            <div className="rounded-lg border border-border-subtle bg-bg-elevated p-4">
              <h2 className="mb-3 text-sm font-semibold">{t("admin:dashboard.userGrowth")}</h2>
              {growth.data && growth.data.length > 0 ? (
                <SparkLine points={growth.data} />
              ) : (
                <div className="h-24 animate-pulse rounded bg-bg-subtle" />
              )}
            </div>
            <div className="rounded-lg border border-border-subtle bg-bg-elevated p-4">
              <h2 className="mb-3 text-sm font-semibold">{t("admin:dashboard.applicationFunnel")}</h2>
              {funnel.data && funnel.data.length > 0 ? (
                <FunnelBars points={funnel.data} />
              ) : (
                <div className="h-24 animate-pulse rounded bg-bg-subtle" />
              )}
            </div>
          </section>
        </>
      )}
    </div>
  );
}
