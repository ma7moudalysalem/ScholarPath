import { useEffect, useMemo, useState } from "react";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { paymentsApi } from "@/services/api/payments";

interface StripeCheckoutProps {
  bookingId: string;
  amountCents: number;
  currency?: string;
  /**
   * When provided, this PaymentIntent's client secret is confirmed directly —
   * the widget does NOT create a new intent. The booking flow passes the intent
   * RequestBooking already created, so a booking keeps exactly one intent
   * (PB-006 gap report, Problem 1).
   */
  clientSecret?: string | null;
  onSuccess?: () => void;
}

/**
 * Real Stripe Elements checkout for a consultant booking.
 *
 * Flow: create a manual-capture PaymentIntent for the booking → render the
 * Stripe PaymentElement → confirm → the session fee is authorized (held).
 * Capture happens server-side when the consultant accepts the booking.
 */
export function StripeCheckout({
  bookingId,
  amountCents,
  currency = "USD",
  clientSecret: presetClientSecret,
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

  useEffect(() => {
    // The booking flow passes an existing intent's client secret — the intent
    // is already created, so the widget must not create a second one
    // (PB-006 gap report, Problem 1).
    if (!configured || presetClientSecret) return;
    let cancelled = false;
    paymentsApi
      .createIntent({
        type: "ConsultantBooking",
        amountCents,
        currency,
        relatedBookingId: bookingId,
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
  }, [amountCents, currency, bookingId, configured, presetClientSecret, t]);

  if (!configured) {
    return (
      <div className="rounded-lg border border-warning-500/30 bg-warning-50 p-4 text-sm text-warning-600">
        Set <code>VITE_STRIPE_PUBLISHABLE_KEY</code> to enable checkout.
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
        {error}
      </div>
    );
  }

  if (!clientSecret || !stripePromise) {
    return <div className="text-sm text-text-tertiary">{t("checkout.payment.preparing")}</div>;
  }

  return (
    <Elements stripe={stripePromise} options={{ clientSecret, appearance: { theme: "stripe" } }}>
      <CheckoutInner onSuccess={onSuccess} />
    </Elements>
  );
}

function CheckoutInner({ onSuccess }: { onSuccess?: () => void }) {
  const { t } = useTranslation("bookings");
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;
    setSubmitting(true);

    const { error } = await stripe.confirmPayment({ elements, redirect: "if_required" });

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
