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
      return "bg-emerald-500/10 text-emerald-500";
    case "Held":
    case "Pending":
      return "bg-amber-500/10 text-amber-600";
    case "Refunded":
    case "PartiallyRefunded":
      return "bg-sky-500/10 text-sky-500";
    case "Failed":
    case "Cancelled":
      return "bg-rose-500/10 text-rose-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

function payoutBadge(s: PayoutStatus): string {
  switch (s) {
    case "Paid":
      return "bg-emerald-500/10 text-emerald-500";
    case "InTransit":
    case "Pending":
      return "bg-amber-500/10 text-amber-600";
    case "Failed":
      return "bg-rose-500/10 text-rose-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

const dash = (iso: string | null) => (iso ? format(new Date(iso), "yyyy-MM-dd") : "—");

export function CompanyBilling() {
  const { t } = useTranslation(["payments", "common"]);

  const paymentsQuery = useQuery<PagedPayments>({
    queryKey: ["company", "billing", "payments"],
    queryFn: () => paymentsApi.listPayments({ pageSize: 100 }),
  });
  const payoutsQuery = useQuery<PayoutDto[]>({
    queryKey: ["company", "billing", "payouts"],
    queryFn: () => paymentsApi.listMyPayouts(),
  });

  const payments = paymentsQuery.data?.items ?? [];
  const payouts = payoutsQuery.data ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("payments:billing.title")}</h1>
        <p className="mt-1 text-sm text-text-secondary">{t("payments:billing.subtitle")}</p>
      </div>

      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{t("payments:billing.paymentsTitle")}</h2>
        <div className="overflow-x-auto rounded-lg border border-border-subtle bg-bg-elevated">
          <table className="w-full text-sm">
            <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
              <tr>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.amount")}</th>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.status")}</th>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.payee")}</th>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.refunded")}</th>
                <th className="px-4 py-3 text-start">{t("payments:paymentHeaders.created")}</th>
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
                    {t("payments:billing.noPayments")}
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
                  <td className="px-4 py-3 text-text-secondary">
                    {formatMoneyCents(p.refundedAmountCents, p.currency)}
                  </td>
                  <td className="px-4 py-3 text-xs text-text-tertiary">{dash(p.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="text-lg font-semibold">{t("payments:billing.payoutsTitle")}</h2>
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
                    {t("payments:billing.noPayouts")}
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
