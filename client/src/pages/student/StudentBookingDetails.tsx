import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";
import { toast } from "sonner";
import { Star } from "lucide-react";
import { useBookingDetailQuery, useCancelBookingMutation } from "@/hooks/useBookingsQuery";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { BookingRecordings } from "@/components/booking/BookingRecordings";
import { RateConsultantModal } from "@/components/booking/RateConsultantModal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { apiErrorMessage } from "@/services/api/client";
import type { BookingDetail } from "@/services/api/bookings";
import {
  durationLabel,
  formatDate,
  formatTime,
  formatUsd,
  statusBadgeClass,
  statusBucket,
  statusLabelKey,
} from "@/lib/bookingFormat";

type TimelineStep = {
  key: string;
  titleKey: string;
  descriptionKey: string;
  isDone: boolean;
};

/** Builds the four-step student timeline from the booking's real status. */
function buildTimeline(booking: BookingDetail): TimelineStep[] {
  const bucket = statusBucket(booking.status);
  const requested = true;
  const reviewed =
    booking.status !== "Requested" && booking.status !== "Expired";
  const confirmed = Boolean(booking.confirmedAt) || bucket === "completed";
  const finalised =
    bucket === "completed" || bucket === "closed";

  return [
    {
      key: "requested",
      titleKey: "timeline.requestedTitle",
      descriptionKey: "timeline.requestedDescription",
      isDone: requested,
    },
    {
      key: "review",
      titleKey: "timeline.reviewTitle",
      descriptionKey: "timeline.reviewDescription",
      isDone: reviewed,
    },
    {
      key: "confirmed",
      titleKey: "timeline.confirmedTitle",
      descriptionKey: "timeline.confirmedDescription",
      isDone: confirmed,
    },
    {
      key: "outcome",
      titleKey: "timeline.outcomeTitle",
      descriptionKey: "timeline.outcomeDescription",
      isDone: finalised || confirmed,
    },
  ];
}

export function StudentBookingDetails() {
  const { t, i18n } = useTranslation(["bookings", "payments"]);
  const lang = i18n.language;
  const { id } = useParams();

  const { data: booking, isLoading, isError } = useBookingDetailQuery(id);
  const cancelMut = useCancelBookingMutation();
  const [cancelOpen, setCancelOpen] = useState(false);
  // Master payments switch — hides the payment status card and refund row
  // when the platform is in free mode.
  const paymentsEnabled = usePaymentsEnabled();
  const [rateOpen, setRateOpen] = useState(false);

  // A booking can be cancelled by the student while it is still awaiting the
  // consultant's response — the held payment is released, no charge is taken.
  const handleCancel = () => {
    if (!id) return;
    cancelMut.mutate(id, {
      onSuccess: () => {
        toast.success(t("details.cancelSuccess"));
        setCancelOpen(false);
      },
      // Show the actual server reason ("Booking has already been confirmed",
      // "Free-cancel window closed", etc.) instead of a generic fallback so
      // the student knows what to do next.
      onError: (err) => {
        toast.error(apiErrorMessage(err, t("details.cancelError")));
        setCancelOpen(false);
      },
    });
  };

  const timeline = useMemo(
    () => (booking ? buildTimeline(booking) : []),
    [booking],
  );

  if (isLoading) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-4">
            <div className="h-10 w-64 animate-pulse rounded-lg bg-bg-elevated" />
            <div className="h-72 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm" />
            <div className="h-40 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm" />
          </div>
        </section>
      </main>
    );
  }

  if (isError || !booking) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-8 shadow-sm">
            <h1 className="text-2xl font-semibold text-text-primary">{t("notFound.title")}</h1>
            <p className="mt-3 text-sm leading-7 text-text-secondary">{t("notFound.description")}</p>
            <div className="mt-6">
              <Link
                to="/student/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
              >
                {t("notFound.backToBookings")}
              </Link>
            </div>
          </div>
        </section>
      </main>
    );
  }

  const bucket = statusBucket(booking.status);

  return (
    <main className="min-h-screen bg-bg-subtle">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
                {t("details.title")}
              </h1>

              <p className="mt-3 max-w-3xl text-base leading-7 text-text-secondary">
                {t("details.subtitle")}
              </p>
            </div>

            <span
              className={`inline-flex rounded-full px-3 py-1 text-xs font-medium ${statusBadgeClass(
                booking.status,
              )}`}
            >
              {t(statusLabelKey(booking.status))}
            </span>
          </div>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="space-y-6 lg:col-span-8">
            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("details.summaryTitle")}
              </h2>

              <p className="mt-3 text-sm leading-7 text-text-secondary">{t(`summaries.${bucket}`)}</p>

              <div className="mt-5 grid gap-4 rounded-xl bg-bg-muted p-5 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("fields.consultant")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {booking.consultantName}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("fields.sessionType")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">{t("sessionType")}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("fields.selectedDate")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {formatDate(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("fields.selectedTime")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {formatTime(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("fields.duration")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {durationLabel(booking.durationMinutes, t)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("fields.fee")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {!paymentsEnabled || booking.priceUsd === 0
                      ? t("scholarships:freeListing")
                      : formatUsd(booking.priceUsd)}
                  </p>
                </div>
              </div>
            </div>

            {paymentsEnabled && booking.paymentStatus ? (
              <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
                <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                  {t("details.paymentTitle")}
                </h2>
                <div className="mt-5 grid gap-4 rounded-xl bg-bg-muted p-5 sm:grid-cols-2">
                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                      {t("details.paymentStatus")}
                    </p>
                    <p className="mt-1 text-sm font-medium text-text-primary">
                      {t(`payments:paymentStatus.${booking.paymentStatus}`)}
                    </p>
                  </div>
                  {booking.refundedAmountCents != null && booking.refundedAmountCents > 0 ? (
                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                        {t("details.refundedAmount")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-text-primary">
                        {formatUsd(booking.refundedAmountCents / 100)}
                      </p>
                    </div>
                  ) : null}
                </div>
              </div>
            ) : null}

            {booking.status === "Confirmed" ? (
              <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
                <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                  {t("details.meetingTitle")}
                </h2>

                <div className="mt-5 rounded-xl border border-border-subtle bg-bg-muted p-5">
                  <p className="text-sm leading-7 text-text-secondary">{t("details.meetingNote")}</p>
                  <Link
                    to={`/meeting/${booking.id}`}
                    className="mt-4 inline-flex h-11 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                  >
                    {t("details.joinMeeting")}
                  </Link>
                </div>
              </div>
            ) : null}

            {booking.status !== "Requested" ? (
              <BookingRecordings bookingId={booking.id} />
            ) : null}

            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("details.timelineTitle")}
              </h2>

              <div className="mt-5 space-y-4">
                {timeline.map((step, index) => (
                  <div key={step.key} className="flex gap-4">
                    <div className="flex flex-col items-center">
                      <div
                        className={[
                          "mt-1 h-3.5 w-3.5 rounded-full",
                          step.isDone ? "bg-brand-500" : "bg-border-default",
                        ].join(" ")}
                      />
                      {index < timeline.length - 1 ? (
                        <div className="mt-2 h-full min-h-[48px] w-px bg-border-subtle" />
                      ) : null}
                    </div>

                    <div className="pb-4">
                      <p className="text-sm font-medium text-text-primary">{t(step.titleKey)}</p>
                      <p className="mt-1 text-sm leading-6 text-text-secondary">
                        {t(step.descriptionKey)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </section>

          <aside className="space-y-6 lg:col-span-4">
            {booking.status === "Completed" && !booking.hasStudentReview ? (
              <div className="rounded-2xl border border-brand-200 bg-brand-50 p-6 shadow-sm">
                <div className="flex items-start gap-3">
                  <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-brand-100 text-brand-600">
                    <Star aria-hidden className="size-5" />
                  </div>
                  <div className="min-w-0">
                    <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                      {t("rating.ctaTitle")}
                    </h2>
                    <p className="mt-1 text-sm leading-6 text-text-secondary">
                      {t("rating.ctaDescription")}
                    </p>
                    <button
                      type="button"
                      onClick={() => setRateOpen(true)}
                      className="mt-4 inline-flex h-11 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                    >
                      {t("rating.ctaButton")}
                    </button>
                  </div>
                </div>
              </div>
            ) : null}

            {booking.status === "Completed" && booking.hasStudentReview ? (
              <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
                <div className="flex items-start gap-3">
                  <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-success-50 text-success-600">
                    <Star aria-hidden className="size-5" />
                  </div>
                  <div className="min-w-0">
                    <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                      {t("rating.alreadyRatedTitle")}
                    </h2>
                    <p className="mt-1 text-sm leading-6 text-text-secondary">
                      {t("rating.alreadyRatedDescription")}
                    </p>
                  </div>
                </div>
              </div>
            ) : null}

            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("details.quickActionsTitle")}
              </h2>

              <div className="mt-5 flex flex-col gap-3">
                <Link
                  to="/student/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                >
                  {t("details.backToBookings")}
                </Link>

                <Link
                  to={`/student/consultants/${booking.consultantId}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-brand-500 bg-transparent px-5 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
                >
                  {t("details.viewConsultant")}
                </Link>

                <Link
                  to="/student/consultants"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-brand-500 bg-transparent px-5 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
                >
                  {t("details.bookAnother")}
                </Link>

                {booking.status === "Requested" && (
                  <button
                    type="button"
                    onClick={() => setCancelOpen(true)}
                    disabled={cancelMut.isPending}
                    className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-danger-500 bg-transparent px-5 text-sm font-medium text-danger-500 transition hover:bg-danger-50 disabled:opacity-50"
                  >
                    {cancelMut.isPending
                      ? t("details.cancelling")
                      : t("details.cancelBooking")}
                  </button>
                )}
              </div>
            </div>

            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("details.guidanceTitle")}
              </h2>

              <div className="mt-5 space-y-3 text-sm leading-7 text-text-secondary">
                <p>{t("guidance.pending")}</p>
                <p>{t("guidance.confirmed")}</p>
                <p>{t("guidance.closed")}</p>
              </div>
            </div>
          </aside>
        </div>
      </section>

      <ConfirmDialog
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        title={t("details.cancelBooking")}
        description={t("details.cancelConfirm")}
        variant="destructive"
        confirmLabel={t("details.cancelBooking")}
        loading={cancelMut.isPending}
        onConfirm={handleCancel}
      />

      <RateConsultantModal
        isOpen={rateOpen}
        onOpenChange={setRateOpen}
        bookingId={booking.id}
        consultantName={booking.consultantName}
      />
    </main>
  );
}
