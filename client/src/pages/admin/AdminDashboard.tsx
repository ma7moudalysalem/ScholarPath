import { useMemo, useState } from "react";
import { Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Users,
  ClipboardCheck,
  ArrowRight,
  ScrollText,
  Settings,
  BarChart2,
  Activity,
  CircleDollarSign,
  UserCheck,
  Clock,
  Cog,
  type LucideIcon,
} from "lucide-react";
import {
  adminApi,
  type AnalyticsOverviewDto,
  type ApplicationStatusPoint,
  type GrowthPoint,
} from "@/services/api/admin";
import {
  WelcomeBanner,
  StatCard,
  QuickActions,
  ChartCard,
  SmoothAreaChart,
  TimeRangeTabs,
  type StatAccent,
} from "@/components/dashboard/primitives";
import { formatRelativeTime } from "@/components/dashboard/utils";
import { cn } from "@/lib/utils";
import { useAuthStore } from "@/stores/authStore";

const AUDIT_ICON: Record<string, { icon: LucideIcon; accent: StatAccent }> = {
  UserStatusChange: { icon: UserCheck, accent: "warning" },
  UserRoleChange: { icon: UserCheck, accent: "brand" },
  ScholarshipApproved: { icon: ClipboardCheck, accent: "success" },
  ScholarshipRejected: { icon: ClipboardCheck, accent: "danger" },
  PaymentCaptured: { icon: CircleDollarSign, accent: "success" },
  PaymentRefunded: { icon: CircleDollarSign, accent: "warning" },
  ConfigChanged: { icon: Settings, accent: "brand" },
  BroadcastSent: { icon: Activity, accent: "brand" },
};

function formatCents(cents: number): string {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(cents / 100);
}

interface FunnelBarProps {
  points: ApplicationStatusPoint[];
}

function FunnelBars({ points }: FunnelBarProps) {
  const { t } = useTranslation(["admin"]);
  if (points.length === 0) return null;
  const max = Math.max(...points.map((p) => p.count), 1);
  const colors = ["bg-brand-500", "bg-success-500", "bg-warning-500", "bg-danger-500", "bg-text-tertiary"];
  return (
    <div className="space-y-3">
      {points.map((p, idx) => (
        <div key={p.status} className="flex items-center gap-3">
          <span className="w-28 shrink-0 truncate text-xs font-medium text-text-secondary">
            {t(`admin:applicationStatus.${p.status}`, { defaultValue: p.status })}
          </span>
          <div className="relative h-2 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className={cn("h-full rounded-full transition-all", colors[idx % colors.length])}
              style={{ width: `${(p.count / max) * 100}%` }}
            />
          </div>
          <span className="w-10 shrink-0 text-end text-xs font-semibold tabular-nums text-text-primary">
            {p.count}
          </span>
        </div>
      ))}
    </div>
  );
}

function greetingKey(): "morning" | "afternoon" | "evening" {
  const h = new Date().getHours();
  if (h < 12) return "morning";
  if (h < 18) return "afternoon";
  return "evening";
}

export function AdminDashboard() {
  const { t, i18n } = useTranslation(["admin", "dashboard"]);
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");

  // Period toggle for the user-growth chart (7 / 30 / 90 days). The day count
  // is part of the query key so switching ranges refetches + caches per range.
  const [growthDays, setGrowthDays] = useState(30);

  const overview = useQuery<AnalyticsOverviewDto>({
    queryKey: ["admin", "analytics", "overview"],
    queryFn: () => adminApi.analyticsOverview(),
  });

  const growth = useQuery<GrowthPoint[]>({
    queryKey: ["admin", "analytics", "user-growth", growthDays],
    queryFn: () => adminApi.userGrowth(growthDays),
  });

  const funnel = useQuery<ApplicationStatusPoint[]>({
    queryKey: ["admin", "analytics", "application-funnel"],
    queryFn: () => adminApi.applicationFunnel(),
  });

  const audit = useQuery({
    queryKey: ["admin", "audit-log", "recent"],
    queryFn: () => adminApi.getAuditLog({ page: 1, pageSize: 5 }),
    staleTime: 30_000,
  });

  const o = overview.data;
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
      <WelcomeBanner
        eyebrow={t(`dashboard:greeting.${greetingKey()}`, { name: firstName || t("admin:title") })}
        title={
          <>
            {t("dashboard:admin.headlinePrefix")}{" "}
            <span className="text-gradient">{t("dashboard:admin.headlineSuffix")}</span>
          </>
        }
        subtitle={t("dashboard:admin.banner.subtitle")}
        actions={
          <>
            <Link to="/admin/analytics" className="btn btn-primary">
              {t("dashboard:admin.exploreBtn")}
              <ArrowRight aria-hidden className="size-4 rtl:rotate-180" />
            </Link>
            <Link to="/admin/users" className="btn btn-secondary">
              {t("dashboard:admin.secondaryBtn")}
            </Link>
          </>
        }
      />

      {overview.isLoading || !o ? (
        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-32 animate-pulse rounded-2xl bg-bg-subtle" />
          ))}
        </div>
      ) : (
        <>
          {/* Top-line stats (4 hero cards). Hardcoded mock deltas and trends
              are removed so the admin never sees fabricated growth numbers. */}
          <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
            <StatCard
              label={t("dashboard:admin.stats.users")}
              value={o.totalUsers}
              to="/admin/users"
              icon={Users}
              accent="brand"
              delay={0.02}
            />
            <StatCard
              label={t("dashboard:admin.stats.active")}
              value={o.activeUsers}
              icon={Activity}
              accent="success"
              delay={0.06}
            />
            <StatCard
              label={t("dashboard:admin.stats.pending")}
              value={o.pendingApprovals}
              to="/admin/onboarding"
              icon={Clock}
              accent={o.pendingApprovals > 0 ? "warning" : "neutral"}
              delay={0.1}
            />
            <StatCard
              label={t("dashboard:admin.stats.revenue")}
              value={formatCents(o.revenueCentsCaptured)}
              to="/admin/payments"
              icon={CircleDollarSign}
              accent="success"
              delay={0.14}
            />
          </section>

          {/* Secondary KPIs */}
          <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-6">
            {[
              { label: t("admin:dashboard.totalScholarships"), value: o.totalScholarships },
              { label: t("admin:dashboard.openScholarships"), value: o.openScholarships },
              { label: t("admin:dashboard.submittedApplications"), value: o.submittedApplications },
              { label: t("admin:dashboard.completedBookings"), value: o.completedBookings },
              { label: t("admin:dashboard.aiInteractions24h"), value: o.aiInteractions24h },
              { label: t("admin:dashboard.profitShare"), value: formatCents(o.profitShareCentsAccumulated) },
            ].map((k) => (
              <div
                key={k.label}
                className="rounded-2xl border border-border-subtle bg-bg-elevated p-4 transition-colors hover:border-border-default"
              >
                <div className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
                  {k.label}
                </div>
                <div className="mt-1 text-xl font-bold tabular-nums text-text-primary">{k.value}</div>
              </div>
            ))}
          </section>

          {/* Main grid */}
          <div className="grid gap-6 lg:grid-cols-12">
            {/* Charts column */}
            <div className="space-y-6 lg:col-span-8">
              <ChartCard
                title={t("dashboard:admin.charts.growthTitle")}
                subtitle={t("dashboard:admin.charts.growthSubtitle")}
                trailing={
                  <TimeRangeTabs
                    value={growthDays}
                    onChange={setGrowthDays}
                    ariaLabel={t("dashboard:admin.charts.growthTitle")}
                    options={[
                      { value: 7, label: t("dashboard:ranges.7d") },
                      { value: 30, label: t("dashboard:ranges.30d") },
                      { value: 90, label: t("dashboard:ranges.90d") },
                    ]}
                  />
                }
              >
                {growth.isLoading ? (
                  <div className="h-48 animate-pulse rounded-lg bg-bg-subtle sm:h-56" />
                ) : (
                  <SmoothAreaChart
                    values={growthValues}
                    labels={growthLabels}
                    ariaLabel={t("dashboard:admin.charts.growthTitle")}
                  />
                )}
              </ChartCard>

              <ChartCard
                title={t("dashboard:admin.charts.funnelTitle")}
                subtitle={t("dashboard:admin.charts.funnelSubtitle")}
              >
                {funnel.isLoading ? (
                  <div className="h-32 animate-pulse rounded-lg bg-bg-subtle" />
                ) : (
                  <FunnelBars points={funnel.data ?? []} />
                )}
              </ChartCard>
            </div>

            {/* Sidebar column */}
            <aside className="space-y-6 lg:col-span-4">
              <QuickActions
                title={t("dashboard:quickActions.title")}
                actions={[
                  { icon: Users, label: t("dashboard:admin.quick.users"), to: "/admin/users", accent: "brand" },
                  { icon: ClipboardCheck, label: t("dashboard:admin.quick.approvals"), to: "/admin/onboarding", accent: "warning" },
                  { icon: ScrollText, label: t("dashboard:admin.quick.audit"), to: "/admin/audit-log", accent: "neutral" },
                  { icon: Cog, label: t("dashboard:admin.quick.settings"), to: "/admin/settings", accent: "success" },
                ]}
              />

              {/* Audit log activity feed */}
              <section className="card-premium p-5 sm:p-6">
                <header className="mb-4 flex items-center justify-between">
                  <h2 className="text-sm font-semibold text-text-primary">
                    {t("dashboard:activity.title")}
                  </h2>
                  <Link
                    to="/admin/audit-log"
                    className="text-xs font-medium text-brand-600 transition-colors hover:text-brand-700 hover:underline"
                  >
                    {t("dashboard:activity.viewAll")}
                  </Link>
                </header>
                {audit.isLoading ? (
                  <ul className="space-y-3">
                    {[1, 2, 3].map((i) => (
                      <li key={i} className="flex gap-3">
                        <div className="size-8 shrink-0 animate-pulse rounded-full bg-bg-subtle" />
                        <div className="flex-1 space-y-2 pt-1">
                          <div className="h-3 w-3/4 animate-pulse rounded bg-bg-subtle" />
                          <div className="h-2.5 w-1/3 animate-pulse rounded bg-bg-subtle" />
                        </div>
                      </li>
                    ))}
                  </ul>
                ) : !audit.data || audit.data.items.length === 0 ? (
                  <div className="rounded-xl border border-dashed border-border-subtle bg-bg-subtle/30 p-6 text-center">
                    <p className="text-sm font-medium text-text-primary">
                      {t("dashboard:activity.emptyTitle")}
                    </p>
                    <p className="mt-1 text-xs text-text-tertiary">{t("dashboard:activity.emptyBody")}</p>
                  </div>
                ) : (
                  <ul className="space-y-3">
                    {audit.data.items.map((row) => {
                      const meta = AUDIT_ICON[row.action] ?? { icon: ScrollText, accent: "brand" as StatAccent };
                      const Icon = meta.icon;
                      const colors: Record<StatAccent, string> = {
                        brand: "bg-brand-50 text-brand-600",
                        warning: "bg-warning-50 text-warning-600",
                        success: "bg-success-50 text-success-600",
                        danger: "bg-danger-50 text-danger-500",
                        neutral: "bg-bg-subtle text-text-secondary",
                      };
                      return (
                        <li key={row.id} className="flex gap-3">
                          <div
                            className={cn(
                              "flex size-8 shrink-0 items-center justify-center rounded-full",
                              colors[meta.accent],
                            )}
                          >
                            <Icon aria-hidden className="size-4" />
                          </div>
                          <div className="min-w-0 flex-1">
                            <p className="line-clamp-2 text-sm text-text-primary">
                              <span className="font-medium">
                                {t(`admin:audit.actions.${row.action}`, { defaultValue: row.action })}
                              </span>
                              {row.summary && <span className="text-text-secondary"> · {row.summary}</span>}
                            </p>
                            <p className="mt-0.5 text-xs text-text-tertiary">
                              {row.actorEmail ? `${row.actorEmail} · ` : ""}
                              {formatRelativeTime(row.occurredAt, i18n.language)}
                            </p>
                          </div>
                        </li>
                      );
                    })}
                  </ul>
                )}
              </section>

              {/* Tip card */}
              <section className="relative overflow-hidden rounded-2xl border border-brand-200 bg-gradient-to-br from-brand-50 to-bg-elevated p-5">
                <div className="orb orb-brand absolute -end-12 -top-12 size-32 opacity-20" />
                <div className="relative">
                  <div className="mb-2 flex size-8 items-center justify-center rounded-xl bg-brand-500 text-white">
                    <BarChart2 aria-hidden className="size-4" />
                  </div>
                  <h3 className="text-sm font-semibold text-text-primary">{t("admin:analytics.title")}</h3>
                  <p className="mt-1 text-xs text-text-secondary">{t("admin:analytics.subtitle")}</p>
                  <Link
                    to="/admin/analytics"
                    className="mt-3 inline-flex items-center gap-1 text-xs font-semibold text-brand-600 hover:underline"
                  >
                    {t("dashboard:admin.exploreBtn")}
                    <ArrowRight aria-hidden className="size-3 rtl:rotate-180" />
                  </Link>
                </div>
              </section>
            </aside>
          </div>
        </>
      )}
    </div>
  );
}
