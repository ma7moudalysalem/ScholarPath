import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  RefreshCw,
  Trophy,
  FileText,
  CheckCircle2,
  CalendarCheck,
  Sparkles,
  Clock,
} from "lucide-react";
import { analyticsApi, type StudentJourneyDto } from "@/services/api/analytics";
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

// ─── Smooth funnel (vertical bars) ───────────────────────────────────────────

interface FunnelStep {
  label: string;
  count: number;
  pct: number;
  color: string;
}

function GradientFunnel({ steps, locale }: { steps: FunnelStep[]; locale: string }) {
  const maxCount = Math.max(...steps.map((s) => s.count), 1);
  const percentFormatter = new Intl.NumberFormat(locale, {
    style: "percent",
    maximumFractionDigits: 0,
  });
  return (
    <div className="grid grid-cols-3 gap-4 sm:gap-6">
      {steps.map((step, idx) => {
        const heightPct = (step.count / maxCount) * 100;
        const isLast = idx === steps.length - 1;
        return (
          <div key={step.label} className="flex flex-col items-center gap-3">
            <div className="relative flex h-44 w-full items-end justify-center rounded-xl bg-bg-subtle/40 p-2">
              <div
                className={cn("w-full rounded-lg transition-all duration-500", step.color)}
                style={{ height: `${Math.max(heightPct, 6)}%`, minHeight: 8 }}
              />
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold tabular-nums text-text-primary">
                {step.count.toLocaleString(locale)}
              </p>
              <p className="mt-0.5 text-xs font-medium text-text-secondary">{step.label}</p>
              {!isLast && (
                <p className="mt-1 text-xs tabular-nums text-text-tertiary">
                  {percentFormatter.format(step.pct / 100)}
                </p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

// ─── Page ────────────────────────────────────────────────────────────────────

/** PB-015 T-011 — Student Self-Analytics dashboard (custom SVG charts). */
export function StudentAnalytics() {
  const { t, i18n } = useTranslation(["analytics"]);

  const { data, isLoading, isError, refetch } = useQuery<StudentJourneyDto>({
    queryKey: ["analytics", "student-journey"],
    queryFn: () => analyticsApi.getStudentJourney(),
  });

  const dateLocale = i18n.language === "ar" ? "ar-EG" : "en-US";

  function formatDate(iso: string | null): string {
    if (!iso) return t("analytics:studentJourney.never");
    return new Date(iso).toLocaleDateString(dateLocale, {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  }

  const funnelSteps = useMemo<FunnelStep[]>(() => {
    if (!data) return [];
    const total = Math.max(data.totalApplications, 1);
    return [
      {
        label: t("analytics:studentJourney.totalApplications"),
        count: data.totalApplications,
        pct: 100,
        color: "bg-brand-500/80",
      },
      {
        label: t("analytics:studentJourney.submitted"),
        count: data.submittedApplications,
        pct: (data.submittedApplications / total) * 100,
        color: "bg-success-500/80",
      },
      {
        label: t("analytics:studentJourney.accepted"),
        count: data.acceptedApplications,
        pct: (data.acceptedApplications / total) * 100,
        color: "bg-warning-500/80",
      },
    ];
  }, [data, t]);

  return (
    <div className="space-y-6">
      {/* Header banner */}
      <section className="relative overflow-hidden rounded-3xl border border-border-subtle bg-bg-elevated p-6 sm:p-8">
        <div className="orb orb-brand orb-animated -end-24 -top-24 size-72 opacity-30" />
        <div className="orb orb-aurora -start-32 -bottom-32 size-80 opacity-20" />
        <div className="relative z-10 flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="mb-2 flex size-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
              <Trophy aria-hidden className="size-4" />
            </div>
            <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
              {t("analytics:studentJourney.title")}
            </h1>
            <p className="mt-1 max-w-2xl text-sm text-text-secondary">
              {t("analytics:studentJourney.subtitle")}
            </p>
          </div>
          {data?.onboardingComplete && (
            <span className="inline-flex items-center gap-1.5 rounded-full bg-success-100 px-3 py-1.5 text-xs font-semibold text-success-700">
              <CheckCircle2 aria-hidden className="size-3.5" />
              {t("analytics:studentJourney.onboardingComplete")}
            </span>
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
              label={t("analytics:studentJourney.totalApplications")}
              value={data.totalApplications}
              icon={FileText}
              accent="brand"
              delay={0.02}
            />
            <StatCard
              label={t("analytics:studentJourney.submitted")}
              value={data.submittedApplications}
              icon={CheckCircle2}
              accent="success"
              delay={0.06}
            />
            <StatCard
              label={t("analytics:studentJourney.accepted")}
              value={data.acceptedApplications}
              icon={Sparkles}
              accent="warning"
              delay={0.1}
            />
            <StatCard
              label={t("analytics:studentJourney.bookings")}
              value={data.totalBookings}
              icon={CalendarCheck}
              accent="brand"
              delta={
                data.completedBookings > 0
                  ? {
                      value: Math.round((data.completedBookings / Math.max(data.totalBookings, 1)) * 100),
                      label: t("analytics:studentJourney.completedBookings"),
                    }
                  : null
              }
              delay={0.14}
            />
          </>
        ) : null}
      </section>

      {/* Application funnel */}
      {!isError && (
        <ChartCard
          title={t("analytics:studentJourney.applicationFunnel")}
          subtitle={t("analytics:context.funnelSubtitle")}
          trailing={
            <LegendRow
              items={[
                { label: t("analytics:studentJourney.totalApplications"), colorClass: "text-brand-500" },
                { label: t("analytics:studentJourney.submitted"), colorClass: "text-success-500" },
                { label: t("analytics:studentJourney.accepted"), colorClass: "text-warning-500" },
              ]}
            />
          }
        >
          {isLoading ? (
            <div className="grid grid-cols-3 gap-4">
              {[80, 60, 40].map((h, i) => (
                <div key={i} className="space-y-2">
                  <div className="h-44 animate-pulse rounded-xl bg-bg-subtle" style={{ height: `${h + 80}px` }} />
                  <div className="mx-auto h-5 w-12 animate-pulse rounded bg-bg-subtle" />
                  <div className="mx-auto h-3 w-20 animate-pulse rounded bg-bg-subtle" />
                </div>
              ))}
            </div>
          ) : (
            <GradientFunnel steps={funnelSteps} locale={dateLocale} />
          )}
        </ChartCard>
      )}

      {/* Last-activity timeline */}
      {!isError && data && (
        <ChartCard
          title={t("analytics:studentJourney.lastApplication")}
          subtitle={
            data.onboardingComplete
              ? t("analytics:studentJourney.onboardingComplete")
              : t("analytics:studentJourney.onboardingPending")
          }
        >
          {isLoading ? (
            <div className="space-y-3">
              <div className="h-4 w-64 animate-pulse rounded bg-bg-subtle" />
              <div className="h-4 w-48 animate-pulse rounded bg-bg-subtle" />
            </div>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="flex items-start gap-3 rounded-xl border border-border-subtle bg-bg-subtle/30 p-4">
                <div className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
                  <FileText aria-hidden className="size-4" />
                </div>
                <div className="min-w-0">
                  <p className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
                    {t("analytics:studentJourney.lastApplication")}
                  </p>
                  <p className="mt-1 text-sm font-semibold tabular-nums text-text-primary">
                    {formatDate(data.lastApplicationAt)}
                  </p>
                </div>
              </div>
              <div className="flex items-start gap-3 rounded-xl border border-border-subtle bg-bg-subtle/30 p-4">
                <div className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-success-50 text-success-600">
                  <Clock aria-hidden className="size-4" />
                </div>
                <div className="min-w-0">
                  <p className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
                    {t("analytics:studentJourney.lastBooking")}
                  </p>
                  <p className="mt-1 text-sm font-semibold tabular-nums text-text-primary">
                    {formatDate(data.lastBookingAt)}
                  </p>
                </div>
              </div>
            </div>
          )}
        </ChartCard>
      )}
    </div>
  );
}
