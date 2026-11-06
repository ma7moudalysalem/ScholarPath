import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { KeyRound } from "lucide-react";
import { aiApi, type UnlockSuggestion } from "@/services/api/ai";

/**
 * Counterfactual advice under the recommendations: the single profile changes
 * (a destination to add, a level to reach) that would make the student eligible
 * for more open scholarships, read from the same rules as the eligibility check.
 * Renders nothing when no single change opens a listing.
 */
export function UnlockSuggestions() {
  const { t, i18n } = useTranslation("ai");
  const isAr = i18n.language.startsWith("ar");

  const q = useQuery<UnlockSuggestion[]>({
    queryKey: ["ai", "unlocks"],
    queryFn: () => aiApi.unlocks(3),
    staleTime: 5 * 60 * 1000,
  });

  if (!q.data || q.data.length === 0) return null;

  return (
    <div className="rounded-md border border-brand-200 bg-brand-50/40 p-3">
      <div className="mb-2 flex items-center gap-2">
        <KeyRound aria-hidden className="size-4 text-brand-600" />
        <h3 className="text-sm font-semibold">{t("unlocks.heading")}</h3>
      </div>
      <p className="mb-2 text-xs text-text-secondary">{t("unlocks.hint")}</p>
      <ul className="space-y-1.5">
        {q.data.map((s) => (
          <li key={`${s.change}-${s.value}`} className="flex items-center justify-between gap-3 text-sm">
            <span>{isAr ? s.labelAr : s.labelEn}</span>
            <span className="shrink-0 rounded-full bg-brand-100 px-2 py-0.5 text-xs font-medium text-brand-700">
              {t("unlocks.opens", { count: s.listingsUnlocked })}
            </span>
          </li>
        ))}
      </ul>
      <Link to="/profile" className="mt-2 inline-block text-xs font-medium text-brand-600 hover:underline">
        {t("unlocks.edit")}
      </Link>
    </div>
  );
}
