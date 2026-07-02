import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import * as Dialog from '@radix-ui/react-dialog';
import { X, Star } from 'lucide-react';
import { toast } from 'sonner';

interface RatingModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  applicationId: string;
  scholarshipProviderId: string;
  scholarshipProviderName: string;
  onSubmitRating: (applicationId: string, scholarshipProviderId: string, rating: number, comment: string) => Promise<void>;
}

export function RatingModal({
  isOpen,
  onOpenChange,
  applicationId,
  scholarshipProviderId,
  scholarshipProviderName,
  onSubmitRating,
}: RatingModalProps) {
  const { t } = useTranslation('company');
  const [rating, setRating] = useState(0);
  const [hoverRating, setHoverRating] = useState(0);
  const [comment, setComment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (rating === 0) {
      toast.error(t('reviews.pleaseSelectRating'));
      return;
    }

    try {
      setIsSubmitting(true);
      await onSubmitRating(applicationId, scholarshipProviderId, rating, comment);
      toast.success(t('reviews.submitSuccess'));
      onOpenChange(false);
    } catch {
      toast.error(t('reviews.submitError'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog.Root open={isOpen} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-xl bg-bg-elevated p-6 shadow-2xl">
          <div className="flex items-center justify-between mb-4">
            <Dialog.Title className="text-xl font-semibold text-text-primary">
              {t('reviews.rateScholarshipProviderTitle', { company: scholarshipProviderName })}
            </Dialog.Title>
            <Dialog.Close className="text-text-tertiary hover:text-text-secondary transition-colors">
              <X size={20} />
            </Dialog.Close>
          </div>

          <Dialog.Description className="mb-6 text-sm text-text-secondary">
            {t('reviews.rateScholarshipProviderDescription')}
          </Dialog.Description>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="flex justify-center space-x-2">
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  key={star}
                  type="button"
                  onMouseEnter={() => setHoverRating(star)}
                  onMouseLeave={() => setHoverRating(0)}
                  onClick={() => setRating(star)}
                  className="focus:outline-none focus:ring-2 focus:ring-brand-300 focus:ring-offset-2 rounded-full p-1 transition-transform hover:scale-110"
                >
                  <Star
                    size={32}
                    className={`${
                      (hoverRating || rating) >= star
                        ? 'fill-amber-400 text-amber-400'
                        : 'text-border-strong'
                    } transition-colors`}
                  />
                </button>
              ))}
            </div>

            <div className="space-y-2">
              <label htmlFor="comment" className="block text-sm font-medium text-text-secondary">
                {t('reviews.commentLabel')} <span className="text-text-tertiary text-xs font-normal">({t('reviews.optional')})</span>
              </label>
              <textarea
                id="comment"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder={t('reviews.commentPlaceholder')}
                rows={4}
                className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-300 focus:outline-none focus:ring-1 focus:ring-brand-100"
              />
            </div>

            <div className="flex justify-end space-x-3 pt-4">
              <Dialog.Close asChild>
                <button
                  type="button"
                  className="rounded-md px-4 py-2 text-sm font-medium text-text-secondary hover:bg-bg-subtle transition-colors"
                >
                  {t('common.cancel')}
                </button>
              </Dialog.Close>
              <button
                type="submit"
                disabled={isSubmitting || rating === 0}
                className="rounded-md bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-sm"
              >
                {isSubmitting ? t('common.submitting') : t('common.submit')}
              </button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
