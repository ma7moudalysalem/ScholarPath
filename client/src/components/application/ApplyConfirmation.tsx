import { useTranslation } from 'react-i18next';
import { ShieldCheck, Info, CreditCard } from 'lucide-react';
import { usePaymentsEnabled } from '@/hooks/usePlatformStatus';

interface ApplyConfirmationProps {
  scholarshipTitle: string;
  companyName: string;
  reviewFeeUsd: number;
  onConfirm: () => void;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function ApplyConfirmation({
  scholarshipTitle,
  companyName,
  reviewFeeUsd,
  onConfirm,
  onCancel,
  isSubmitting = false,
}: ApplyConfirmationProps) {
  const { t } = useTranslation(['company', 'scholarships']);
  // Master payments switch: when off, treat every application as free
  // regardless of the stored fee.
  const paymentsEnabled = usePaymentsEnabled();
  const isFree = !paymentsEnabled || reviewFeeUsd === 0;

  return (
    <div className="flex flex-col space-y-6">
      <div className="text-center">
        <h2 className="text-xl font-bold text-text-primary">
          {t('submit.title')}
        </h2>
        <p className="text-sm text-text-secondary mt-1">
          {t('submit.subtitle', { scholarship: scholarshipTitle })}
        </p>
      </div>

      <div className="rounded-xl border border-border-subtle bg-bg-muted p-5">
        <div className="flex items-center justify-between mb-4">
          <span className="text-sm font-medium text-text-secondary">
            {t('submit.reviewFee')}
          </span>
          <span className="text-lg font-bold text-text-primary">
            {isFree ? t('scholarships:freeListing') : `$${reviewFeeUsd.toFixed(2)}`}
          </span>
        </div>

        <div className="flex items-start space-x-3 rounded-lg bg-bg-elevated p-3 shadow-sm">
          <ShieldCheck className="text-success-600 shrink-0 mt-0.5" size={20} />
          <div>
            <h4 className="text-xs font-semibold text-text-primary uppercase tracking-wider">
              {t('submit.escrowNoticeTitle')}
            </h4>
            <p className="text-xs text-text-secondary mt-1 leading-relaxed">
              {t('submit.escrowNoticeDesc', { company: companyName })}
            </p>
          </div>
        </div>
      </div>

      {/* Refund / escrow disclaimer only renders for paid applications —
          there's nothing to refund when the platform is in free mode. */}
      {!isFree && (
        <div className="flex items-start space-x-3 text-text-tertiary">
          <Info size={16} className="shrink-0 mt-0.5" />
          <p className="text-[11px] leading-relaxed">
            By proceeding, you agree to our terms of service. The fee is non-refundable if the application is rejected, but fully refundable if the company fails to review it within 14 days.
          </p>
        </div>
      )}

      <div className="flex flex-col space-y-3 pt-2">
        <button
          onClick={onConfirm}
          disabled={isSubmitting}
          className="flex items-center justify-center space-x-2 w-full rounded-lg bg-brand-600 py-3 text-sm font-semibold text-white hover:bg-brand-700 focus:outline-none focus:ring-2 focus:ring-brand-300 focus:ring-offset-2 disabled:opacity-50 transition-all shadow-sm"
        >
          {isSubmitting ? (
            <div className="h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent" />
          ) : (
            <>
              <CreditCard size={18} />
              <span>{t('common.submit')}</span>
            </>
          )}
        </button>
        <button
          onClick={onCancel}
          disabled={isSubmitting}
          className="w-full rounded-lg py-2 text-sm font-medium text-text-secondary hover:text-text-primary transition-colors"
        >
          {t('common.cancelAndReturn')}
        </button>
      </div>
    </div>
  );
}
