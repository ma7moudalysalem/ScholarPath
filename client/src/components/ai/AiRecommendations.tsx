import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { formatDistanceToNow, differenceInDays, isPast } from "date-fns";
import { ar as arLocale } from "date-fns/locale";
import { AlertCircle, Calendar, RefreshCw, Sparkles } from "lucide-react";
import { aiApi, type RecommendationItem, type RecommendationsDto } from "@/services/api/ai";
import { AiDisclaimer } from "./AiDisclaimer";
import { MatchScoreBadge } from "./MatchScoreBadge";

const KEY = ["ai", "recommendations"] as const;

function DeadlinePill({ deadline, isAr }: { deadline: string; isAr: boolean }) {
  const { t } = useTranslation("ai");
  if (!deadline || deadline === "0001-01-01T00:00:00Z") return null;

  const date = new Date(deadline);
  const daysLeft = differenceInDays(date, new Date());

  if (isPast(date)) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-danger-100 px-2 py-0.5 text-[10px] font-medium text-danger-600">
        {t("recommendations.deadlineClosed")}
      </span>
    );
  }

  const colorClass =
    daysLeft <= 14 ? "bg-danger-50 text-danger-500" :
    daysLeft <= 45 ? "bg-warning-50 text-warning-600" :
    "bg-bg-subtle text-text-secondary";

  const label =
    daysLeft <= 60
      ? t("recommendations.daysLeft", { count: daysLeft })
      : formatDistanceToNow(date, { addSuffix: true, locale: isAr ? arLocale : undefined });

  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[10px] font-medium ${colorClass}`}>
      <Calendar aria-hidden className="size-2.5 shrink-0" />
      {label}
    </span>
  );
}

function FundingPill({ fundingType, amountUsd }: { fundingType: string; amountUsd: number | null }) {
  const { t } = useTranslation("ai");
  if (!fundingType) return null;

  const colorClass =
    fundingType === "FullyFunded" ? "bg-success-100 text-success-700" :
    fundingType === "PartiallyFunded" ? "bg-brand-100 text-brand-700" :
    "bg-bg-subtle text-text-secondary";

  const label = t(`recommendations.funding.${fundingType}`, {
    defaultValue: fundingType,
    amount: amountUsd ? `$${Math.round(amountUsd).toLocaleString()}` : "",
  });

  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-medium ${colorClass}`}>
      {label}
    </span>
  );
}

function RecommendationCard({ item, isAr }: { item: RecommendationItem; isAr: boolean }) {
  const { t } = useTranslation("ai");
  return (
    <li className="flex items-start gap-3 rounded-md border border-border-subtle bg-bg-canvas p-3 transition hover:border-brand-500/50">
      <MatchScoreBadge score={item.matchScore} />
      <div className="min-w-0 flex-1">
        <Link
          to={`/student/scholarships/${item.scholarshipId}`}
          onClick={() => {
            void aiApi.logRecommendationClick(item.scholarshipId, null, "card");
          }}
          className="block truncate font-medium hover:text-brand-500"
        >
          {isAr ? item.titleAr || item.titleEn : item.titleEn || item.titleAr}
        </Link>
        <div className="mt-1 flex flex-wrap items-center gap-1.5">
          <FundingPill fundingType={item.fundingType} amountUsd={item.fundingAmountUsd} />
          <DeadlinePill deadline={item.deadline} isAr={isAr} />
        </div>
        <p className="mt-1.5 text-xs text-text-secondary">
          {isAr ? item.explanationAr : item.explanationEn}
        </p>
      </div>
    </li>
  );
}

export function AiRecommendations() {
  const { t, i18n } = useTranslation(["ai", "common"]);
  const qc = useQueryClient();

  // Read path hits the cache (GET). On cache miss (null) we auto-regenerate once
  // so first-time users still see recommendations without clicking refresh.
  const q = useQuery<RecommendationsDto | null>({
    queryKey: KEY,
    queryFn: async () => {
      const cached = await aiApi.cachedRecommendations();
      return cached ?? (await aiApi.recommendations(5));
    },
    staleTime: 5 * 60 * 1000,
  });

  // Explicit refresh always regenerates (counts against daily budget).
  const refreshMut = useMutation({
    mutationFn: () => aiApi.recommendations(5),
    onSuccess: (data) => qc.setQueryData(KEY, data),
  });

  const isAr = i18n.language.startsWith("ar");

  return (
    <section className="space-y-4 rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <header className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Sparkles aria-hidden className="size-5 text-brand-500" />
          <h2 className="text-lg font-semibold">{t("ai:recommendations.heading")}</h2>
        </div>
        <button
          type="button"
          onClick={() => refreshMut.mutate()}
          disabled={refreshMut.isPending}
          className="inline-flex items-center gap-1.5 rounded-md border border-border-subtle px-3 py-1.5 text-xs font-medium hover:border-brand-500 hover:text-brand-500 disabled:opacity-50"
        >
          <RefreshCw aria-hidden className={`size-3.5 ${refreshMut.isPending ? "animate-spin" : ""}`} />
          {t("ai:recommendations.refresh")}
        </button>
      </header>

      {q.isLoading && (
        <div className="space-y-2">
          {[0, 1, 2].map((i) => <div key={i} className="h-20 animate-pulse rounded-md bg-bg-subtle" />)}
        </div>
      )}

      {q.isError && (
        <div className="flex items-start gap-3 rounded-md border border-danger-200 bg-danger-50 p-3 text-sm text-danger-500">
          <AlertCircle aria-hidden className="mt-0.5 size-4 shrink-0" />
          <div className="flex-1">
            <p className="font-medium">{t("ai:recommendations.error")}</p>
          </div>
          <button
            type="button"
            onClick={() => refreshMut.mutate()}
            disabled={refreshMut.isPending}
            className="shrink-0 rounded-md border border-danger-200 px-2.5 py-1 text-xs font-medium text-danger-600 transition hover:bg-danger-100 disabled:opacity-50"
          >
            {t("ai:recommendations.refresh")}
          </button>
        </div>
      )}

      {q.data && q.data.items.length === 0 && (
        <div className="flex flex-col items-center rounded-md border border-dashed border-border-subtle bg-bg-subtle/40 p-6 text-center">
          <div aria-hidden className="mb-3 flex size-10 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
            <Sparkles className="size-5" />
          </div>
          <p className="text-sm text-text-secondary">{t("ai:recommendations.empty")}</p>
          <Link
            to="/profile"
            className="mt-3 text-xs font-medium text-brand-600 hover:underline"
          >
            {t("ai:eligibility.goToProfile")}
          </Link>
        </div>
      )}

      {q.data && q.data.items.length > 0 && (
        <ul className="space-y-2">
          {q.data.items.map((item) => (
            <RecommendationCard key={item.scholarshipId} item={item} isAr={isAr} />
          ))}
        </ul>
      )}

      {q.data?.generatedAt && (
        <p className="text-xs text-text-tertiary">
          {t("ai:recommendations.generatedAt", {
            when: formatDistanceToNow(new Date(q.data.generatedAt), {
              addSuffix: true,
              locale: isAr ? arLocale : undefined,
            }),
          })}
        </p>
      )}

      <AiDisclaimer />
    </section>
  );
}
