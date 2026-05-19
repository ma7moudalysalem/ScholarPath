import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { profitShareApi, type ProfitShareConfigDto } from "@/services/api/profitShare";
import type { PaymentType } from "@/services/api/payments";

const TYPES: PaymentType[] = ["ConsultantBooking", "CompanyReview"];

export function AdminProfitShare() {
  const { t, i18n } = useTranslation(["payments", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;

  const activeQuery = useQuery<ProfitShareConfigDto[]>({
    queryKey: ["admin", "profit-share", "active"],
    queryFn: () => profitShareApi.active(),
  });

  const historyQuery = useQuery<ProfitShareConfigDto[]>({
    queryKey: ["admin", "profit-share", "history"],
    queryFn: () => profitShareApi.history(),
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("payments:profitShare.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("payments:profitShare.subtitle")}
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        {TYPES.map((type) => (
          <ProfitShareCard
            key={type}
            type={type}
            active={activeQuery.data?.find((c) => c.paymentType === type) ?? null}
            loading={activeQuery.isLoading}
          />
        ))}
      </div>

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-3 text-lg font-semibold">{t("payments:profitShare.historyTitle")}</h2>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
              <tr>
                <th className="px-3 py-2 text-start">{t("payments:profitShare.hType")}</th>
                <th className="px-3 py-2 text-start">{t("payments:profitShare.hRate")}</th>
                <th className="px-3 py-2 text-start">{t("payments:profitShare.hFrom")}</th>
                <th className="px-3 py-2 text-start">{t("payments:profitShare.hTo")}</th>
                <th className="px-3 py-2 text-start">{t("payments:profitShare.hNotes")}</th>
              </tr>
            </thead>
            <tbody>
              {historyQuery.isLoading && (
                <tr>
                  <td colSpan={5} className="px-3 py-6 text-center text-text-tertiary">
                    {t("payments:common.loading")}
                  </td>
                </tr>
              )}
              {!historyQuery.isLoading && (historyQuery.data?.length ?? 0) === 0 && (
                <tr>
                  <td colSpan={5} className="px-3 py-6 text-center text-text-tertiary">
                    {t("payments:profitShare.historyEmpty")}
                  </td>
                </tr>
              )}
              {historyQuery.data?.map((c) => (
                <tr key={c.id} className="border-t border-border-subtle">
                  <td className="px-3 py-2">{t(`payments:paymentType.${c.paymentType}`)}</td>
                  <td className="px-3 py-2 font-medium">{(c.percentage * 100).toFixed(1)}%</td>
                  <td className="px-3 py-2 text-xs text-text-tertiary">
                    {format(new Date(c.effectiveFrom), "yyyy-MM-dd", { locale: dateLocale })}
                  </td>
                  <td className="px-3 py-2 text-xs text-text-tertiary">
                    {c.effectiveTo ? (
                      format(new Date(c.effectiveTo), "yyyy-MM-dd", { locale: dateLocale })
                    ) : (
                      <span className="rounded-full bg-success-100 px-2 py-0.5 font-medium text-success-600">
                        {t("payments:profitShare.active")}
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-text-secondary">{c.notes ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}

function ProfitShareCard({
  type,
  active,
  loading,
}: {
  type: PaymentType;
  active: ProfitShareConfigDto | null;
  loading: boolean;
}) {
  const { t } = useTranslation(["payments", "common"]);
  const qc = useQueryClient();
  const [rate, setRate] = useState("");
  const [notes, setNotes] = useState("");

  const mut = useMutation({
    mutationFn: (body: { percentage: number; notes?: string }) =>
      profitShareApi.set(type, body),
    onSuccess: () => {
      toast.success(t("payments:profitShare.saveSuccess"));
      setRate("");
      setNotes("");
      void qc.invalidateQueries({ queryKey: ["admin", "profit-share"] });
    },
    onError: () => toast.error(t("payments:profitShare.saveError")),
  });

  const submit = () => {
    const pct = Number(rate);
    if (!Number.isFinite(pct) || pct < 0 || pct > 50) {
      toast.error(t("payments:profitShare.invalidRate"));
      return;
    }
    mut.mutate({ percentage: pct / 100, notes: notes.trim() || undefined });
  };

  return (
    <div className="space-y-4 rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <div>
        <h3 className="font-semibold">{t(`payments:paymentType.${type}`)}</h3>
        {loading ? (
          <div className="mt-2 h-8 w-24 animate-pulse rounded bg-bg-subtle" />
        ) : (
          <p className="mt-1 text-3xl font-semibold text-brand-500">
            {active ? `${(active.percentage * 100).toFixed(1)}%` : "—"}
          </p>
        )}
        <p className="text-xs text-text-tertiary">
          {active ? t("payments:profitShare.currentRate") : t("payments:profitShare.notConfigured")}
        </p>
      </div>

      <div className="space-y-3 border-t border-border-subtle pt-4">
        <label className="block text-sm">
          <span className="text-text-secondary">{t("payments:profitShare.newRate")}</span>
          <input
            type="number"
            min={0}
            max={50}
            step={0.5}
            value={rate}
            onChange={(e) => setRate(e.target.value)}
            className="mt-1 h-10 w-full rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm focus:border-brand-500 focus:outline-none"
          />
          <span className="mt-1 block text-xs text-text-tertiary">
            {t("payments:profitShare.rateRange")}
          </span>
        </label>

        <label className="block text-sm">
          <span className="text-text-secondary">{t("payments:profitShare.notes")}</span>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            placeholder={t("payments:profitShare.notesPlaceholder")}
            className="mt-1 w-full rounded-md border border-border-subtle bg-bg-elevated px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
          />
        </label>

        <button
          type="button"
          onClick={submit}
          disabled={mut.isPending || rate === ""}
          className="inline-flex items-center justify-center rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
        >
          {mut.isPending ? t("payments:profitShare.saving") : t("payments:profitShare.save")}
        </button>
      </div>
    </div>
  );
}
