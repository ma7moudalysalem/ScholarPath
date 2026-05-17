import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { Link, useLocation } from "react-router";
import { toast } from "sonner";
import { useConsultantDetailQuery } from "@/hooks/useConsultantsQuery";
import { useRequestBookingMutation } from "@/hooks/useBookingsQuery";
import { durationLabel, formatDate, formatTime, formatUsd } from "@/lib/bookingFormat";

// ── Checkout slot contract ────────────────────────────────────────────────────
//
// The consultant browse / detail pages link here with the chosen slot encoded
// in the query string: `?consultantId=…&availabilityId=…&start=<ISO>&end=<ISO>`.
// `start`/`end` are ISO-8601 instants taken straight from a real `BookableSlot`.

type CheckoutFormState = {
  cardholderName: string;
  cardNumber: string;
  expiryDate: string;
  cvc: string;
  billingEmail: string;
  billingCountry: string;
  savePaymentMethod: boolean;
  acceptHoldNotice: boolean;
};

type CheckoutErrors = Partial<Record<keyof CheckoutFormState, string>>;

const defaultFormState: CheckoutFormState = {
  cardholderName: "",
  cardNumber: "",
  expiryDate: "",
  cvc: "",
  billingEmail: "",
  billingCountry: "Egypt",
  savePaymentMethod: false,
  acceptHoldNotice: false,
};

const billingCountries = [
  "Egypt",
  "Saudi Arabia",
  "United Arab Emirates",
  "Jordan",
  "Qatar",
] as const;

const billingCountryKeys: Record<string, string> = {
  Egypt: "checkout.countries.egypt",
  "Saudi Arabia": "checkout.countries.saudiArabia",
  "United Arab Emirates": "checkout.countries.unitedArabEmirates",
  Jordan: "checkout.countries.jordan",
  Qatar: "checkout.countries.qatar",
};

function sanitizeDigits(value: string) {
  return value.replace(/\D/g, "");
}

function formatCardNumber(value: string) {
  return sanitizeDigits(value)
    .slice(0, 16)
    .replace(/(.{4})/g, "$1 ")
    .trim();
}

function formatExpiryDate(value: string) {
  const digits = sanitizeDigits(value).slice(0, 4);
  if (digits.length <= 2) return digits;
  return `${digits.slice(0, 2)}/${digits.slice(2)}`;
}

function validateForm(values: CheckoutFormState, t: TFunction): CheckoutErrors {
  const errors: CheckoutErrors = {};

  if (!values.cardholderName.trim()) {
    errors.cardholderName = t("errors.cardholderNameRequired");
  }

  if (sanitizeDigits(values.cardNumber).length < 16) {
    errors.cardNumber = t("errors.cardNumberDigits");
  }

  const expiryDigits = sanitizeDigits(values.expiryDate);
  if (expiryDigits.length !== 4) {
    errors.expiryDate = t("errors.expiryFormat");
  } else {
    const month = Number(expiryDigits.slice(0, 2));
    if (month < 1 || month > 12) {
      errors.expiryDate = t("errors.expiryMonthRange");
    }
  }

  if (sanitizeDigits(values.cvc).length < 3) {
    errors.cvc = t("errors.cvcDigits");
  }

  if (!values.billingEmail.trim()) {
    errors.billingEmail = t("errors.billingEmailRequired");
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.billingEmail.trim())) {
    errors.billingEmail = t("errors.billingEmailInvalid");
  }

  if (!values.billingCountry.trim()) {
    errors.billingCountry = t("errors.billingCountryRequired");
  }

  if (!values.acceptHoldNotice) {
    errors.acceptHoldNotice = t("errors.holdNoticeRequired");
  }

  return errors;
}

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

  const [form, setForm] = useState<CheckoutFormState>(defaultFormState);
  const [errors, setErrors] = useState<CheckoutErrors>({});
  const [createdBookingId, setCreatedBookingId] = useState<string | null>(null);

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

  function updateField<K extends keyof CheckoutFormState>(key: K, value: CheckoutFormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
    setErrors((current) => ({ ...current, [key]: undefined }));
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!hasCompleteSlotParams || !consultant) return;

    const nextErrors = validateForm(form, t);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;

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
          toast.success(t("checkout.success.title"));
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

  // ── Booking created ────────────────────────────────────────────────────────
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

              <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 lg:grid-cols-3">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.requestStatus")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {t("checkout.success.requestStatusValue")}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.consultant")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.name}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.sessionType")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{t("sessionType")}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.selectedDate")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {formatDate(startAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.selectedTime")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {formatTime(startAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.duration")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {durationLabel(durationMinutes, t)}
                  </p>
                </div>
              </div>
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

  // ── Checkout form ──────────────────────────────────────────────────────────
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
            onSubmit={handleSubmit}
          >
            <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              {t("checkout.sessionSummary.title")}
            </h2>

            <p className="mt-2 text-sm leading-6 text-[#4b5563]">
              {t("checkout.sessionSummary.description")}
            </p>

            <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2">
              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("fields.consultant")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.name}</p>
                {consultant.expertiseTags.length > 0 ? (
                  <p className="mt-2 text-sm leading-6 text-[#4b5563]">
                    {consultant.expertiseTags.join(" · ")}
                  </p>
                ) : null}
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("fields.sessionType")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{t("sessionType")}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("fields.selectedDate")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {formatDate(startAt, lang)}
                </p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("fields.selectedTime")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {formatTime(startAt, lang)} – {formatTime(endAt, lang)}
                </p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("fields.duration")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {durationLabel(durationMinutes, t)}
                </p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("fields.consultantFee")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{formatUsd(feeAmount)}</p>
              </div>
            </div>

            <div className="mt-6">
              <h3 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("checkout.payment.methodTitle")}
              </h3>

              <div className="mt-4 rounded-xl border border-[#2563eb] bg-[#eff6ff] p-4">
                <div className="flex items-start gap-3">
                  <input checked readOnly type="radio" className="mt-1 h-4 w-4 accent-[#2563eb]" />
                  <div>
                    <p className="text-sm font-medium text-[#1d1d1f]">
                      {t("checkout.payment.cardOption")}
                    </p>
                    <p className="mt-1 text-sm leading-6 text-[#4b5563]">
                      {t("checkout.payment.cardOptionDescription")}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-6 grid gap-5 sm:grid-cols-2">
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-[#1d1d1f]">
                  {t("checkout.payment.cardholderName")}
                </label>
                <input
                  type="text"
                  value={form.cardholderName}
                  onChange={(event) => updateField("cardholderName", event.target.value)}
                  placeholder={t("checkout.payment.cardholderNamePlaceholder")}
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.cardholderName
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.cardholderName && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.cardholderName}</p>
                )}
              </div>

              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-[#1d1d1f]">
                  {t("checkout.payment.cardNumber")}
                </label>
                <input
                  type="text"
                  inputMode="numeric"
                  value={form.cardNumber}
                  onChange={(event) =>
                    updateField("cardNumber", formatCardNumber(event.target.value))
                  }
                  placeholder={t("checkout.payment.cardNumberPlaceholder")}
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.cardNumber
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.cardNumber && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.cardNumber}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">
                  {t("checkout.payment.expiryDate")}
                </label>
                <input
                  type="text"
                  inputMode="numeric"
                  value={form.expiryDate}
                  onChange={(event) =>
                    updateField("expiryDate", formatExpiryDate(event.target.value))
                  }
                  placeholder={t("checkout.payment.expiryDatePlaceholder")}
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.expiryDate
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.expiryDate && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.expiryDate}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">
                  {t("checkout.payment.cvc")}
                </label>
                <input
                  type="text"
                  inputMode="numeric"
                  value={form.cvc}
                  onChange={(event) =>
                    updateField("cvc", sanitizeDigits(event.target.value).slice(0, 3))
                  }
                  placeholder={t("checkout.payment.cvcPlaceholder")}
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.cvc
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.cvc && <p className="mt-2 text-sm text-[#dc2626]">{errors.cvc}</p>}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">
                  {t("checkout.payment.billingEmail")}
                </label>
                <input
                  type="email"
                  value={form.billingEmail}
                  onChange={(event) => updateField("billingEmail", event.target.value)}
                  placeholder={t("checkout.payment.billingEmailPlaceholder")}
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] ${
                    errors.billingEmail
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                />
                {errors.billingEmail && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.billingEmail}</p>
                )}
              </div>

              <div>
                <label className="block text-sm font-medium text-[#1d1d1f]">
                  {t("checkout.payment.billingCountry")}
                </label>
                <select
                  value={form.billingCountry}
                  onChange={(event) => updateField("billingCountry", event.target.value)}
                  className={`mt-2 h-12 w-full rounded-lg border bg-white px-4 text-sm text-[#1d1d1f] transition outline-none ${
                    errors.billingCountry
                      ? "border-[#ef4444] focus:border-[#ef4444]"
                      : "border-[#d1d5db] focus:border-[#2563eb]"
                  }`}
                >
                  {billingCountries.map((country) => (
                    <option key={country} value={country}>
                      {t(billingCountryKeys[country])}
                    </option>
                  ))}
                </select>
                {errors.billingCountry && (
                  <p className="mt-2 text-sm text-[#dc2626]">{errors.billingCountry}</p>
                )}
              </div>
            </div>

            <label className="mt-6 flex items-start gap-3 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
              <input
                type="checkbox"
                checked={form.savePaymentMethod}
                onChange={(event) => updateField("savePaymentMethod", event.target.checked)}
                className="mt-1 h-4 w-4 accent-[#2563eb]"
              />
              <div>
                <p className="text-sm font-medium text-[#1d1d1f]">
                  {t("checkout.savePaymentMethod.label")}
                </p>
                <p className="mt-1 text-sm leading-6 text-[#4b5563]">
                  {t("checkout.savePaymentMethod.description")}
                </p>
              </div>
            </label>

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
                  checked={form.acceptHoldNotice}
                  onChange={(event) => updateField("acceptHoldNotice", event.target.checked)}
                  className="mt-1 h-4 w-4 accent-[#2563eb]"
                />
                <span className="text-sm text-[#1d1d1f]">
                  {t("checkout.holdNotice.acceptLabel")}
                </span>
              </label>

              {errors.acceptHoldNotice && (
                <p className="mt-2 text-sm text-[#dc2626]">{errors.acceptHoldNotice}</p>
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

            <div className="mt-4 rounded-xl bg-[#f9fafb] p-4">
              <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                {t("fields.confirmation")}
              </p>
              <p className="mt-2 text-sm leading-7 text-[#4b5563]">
                {t("checkout.confirmationNote")}
              </p>
            </div>

            <div className="mt-6 flex flex-col gap-3">
              <button
                type="submit"
                form="checkout-form"
                disabled={requestBooking.isPending}
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {requestBooking.isPending ? t("states.submitting") : t("checkout.payNow")}
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
