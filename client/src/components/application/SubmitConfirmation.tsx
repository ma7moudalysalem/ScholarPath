import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CreditCard, Info } from 'lucide-react';
import { StripeCheckout } from '../common/StripeCheckout';
import { usePaymentsEnabled } from '@/hooks/usePlatformStatus';

interface SubmitConfirmationProps {
  applicationId: string;
  scholarshipTitle: string;
  scholarshipProviderName: string;
  reviewFeeUsd: number;
  onPaymentSuccess: () => void;
  onCancel: () => void;
}

/**
 * ScholarshipProviderReview fee confirmation modal. Hosts the StripeCheckout widget
 * configured for the ScholarshipProviderReview manual-capture flow: the student
 * authorizes a hold now and the company captures it when the review is
 * accepted. (PB-005 v1.)
 */
export function ApplicationSubmitConfirmation({
  applicationId,
  scholarshipTitle,
  scholarshipProviderName,
  reviewFeeUsd,
  onPaymentSuccess,
  onCancel,
}: SubmitConfirmationProps) {
  const { t } = useTranslation(['company']);
  // Master payments switch — free mode skips the Stripe widget entirely and
  // shows a single "Submit" button that triggers the success callback. The
  // existing free-path on the server already creates a request with no
  // Payment row when fee is 0.
  const paymentsEnabled = usePaymentsEnabled();
  const isFree = !paymentsEnabled || reviewFeeUsd === 0;
  // Guard the free-path Submit against a double-click firing the mutation twice
  // (the server 409s the duplicate, surfacing a spurious error toast).
  const [submitting, setSubmitting] = useState(false);

  return (
    <div className="mx-auto max-w-2xl p-6">
      <div className="mb-8 text-center">
        {!isFree && (
          <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-brand-50">
            <CreditCard className="h-8 w-8 text-brand-500" />
          </div>
        )}
        <h1 className="text-2xl font-bold tracking-tight text-text-primary">
          {t(isFree ? 'submit.titleFree' : 'submit.title')}
        </h1>
        <p className="mt-2 text-text-secondary">
          {t(isFree ? 'submit.subtitleFree' : 'submit.subtitle', { scholarship: scholarshipTitle })}
        </p>
      </div>

      <div className="rounded-xl border border-border-subtle bg-bg-elevated shadow-sm overflow-hidden mb-6">
        {!isFree && (
          <div className="border-b border-border-subtle bg-bg-muted/50 p-6">
            <div className="flex items-center justify-between mb-4">
              <span className="text-sm font-medium text-text-secondary">{t('submit.reviewFee')}</span>
              <span className="text-xl font-bold text-text-primary">
                {`$${reviewFeeUsd.toFixed(2)}`}
              </span>
            </div>

            <div className="flex items-start space-x-3 rounded-lg bg-brand-50 p-4 text-sm text-brand-700">
              <Info className="mt-0.5 shrink-0" size={16} />
              <div className="space-y-1">
                <p className="font-medium">{t('submit.escrowNoticeTitle')}</p>
                <p className="text-brand-600">
                  {t('submit.escrowNoticeDesc', { company: scholarshipProviderName })}
                </p>
              </div>
            </div>
          </div>
        )}

        <div className="p-6">
          {isFree ? (
            // No Stripe widget — submitting the request directly is enough.
            <button
              type="button"
              onClick={() => {
                if (submitting) return;
                setSubmitting(true);
                onPaymentSuccess();
              }}
              disabled={submitting}
              className="w-full rounded-lg bg-brand-600 py-3 text-sm font-semibold text-white hover:bg-brand-700 transition disabled:opacity-50"
            >
              {t('common.submit')}
            </button>
          ) : (
            <div className="space-y-4">
              <p className="text-sm font-medium text-text-secondary mb-4">
                {t('submit.paymentDetails')}
              </p>
              <StripeCheckout
                paymentType="ScholarshipProviderReview"
                applicationId={applicationId}
                amountCents={Math.round(reviewFeeUsd * 100)}
                currency="USD"
                returnUrlPath={`/student/applications/${applicationId}`}
                onSuccess={onPaymentSuccess}
              />
            </div>
          )}
        </div>
      </div>

      <div className="text-center">
        <button
          onClick={onCancel}
          className="text-sm font-medium text-text-secondary hover:text-text-primary transition-colors"
        >
          {t('common.cancelAndReturn')}
        </button>
      </div>
    </div>
  );
}
