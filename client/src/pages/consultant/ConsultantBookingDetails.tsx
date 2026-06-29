import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";
import { toast } from "sonner";
import {
  useAcceptBookingMutation,
  useBookingDetailQuery,
  useCancelBookingMutation,
  useMarkNoShowMutation,
  useRejectBookingMutation,
} from "@/hooks/useBookingsQuery";
import { apiErrorMessage } from "@/services/api/client";
import { BookingRecordings } from "@/components/booking/BookingRecordings";
import {
  durationLabel,
  formatDate,
  formatTimeWithTz,
  formatUsd,
  statusBadgeClass,
  statusBucket,
  statusLabelKey,
} from "@/lib/bookingFormat";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";

export function ConsultantBookingDetails() {
  const { t, i18n } = useTranslation("consultantPortal");
  const lang = i18n.language;
  const { id } = useParams();

  const { data: booking, isLoading, isError } = useBookingDetailQuery(id);
  // Master payments switch — gates the price label.
  const paymentsEnabled = usePaymentsEnabled();

  const acceptMutation = useAcceptBookingMutation();
  const rejectMutation = useRejectBookingMutation();
  const cancelMutation = useCancelBookingMutation();
  const noShowMutation = useMarkNoShowMutation();

  if (isLoading) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-4">
            <div className="h-10 w-72 animate-pulse rounded-lg bg-bg-elevated" />
            <div className="h-80 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm" />
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
            <h1 className="text-2xl font-semibold text-text-primary">{t("details.notFound.title")}</h1>
            <p className="mt-3 text-sm leading-7 text-text-secondary">
              {t("details.notFound.description")}
            </p>
            <div className="mt-6">
              <Link
                to="/consultant/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
              >
                {t("details.notFound.back")}
              </Link>
            </div>
          </div>
        </section>
      </main>
    );
  }

  const bucket = statusBucket(booking.status);
  const isRequested = booking.status === "Requested";
  const isConfirmed = booking.status === "Confirmed";
  const isBusy =
    acceptMutation.isPending ||
    rejectMutation.isPending ||
    cancelMutation.isPending ||
    noShowMutation.isPending;

  // Mutation onError handlers all route through apiErrorMessage so the
  // consultant sees the actual server reason — "Only requested bookings can
  // be rejected", "Booking has no Stripe payment intent to cancel", etc. —
  // instead of a generic "We couldn't load your bookings" fallback that's
  // misleading here because the load already succeeded.
  const handleAccept = () => {
    if (!isRequested) return;

    acceptMutation.mutate(booking.id, {
      onSuccess: () => toast.success(t("details.banner.accepted")),
      onError: (err) => toast.error(apiErrorMessage(err, t("states.error"))),
    });
  };

  const handleReject = () => {
    if (!isRequested) return;
    rejectMutation.mutate(booking.id, {
      onSuccess: () => toast.success(t("details.banner.rejected")),
      onError: (err) => toast.error(apiErrorMessage(err, t("states.error"))),
    });
  };

  const handleCancel = () => {
    if (!isRequested && !isConfirmed) return;
    cancelMutation.mutate(booking.id, {
      onSuccess: () => toast.success(t("details.banner.cancelled")),
      onError: (err) => toast.error(apiErrorMessage(err, t("states.error"))),
    });
  };

  const handleNoShow = () => {
    if (!isConfirmed) return;
    noShowMutation.mutate(booking.id, {
      onSuccess: () => toast.success(t("details.banner.noShow")),
      onError: (err) => toast.error(apiErrorMessage(err, t("states.error"))),
    });
  };

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
                {t("details.summaryCard.title")}
              </h2>

              <p className="mt-3 text-sm leading-7 text-text-secondary">
                {t(`details.summaryCard.text.${bucket}`)}
              </p>

              <div className="mt-5 grid gap-4 rounded-xl bg-bg-muted p-5 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("details.summaryCard.studentName")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">{booking.studentName}</p>
                </div>

                {booking.studentEmail ? (
                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                      {t("details.summaryCard.studentEmail")}
                    </p>
                    <p className="mt-1 text-sm font-medium text-text-primary">
                      {booking.studentEmail}
                    </p>
                  </div>
                ) : null}

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("details.summaryCard.sessionType")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">{t("sessionType")}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("details.summaryCard.selectedDate")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {formatDate(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("details.summaryCard.selectedTime")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {formatTimeWithTz(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("details.summaryCard.duration")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {durationLabel(booking.durationMinutes, t)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("details.summaryCard.fee")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-text-primary">
                    {!paymentsEnabled || booking.priceUsd === 0
                      ? t("scholarships:freeListing")
                      : formatUsd(booking.priceUsd)}
                  </p>
                </div>
              </div>

              {/* Student's optional context note — only renders when the student
                  actually attached one. Keeps the layout uncluttered for the
                  common "no note" case. */}
              {booking.studentNotes ? (
                <div className="mt-5 rounded-xl border border-border-subtle bg-bg-elevated p-5">
                  <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                    {t("details.summaryCard.studentNotes")}
                  </p>
                  <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-text-primary">
                    {booking.studentNotes}
                  </p>
                </div>
              ) : null}
            </div>

            {isConfirmed ? (
              <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
                <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                  {t("details.meeting.title")}
                </h2>

                <div className="mt-5 rounded-xl border border-border-subtle bg-bg-muted p-5">
                  <p className="text-sm leading-7 text-text-secondary">{t("details.meeting.note")}</p>
                  <Link
                    to={`/meeting/${booking.id}`}
                    className="mt-4 inline-flex h-11 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                  >
                    {t("details.meeting.join")}
                  </Link>
                </div>
              </div>
            ) : null}

            {!isRequested ? <BookingRecordings bookingId={booking.id} /> : null}

            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("details.guidance.title")}
              </h2>

              <div className="mt-5 rounded-xl border border-border-subtle bg-bg-muted p-5">
                <p className="text-sm leading-7 text-text-secondary">
                  {t(`details.guidance.note.${bucket}`)}
                </p>
              </div>
            </div>
          </section>

          <aside className="space-y-6 lg:col-span-4">
            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("details.actions.title")}
              </h2>

              <div className="mt-5 flex flex-col gap-3">
                <button
                  type="button"
                  onClick={handleAccept}
                  disabled={!isRequested || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    isRequested && !isBusy
                      ? "bg-brand-500 text-white hover:bg-brand-600"
                      : "cursor-not-allowed bg-border-subtle text-text-tertiary",
                  ].join(" ")}
                >
                  {acceptMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.accept")}
                </button>

                <button
                  type="button"
                  onClick={handleReject}
                  disabled={!isRequested || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    isRequested && !isBusy
                      ? "border border-danger-500 bg-bg-elevated text-danger-500 hover:bg-danger-50"
                      : "cursor-not-allowed border border-border-subtle bg-bg-elevated text-text-tertiary",
                  ].join(" ")}
                >
                  {rejectMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.reject")}
                </button>

                <button
                  type="button"
                  onClick={handleNoShow}
                  disabled={!isConfirmed || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    isConfirmed && !isBusy
                      ? "border border-warning-600 bg-bg-elevated text-warning-600 hover:bg-warning-50"
                      : "cursor-not-allowed border border-border-subtle bg-bg-elevated text-text-tertiary",
                  ].join(" ")}
                >
                  {noShowMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.noShow")}
                </button>

                <button
                  type="button"
                  onClick={handleCancel}
                  disabled={!isConfirmed || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    isConfirmed && !isBusy
                      ? "border border-danger-500 bg-bg-elevated text-danger-500 hover:bg-danger-50"
                      : "cursor-not-allowed border border-border-subtle bg-bg-elevated text-text-tertiary",
                  ].join(" ")}
                >
                  {cancelMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.cancel")}
                </button>

                <Link
                  to="/consultant/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-brand-500 bg-transparent px-5 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
                >
                  {t("details.actions.back")}
                </Link>
              </div>
            </div>

            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("details.decisionHelp.title")}
              </h2>

              <div className="mt-5 space-y-3 text-sm leading-7 text-text-secondary">
                <p>{t("details.decisionHelp.accept")}</p>
                <p>{t("details.decisionHelp.reject")}</p>
                <p>{t("details.decisionHelp.noShow")}</p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
