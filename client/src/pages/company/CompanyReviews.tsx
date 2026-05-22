import { useTranslation } from "react-i18next";
import { ReviewsReceived } from "@/components/reviews/ReviewsReceived";
import { useCompanyReceivedReviewsQuery } from "@/hooks/useReviewsQuery";

/**
 * Company "Reviews received" page (PB-008). Surfaces the ratings students have
 * left after a finalized application, so a company can finally read the feedback
 * behind the "you got a 5-star rating" notifications. Read-only.
 */
export function CompanyReviews() {
  const { t } = useTranslation("reviews");
  const { data, isLoading, isError, refetch } = useCompanyReceivedReviewsQuery();

  return (
    <ReviewsReceived
      data={data}
      isLoading={isLoading}
      isError={isError}
      onRetry={() => void refetch()}
      title={t("company.title")}
      subtitle={t("company.subtitle")}
    />
  );
}
