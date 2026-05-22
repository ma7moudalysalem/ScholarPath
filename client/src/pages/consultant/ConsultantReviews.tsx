import { useTranslation } from "react-i18next";
import { ReviewsReceived } from "@/components/reviews/ReviewsReceived";
import { useConsultantReceivedReviewsQuery } from "@/hooks/useReviewsQuery";

/**
 * Consultant "Reviews received" page (PB-006). Surfaces the ratings students
 * have left after completed sessions, giving the consultant a read surface for
 * the feedback that previously only existed on their public profile preview.
 * Read-only.
 */
export function ConsultantReviews() {
  const { t } = useTranslation("reviews");
  const { data, isLoading, isError, refetch } = useConsultantReceivedReviewsQuery();

  return (
    <ReviewsReceived
      data={data}
      isLoading={isLoading}
      isError={isError}
      onRetry={() => void refetch()}
      title={t("consultant.title")}
      subtitle={t("consultant.subtitle")}
    />
  );
}
