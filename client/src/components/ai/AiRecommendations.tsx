import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { formatDistanceToNow } from "date-fns";
import { RefreshCw, Sparkles } from "lucide-react";
import { aiApi, type RecommendationsDto } from "@/services/api/ai";
import { AiDisclaimer } from "./AiDisclaimer";
import { MatchScoreBadge } from "./MatchScoreBadge";

const KEY = ["ai", "recommendations"] as const;

export function AiRecommendations() {
  const { t, i18n } = useTranslation(["ai", "common"]);
  const qc = useQueryClient();

  // We use POST on refresh (server persists an interaction row), but read path
  // can reuse the last result — prime it via the query.
  const q = useQuery<RecommendationsDto>({
    queryKey: KEY,
    queryFn: () => aiApi.recommendations(5),
    staleTime: 5 * 60 * 1000,
  });

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
          {[0, 1, 2].map((i) => <div key={i} className="h-14 animate-pulse rounded-md bg-bg-subtle" />)}
        </div>
      )}

      {q.data && q.data.items.length === 0 && (
        <p className="text-sm text-text-secondary">{t("ai:recommendations.empty")}</p>
      )}

      {q.data && q.data.items.length > 0 && (
        <ul className="space-y-2">
          {q.data.items.map((item) => (
            <li
              key={item.scholarshipId}
              className="flex items-start gap-3 rounded-md border border-border-subtle bg-bg-canvas p-3 transition hover:border-brand-500/50"
            >
              <MatchScoreBadge score={item.matchScore} />
              <div className="min-w-0 flex-1">
                <Link
                  to={`/student/scholarships/${item.scholarshipId}`}
                  className="block truncate font-medium hover:text-brand-500"
                >
                  {isAr ? item.titleAr || item.titleEn : item.titleEn || item.titleAr}
                </Link>
                <p className="mt-0.5 text-xs text-text-secondary">
                  {isAr ? item.explanationAr : item.explanationEn}
                </p>
              </div>
            </li>
          ))}
        </ul>
      )}

      {q.data?.generatedAt && (
        <p className="text-xs text-text-tertiary">
          {t("ai:recommendations.generatedAt", {
            when: formatDistanceToNow(new Date(q.data.generatedAt), { addSuffix: true }),
          })}
        </p>
      )}

      <AiDisclaimer />
    </section>
  );
}
