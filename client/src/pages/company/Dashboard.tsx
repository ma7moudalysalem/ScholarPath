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
  FileText,
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
  CategoryBars,
  type CategoryBar,
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

// Status buckets shown in the "Applications by status" breakdown, in order.
const COMPANY_STATUS_ORDER = ['Applied', 'Pending', 'UnderReview', 'WaitingResult', 'Accepted', 'Rejected'];

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

  const { data: applicationsPage } = useQuery({
    queryKey: ['applications', 'company', 'all'],
    queryFn: () => applicationsApi.getScholarshipProviderApplications(undefined, 1, 50),
    staleTime: 60_000,
  });

  const activeScholarships = useMemo(
    () => scholarships.filter((s) => s.status === 'Open').length,
    [scholarships],
  );

  const apps = useMemo(() => applicationsPage?.items ?? [], [applicationsPage]);
  // totalCount is the full server-side total from the paged result. `apps.length`
  // would only reflect the 50-row page and understate companies with > 50 apps.
  const totalApplicants = applicationsPage?.totalCount ?? 0;
  const pendingReview = apps.filter(
    (a) => a.status === 'Pending' || a.status === 'UnderReview' || a.status === 'Applied',
  ).length;

  // Real status distribution across the loaded applications (no fabricated
  // numbers — derived straight from the rows). Ordered + zero buckets dropped.
  const statusBreakdown = useMemo<CategoryBar[]>(() => {
    const counts = new Map<string, number>();
    for (const a of apps) counts.set(a.status, (counts.get(a.status) ?? 0) + 1);
    return COMPANY_STATUS_ORDER.map((s) => ({
      label: t(`applications:scholarshipProviderReview.status.${s}`, { defaultValue: s }),
      count: counts.get(s) ?? 0,
    })).filter((x) => x.count > 0);
  }, [apps, t]);

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
        <div className="space-y-6 lg:col-span-8">
          <ChartCard
            title={t('dashboard:company.applicationsByStatus.title')}
            subtitle={t('dashboard:company.applicationsByStatus.subtitle')}
          >
            <CategoryBars
              items={statusBreakdown}
              emptyLabel={t('dashboard:company.applicationsByStatus.empty')}
            />
          </ChartCard>

          <section className="card-premium p-5 sm:p-6">
            <header className="mb-5 flex flex-wrap items-start justify-between gap-3">
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
                <div className="flex items-center gap-2">
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
              <div className="grid gap-4 md:grid-cols-2">
                {ratings.recentReviews.slice(0, 4).map((review) => (
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
        </div>

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

          {/* Recent applications mini-feed */}
          <section className="card-premium p-5 sm:p-6">
            <header className="mb-4 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-text-primary">
                {t('dashboard:activity.title')}
              </h2>
              <Link
                to="/company/applications-review"
                className="text-xs font-medium text-brand-600 transition-colors hover:text-brand-700 hover:underline"
              >
                {t('dashboard:activity.viewAll')}
              </Link>
            </header>
            {apps.length === 0 ? (
              <div className="rounded-xl border border-dashed border-border-subtle bg-bg-subtle/30 p-6 text-center">
                <p className="text-sm font-medium text-text-primary">{t('dashboard:activity.emptyTitle')}</p>
                <p className="mt-1 text-xs text-text-tertiary">{t('dashboard:activity.emptyBody')}</p>
              </div>
            ) : (
              <ul className="space-y-3">
                {apps.slice(0, 5).map((app) => (
                  <li key={app.applicationId}>
                    <Link
                      to="/company/applications-review"
                      className="flex gap-3 rounded-lg p-1 -m-1 transition-colors hover:bg-bg-subtle/60"
                    >
                      <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-brand-50 text-brand-600">
                        <FileText aria-hidden className="size-4" />
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="line-clamp-1 text-sm text-text-primary">
                          {app.studentName} <span className="text-text-tertiary">·</span>{' '}
                          <span className="text-text-secondary">{app.scholarshipTitle}</span>
                        </p>
                        {app.submittedAt && (
                          <p className="mt-0.5 text-xs text-text-tertiary">
                            {formatRelativeTime(app.submittedAt, i18n.language)}
                          </p>
                        )}
                      </div>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </aside>
      </div>
    </div>
  );
}
