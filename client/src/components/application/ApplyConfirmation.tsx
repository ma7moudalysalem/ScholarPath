import { useTranslation } from 'react-i18next';
import { ShieldCheck, Info, CreditCard } from 'lucide-react';

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
  const { t } = useTranslation('company');

  return (
    <div className="flex flex-col space-y-6">
      <div className="text-center">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white">
          {t('submit.title')}
        </h2>
        <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
          {t('submit.subtitle', { scholarship: scholarshipTitle })}
        </p>
      </div>

      <div className="rounded-xl border border-slate-200 bg-slate-50 p-5 dark:border-slate-800 dark:bg-slate-800/50">
        <div className="flex items-center justify-between mb-4">
          <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
            {t('submit.reviewFee')}
          </span>
          <span className="text-lg font-bold text-slate-900 dark:text-white">
            ${reviewFeeUsd.toFixed(2)}
          </span>
        </div>
        
        <div className="flex items-start space-x-3 rounded-lg bg-white p-3 shadow-sm dark:bg-slate-900">
          <ShieldCheck className="text-emerald-500 shrink-0 mt-0.5" size={20} />
          <div>
            <h4 className="text-xs font-semibold text-slate-900 dark:text-white uppercase tracking-wider">
              {t('submit.escrowNoticeTitle')}
            </h4>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">
              {t('submit.escrowNoticeDesc', { company: companyName })}
            </p>
          </div>
        </div>
      </div>

      <div className="flex items-start space-x-3 text-slate-400">
        <Info size={16} className="shrink-0 mt-0.5" />
        <p className="text-[11px] leading-relaxed">
          By proceeding, you agree to our terms of service. The fee is non-refundable if the application is rejected, but fully refundable if the company fails to review it within 14 days.
        </p>
      </div>

      <div className="flex flex-col space-y-3 pt-2">
        <button
          onClick={onConfirm}
          disabled={isSubmitting}
          className="flex items-center justify-center space-x-2 w-full rounded-lg bg-primary-600 py-3 text-sm font-semibold text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 transition-all shadow-lg shadow-primary-500/20"
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
          className="w-full rounded-lg py-2 text-sm font-medium text-slate-500 hover:text-slate-700 dark:hover:text-slate-300 transition-colors"
        >
          {t('common.cancelAndReturn')}
        </button>
      </div>
    </div>
  );
}
