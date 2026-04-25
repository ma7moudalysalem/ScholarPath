import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Sparkles, DollarSign, MousePointerClick, Timer } from "lucide-react";
import { adminApi, type AiUsageSummaryDto } from "@/services/api/admin";

const WINDOWS = [7, 30, 90] as const;
type Window = (typeof WINDOWS)[number];

function formatUsd(n: number): string {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: n >= 10 ? 0 : 2,
  }).format(n);
}

function CostBars({ points }: { points: AiUsageSummaryDto["dailyCost"] }) {
  if (points.length === 0) {
    return <div className="h-40 rounded bg-bg-subtle/50" />;
  }
  const max = Math.max(...points.map((p) => p.costUsd), 0.0001);
  return (
    <div className="flex h-40 items-end gap-0.5">
      {points.map((p) => {
        const height = Math.max(2, (p.costUsd / max) * 100);
        return (
          <div
            key={p.date}
            title={`${p.date} — ${formatUsd(p.costUsd)}`}
            className="flex-1 rounded-t bg-brand-500/70 transition hover:bg-brand-500"
            style={{ height: `${height}%` }}
          />
        );
      })}
    </div>
  );
}

function StatCard({
  icon: Icon,
  label,
  value,
  hint,
}: {
  icon: typeof Sparkles;
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <div className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <div className="flex items-center gap-2 text-xs font-medium text-text-secondary">
        <Icon aria-hidden className="size-4 text-brand-500" />
        {label}
      </div>
      <div className="mt-3 text-3xl font-semibold tabular-nums tracking-tight">{value}</div>
      {hint && <div className="mt-1 text-xs text-text-tertiary">{hint}</div>}
    </div>
  );
}

export function AiEconomyPage() {
  const { t } = useTranslation(["admin"]);
  const [windowDays, setWindowDays] = useState<Window>(30);

  const q = useQuery<AiUsageSummaryDto>({
    queryKey: ["admin", "ai-usage", windowDays],
    queryFn: () => adminApi.aiUsage(windowDays),
  });

  const avgLatency = useMemo(() => {
    const rows = q.data?.byFeature ?? [];
    const withLat = rows.filter((r) => r.avgLatencyMs != null);
    if (withLat.length === 0) return null;
    const total = withLat.reduce((acc, r) => acc + (r.avgLatencyMs ?? 0) * r.interactions, 0);
    const count = withLat.reduce((acc, r) => acc + r.interactions, 0);
    return count === 0 ? null : Math.round(total / count);
  }, [q.data]);

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("admin:aiEconomy.title", { defaultValue: "AI economy" })}
        </h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">
          {t("admin:aiEconomy.subtitle", {
            defaultValue:
              "Cost, volume, latency, and recommendation CTR — the AI features as a product, not a line item.",
          })}
        </p>
      </header>

      <div className="flex flex-wrap items-center gap-3">
        <span className="text-sm text-text-secondary">
          {t("admin:analytics.window", { defaultValue: "Window" })}:
        </span>
        <div className="inline-flex rounded-md border border-border-subtle bg-bg-elevated p-0.5">
          {WINDOWS.map((d) => (
            <button
              key={d}
              type="button"
              onClick={() => setWindowDays(d)}
              className={`rounded px-3 py-1 text-xs font-medium transition ${
                windowDays === d ? "bg-brand-500 text-text-on-brand" : "text-text-secondary hover:text-text-primary"
              }`}
            >
              {t(`admin:analytics.windows.${d}`, { defaultValue: `${d}d` })}
            </button>
          ))}
        </div>
      </div>

      {q.isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-32 animate-pulse rounded-lg bg-bg-subtle" />
          ))}
        </div>
      )}

      {q.data && (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              icon={DollarSign}
              label={t("admin:aiEconomy.totalCost", { defaultValue: "Total cost" })}
              value={formatUsd(q.data.totalCostUsd)}
              hint={t("admin:aiEconomy.costHint", {
                defaultValue: `${q.data.windowDays}-day window`,
                count: q.data.windowDays,
              })}
            />
            <StatCard
              icon={Sparkles}
              label={t("admin:aiEconomy.interactions", { defaultValue: "Interactions" })}
              value={q.data.totalInteractions.toLocaleString()}
              hint={t("admin:aiEconomy.interactionsHint", {
                defaultValue: "all AI features combined",
              })}
            />
            <StatCard
              icon={MousePointerClick}
              label={t("admin:aiEconomy.ctr", { defaultValue: "Recommendation CTR" })}
              value={`${q.data.recommendations.ctrPercent.toFixed(1)}%`}
              hint={`${q.data.recommendations.clicks.toLocaleString()} / ${q.data.recommendations.impressions.toLocaleString()}`}
            />
            <StatCard
              icon={Timer}
              label={t("admin:aiEconomy.avgLatency", { defaultValue: "Avg latency" })}
              value={avgLatency == null ? "—" : `${avgLatency} ms`}
              hint={t("admin:aiEconomy.latencyHint", { defaultValue: "mean, completed calls only" })}
            />
          </div>

          <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
            <h2 className="mb-4 text-sm font-semibold">
              {t("admin:aiEconomy.dailyCost", { defaultValue: "Daily cost" })}
            </h2>
            <CostBars points={q.data.dailyCost} />
          </section>

          <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
            <h2 className="mb-4 text-sm font-semibold">
              {t("admin:aiEconomy.byFeature", { defaultValue: "By feature" })}
            </h2>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="text-start text-xs uppercase tracking-wide text-text-tertiary">
                  <tr>
                    <th className="py-2 text-start">{t("admin:aiEconomy.feature", { defaultValue: "Feature" })}</th>
                    <th className="py-2 text-end">{t("admin:aiEconomy.interactions", { defaultValue: "Interactions" })}</th>
                    <th className="py-2 text-end">{t("admin:aiEconomy.cost", { defaultValue: "Cost" })}</th>
                    <th className="py-2 text-end">{t("admin:aiEconomy.latency", { defaultValue: "Avg latency" })}</th>
                  </tr>
                </thead>
                <tbody>
                  {q.data.byFeature.length === 0 && (
                    <tr>
                      <td colSpan={4} className="py-4 text-center text-text-tertiary">
                        {t("admin:aiEconomy.noData", { defaultValue: "No AI usage in this window." })}
                      </td>
                    </tr>
                  )}
                  {q.data.byFeature.map((row) => (
                    <tr key={row.feature} className="border-t border-border-subtle">
                      <td className="py-2 font-medium">{row.feature}</td>
                      <td className="py-2 text-end tabular-nums">{row.interactions.toLocaleString()}</td>
                      <td className="py-2 text-end tabular-nums">{formatUsd(row.costUsd)}</td>
                      <td className="py-2 text-end tabular-nums text-text-secondary">
                        {row.avgLatencyMs == null ? "—" : `${row.avgLatencyMs} ms`}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  );
}
