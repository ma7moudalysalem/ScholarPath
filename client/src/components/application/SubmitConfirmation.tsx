import { useTranslation } from 'react-i18next';
import { CreditCard, Info } from 'lucide-react';
import { StripeCheckout } from '../common/StripeCheckout';

interface SubmitConfirmationProps {
  applicationId: string;
  scholarshipTitle: string;
  companyName: string;
  reviewFeeUsd: number;
  onPaymentSuccess: () => void;
  onCancel: () => void;
}

export function ApplicationSubmitConfirmation({
  applicationId,
  scholarshipTitle,
  companyName,
  reviewFeeUsd,
  onPaymentSuccess,
  onCancel
}: SubmitConfirmationProps) {
  const { t } = useTranslation('company');

  return (
    <div className="mx-auto max-w-2xl p-6">
      <div className="mb-8 text-center">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-brand-50">
          <CreditCard className="h-8 w-8 text-brand-500" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight text-text-primary">
          {t('submit.title')}
        </h1>
        <p className="mt-2 text-text-secondary">
          {t('submit.subtitle', { scholarship: scholarshipTitle })}
        </p>
      </div>

      <div className="rounded-xl border border-border-subtle bg-bg-elevated shadow-sm overflow-hidden mb-6">
        <div className="border-b border-border-subtle bg-bg-muted/50 p-6">
          <div className="flex items-center justify-between mb-4">
            <span className="text-sm font-medium text-text-secondary">{t('submit.reviewFee')}</span>
            <span className="text-xl font-bold text-text-primary">${reviewFeeUsd.toFixed(2)}</span>
          </div>

          <div className="flex items-start space-x-3 rounded-lg bg-brand-50 p-4 text-sm text-brand-700">
            <Info className="mt-0.5 shrink-0" size={16} />
            <div className="space-y-1">
              <p className="font-medium">{t('submit.escrowNoticeTitle')}</p>
              <p className="text-brand-600">
                {t('submit.escrowNoticeDesc', { company: companyName })}
              </p>
            </div>
          </div>
        </div>

        <div className="p-6">
          <div className="space-y-4">
            <p className="text-sm font-medium text-text-secondary mb-4">
              {t('submit.paymentDetails')}
            </p>
            <StripeCheckout
              bookingId={applicationId}
              amountCents={Math.round(reviewFeeUsd * 100)}
              onSuccess={onPaymentSuccess}
            />
          </div>
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
