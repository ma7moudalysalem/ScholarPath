import { useEffect, useMemo, useState } from "react";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { paymentsApi, type PaymentType } from "@/services/api/payments";

interface StripeCheckoutProps {
  /**
   * Which payment kind this checkout authorises. ConsultantBooking and
   * ScholarshipProviderReview both use manual-capture intents; the capture happens
   * server-side when the consultant accepts the booking or the company
   * accepts the application review. Defaults to ConsultantBooking for
   * legacy callers that pre-date the dual-flow rework.
   */
  paymentType?: PaymentType;
  /** Consultant booking id — required when paymentType is ConsultantBooking. */
  bookingId?: string;
  /** Application id — required when paymentType is ScholarshipProviderReview. */
  applicationId?: string;
  amountCents: number;
  currency?: string;
  /**
   * When provided, this PaymentIntent's client secret is confirmed directly —
   * the widget does NOT create a new intent. The booking flow passes the intent
   * RequestBooking already created, so a booking keeps exactly one intent
   * (PB-006 gap report, Problem 1).
   */
  clientSecret?: string | null;
  /** Path to redirect to after successful authorisation. Defaults match the type. */
  returnUrlPath?: string;
  onSuccess?: () => void;
}

/**
 * Real Stripe Elements checkout for either a consultant booking or a company
 * review (application) fee.
 *
 * Flow (auto-create branch):
 *   - Calls `POST /api/payments/intent` with the correct type + related id.
 *   - Renders the Stripe PaymentElement.
 *   - Student confirms → funds authorized (held) on the card.
 *   - Capture happens server-side when the counterparty accepts.
 *
 * Flow (preset clientSecret branch):
 *   - The intent was already created by the back-end (e.g. RequestBooking
 *     returns one) and the caller passes its clientSecret — we never create a
 *     second one.
 */
export function StripeCheckout({
  paymentType = "ConsultantBooking",
  bookingId,
  applicationId,
  amountCents,
  currency = "USD",
  clientSecret: presetClientSecret,
  returnUrlPath,
  onSuccess,
}: StripeCheckoutProps) {
  const { t } = useTranslation("bookings");
  const publishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY;
  const [clientSecret, setClientSecret] = useState<string | null>(presetClientSecret ?? null);
  const [error, setError] = useState<string | null>(null);

  const configured = Boolean(publishableKey) && publishableKey !== "pk_test_PLACEHOLDER";

  const stripePromise = useMemo<Promise<Stripe | null> | null>(
    () => (configured ? loadStripe(publishableKey) : null),
    [configured, publishableKey],
  );

  // Compute the related-id once per render so the effect doesn't have to
  // branch on payment type itself; this also lets us validate the props
  // outside the effect (avoids react-hooks/set-state-in-effect).
  const relatedBookingId =
    paymentType === "ConsultantBooking" ? bookingId ?? null : null;
  const relatedApplicationId =
    paymentType === "ScholarshipProviderReview" ? applicationId ?? null : null;

  const propsError = !configured
    ? null
    : paymentType === "ConsultantBooking" && !relatedBookingId && !presetClientSecret
      ? "Missing bookingId for ConsultantBooking checkout."
      : paymentType === "ScholarshipProviderReview" && !relatedApplicationId && !presetClientSecret
        ? "Missing applicationId for ScholarshipProviderReview checkout."
        : null;

  useEffect(() => {
    // The booking flow passes an existing intent's client secret — the intent
    // is already created, so the widget must not create a second one
    // (PB-006 gap report, Problem 1).
    if (!configured || presetClientSecret || propsError) return;

    let cancelled = false;
    paymentsApi
      .createIntent({
        type: paymentType,
        amountCents,
        currency,
        relatedBookingId,
        relatedApplicationId,
      })
      .then((res) => {
        if (!cancelled) setClientSecret(res.clientSecret);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : t("checkout.payment.failed"));
        }
      });
    return () => {
      cancelled = true;
    };
  }, [
    paymentType,
    amountCents,
    currency,
    relatedBookingId,
    relatedApplicationId,
    configured,
    presetClientSecret,
    propsError,
    t,
  ]);

  const effectiveError = error ?? propsError;

  if (!configured) {
    return (
      <div className="rounded-lg border border-warning-500/30 bg-warning-50 p-4 text-sm text-warning-600">
        Set <code>VITE_STRIPE_PUBLISHABLE_KEY</code> to enable checkout.
      </div>
    );
  }

  if (effectiveError) {
    return (
      <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
        {effectiveError}
      </div>
    );
  }

  if (!clientSecret || !stripePromise) {
    return <div className="text-sm text-text-tertiary">{t("checkout.payment.preparing")}</div>;
  }

  const resolvedReturnPath =
    returnUrlPath
    ?? (paymentType === "ScholarshipProviderReview" && applicationId
      ? `/student/applications/${applicationId}`
      : `/student/bookings/${bookingId ?? ""}`);

  return (
    <Elements stripe={stripePromise} options={{ clientSecret, appearance: { theme: "stripe" } }}>
      <CheckoutInner returnUrlPath={resolvedReturnPath} onSuccess={onSuccess} />
    </Elements>
  );
}

function CheckoutInner({
  returnUrlPath,
  onSuccess,
}: {
  returnUrlPath: string;
  onSuccess?: () => void;
}) {
  const { t } = useTranslation("bookings");
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;
    setSubmitting(true);

    // `return_url` is required for redirect-based payment methods (Cash App Pay,
    // Amazon Pay, etc.).  Without it Stripe returns a 400 for those methods even
    // when `redirect: "if_required"` is set — Stripe still needs a fallback URL
    // in case the payment method forces a redirect.
    const returnUrl = `${window.location.origin}${returnUrlPath}`;
    const { error } = await stripe.confirmPayment({
      elements,
      redirect: "if_required",
      confirmParams: { return_url: returnUrl },
    });

    setSubmitting(false);

    if (error) {
      toast.error(error.message ?? t("checkout.payment.failed"));
      return;
    }

    toast.success(t("checkout.payment.authorized"));
    onSuccess?.();
  };

  return (
    <form onSubmit={(e) => void onSubmit(e)} className="space-y-4">
      <PaymentElement />
      <button
        type="submit"
        disabled={!stripe || submitting}
        className="inline-flex h-12 w-full items-center justify-center rounded-lg bg-brand-500 px-5 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
      >
        {submitting
          ? t("checkout.payment.authorizing")
          : t("checkout.payment.authorize")}
      </button>
    </form>
  );
}
