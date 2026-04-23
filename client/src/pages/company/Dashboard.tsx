import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Star } from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { companyReviewsApi } from '@/services/api/companyReviews';
import { EmptyState } from '@/components/common/EmptyState';
import { queryKeys } from '@/lib/queryClient';

// Dummy types mapping to our backend DTOs
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
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl p-6">
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
            {t('dashboard.title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('dashboard.subtitle')}
          </p>
        </div>
        
        {data && data.totalRatings > 0 && (
          <div className="flex flex-col items-end">
            <div className="flex items-center space-x-2">
              <span className="text-3xl font-bold text-slate-900 dark:text-white">{data.averageRating.toFixed(1)}</span>
              <Star className="fill-amber-400 text-amber-400" size={28} />
            </div>
            <span className="text-sm text-slate-500 dark:text-slate-400">
              {t('dashboard.basedOn', { count: data.totalRatings })}
            </span>
          </div>
        )}
      </div>

      <div className="rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="border-b border-slate-200 px-6 py-4 dark:border-slate-800">
          <h2 className="text-lg font-semibold text-slate-900 dark:text-white">
            {t('dashboard.recentReviews')}
          </h2>
        </div>
        
        <div className="p-6">
          {!data || data.totalRatings === 0 ? (
            <EmptyState
              owner="@yousra-elnoby"
              module="Company Reviews"
              title={t('dashboard.noReviewsTitle')}
              body={t('dashboard.noReviewsDescription')}
            />
          ) : (
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              {data.recentReviews.map((review) => (
                <div key={review.reviewId} className="flex flex-col space-y-3 rounded-lg border border-slate-100 bg-slate-50 p-5 dark:border-slate-800 dark:bg-slate-800/50 transition-shadow hover:shadow-md">
                  <div className="flex items-center justify-between">
                    <span className="font-medium text-slate-900 dark:text-white truncate pr-2">
                      {review.studentName}
                    </span>
                    <div className="flex items-center shrink-0">
                      <span className="mr-1 font-semibold text-slate-900 dark:text-white">{review.rating}</span>
                      <Star className="fill-amber-400 text-amber-400" size={16} />
                    </div>
                  </div>
                  
                  {review.comment ? (
                    <p className="text-sm text-slate-600 dark:text-slate-300 italic line-clamp-4">
                      "{review.comment}"
                    </p>
                  ) : (
                    <p className="text-sm text-slate-400 dark:text-slate-500 italic">
                      {t('dashboard.noComment')}
                    </p>
                  )}
                  
                  <div className="mt-auto pt-4 text-xs text-slate-400 dark:text-slate-500">
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
