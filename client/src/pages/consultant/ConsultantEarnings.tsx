import { useMemo } from "react";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { ExternalLink, Loader2 } from "lucide-react";
import {
  paymentsApi,
  formatMoneyCents,
  type PagedPayments,
  type PaymentDto,
  type PayoutDto,
  type PaymentStatus,
  type PayoutStatus,
} from "@/services/api/payments";

function paymentBadge(s: PaymentStatus): string {
  switch (s) {
    case "Captured":
      return "bg-success-100 text-success-600";
    case "Held":
    case "Pending":
      return "bg-warning-50 text-warning-600";
    case "Refunded":
    case "PartiallyRefunded":
      return "bg-brand-50 text-brand-600";
    case "Failed":
    case "Cancelled":
      return "bg-danger-50 text-danger-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

function payoutBadge(s: PayoutStatus): string {
  switch (s) {
    case "Paid":
      return "bg-success-100 text-success-600";
    case "InTransit":
    case "Pending":
      return "bg-warning-50 text-warning-600";
    case "Failed":
      return "bg-danger-50 text-danger-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

/**
 * Per-payment percentage of the gross that was kept by the platform — the
 * consultant cares about this far more than the absolute amount. Falls back to
 * a dash when the payment is zero-gross (a guard rail; should never happen
 * for a real booking).
 */
function feePercent(p: PaymentDto): string {
  if (p.amountCents <= 0) return "—";
  const pct = (p.profitShareAmountCents / p.amountCents) * 100;
  return `${pct.toFixed(0)}%`;
}

export function ConsultantEarnings() {
  const { t, i18n } = useTranslation(["payments", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const dateLocale = isAr ? ar : undefined;
  // Explicit BCP-47 tag so currency formatting renders Arabic-Indic digits in AR.
  const numberLocale = isAr ? "ar-EG" : "en-US";
  const dash = (iso: string | null) =>
    iso ? format(new Date(iso), "yyyy-MM-dd", { locale: dateLocale }) : "—";

  const paymentsQuery = useQuery<PagedPayments>({
    queryKey: ["consultant", "earnings", "payments"],
    queryFn: () => paymentsApi.listPayments({ pageSize: 100 }),
  });
  const payoutsQuery = useQuery<PayoutDto[]>({
    queryKey: ["consultant", "earnings", "payouts"],
    queryFn: () => paymentsApi.listMyPayouts(),
  });

  // Memoise the unwrapped arrays — the literal `?? []` returns a fresh array
  // every render, which would invalidate every downstream useMemo's dep array
  // (react-hooks/exhaustive-deps flags this).
  const payments = useMemo(
    () => paymentsQuery.data?.items ?? [],
    [paymentsQuery.data],
  );
  const payouts = useMemo(() => payoutsQuery.data ?? [], [payoutsQuery.data]);

  // Roll up the gross / fee / net across every payment whose money actually
  // settled. Captured covers the simple flow; PartiallyRefunded means the
  // consultant still kept the un-refunded share (the server recomputes
  // PayeeAmountCents at refund time per FR-090, so payeeAmount is the kept
  // net even after a partial refund).
  const totals = useMemo(() => {
    const settled = payments.filter(
      (p) => p.status === "Captured" || p.status === "PartiallyRefunded",
    );
    const gross = settled.reduce((sum, p) => sum + p.amountCents, 0);
    const fees = settled.reduce((sum, p) => sum + p.profitShareAmountCents, 0);
    const net = settled.reduce((sum, p) => sum + p.payeeAmountCents, 0);
    return { gross, fees, net };
  }, [payments]);

  // Settled amounts share a single currency (USD on this platform); pick the
  // first available currency so future multi-currency support degrades safely
  // instead of mis-formatting EGP as USD.
  const displayCurrency = payments[0]?.currency ?? "USD";

  const totalPaidOut = payouts
    .filter((p) => p.status === "Paid")
    .reduce((sum, p) => sum + p.amountCents, 0);
  const awaitingPayout = payouts
    .filter((p) => p.status === "Pending" || p.status === "InTransit")
    .reduce((sum, p) => sum + p.amountCents, 0);

  // Stat tiles — ordered by how relevant each number is to the consultant:
  // their net first, then the gross + fee context, then payout state.
  const stats: { key: string; label: string; hint: string; value: number; emphasised?: boolean }[] = [
    {
      key: "net",
      label: t("payments:earnings.totalEarned"),
      hint: t("payments:earnings.totalEarnedHint"),
      value: totals.net,
      emphasised: true,
    },
    {
      key: "gross",
      label: t("payments:earnings.totalGross"),
      hint: t("payments:earnings.totalGrossHint"),
      value: totals.gross,
    },
    {
      key: "fees",
      label: t("payments:earnings.totalFees"),
      hint: t("payments:earnings.totalFeesHint"),
      value: totals.fees,
    },
    {
      key: "paid",
      label: t("payments:earnings.totalPaidOut"),
      hint: t("payments:earnings.totalPaidOutHint"),
      value: totalPaidOut,
    },
    {
      key: "awaiting",
      label: t("payments:earnings.awaitingPayout"),
      hint: t("payments:earnings.awaitingPayoutHint"),
      value: awaitingPayout,
    },
  ];

  const loading = paymentsQuery.isLoading || payoutsQuery.isLoading;

  const connectMut = useMutation({
    mutationFn: () => {
      const base = window.location.origin;
      return paymentsApi.connectOnboard(
        `${base}/consultant/earnings`,
        `${base}/consultant/earnings`,
      );
    },
    onSuccess: ({ onboardingUrl }) => {
      window.location.href = onboardingUrl;
    },
    onError: () => {
      toast.error(t("payments:earnings.setupPayoutsError"));
    },
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("payments:earnings.title")}</h1>
        <p className="mt-1 text-sm text-text-secondary">{t("payments:earnings.subtitle")}</p>
      </div>

      {/* Stripe Connect onboarding banner */}
      <div className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-brand-200 bg-brand-50/40 px-5 py-4">
        <div>
          <p className="font-medium text-text-primary">{t("payments:earnings.setupPayoutsTitle")}</p>
          <p className="mt-0.5 text-sm text-text-secondary">{t("payments:earnings.setupPayoutsBody")}</p>
        </div>
        <button
          type="button"
          onClick={() => connectMut.mutate()}
          disabled={connectMut.isPending}
          className="inline-flex shrink-0 items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-brand-600 disabled:opacity-50"
        >
          {connectMut.isPending ? (
            <>
              <Loader2 className="size-4 animate-spin" />
              {t("payments:earnings.setupPayoutsLoading")}
            </>
          ) : (
            <>
              <ExternalLink className="size-4" />
              {t("payments:earnings.setupPayoutsCta")}
            </>
          )}
        </button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        {stats.map((s) => (
          <div
            key={s.key}
            className={`rounded-lg border p-5 ${
              s.emphasised
                ? "border-brand-200 bg-brand-50/30"
                : "border-border-subtle bg-bg-elevated"
            }`}
          >
            <p className="text-sm text-text-secondary">{s.label}</p>
            {loading ? (
              <div className="mt-2 h-7 w-24 animate-pulse rounded bg-bg-subtle" />
            ) : (
              <p
                className={`mt-1 text-2xl font-semibold tabular-nums ${
                  s.emphasised ? "text-brand-600" : "text-text-primary"
                }`}
              >
                {formatMoneyCents(s.value, displayCurrency, numberLocale)}
              </p>
            )}
            <p className="mt-1 text-xs text-text-tertiary">{s.hint}</p>
          </div>
        ))}
      </div>

      <section className="space-y-3">
        <div className="space-y-1">
          <h2 className="text-lg font-semibold">{t("payments:earnings.paymentsTitle")}</h2>
          <p className="text-sm text-text-secondary">
            {t("payments:earnings.paymentsSubtitle")}
          </p>
        </div>
        <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
          <table className="w-full text-sm">
            <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
              <tr>
                <th className="px-4 py-3 text-start">{t("payments:earnings.columns.gross")}</th>
                <th className="px-4 py-3 text-start">{t("payments:earnings.columns.fee")}</th>
                <th className="px-4 py-3 text-start">{t("payments:earnings.columns.net")}</th>
                <th className="px-4 py-3 text-start">{t("payments:earnings.columns.status")}</th>
                <th className="px-4 py-3 text-start">{t("payments:earnings.columns.date")}</th>
              </tr>
            </thead>
            <tbody>
              {paymentsQuery.isLoading && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                    {t("payments:common.loading")}
                  </td>
                </tr>
              )}
              {!paymentsQuery.isLoading && payments.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                    {t("payments:earnings.noPayments")}
                  </td>
                </tr>
              )}
              {payments.map((p) => (
                <tr key={p.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                  <td className="px-4 py-3 font-medium tabular-nums">
                    {formatMoneyCents(p.amountCents, p.currency, numberLocale)}
                  </td>
                  <td className="px-4 py-3 text-text-secondary tabular-nums">
                    <span>−{formatMoneyCents(p.profitShareAmountCents, p.currency, numberLocale)}</span>
                    <span className="ms-1 text-xs text-text-tertiary">({feePercent(p)})</span>
                  </td>
                  <td className="px-4 py-3 font-semibold text-brand-600 tabular-nums">
                    {formatMoneyCents(p.payeeAmountCents, p.currency, numberLocale)}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs font-medium ${paymentBadge(p.status)}`}
                    >
                      {t(`payments:paymentStatus.${p.status}`)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-xs text-text-tertiary">{dash(p.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="space-y-3">
        <div className="space-y-1">
          <h2 className="text-lg font-semibold">{t("payments:earnings.payoutsTitle")}</h2>
          <p className="text-sm text-text-secondary">
            {t("payments:earnings.payoutsSubtitle")}
          </p>
        </div>
        <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
          <table className="w-full text-sm">
            <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
              <tr>
                <th className="px-4 py-3 text-start">{t("payments:payoutHeaders.amount")}</th>
                <th className="px-4 py-3 text-start">{t("payments:payoutHeaders.status")}</th>
                <th className="px-4 py-3 text-start">{t("payments:payoutHeaders.payments")}</th>
                <th className="px-4 py-3 text-start">{t("payments:payoutHeaders.initiated")}</th>
                <th className="px-4 py-3 text-start">{t("payments:payoutHeaders.paid")}</th>
              </tr>
            </thead>
            <tbody>
              {payoutsQuery.isLoading && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                    {t("payments:common.loading")}
                  </td>
                </tr>
              )}
              {!payoutsQuery.isLoading && payouts.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-text-tertiary">
                    {t("payments:earnings.noPayouts")}
                  </td>
                </tr>
              )}
              {payouts.map((p) => (
                <tr key={p.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                  <td className="px-4 py-3 font-medium tabular-nums">
                    {formatMoneyCents(p.amountCents, p.currency, numberLocale)}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs font-medium ${payoutBadge(p.status)}`}
                    >
                      {t(`payments:payoutStatus.${p.status}`)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-text-secondary">{p.includedPaymentCount}</td>
                  <td className="px-4 py-3 text-xs text-text-tertiary">{dash(p.initiatedAt)}</td>
                  <td className="px-4 py-3 text-xs text-text-tertiary">{dash(p.paidAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
