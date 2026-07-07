import { useMemo } from 'react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Star,
  ListChecks,
  Briefcase,
  Receipt,
  BarChart2,
  Plus,
  ArrowRight,
  Users,
  Clock,
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { usePaymentsEnabled } from '@/hooks/usePlatformStatus';
import { scholarshipProviderReviewsApi } from '@/services/api/scholarshipProviderReviews';
import { scholarshipsApi } from '@/services/api/scholarships';
import { applicationsApi } from '@/services/api/applications';
import { EmptyState } from '@/components/common/EmptyState';
import { formatDate } from '@/lib/bookingFormat';
import {
  WelcomeBanner,
  StatCard,
  QuickActions,
  ChartCard,
  DonutChart,
  StatusPill,
  type DonutSegment,
  type StatAccent,
} from '@/components/dashboard/primitives';
import { formatRelativeTime } from '@/components/dashboard/utils';

export interface ScholarshipProviderReviewRow {
  reviewId: string;
  studentId: string;
  studentName: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface ScholarshipProviderRatingsSummaryDto {
  scholarshipProviderId: string;
  averageRating: number;
  totalRatings: number;
  recentReviews: ScholarshipProviderReviewRow[];
}

// Status buckets shown in the "Applications by status" donut, in order.
// Shortlisted MUST be included — a shortlisted application is a real, non-terminal
// state; omitting it dropped those rows from the chart and the pending count.
const COMPANY_STATUS_ORDER = ['Applied', 'Pending', 'UnderReview', 'WaitingResult', 'Shortlisted', 'Accepted', 'Rejected'];

// Per-status visual encoding: a pill tone (background chip) + a donut color
// (design-system status token). Mirrors StudentDashboard's STATUS_META so a given
// status reads the same everywhere (donut slice color == row pill tone). Draft /
// Withdrawn are here purely so recent-row pills resolve a tone if a row lands in
// one of those states — the donut only iterates COMPANY_STATUS_ORDER.
const STATUS_META: Record<string, { tone: StatAccent; color: string }> = {
  Applied:       { tone: 'brand',   color: 'var(--color-status-applied)' },
  Pending:       { tone: 'warning', color: 'var(--color-status-pending)' },
  UnderReview:   { tone: 'warning', color: 'var(--color-brand-400)' },
  Shortlisted:   { tone: 'brand',   color: 'var(--color-status-planned)' },
  WaitingResult: { tone: 'warning', color: 'var(--color-warning-500)' },
  Accepted:      { tone: 'success', color: 'var(--color-status-accepted)' },
  Rejected:      { tone: 'danger',  color: 'var(--color-status-rejected)' },
  Withdrawn:     { tone: 'neutral', color: 'var(--color-status-withdrawn)' },
  Draft:         { tone: 'neutral', color: 'var(--color-status-withdrawn)' },
};

function greetingKey(): 'morning' | 'afternoon' | 'evening' {
  const h = new Date().getHours();
  if (h < 12) return 'morning';
  if (h < 18) return 'afternoon';
  return 'evening';
}

export function ScholarshipProviderDashboard() {
  const { t, i18n } = useTranslation(['company', 'dashboard', 'applications']);
  const user = useAuthStore((state) => state.user);
  const firstName = user?.firstName ?? '';
  const paymentsEnabled = usePaymentsEnabled();

  const { data: ratings, isLoading: ratingsLoading } = useQuery({
    queryKey: ['company', 'ratings', user?.id],
    queryFn: () => scholarshipProviderReviewsApi.getScholarshipProviderRatings(user!.id),
    enabled: !!user?.id,
  });

  const { data: scholarships = [] } = useQuery({
    queryKey: ['scholarships', 'mine', 'company'],
    queryFn: () => scholarshipsApi.getMine(),
    staleTime: 60_000,
  });

  const { data: applicationsPage, isLoading: appsLoading } = useQuery({
    queryKey: ['applications', 'company', 'all'],
    queryFn: () => applicationsApi.getScholarshipProviderApplications(undefined, 1, 50),
    staleTime: 60_000,
  });

  // Per-status counts across ALL of the provider's applications (server-side
  // aggregate). The KPI + status chart derive from this so they stay correct
  // for providers with more applications than the 50-row recent-list page holds.
  const { data: statusCounts } = useQuery({
    queryKey: ['applications', 'company', 'status-counts'],
    queryFn: () => applicationsApi.getScholarshipProviderApplicationStatusCounts(),
    staleTime: 60_000,
  });

  const activeScholarships = useMemo(
    () => scholarships.filter((s) => s.status === 'Open').length,
    [scholarships],
  );

  // The recent-applications list below shows the newest page of rows; the KPI and
  // status chart use the full server-side aggregate (`statusCounts`) instead so
  // they never undercount a provider with more apps than one page.
  const apps = useMemo(() => applicationsPage?.items ?? [], [applicationsPage]);
  const byStatus = useMemo(() => statusCounts?.byStatus ?? {}, [statusCounts]);
  const totalApplicants = statusCounts?.total ?? applicationsPage?.totalCount ?? 0;
  const pendingReview =
    (byStatus['Pending'] ?? 0) +
    (byStatus['UnderReview'] ?? 0) +
    (byStatus['Applied'] ?? 0) +
    (byStatus['Shortlisted'] ?? 0);

  // Real status distribution across ALL applications (no fabricated numbers —
  // straight from the server aggregate). Ordered, colored to match the pills,
  // zero buckets dropped. Feeds the DonutChart.
  const donutSegments = useMemo<DonutSegment[]>(
    () =>
      COMPANY_STATUS_ORDER.map((s) => ({
        label: t(`applications:scholarshipProviderReview.status.${s}`, { defaultValue: s }),
        count: byStatus[s] ?? 0,
        color: STATUS_META[s]?.color ?? 'var(--color-text-tertiary)',
      })).filter((x) => x.count > 0),
    [byStatus, t],
  );

  // Newest-first slice of the recent applicant rows, surfaced as a real list with
  // status pills (avatar initial + student + scholarship + relative time).
  const recentApps = useMemo(
    () =>
      [...apps]
        .sort((a, b) => {
          const ta = a.submittedAt ? new Date(a.submittedAt).getTime() : 0;
          const tb = b.submittedAt ? new Date(b.submittedAt).getTime() : 0;
          return tb - ta;
        })
        .slice(0, 6),
    [apps],
  );

  if (ratingsLoading) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-10">
        <div className="space-y-6">
          <div className="h-44 animate-pulse rounded-3xl bg-bg-subtle" />
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="h-32 animate-pulse rounded-2xl bg-bg-subtle" />
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6 px-4 py-8 sm:px-6 lg:py-10">
      <WelcomeBanner
        eyebrow={t(`dashboard:greeting.${greetingKey()}`, { name: firstName })}
        title={
          <>
            {t('dashboard:company.headlinePrefix')}{' '}
            <span className="text-gradient">{t('dashboard:company.headlineSuffix')}</span>
          </>
        }
        subtitle={t('dashboard:company.banner.subtitle')}
        actions={
          <>
            <Link to="/company/scholarships/new" className="btn btn-primary">
              <Plus aria-hidden className="size-4" />
              {t('dashboard:company.exploreBtn')}
            </Link>
            <Link to="/company/applications-review" className="btn btn-secondary">
              {t('dashboard:company.secondaryBtn')}
              <ArrowRight aria-hidden className="size-4 rtl:rotate-180" />
            </Link>
          </>
        }
      />

      {/* KPI tiles — no fabricated delta/trend props (those were hard-coded
          mock numbers that misled the user about real growth). */}
      <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <StatCard
          label={t('dashboard:company.stats.scholarships')}
          value={activeScholarships}
          to="/company/scholarships"
          icon={Briefcase}
          accent="brand"
          delay={0.02}
        />
        <StatCard
          label={t('dashboard:company.stats.applicants')}
          value={totalApplicants}
          to="/company/applications-review"
          icon={Users}
          accent="success"
          delay={0.06}
        />
        <StatCard
          label={t('dashboard:company.stats.rating')}
          value={ratings && ratings.totalRatings > 0 ? ratings.averageRating.toFixed(1) : '—'}
          icon={Star}
          accent="warning"
          delay={0.1}
        />
        <StatCard
          label={t('dashboard:company.stats.pending')}
          value={pendingReview}
          to="/company/applications-review"
          icon={Clock}
          accent={pendingReview > 0 ? 'danger' : 'neutral'}
          delay={0.14}
        />
      </section>

      <div className="grid gap-6 lg:grid-cols-12">
        {/* Left column: application pipeline donut + live applicant list */}
        <div className="space-y-6 lg:col-span-8">
          <ChartCard
            title={t('dashboard:company.applicationsByStatus.title')}
            subtitle={t('dashboard:company.applicationsByStatus.subtitle')}
          >
            <DonutChart
              segments={donutSegments}
              centerValue={totalApplicants}
              centerLabel={t('dashboard:company.stats.applicants')}
              emptyLabel={t('dashboard:company.applicationsByStatus.empty')}
            />
          </ChartCard>

          <section className="card-premium p-5 sm:p-6">
            <header className="mb-4 flex items-center justify-between">
              <h2 className="text-base font-semibold text-text-primary">
                {t('applications:scholarshipProviderReview.title', { defaultValue: 'Review Applications' })}
              </h2>
              <Link
                to="/company/applications-review"
                className="text-xs font-medium text-brand-600 transition-colors hover:text-brand-700 hover:underline"
              >
                {t('dashboard:activity.viewAll')}
              </Link>
            </header>

            {appsLoading ? (
              <ul className="-mx-2 divide-y divide-border-subtle">
                {[0, 1, 2, 3].map((i) => (
                  <li key={i} className="flex items-center gap-3 px-2 py-3">
                    <div className="size-10 shrink-0 animate-pulse rounded-xl bg-bg-subtle" />
                    <div className="flex-1 space-y-2">
                      <div className="h-3 w-1/2 animate-pulse rounded bg-bg-subtle" />
                      <div className="h-2.5 w-2/3 animate-pulse rounded bg-bg-subtle" />
                    </div>
                    <div className="h-5 w-16 shrink-0 animate-pulse rounded-full bg-bg-subtle" />
                  </li>
                ))}
              </ul>
            ) : recentApps.length === 0 ? (
              <div className="rounded-xl border border-dashed border-border-subtle bg-bg-subtle/30 p-8 text-center">
                <p className="text-sm font-medium text-text-primary">{t('dashboard:activity.emptyTitle')}</p>
                <p className="mt-1 text-xs text-text-tertiary">{t('dashboard:activity.emptyBody')}</p>
              </div>
            ) : (
              <ul className="-mx-2 divide-y divide-border-subtle">
                {recentApps.map((app) => {
                  const meta = STATUS_META[app.status] ?? { tone: 'neutral' as StatAccent };
                  const initial = (app.studentName || '?').trim().charAt(0).toUpperCase();
                  return (
                    <li key={app.applicationId}>
                      <Link
                        to="/company/applications-review"
                        className="flex items-center gap-3 rounded-lg px-2 py-3 transition-colors hover:bg-bg-subtle/60"
                      >
                        <span
                          className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-sm font-bold text-brand-600"
                          aria-hidden
                        >
                          {initial}
                        </span>
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-semibold text-text-primary">
                            {app.studentName}
                          </p>
                          <p className="truncate text-xs text-text-tertiary">
                            {app.scholarshipTitle}
                            {app.submittedAt && (
                              <>
                                {' · '}
                                {formatRelativeTime(app.submittedAt, i18n.language)}
                              </>
                            )}
                          </p>
                        </div>
                        <StatusPill
                          tone={meta.tone}
                          label={t(`applications:scholarshipProviderReview.status.${app.status}`, {
                            defaultValue: app.status,
                          })}
                        />
                      </Link>
                    </li>
                  );
                })}
              </ul>
            )}
          </section>
        </div>

        {/* Right column: quick actions + student ratings */}
        <aside className="space-y-6 lg:col-span-4">
          <QuickActions
            title={t('dashboard:quickActions.title')}
            actions={[
              { icon: Plus, label: t('dashboard:company.quick.post'), to: '/company/scholarships/new', accent: 'brand' },
              { icon: ListChecks, label: t('dashboard:company.quick.review'), to: '/company/applications-review', accent: 'success' },
              ...(paymentsEnabled
                ? [{ icon: Receipt, label: t('dashboard:company.quick.billing'), to: '/company/billing', accent: 'warning' as const }]
                : []),
              { icon: BarChart2, label: t('dashboard:company.quick.analytics'), to: '/company/insights', accent: 'neutral' },
            ]}
          />

          <section className="card-premium p-5 sm:p-6">
            <header className="mb-4 flex flex-wrap items-start justify-between gap-3">
              <div>
                <h2 className="text-base font-semibold text-text-primary">
                  {t('dashboard:company.reviews.title')}
                </h2>
                {ratings && ratings.totalRatings > 0 && (
                  <p className="mt-0.5 text-xs text-text-tertiary">
                    {t('company:dashboard.basedOn', { count: ratings.totalRatings })}
                  </p>
                )}
              </div>
              {ratings && ratings.totalRatings > 0 && (
                <div className="flex items-center gap-1.5">
                  <Star aria-hidden className="size-5 fill-amber-400 text-amber-400" />
                  <span className="text-2xl font-bold tabular-nums text-text-primary">
                    {ratings.averageRating.toFixed(1)}
                  </span>
                  <span className="text-xs text-text-tertiary">/ 5</span>
                </div>
              )}
            </header>

            {!ratings || ratings.totalRatings === 0 ? (
              <EmptyState
                title={t('dashboard:company.reviews.emptyTitle')}
                body={t('dashboard:company.reviews.emptyBody')}
              />
            ) : (
              <div className="space-y-3">
                {(ratings.recentReviews ?? []).slice(0, 4).map((review) => (
                  <article
                    key={review.reviewId}
                    className="rounded-2xl border border-border-subtle bg-bg-subtle/40 p-4 transition-all hover:border-brand-200 hover:bg-bg-subtle/70"
                  >
                    <header className="mb-2 flex items-center justify-between gap-2">
                      <span className="truncate text-sm font-semibold text-text-primary">
                        {review.studentName}
                      </span>
                      <span className="inline-flex items-center gap-0.5 rounded-full bg-amber-50 px-1.5 py-0.5 text-xs font-semibold text-amber-700">
                        <Star aria-hidden className="size-3 fill-amber-400 text-amber-400" />
                        {review.rating}
                      </span>
                    </header>

                    {review.comment ? (
                      <p className="line-clamp-3 text-sm italic text-text-secondary">
                        "{review.comment}"
                      </p>
                    ) : (
                      <p className="text-sm italic text-text-tertiary">
                        {t('company:dashboard.noComment')}
                      </p>
                    )}

                    <footer className="mt-3 flex items-center justify-between text-xs text-text-tertiary">
                      <span>{formatDate(review.createdAt, i18n.language)}</span>
                      <span>{formatRelativeTime(review.createdAt, i18n.language)}</span>
                    </footer>
                  </article>
                ))}
              </div>
            )}
          </section>
        </aside>
      </div>
    </div>
  );
}
