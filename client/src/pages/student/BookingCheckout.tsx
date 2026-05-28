import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useLocation } from "react-router";
import { toast } from "sonner";
import { useConsultantDetailQuery } from "@/hooks/useConsultantsQuery";
import { useRequestBookingMutation } from "@/hooks/useBookingsQuery";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { durationLabel, formatDate, formatTime, formatUsd } from "@/lib/bookingFormat";
import { StripeCheckout } from "@/components/common/StripeCheckout";
import { apiErrorMessage } from "@/services/api/client";

/** Validator on the server caps Notes at 1000 chars — mirror that here. */
const NOTES_MAX = 1000;

// ── Checkout slot contract ────────────────────────────────────────────────────
//
// The consultant browse / detail pages link here with the chosen slot encoded
// in the query string: `?consultantId=…&availabilityId=…&start=<ISO>&end=<ISO>`.
// `start`/`end` are ISO-8601 instants taken straight from a real `BookableSlot`.
//
// Flow: (1) review the slot + accept the hold notice → request the booking,
// (2) authorize the session fee with Stripe (a manual-capture hold), (3) done.

export function BookingCheckout() {
  const { t, i18n } = useTranslation("bookings");
  const lang = i18n.language;
  const location = useLocation();
  const query = useMemo(() => new URLSearchParams(location.search), [location.search]);

  const consultantId = query.get("consultantId") ?? "";
  const availabilityId = query.get("availabilityId");
  const startAt = query.get("start") ?? "";
  const endAt = query.get("end") ?? "";

  const hasCompleteSlotParams =
    Boolean(consultantId) &&
    Boolean(startAt) &&
    Boolean(endAt) &&
    !Number.isNaN(new Date(startAt).getTime()) &&
    !Number.isNaN(new Date(endAt).getTime());

  const { data: consultant, isLoading, isError } = useConsultantDetailQuery(
    hasCompleteSlotParams ? consultantId : undefined,
  );
  const requestBooking = useRequestBookingMutation();

  const [acceptHold, setAcceptHold] = useState(false);
  const [acceptError, setAcceptError] = useState(false);
  const [notes, setNotes] = useState("");
  const [createdBookingId, setCreatedBookingId] = useState<string | null>(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [paid, setPaid] = useState(false);

  const durationMinutes = useMemo(() => {
    if (!hasCompleteSlotParams) return 0;
    return Math.max(
      0,
      Math.round((new Date(endAt).getTime() - new Date(startAt).getTime()) / 60_000),
    );
  }, [endAt, hasCompleteSlotParams, startAt]);

  // Master payments switch: when off, the platform runs in fully-free mode,
  // so every booking is priced at 0 regardless of the consultant's stored fee.
  const paymentsEnabled = usePaymentsEnabled();
  const feeAmount = paymentsEnabled ? (consultant?.sessionFeeUsd ?? 0) : 0;

  const slotSummary = useMemo(() => {
    if (!hasCompleteSlotParams) return t("checkout.holdSummaryEmpty");
    return `${formatDate(startAt, lang)} · ${formatTime(startAt, lang)} – ${formatTime(
      endAt,
      lang,
    )}`;
  }, [endAt, hasCompleteSlotParams, lang, startAt, t]);

  function handleRequestBooking(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!hasCompleteSlotParams || !consultant) return;

    if (!acceptHold) {
      setAcceptError(true);
      return;
    }

    // Trim + null-out blank notes so the server stores a real null rather
    // than an empty string — keeps the consultant's details page tidy.
    const trimmedNotes = notes.trim();

    requestBooking.mutate(
      {
        consultantId: consultant.id,
        availabilityId: availabilityId || null,
        scheduledStartAt: new Date(startAt).toISOString(),
        scheduledEndAt: new Date(endAt).toISOString(),
        timezone:
          consultant.timezone ||
          Intl.DateTimeFormat().resolvedOptions().timeZone ||
          "UTC",
        notes: trimmedNotes.length === 0 ? null : trimmedNotes,
      },
      {
        onSuccess: (result) => {
          setCreatedBookingId(result.bookingId);
          setClientSecret(result.clientSecret);
          // Free session (consultant fee = 0): the server skips Stripe entirely
          // and returns isFree=true with no client secret. Skip the payment
          // step on the UI side too — the booking is already in Requested.
          if (result.isFree || feeAmount <= 0) setPaid(true);
        },
        // Surface the server-side detail (e.g. "Slot already taken") instead
        // of a generic "We couldn't load your bookings" — the latter is
        // misleading here because the load already succeeded.
        onError: (err) => toast.error(apiErrorMessage(err, t("states.error"))),
      },
    );
  }

  // ── No slot selected ───────────────────────────────────────────────────────
  if (!hasCompleteSlotParams) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
              {t("checkout.title")}
            </h1>
            <p className="max-w-3xl text-base leading-7 text-text-secondary">{t("checkout.subtitle")}</p>
          </div>

          <div className="mt-8 rounded-2xl border border-border-subtle bg-bg-elevated p-8 shadow-sm">
            <h2 className="text-2xl font-semibold tracking-[-0.01em] text-text-primary">
              {t("checkout.noSlot.title")}
            </h2>
            <p className="mt-3 text-sm leading-7 text-text-secondary">
              {t("checkout.noSlot.description")}
            </p>
            <div className="mt-6 flex flex-col gap-3 sm:flex-row">
              <Link
                to="/student/consultants"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
              >
                {t("checkout.noSlot.browseConsultants")}
              </Link>
              <Link
                to="/student/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-brand-500 bg-transparent px-5 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
              >
                {t("checkout.noSlot.openBookings")}
              </Link>
            </div>
          </div>
        </section>
      </main>
    );
  }

  // ── Loading consultant ─────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-4">
            <div className="h-10 w-64 animate-pulse rounded-lg bg-bg-elevated" />
            <div className="h-96 animate-pulse rounded-2xl border border-border-subtle bg-bg-elevated shadow-sm" />
          </div>
        </section>
      </main>
    );
  }

  // ── Error loading consultant ───────────────────────────────────────────────
  if (isError || !consultant) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm font-medium text-danger-500">
            {t("states.error")}
          </div>
          <div className="mt-6">
            <Link
              to="/student/consultants"
              className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
            >
              {t("checkout.noSlot.browseConsultants")}
            </Link>
          </div>
        </section>
      </main>
    );
  }

  const sessionDetails = (
    <div className="grid gap-4 rounded-xl bg-bg-muted p-5 sm:grid-cols-2 lg:grid-cols-3">
      <Detail label={t("fields.consultant")} value={consultant.name} />
      <Detail label={t("fields.sessionType")} value={t("sessionType")} />
      <Detail label={t("fields.selectedDate")} value={formatDate(startAt, lang)} />
      <Detail
        label={t("fields.selectedTime")}
        value={`${formatTime(startAt, lang)} – ${formatTime(endAt, lang)}`}
      />
      <Detail label={t("fields.duration")} value={durationLabel(durationMinutes, t)} />
      <Detail label={t("fields.consultantFee")} value={feeAmount === 0 ? t("scholarships:freeListing") : formatUsd(feeAmount)} />
    </div>
  );

  const priceSummary = (
    <aside className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-sm lg:col-span-4">
      <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
        {t("checkout.priceSummary.title")}
      </h2>
      <div className="mt-5 space-y-4 text-sm">
        <div className="flex items-center justify-between">
          <span className="text-text-secondary">{t("checkout.priceSummary.sessionFee")}</span>
          <span className="font-medium text-text-primary">{feeAmount === 0 ? t("scholarships:freeListing") : formatUsd(feeAmount)}</span>
        </div>
        <div className="border-t border-border-subtle pt-4">
          <div className="flex items-center justify-between">
            <span className="font-medium text-text-primary">{t("checkout.priceSummary.total")}</span>
            <span className="text-2xl font-semibold text-text-primary">{feeAmount === 0 ? t("scholarships:freeListing") : formatUsd(feeAmount)}</span>
          </div>
        </div>
      </div>
      <div className="mt-6 rounded-xl bg-bg-muted p-4">
        <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
          {t("fields.selectedSlot")}
        </p>
        <p className="mt-2 text-sm font-medium text-text-primary">{slotSummary}</p>
      </div>
    </aside>
  );

  // ── Done — booking requested + payment authorized ──────────────────────────
  if (createdBookingId && paid) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
              {t("checkout.title")}
            </h1>
            <p className="max-w-3xl text-base leading-7 text-text-secondary">{t("checkout.subtitle")}</p>
          </div>

          <div className="mt-8 rounded-xl border border-success-200 bg-success-50 p-6">
            <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
              <div>
                <h2 className="text-2xl font-semibold tracking-[-0.01em] text-success-700">
                  {t("checkout.success.title")}
                </h2>
                <p className="mt-3 max-w-3xl text-sm leading-7 text-success-700">
                  {t("checkout.success.description")}
                </p>
              </div>
              <span className="inline-flex rounded-full bg-success-100 px-3 py-1 text-xs font-medium text-success-600">
                {t("checkout.success.badge")}
              </span>
            </div>
          </div>

          <div className="mt-6 grid gap-6 lg:grid-cols-12">
            <section className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-sm lg:col-span-8">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("checkout.success.summaryTitle")}
              </h3>
              <div className="mt-5">{sessionDetails}</div>
            </section>

            <aside className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-sm lg:col-span-4">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("checkout.priceSummary.title")}
              </h3>
              <div className="mt-5 space-y-4 text-sm">
                <div className="flex items-center justify-between">
                  <span className="text-text-secondary">{t("checkout.priceSummary.sessionFee")}</span>
                  <span className="font-medium text-text-primary">{feeAmount === 0 ? t("scholarships:freeListing") : formatUsd(feeAmount)}</span>
                </div>
                <div className="border-t border-border-subtle pt-4">
                  <div className="flex items-center justify-between">
                    <span className="font-medium text-text-primary">
                      {t("checkout.priceSummary.total")}
                    </span>
                    <span className="text-2xl font-semibold text-text-primary">
                      {feeAmount === 0 ? t("scholarships:freeListing") : formatUsd(feeAmount)}
                    </span>
                  </div>
                </div>
              </div>
              <div className="mt-6 flex flex-col gap-3">
                <Link
                  to={`/student/bookings/${createdBookingId}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600"
                >
                  {t("checkout.success.openThisBooking")}
                </Link>
                <Link
                  to="/student/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-brand-500 bg-transparent px-5 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
                >
                  {t("checkout.success.openBookings")}
                </Link>
              </div>
            </aside>
          </div>
        </section>
      </main>
    );
  }

  // ── Payment step — booking requested, authorize the fee with Stripe ────────
  if (createdBookingId) {
    return (
      <main className="min-h-screen bg-bg-subtle">
        <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
              {t("checkout.title")}
            </h1>
            <p className="max-w-3xl text-base leading-7 text-text-secondary">{t("checkout.subtitle")}</p>
          </div>

          <div className="mt-8 grid gap-6 lg:grid-cols-12">
            <section className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-sm lg:col-span-8">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
                {t("checkout.paymentStep.title")}
              </h2>
              <p className="mt-2 text-sm leading-6 text-text-secondary">
                {t("checkout.paymentStep.description")}
              </p>

              <div className="mt-5">{sessionDetails}</div>

              <div className="mt-6 rounded-xl border border-border-subtle p-5">
                {clientSecret ? (
                  <StripeCheckout
                    bookingId={createdBookingId}
                    amountCents={Math.round(feeAmount * 100)}
                    currency="USD"
                    clientSecret={clientSecret}
                    onSuccess={() => setPaid(true)}
                  />
                ) : (
                  // Defensive: an idempotency-replay of RequestBookingCommand can
                  // return the cached PaymentIntent without surfacing its client
                  // secret again. Mount-time Stripe Elements then throws on a
                  // null secret and the user sees a cryptic JS error — show a
                  // friendly fallback instead and surface the booking link so
                  // they can cancel + retry rather than getting stuck.
                  <div className="rounded-lg border border-warning-200 bg-warning-50 p-4 text-sm leading-6 text-warning-700">
                    <p className="font-medium">{t("checkout.paymentMissing")}</p>
                    <div className="mt-4 flex gap-3">
                      <Link
                        to={`/student/bookings/${createdBookingId}`}
                        className="inline-flex h-10 items-center justify-center rounded-lg bg-brand-500 px-4 text-sm font-medium text-white transition hover:bg-brand-600"
                      >
                        {t("checkout.success.openThisBooking")}
                      </Link>
                    </div>
                  </div>
                )}
              </div>
            </section>

            {priceSummary}
          </div>
        </section>
      </main>
    );
  }

  // ── Review + request the booking ───────────────────────────────────────────
  return (
    <main className="min-h-screen bg-bg-subtle">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-text-primary">
            {t("checkout.title")}
          </h1>
          <p className="max-w-3xl text-base leading-7 text-text-secondary">{t("checkout.subtitle")}</p>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <form
            id="checkout-form"
            className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-sm lg:col-span-8"
            onSubmit={handleRequestBooking}
          >
            <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
              {t("checkout.sessionSummary.title")}
            </h2>
            <p className="mt-2 text-sm leading-6 text-text-secondary">
              {t("checkout.sessionSummary.description")}
            </p>

            <div className="mt-5">{sessionDetails}</div>

            <div className="mt-6">
              <label htmlFor="booking-notes" className="block text-sm font-medium text-text-primary">
                {t("checkout.notes.label")}
              </label>
              <textarea
                id="booking-notes"
                value={notes}
                maxLength={NOTES_MAX}
                onChange={(event) => setNotes(event.target.value)}
                placeholder={t("checkout.notes.placeholder")}
                rows={4}
                className="mt-2 w-full resize-y rounded-xl border border-border-default bg-bg-elevated p-3 text-sm text-text-primary transition outline-none placeholder:text-text-tertiary focus:border-brand-300 focus:ring-2 focus:ring-brand-100"
              />
              <div className="mt-1 flex items-center justify-between text-xs text-text-tertiary">
                <span>{t("checkout.notes.hint")}</span>
                <span>{notes.length}/{NOTES_MAX}</span>
              </div>
            </div>

            <div className="mt-6 rounded-xl border border-border-subtle bg-bg-muted p-4">
              <p className="text-sm font-medium text-text-primary">{t("checkout.holdNotice.title")}</p>
              <p className="mt-2 text-sm leading-7 text-text-secondary">
                {t("checkout.holdNotice.description")}
              </p>
            </div>

            <div className="mt-6">
              <label className="flex items-start gap-3">
                <input
                  type="checkbox"
                  checked={acceptHold}
                  onChange={(event) => {
                    setAcceptHold(event.target.checked);
                    setAcceptError(false);
                  }}
                  className="mt-1 h-4 w-4 accent-brand-500"
                />
                <span className="text-sm text-text-primary">
                  {t("checkout.holdNotice.acceptLabel")}
                </span>
              </label>
              {acceptError && (
                <p className="mt-2 text-sm text-danger-500">{t("errors.holdNoticeRequired")}</p>
              )}
            </div>
          </form>

          <aside className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-sm lg:col-span-4">
            <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
              {t("checkout.priceSummary.title")}
            </h2>
            <div className="mt-5 space-y-4 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-text-secondary">{t("checkout.priceSummary.sessionFee")}</span>
                <span className="font-medium text-text-primary">{feeAmount === 0 ? t("scholarships:freeListing") : formatUsd(feeAmount)}</span>
              </div>
              <div className="border-t border-border-subtle pt-4">
                <div className="flex items-center justify-between">
                  <span className="font-medium text-text-primary">
                    {t("checkout.priceSummary.total")}
                  </span>
                  <span className="text-2xl font-semibold text-text-primary">
                    {feeAmount === 0 ? t("scholarships:freeListing") : formatUsd(feeAmount)}
                  </span>
                </div>
              </div>
            </div>

            <div className="mt-6 rounded-xl bg-bg-muted p-4">
              <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">
                {t("fields.selectedSlot")}
              </p>
              <p className="mt-2 text-sm font-medium text-text-primary">{slotSummary}</p>
            </div>

            <div className="mt-6 flex flex-col gap-3">
              <button
                type="submit"
                form="checkout-form"
                disabled={requestBooking.isPending}
                className="inline-flex h-12 items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {requestBooking.isPending
                  ? t("states.submitting")
                  : t("checkout.continueToPayment")}
              </button>
              <Link
                to={`/student/consultants/${consultant.id}`}
                className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-brand-500 bg-transparent px-5 text-sm font-medium text-brand-500 transition hover:bg-brand-50"
              >
                {t("checkout.backToProfile")}
              </Link>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[10px] font-medium tracking-[0.02em] text-text-tertiary uppercase">{label}</p>
      <p className="mt-1 text-sm font-medium text-text-primary">{value}</p>
    </div>
  );
}
