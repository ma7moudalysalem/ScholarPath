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
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-primary-100 dark:bg-primary-900/30">
          <CreditCard className="h-8 w-8 text-primary-600 dark:text-primary-400" />
        </div>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
          {t('submit.title')}
        </h1>
        <p className="mt-2 text-slate-500 dark:text-slate-400">
          {t('submit.subtitle', { scholarship: scholarshipTitle })}
        </p>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900 overflow-hidden mb-6">
        <div className="border-b border-slate-100 bg-slate-50/50 p-6 dark:border-slate-800/50 dark:bg-slate-800/20">
          <div className="flex items-center justify-between mb-4">
            <span className="text-sm font-medium text-slate-500 dark:text-slate-400">{t('submit.reviewFee')}</span>
            <span className="text-xl font-bold text-slate-900 dark:text-white">${reviewFeeUsd.toFixed(2)}</span>
          </div>
          
          <div className="flex items-start space-x-3 rounded-lg bg-blue-50 p-4 text-sm text-blue-800 dark:bg-blue-900/20 dark:text-blue-300">
            <Info className="mt-0.5 shrink-0" size={16} />
            <div className="space-y-1">
              <p className="font-medium">{t('submit.escrowNoticeTitle')}</p>
              <p className="text-blue-700 dark:text-blue-400/80">
                {t('submit.escrowNoticeDesc', { company: companyName })}
              </p>
            </div>
          </div>
        </div>
        
        <div className="p-6">
          <div className="space-y-4">
            <p className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-4">
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
          className="text-sm font-medium text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 transition-colors"
        >
          {t('common.cancelAndReturn')}
        </button>
      </div>
    </div>
  );
}
