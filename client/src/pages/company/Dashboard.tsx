import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Star } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { companyReviewsApi } from '@/services/api/companyReviews';
import { EmptyState } from '@/components/common/EmptyState';

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

export function CompanyDashboard() {
  const { t } = useTranslation('company');
  const user = useAuthStore((state) => state.user);

  const { data, isLoading } = useQuery({
    queryKey: ['company', 'ratings', user?.id],
    queryFn: () => companyReviewsApi.getCompanyRatings(user!.id),
    enabled: !!user?.id,
  });

  if (isLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-brand-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl p-6">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary">
            {t('dashboard.title')}
          </h1>
          <p className="text-sm text-text-secondary">
            {t('dashboard.subtitle')}
          </p>
        </div>

        {data && data.totalRatings > 0 && (
          <div className="flex flex-col items-end">
            <div className="flex items-center space-x-2">
              <span className="text-3xl font-bold text-text-primary">{data.averageRating.toFixed(1)}</span>
              <Star className="fill-amber-400 text-amber-400" size={28} />
            </div>
            <span className="text-sm text-text-secondary">
              {t('dashboard.basedOn', { count: data.totalRatings })}
            </span>
          </div>
        )}
      </div>

      <div className="rounded-xl border border-border-subtle bg-bg-elevated shadow-sm">
        <div className="border-b border-border-subtle px-6 py-4">
          <h2 className="text-lg font-semibold text-text-primary">
            {t('dashboard.recentReviews')}
          </h2>
        </div>

        <div className="p-6">
          {!data || data.totalRatings === 0 ? (
            <EmptyState
              title={t('dashboard.noReviewsTitle')}
              body={t('dashboard.noReviewsDescription')}
            />
          ) : (
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {data.recentReviews.map((review) => (
                <div
                  key={review.reviewId}
                  className="flex flex-col space-y-3 rounded-lg border border-border-subtle bg-bg-muted p-5 transition-shadow hover:shadow-md"
                >
                  <div className="flex items-center justify-between">
                    <span className="truncate pe-2 font-medium text-text-primary">
                      {review.studentName}
                    </span>
                    <div className="flex shrink-0 items-center">
                      <span className="me-1 font-semibold text-text-primary">{review.rating}</span>
                      <Star className="fill-amber-400 text-amber-400" size={16} />
                    </div>
                  </div>

                  {review.comment ? (
                    <p className="line-clamp-4 text-sm italic text-text-secondary">
                      "{review.comment}"
                    </p>
                  ) : (
                    <p className="text-sm italic text-text-tertiary">
                      {t('dashboard.noComment')}
                    </p>
                  )}

                  <div className="mt-auto pt-4 text-xs text-text-tertiary">
                    {new Date(review.createdAt).toLocaleDateString()}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
