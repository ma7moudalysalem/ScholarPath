import { useState } from "react";
import { useTranslation } from "react-i18next";
import * as Dialog from "@radix-ui/react-dialog";
import { X, Star } from "lucide-react";
import { toast } from "sonner";
import { useSubmitRatingMutation } from "@/hooks/useBookingsQuery";
import { apiErrorMessage } from "@/services/api/client";

interface RateConsultantModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  bookingId: string;
  consultantName: string;
}

/**
 * Student-side rating modal shown on a Completed booking's detail page.
 * Mirrors the company `RatingModal` pattern but wires to the booking-side
 * `useSubmitRatingMutation`. The CTA that mounts this modal is gated on
 * `status === "Completed" && !hasStudentReview`, so the duplicate-submission
 * surface is removed on refresh once the backend records the review.
 */
export function RateConsultantModal({
  isOpen,
  onOpenChange,
  bookingId,
  consultantName,
}: RateConsultantModalProps) {
  const { t } = useTranslation("bookings");
  const [rating, setRating] = useState(0);
  const [hoverRating, setHoverRating] = useState(0);
  const [comment, setComment] = useState("");
  const submitMut = useSubmitRatingMutation();

  const resetForm = () => {
    setRating(0);
    setHoverRating(0);
    setComment("");
  };

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      resetForm();
    }
    onOpenChange(open);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (rating < 1 || rating > 5) {
      toast.error(t("rating.errors.pleaseSelectRating"));
      return;
    }

    submitMut.mutate(
      {
        id: bookingId,
        input: {
          rating,
          comment: comment.trim() === "" ? null : comment.trim(),
        },
      },
      {
        onSuccess: () => {
          toast.success(t("rating.submitSuccess"));
          resetForm();
          onOpenChange(false);
        },
        onError: (err) => {
          toast.error(apiErrorMessage(err, t("rating.submitError")));
        },
      },
    );
  };

  return (
    <Dialog.Root open={isOpen} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-xl bg-bg-elevated p-6 shadow-2xl">
          <div className="mb-4 flex items-center justify-between">
            <Dialog.Title className="text-xl font-semibold text-text-primary">
              {t("rating.title", { consultant: consultantName })}
            </Dialog.Title>
            <Dialog.Close
              className="text-text-tertiary transition-colors hover:text-text-secondary"
              aria-label={t("rating.close")}
            >
              <X size={20} />
            </Dialog.Close>
          </div>

          <Dialog.Description className="mb-6 text-sm text-text-secondary">
            {t("rating.description")}
          </Dialog.Description>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div
              className="flex justify-center gap-2"
              role="radiogroup"
              aria-label={t("rating.starsAriaLabel")}
            >
              {[1, 2, 3, 4, 5].map((star) => {
                const isFilled = (hoverRating || rating) >= star;
                return (
                  <button
                    key={star}
                    type="button"
                    role="radio"
                    aria-checked={rating === star}
                    aria-label={t("rating.starAriaLabel", { count: star })}
                    onMouseEnter={() => setHoverRating(star)}
                    onMouseLeave={() => setHoverRating(0)}
                    onClick={() => setRating(star)}
                    className="rounded-full p-1 transition-transform hover:scale-110 focus:outline-none focus:ring-2 focus:ring-brand-300 focus:ring-offset-2"
                  >
                    <Star
                      size={32}
                      className={`${
                        isFilled
                          ? "fill-amber-400 text-amber-400"
                          : "text-border-strong"
                      } transition-colors`}
                    />
                  </button>
                );
              })}
            </div>

            <div className="space-y-2">
              <label
                htmlFor="rate-consultant-comment"
                className="block text-sm font-medium text-text-secondary"
              >
                {t("rating.commentLabel")}{" "}
                <span className="text-xs font-normal text-text-tertiary">
                  ({t("rating.optional")})
                </span>
              </label>
              <textarea
                id="rate-consultant-comment"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder={t("rating.commentPlaceholder")}
                rows={4}
                maxLength={2000}
                className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-300 focus:outline-none focus:ring-1 focus:ring-brand-100"
              />
            </div>

            <div className="flex justify-end gap-3 pt-4">
              <Dialog.Close asChild>
                <button
                  type="button"
                  className="rounded-md px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle"
                >
                  {t("rating.cancel")}
                </button>
              </Dialog.Close>
              <button
                type="submit"
                disabled={submitMut.isPending || rating === 0}
                className="rounded-md bg-brand-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {submitMut.isPending ? t("rating.submitting") : t("rating.submit")}
              </button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
