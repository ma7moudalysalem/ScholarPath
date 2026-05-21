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
import { companyReviewsApi } from '@/services/api/companyReviews';
import { scholarshipsApi } from '@/services/api/scholarships';
import { applicationsApi } from '@/services/api/applications';
import { EmptyState } from '@/components/common/EmptyState';
import { formatDate } from '@/lib/bookingFormat';
import {
  WelcomeBanner,
  StatCard,
  QuickActions,
} from '@/components/dashboard/primitives';
import { formatRelativeTime } from '@/components/dashboard/utils';

export interface CompanyReviewRow {
  reviewId: string;
  studentId: string;
  studentName: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface CompanyRatingsSummaryDto {
  companyId: string;
  averageRating: number;
  totalRatings: number;
  recentReviews: CompanyReviewRow[];
}

function greetingKey(): 'morning' | 'afternoon' | 'evening' {
  const h = new Date().getHours();
  if (h < 12) return 'morning';
  if (h < 18) return 'afternoon';
  return 'evening';
}

export function CompanyDashboard() {
  const { t, i18n } = useTranslation(['company', 'dashboard']);
  const user = useAuthStore((state) => state.user);
  const firstName = user?.firstName ?? '';

  const { data: ratings, isLoading: ratingsLoading } = useQuery({
    queryKey: ['company', 'ratings', user?.id],
    queryFn: () => companyReviewsApi.getCompanyRatings(user!.id),
    enabled: !!user?.id,
  });

  const { data: scholarships = [] } = useQuery({
    queryKey: ['scholarships', 'mine', 'company'],
    queryFn: () => scholarshipsApi.getMine(),
    staleTime: 60_000,
  });

  const { data: applicationsPage } = useQuery({
    queryKey: ['applications', 'company', 'all'],
    queryFn: () => applicationsApi.getCompanyApplications(undefined, 1, 50),
    staleTime: 60_000,
  });

  const activeScholarships = useMemo(
    () => scholarships.filter((s) => s.status === 'Open').length,
    [scholarships],
  );

  const apps = applicationsPage?.items ?? [];
  const totalApplicants = apps.length;
  const pendingReview = apps.filter(
    (a) => a.status === 'Pending' || a.status === 'UnderReview' || a.status === 'Applied',
  ).length;

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

      <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <StatCard
          label={t('dashboard:company.stats.scholarships')}
          value={activeScholarships}
          to="/company/scholarships"
          icon={Briefcase}
          accent="brand"
          delta={{ value: activeScholarships > 0 ? 6 : 0, label: t('dashboard:company.stats.scholarshipsDelta') }}
          trend={[1, 2, 2, 3, 3, 4, 4, 5, 6, 6]}
          delay={0.02}
        />
        <StatCard
          label={t('dashboard:company.stats.applicants')}
          value={totalApplicants}
          to="/company/applications-review"
          icon={Users}
          accent="success"
          delta={{ value: 14, label: t('dashboard:company.stats.applicantsDelta') }}
          trend={[2, 4, 5, 7, 6, 8, 10, 11, 13, 14]}
          delay={0.06}
        />
        <StatCard
          label={t('dashboard:company.stats.rating')}
          value={ratings && ratings.totalRatings > 0 ? ratings.averageRating.toFixed(1) : '—'}
          icon={Star}
          accent="warning"
          delta={
            ratings && ratings.totalRatings > 0
              ? { value: 3, label: t('dashboard:company.stats.ratingDelta') }
              : null
          }
          trend={[3.6, 3.7, 3.8, 4.0, 4.1, 4.2, 4.2, 4.3, 4.4, 4.5]}
          delay={0.1}
        />
        <StatCard
          label={t('dashboard:company.stats.pending')}
          value={pendingReview}
          to="/company/applications-review"
          icon={Clock}
          accent={pendingReview > 0 ? 'danger' : 'neutral'}
          delta={{ value: pendingReview, label: t('dashboard:company.stats.pendingDelta') }}
          trend={[2, 3, 4, 4, 5, 6, 5, 7, 6, 8]}
          delay={0.14}
        />
      </section>

      <div className="grid gap-6 lg:grid-cols-12">
        <div className="space-y-6 lg:col-span-8">
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
              { icon: Receipt, label: t('dashboard:company.quick.billing'), to: '/company/billing', accent: 'warning' },
              { icon: BarChart2, label: t('dashboard:company.quick.analytics'), to: '/company', accent: 'neutral' },
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
                  <li key={app.id}>
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
                        <p className="mt-0.5 text-xs text-text-tertiary">
                          {formatRelativeTime(app.createdAt, i18n.language)}
                        </p>
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
