import { useEffect, useMemo, useState } from "react";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { toast } from "sonner";
import { paymentsApi } from "@/services/api/payments";

interface StripeCheckoutProps {
  bookingId: string;
  amountCents: number;
  currency?: string;
  onSuccess?: () => void;
}

/**
 * REFERENCE STRIPE CHECKOUT — Nora replaces stub backend with real PaymentIntent creation.
 * Flow: create PaymentIntent (capture_method=manual) → render PaymentElement → confirm → hold.
 * Capture happens server-side when Consultant accepts the booking.
 */
export function StripeCheckout({ bookingId, amountCents, currency = "USD", onSuccess }: StripeCheckoutProps) {
  const publishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY;
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const stripePromise = useMemo<Promise<Stripe | null> | null>(
    () => (publishableKey && publishableKey !== "pk_test_PLACEHOLDER" ? loadStripe(publishableKey) : null),
    [publishableKey],
  );

  useEffect(() => {
    let cancelled = false;
    paymentsApi
      .createIntent({ amountCents, currency, captureMethod: "manual", bookingId })
      .then((res) => {
        if (!cancelled) setClientSecret(res.clientSecret);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to create intent");
      });
    return () => {
      cancelled = true;
    };
  }, [amountCents, currency, bookingId]);

  if (!publishableKey || publishableKey === "pk_test_PLACEHOLDER") {
    return (
      <div className="rounded-lg border border-warning-500/40 bg-warning-50 p-4 text-sm text-warning-500">
        Set <code>VITE_STRIPE_PUBLISHABLE_KEY</code> in <code>.env</code> to enable checkout.
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-lg border border-danger-500/40 bg-danger-50 p-4 text-sm text-danger-500">
        {error}
      </div>
    );
  }

  if (!clientSecret || !stripePromise) {
    return <div className="text-sm text-text-tertiary">Preparing secure checkout…</div>;
  }

  return (
    <Elements stripe={stripePromise} options={{ clientSecret, appearance: { theme: "stripe" } }}>
      <CheckoutInner onSuccess={onSuccess} />
    </Elements>
  );
}

function CheckoutInner({ onSuccess }: { onSuccess?: () => void }) {
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!stripe || !elements) return;
    setSubmitting(true);

    const { error } = await stripe.confirmPayment({
      elements,
      redirect: "if_required",
    });

    setSubmitting(false);

    if (error) {
      toast.error(error.message ?? "Payment failed");
      return;
    }

    toast.success("Payment authorized — held until consultant accepts.");
    onSuccess?.();
  };

  return (
    <form onSubmit={(e) => void onSubmit(e)} className="space-y-4">
      <PaymentElement />
      <button
        type="submit"
        disabled={!stripe || submitting}
        className="cta-pill w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 disabled:opacity-50 dark:bg-brand-500 dark:text-text-on-brand"
      >
        {submitting ? "Authorizing…" : "Authorize payment (hold)"}
      </button>
    </form>
  );
}
