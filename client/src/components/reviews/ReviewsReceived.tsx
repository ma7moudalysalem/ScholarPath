import { useTranslation } from "react-i18next";
import { Star, MessageSquare, AlertCircle, RefreshCw } from "lucide-react";
import type { ReceivedReviewsSummary } from "@/services/api/reviews";
import { StarRating } from "@/components/common/StarRating";
import { EmptyState } from "@/components/common/EmptyState";
import { formatRelativeTime } from "@/components/dashboard/utils";
import { formatDate } from "@/lib/bookingFormat";

interface ReviewsReceivedProps {
  data: ReceivedReviewsSummary | undefined;
  isLoading: boolean;
  isError: boolean;
  onRetry: () => void;
  /** Localized page title (e.g. "Reviews received"). */
  title: string;
  /** Localized page subtitle. */
  subtitle: string;
}

/** First-letter initial of a (masked) author name, upper-cased. */
function initial(name: string): string {
  return name.trim()[0]?.toUpperCase() ?? "?";
}

/**
 * Shared "Reviews received" surface for the Company and Consultant portals.
 * Both roles receive an identical wire shape (`ReceivedReviewsSummary`), so the
 * page chrome — header with average stars + total count, loading skeleton,
 * empty state, error state, and the review-card list — lives here once. RTL is
 * handled by logical CSS properties (start/end) and the shared StarRating.
 */
export function ReviewsReceived({
  data,
  isLoading,
  isError,
  onRetry,
  title,
  subtitle,
}: ReviewsReceivedProps) {
  const { t, i18n } = useTranslation("reviews");
  const lang = i18n.language;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {/* ── Header ── */}
      <section className="relative overflow-hidden rounded-3xl border border-border-subtle bg-bg-elevated p-6 sm:p-8">
        <div className="orb orb-brand orb-animated -end-24 -top-24 size-72 opacity-30" />
        <div className="relative z-10">
          <div className="mb-2 flex size-9 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
            <Star aria-hidden className="size-4" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
            {title}
          </h1>
          <p className="mt-1 max-w-2xl text-sm text-text-secondary">{subtitle}</p>

          {/* Aggregate — average stars + total count */}
          {isLoading ? (
            <div className="mt-5 h-10 w-48 animate-pulse rounded-lg bg-bg-subtle" />
          ) : data && data.totalReviews > 0 ? (
            <div className="mt-5 flex flex-wrap items-center gap-x-4 gap-y-2">
              <div className="flex items-baseline gap-1.5">
                <span className="text-3xl font-bold text-text-primary">
                  {data.averageRating.toFixed(1)}
                </span>
                <span className="text-sm text-text-tertiary">/ 5</span>
              </div>
              <StarRating
                value={data.averageRating}
                size={20}
                ariaLabel={t("aria.average", { rating: data.averageRating.toFixed(1) })}
              />
              <span className="text-sm font-medium text-text-secondary">
                {t("summary.count", { count: data.totalReviews })}
              </span>
            </div>
          ) : null}
        </div>
      </section>

      {/* ── Error ── */}
      {isError && (
        <div className="flex flex-col items-center justify-center gap-4 rounded-2xl border border-danger-200 bg-danger-50 p-12 text-center">
          <AlertCircle aria-hidden className="size-8 text-danger-500" />
          <p className="text-sm font-medium text-danger-600">{t("states.error")}</p>
          <button type="button" onClick={onRetry} className="btn btn-primary">
            <RefreshCw aria-hidden className="size-4" />
            {t("states.retry")}
          </button>
        </div>
      )}

      {/* ── Loading skeleton ── */}
      {isLoading && !isError && (
        <div className="space-y-3" aria-hidden>
          {[0, 1, 2].map((i) => (
            <div
              key={i}
              className="rounded-2xl border border-border-subtle bg-bg-elevated p-5"
            >
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="size-9 animate-pulse rounded-full bg-bg-subtle" />
                  <div className="h-4 w-28 animate-pulse rounded bg-bg-subtle" />
                </div>
                <div className="h-4 w-20 animate-pulse rounded bg-bg-subtle" />
              </div>
              <div className="mt-3 h-4 w-3/4 animate-pulse rounded bg-bg-subtle" />
            </div>
          ))}
        </div>
      )}

      {/* ── Empty state ── */}
      {!isLoading && !isError && data && data.totalReviews === 0 && (
        <EmptyState
          icon={MessageSquare}
          title={t("empty.title")}
          body={t("empty.body")}
        />
      )}

      {/* ── Review list ── */}
      {!isLoading && !isError && data && data.reviews.length > 0 && (
        <ul className="space-y-3">
          {data.reviews.map((review) => (
            <li
              key={review.id}
              className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition hover:border-brand-200"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex min-w-0 items-center gap-3">
                  <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-brand-600">
                    {initial(review.authorName)}
                  </div>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-text-primary">
                      {review.authorName}
                    </p>
                    <p className="text-xs text-text-tertiary">
                      {formatDate(review.createdAt, lang)} ·{" "}
                      {formatRelativeTime(review.createdAt, lang)}
                    </p>
                  </div>
                </div>
                <div className="flex shrink-0 items-center gap-1.5">
                  <StarRating
                    value={review.rating}
                    size={14}
                    ariaLabel={t("aria.reviewRating", { rating: review.rating })}
                  />
                  <span className="text-sm font-semibold text-text-primary">
                    {review.rating.toFixed(1)}
                  </span>
                </div>
              </div>

              {review.comment ? (
                <p className="mt-3 whitespace-pre-line text-sm leading-relaxed text-text-secondary">
                  {review.comment}
                </p>
              ) : (
                <p className="mt-3 text-sm italic text-text-tertiary">
                  {t("review.noComment")}
                </p>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
