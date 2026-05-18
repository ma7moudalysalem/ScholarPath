import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useLocation } from "react-router";
import { toast } from "sonner";
import { useConsultantDetailQuery } from "@/hooks/useConsultantsQuery";
import { useRequestBookingMutation } from "@/hooks/useBookingsQuery";
import { durationLabel, formatDate, formatTime, formatUsd } from "@/lib/bookingFormat";
import { StripeCheckout } from "@/components/common/StripeCheckout";

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
  const [createdBookingId, setCreatedBookingId] = useState<string | null>(null);
  const [paid, setPaid] = useState(false);

  const durationMinutes = useMemo(() => {
    if (!hasCompleteSlotParams) return 0;
    return Math.max(
      0,
      Math.round((new Date(endAt).getTime() - new Date(startAt).getTime()) / 60_000),
    );
  }, [endAt, hasCompleteSlotParams, startAt]);

  const feeAmount = consultant?.sessionFeeUsd ?? 0;

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
        notes: null,
      },
      {
        onSuccess: (result) => {
          setCreatedBookingId(result.bookingId);
          // A zero-fee session has nothing to charge — skip straight to done.
          if (feeAmount <= 0) setPaid(true);
        },
        onError: () => toast.error(t("states.error")),
      },
    );
  }

  // ── No slot selected ───────────────────────────────────────────────────────
  if (!hasCompleteSlotParams) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
              {t("checkout.title")}
            </h1>
            <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("checkout.subtitle")}</p>
          </div>

          <div className="mt-8 rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
            <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              {t("checkout.noSlot.title")}
            </h2>
            <p className="mt-3 text-sm leading-7 text-[#4b5563]">
              {t("checkout.noSlot.description")}
            </p>
            <div className="mt-6 flex flex-col gap-3 sm:flex-row">
              <Link
                to="/student/consultants"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
              >
                {t("checkout.noSlot.browseConsultants")}
              </Link>
              <Link
                to="/student/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
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
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-4">
            <div className="h-10 w-64 animate-pulse rounded-lg bg-white" />
            <div className="h-96 animate-pulse rounded-2xl border border-[#e5e7eb] bg-white shadow-sm" />
          </div>
        </section>
      </main>
    );
  }

  // ── Error loading consultant ───────────────────────────────────────────────
  if (isError || !consultant) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-[#fecaca] bg-[#fef2f2] p-6 text-sm font-medium text-[#dc2626]">
            {t("states.error")}
          </div>
          <div className="mt-6">
            <Link
              to="/student/consultants"
              className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
            >
              {t("checkout.noSlot.browseConsultants")}
            </Link>
          </div>
        </section>
      </main>
    );
  }

  const sessionDetails = (
    <div className="grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 lg:grid-cols-3">
      <Detail label={t("fields.consultant")} value={consultant.name} />
      <Detail label={t("fields.sessionType")} value={t("sessionType")} />
      <Detail label={t("fields.selectedDate")} value={formatDate(startAt, lang)} />
      <Detail
        label={t("fields.selectedTime")}
        value={`${formatTime(startAt, lang)} – ${formatTime(endAt, lang)}`}
      />
      <Detail label={t("fields.duration")} value={durationLabel(durationMinutes, t)} />
      <Detail label={t("fields.consultantFee")} value={formatUsd(feeAmount)} />
    </div>
  );

  const priceSummary = (
    <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
      <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
        {t("checkout.priceSummary.title")}
      </h2>
      <div className="mt-5 space-y-4 text-sm">
        <div className="flex items-center justify-between">
          <span className="text-[#4b5563]">{t("checkout.priceSummary.sessionFee")}</span>
          <span className="font-medium text-[#1d1d1f]">{formatUsd(feeAmount)}</span>
        </div>
        <div className="border-t border-[#e5e7eb] pt-4">
          <div className="flex items-center justify-between">
            <span className="font-medium text-[#1d1d1f]">{t("checkout.priceSummary.total")}</span>
            <span className="text-2xl font-semibold text-[#1d1d1f]">{formatUsd(feeAmount)}</span>
          </div>
        </div>
      </div>
      <div className="mt-6 rounded-xl bg-[#f9fafb] p-4">
        <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
          {t("fields.selectedSlot")}
        </p>
        <p className="mt-2 text-sm font-medium text-[#1d1d1f]">{slotSummary}</p>
      </div>
    </aside>
  );

  // ── Done — booking requested + payment authorized ──────────────────────────
  if (createdBookingId && paid) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
              {t("checkout.title")}
            </h1>
            <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("checkout.subtitle")}</p>
          </div>

          <div className="mt-8 rounded-xl border border-[#bbf7d0] bg-[#f0fdf4] p-6">
            <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
              <div>
                <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#166534]">
                  {t("checkout.success.title")}
                </h2>
                <p className="mt-3 max-w-3xl text-sm leading-7 text-[#166534]">
                  {t("checkout.success.description")}
                </p>
              </div>
              <span className="inline-flex rounded-full bg-[#dcfce7] px-3 py-1 text-xs font-medium text-[#15803d]">
                {t("checkout.success.badge")}
              </span>
            </div>
          </div>

          <div className="mt-6 grid gap-6 lg:grid-cols-12">
            <section className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("checkout.success.summaryTitle")}
              </h3>
              <div className="mt-5">{sessionDetails}</div>
            </section>

            <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("checkout.priceSummary.title")}
              </h3>
              <div className="mt-5 space-y-4 text-sm">
                <div className="flex items-center justify-between">
                  <span className="text-[#4b5563]">{t("checkout.priceSummary.sessionFee")}</span>
                  <span className="font-medium text-[#1d1d1f]">{formatUsd(feeAmount)}</span>
                </div>
                <div className="border-t border-[#e5e7eb] pt-4">
                  <div className="flex items-center justify-between">
                    <span className="font-medium text-[#1d1d1f]">
                      {t("checkout.priceSummary.total")}
                    </span>
                    <span className="text-2xl font-semibold text-[#1d1d1f]">
                      {formatUsd(feeAmount)}
                    </span>
                  </div>
                </div>
              </div>
              <div className="mt-6 flex flex-col gap-3">
                <Link
                  to={`/student/bookings/${createdBookingId}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  {t("checkout.success.openThisBooking")}
                </Link>
                <Link
                  to="/student/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
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
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
              {t("checkout.title")}
            </h1>
            <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("checkout.subtitle")}</p>
          </div>

          <div className="mt-8 grid gap-6 lg:grid-cols-12">
            <section className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("checkout.paymentStep.title")}
              </h2>
              <p className="mt-2 text-sm leading-6 text-[#4b5563]">
                {t("checkout.paymentStep.description")}
              </p>

              <div className="mt-5">{sessionDetails}</div>

              <div className="mt-6 rounded-xl border border-[#e5e7eb] p-5">
                <StripeCheckout
                  bookingId={createdBookingId}
                  amountCents={Math.round(feeAmount * 100)}
                  currency="USD"
                  onSuccess={() => setPaid(true)}
                />
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
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            {t("checkout.title")}
          </h1>
          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("checkout.subtitle")}</p>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <form
            id="checkout-form"
            className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8"
            onSubmit={handleRequestBooking}
          >
            <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              {t("checkout.sessionSummary.title")}
            </h2>
            <p className="mt-2 text-sm leading-6 text-[#4b5563]">
              {t("checkout.sessionSummary.description")}
            </p>

            <div className="mt-5">{sessionDetails}</div>

            <div className="mt-6 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
              <p className="text-sm font-medium text-[#1d1d1f]">{t("checkout.holdNotice.title")}</p>
              <p className="mt-2 text-sm leading-7 text-[#4b5563]">
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
                  className="mt-1 h-4 w-4 accent-[#2563eb]"
                />
                <span className="text-sm text-[#1d1d1f]">
                  {t("checkout.holdNotice.acceptLabel")}
                </span>
              </label>
              {acceptError && (
                <p className="mt-2 text-sm text-[#dc2626]">{t("errors.holdNoticeRequired")}</p>
              )}
            </div>
          </form>

          <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
            <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              {t("checkout.priceSummary.title")}
            </h2>
            <div className="mt-5 space-y-4 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-[#4b5563]">{t("checkout.priceSummary.sessionFee")}</span>
                <span className="font-medium text-[#1d1d1f]">{formatUsd(feeAmount)}</span>
              </div>
              <div className="border-t border-[#e5e7eb] pt-4">
                <div className="flex items-center justify-between">
                  <span className="font-medium text-[#1d1d1f]">
                    {t("checkout.priceSummary.total")}
                  </span>
                  <span className="text-2xl font-semibold text-[#1d1d1f]">
                    {formatUsd(feeAmount)}
                  </span>
                </div>
              </div>
            </div>

            <div className="mt-6 rounded-xl bg-[#f9fafb] p-4">
              <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                {t("fields.selectedSlot")}
              </p>
              <p className="mt-2 text-sm font-medium text-[#1d1d1f]">{slotSummary}</p>
            </div>

            <div className="mt-6 flex flex-col gap-3">
              <button
                type="submit"
                form="checkout-form"
                disabled={requestBooking.isPending}
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {requestBooking.isPending
                  ? t("states.submitting")
                  : t("checkout.continueToPayment")}
              </button>
              <Link
                to={`/student/consultants/${consultant.id}`}
                className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
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
      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">{label}</p>
      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{value}</p>
    </div>
  );
}
