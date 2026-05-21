import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { RefreshCw } from "lucide-react";
import { analyticsApi, type StudentJourneyDto } from "@/services/api/analytics";

// ── Skeleton helpers ────────────────────────────────────────────────────────

function SkeletonCard() {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-elevated p-5 space-y-3">
      <div className="h-3 w-24 animate-pulse rounded bg-bg-subtle" />
      <div className="h-7 w-16 animate-pulse rounded bg-bg-subtle" />
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

// ── Application funnel ──────────────────────────────────────────────────────

interface FunnelStep {
  label: string;
  count: number;
  pct: number;
}

function ApplicationFunnel({ steps }: { steps: FunnelStep[] }) {
  const maxCount = Math.max(...steps.map((s) => s.count), 1);

  return (
    <div className="flex items-end gap-4">
      {steps.map((step, idx) => {
        const barH = Math.max((step.count / maxCount) * 120, 8);
        const isLast = idx === steps.length - 1;
        return (
          <div key={step.label} className="flex flex-1 flex-col items-center gap-2">
            {/* bar */}
            <div className="flex w-full flex-col items-center justify-end" style={{ height: 128 }}>
              <div
                className="w-full rounded-t-md bg-brand-500/70 transition-all"
                style={{ height: barH }}
              />
            </div>
            {/* count */}
            <span className="text-xl font-bold tabular-nums text-text-primary">{step.count}</span>
            {/* label */}
            <span className="text-center text-xs text-text-secondary">{step.label}</span>
            {/* percentage */}
            {!isLast && (
              <span className="text-xs tabular-nums text-text-tertiary">{step.pct.toFixed(0)}%</span>
            )}
          </div>
        );
      })}
    </div>
  );
}

// ── Onboarding badge ────────────────────────────────────────────────────────

function OnboardingBadge({ complete, label }: { complete: boolean; label: string }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-sm font-medium ${
        complete
          ? "bg-success-100 text-success-700"
          : "bg-warning-100 text-warning-700"
      }`}
    >
      {complete ? "✓" : "⏳"} {label}
    </span>
  );
}

// ── Main page ───────────────────────────────────────────────────────────────

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

  const funnelSteps: FunnelStep[] = data
    ? (() => {
        const total = Math.max(data.totalApplications, 1);
        return [
          { label: t("analytics:studentJourney.totalApplications"), count: data.totalApplications, pct: 100 },
          { label: t("analytics:studentJourney.submitted"), count: data.submittedApplications, pct: (data.submittedApplications / total) * 100 },
          { label: t("analytics:studentJourney.accepted"), count: data.acceptedApplications, pct: (data.acceptedApplications / total) * 100 },
        ];
      })()
    : [];

  return (
    <div className="space-y-6">
      {/* Heading */}
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("analytics:studentJourney.title")}
        </h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">
          {t("analytics:studentJourney.subtitle")}
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
            <KpiCard label={t("analytics:studentJourney.totalApplications")} value={data.totalApplications} />
            <KpiCard label={t("analytics:studentJourney.submitted")} value={data.submittedApplications} />
            <KpiCard label={t("analytics:studentJourney.accepted")} value={data.acceptedApplications} />
            <KpiCard label={t("analytics:studentJourney.bookings")} value={data.totalBookings} sub={`${data.completedBookings} ${t("analytics:studentJourney.completedBookings")}`} />
          </>
        ) : null}
      </div>

      {/* Application funnel */}
      {!isError && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-4 text-sm font-semibold">{t("analytics:studentJourney.applicationFunnel")}</h2>
          {isLoading ? (
            <div className="flex items-end gap-4">
              {[128, 96, 64].map((h, i) => (
                <div key={i} className="flex flex-1 flex-col items-center gap-2" style={{ height: 160 }}>
                  <div className="mt-auto w-full animate-pulse rounded-t-md bg-bg-subtle" style={{ height: h }} />
                  <div className="h-3 w-10 animate-pulse rounded bg-bg-subtle" />
                  <div className="h-3 w-20 animate-pulse rounded bg-bg-subtle" />
                </div>
              ))}
            </div>
          ) : (
            <ApplicationFunnel steps={funnelSteps} />
          )}
        </section>
      )}

      {/* Last activity + onboarding badge */}
      {!isError && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-4 text-sm font-semibold">{t("analytics:studentJourney.lastApplication")}</h2>
          {isLoading ? (
            <div className="space-y-3">
              <div className="h-4 w-64 animate-pulse rounded bg-bg-subtle" />
              <div className="h-4 w-48 animate-pulse rounded bg-bg-subtle" />
              <div className="h-6 w-40 animate-pulse rounded-full bg-bg-subtle" />
            </div>
          ) : data ? (
            <div className="space-y-3">
              <div className="flex items-baseline gap-2 text-sm">
                <span className="font-medium text-text-secondary">{t("analytics:studentJourney.lastApplication")}:</span>
                <span className="tabular-nums text-text-primary">{formatDate(data.lastApplicationAt)}</span>
              </div>
              <div className="flex items-baseline gap-2 text-sm">
                <span className="font-medium text-text-secondary">{t("analytics:studentJourney.lastBooking")}:</span>
                <span className="tabular-nums text-text-primary">{formatDate(data.lastBookingAt)}</span>
              </div>
              <div className="pt-1">
                <OnboardingBadge
                  complete={data.onboardingComplete}
                  label={
                    data.onboardingComplete
                      ? t("analytics:studentJourney.onboardingComplete")
                      : t("analytics:studentJourney.onboardingPending")
                  }
                />
              </div>
            </div>
          ) : null}
        </section>
      )}
    </div>
  );
}
