import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import {
  paymentsApi,
  formatMoneyCents,
  type PagedPayments,
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

const dash = (iso: string | null) => (iso ? format(new Date(iso), "yyyy-MM-dd") : "—");

export function ConsultantEarnings() {
  const { t } = useTranslation(["payments", "common"]);

  const paymentsQuery = useQuery<PagedPayments>({
    queryKey: ["consultant", "earnings", "payments"],
    queryFn: () => paymentsApi.listPayments({ pageSize: 100 }),
  });
  const payoutsQuery = useQuery<PayoutDto[]>({
    queryKey: ["consultant", "earnings", "payouts"],
    queryFn: () => paymentsApi.listMyPayouts(),
  });

  const payments = paymentsQuery.data?.items ?? [];
  const payouts = payoutsQuery.data ?? [];

  const totalEarned = payments
    .filter((p) => p.status === "Captured")
    .reduce((sum, p) => sum + p.payeeAmountCents, 0);
  const totalPaidOut = payouts
    .filter((p) => p.status === "Paid")
    .reduce((sum, p) => sum + p.amountCents, 0);
  const awaitingPayout = payouts
    .filter((p) => p.status === "Pending" || p.status === "InTransit")
    .reduce((sum, p) => sum + p.amountCents, 0);

  const stats: { label: string; value: number }[] = [
    { label: t("payments:earnings.totalEarned"), value: totalEarned },
    { label: t("payments:earnings.totalPaidOut"), value: totalPaidOut },
    { label: t("payments:earnings.awaitingPayout"), value: awaitingPayout },
  ];

  const loading = paymentsQuery.isLoading || payoutsQuery.isLoading;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("payments:earnings.title")}</h1>
        <p className="mt-1 text-sm text-text-secondary">{t("payments:earnings.subtitle")}</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        {stats.map((s) => (
          <div key={s.label} className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
            <p className="text-sm text-text-secondary">{s.label}</p>
            {loading ? (
              <div className="mt-2 h-7 w-24 animate-pulse rounded bg-bg-subtle" />
            ) : (
              <p className="mt-1 text-2xl font-semibold text-brand-500">
                {formatMoneyCents(s.value, "USD")}
              </p>
            )}
          </div>
        ))}
      </div>

      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{t("payments:earnings.paymentsTitle")}</h2>
        <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
          <table className="w-full text-sm">
            <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
              <tr>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.amount")}</th>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.status")}</th>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.payee")}</th>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.created")}</th>
              </tr>
            </thead>
            <tbody>
              {paymentsQuery.isLoading && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-text-tertiary">
                    {t("payments:common.loading")}
                  </td>
                </tr>
              )}
              {!paymentsQuery.isLoading && payments.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-text-tertiary">
                    {t("payments:earnings.noPayments")}
                  </td>
                </tr>
              )}
              {payments.map((p) => (
                <tr key={p.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                  <td className="px-4 py-3 font-medium">
                    {formatMoneyCents(p.amountCents, p.currency)}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs font-medium ${paymentBadge(p.status)}`}
                    >
                      {t(`payments:paymentStatus.${p.status}`)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-text-secondary">
                    {formatMoneyCents(p.payeeAmountCents, p.currency)}
                  </td>
                  <td className="px-4 py-3 text-xs text-text-tertiary">{dash(p.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{t("payments:earnings.payoutsTitle")}</h2>
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
                  <td className="px-4 py-3 font-medium">
                    {formatMoneyCents(p.amountCents, p.currency)}
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
