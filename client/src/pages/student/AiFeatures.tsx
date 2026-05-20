import { useState } from "react";
import { useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Sparkles, ClipboardCheck, MessageSquare, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { AiRecommendations } from "@/components/ai/AiRecommendations";
import { EligibilityCheckerSection } from "@/components/ai/EligibilityCheckerSection";
import { Chatbot } from "@/components/ai/Chatbot";

type AiTab = "recommendations" | "eligibility" | "chat";

const VALID_TABS = new Set<AiTab>(["recommendations", "eligibility", "chat"]);

const TABS: { id: AiTab; icon: LucideIcon }[] = [
  { id: "recommendations", icon: Sparkles },
  { id: "eligibility",     icon: ClipboardCheck },
  { id: "chat",            icon: MessageSquare },
];

/**
 * SRS PB-016 — the student AI hub. Three tabs map to the AI feature set:
 * profile-based recommendations (FR-113/114/115), the eligibility checker
 * (FR-116/117/118), and the help chatbot (FR-119/120/121). All three panels
 * stay mounted and are toggled with `hidden` so a tab switch never loses the
 * chat thread or a computed eligibility verdict.
 *
 * Supports deep-linking via query params:
 *   ?tab=eligibility&sid=<scholarshipId>&stitle=<encodedTitle>
 * Used by ScholarshipDetail's "Check Eligibility" shortcut button.
 */
export function AiFeatures() {
  const { t } = useTranslation(["ai"]);
  const [searchParams] = useSearchParams();

  // Deep-link: ?tab=eligibility opens that tab directly; unknown values fall
  // back to the default so stale/bookmarked URLs don't break the page.
  const paramTab = searchParams.get("tab") as AiTab | null;
  const [tab, setTab] = useState<AiTab>(
    paramTab && VALID_TABS.has(paramTab) ? paramTab : "recommendations",
  );

  // Deep-link: ?sid=<id>&stitle=<title> pre-selects a scholarship in the
  // eligibility checker so the student skips the search step.
  const paramSid   = searchParams.get("sid") ?? "";
  const paramTitle = searchParams.get("stitle") ?? "";
  const initialScholarship =
    paramSid && paramTitle ? { id: paramSid, title: paramTitle } : undefined;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("ai:title")}</h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">{t("ai:subtitle")}</p>
      </div>

      {/* ── Tab bar ── */}
      <div role="tablist" className="flex flex-wrap gap-1 border-b border-border-subtle">
        {TABS.map(({ id, icon: Icon }) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            onClick={() => setTab(id)}
            className={cn(
              "-mb-px inline-flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-medium transition",
              tab === id
                ? "border-brand-500 text-brand-500"
                : "border-transparent text-text-secondary hover:text-text-primary",
            )}
          >
            <Icon aria-hidden className="size-4" />
            {t(`ai:tabs.${id}`)}
          </button>
        ))}
      </div>

      {/* ── Panels (kept mounted; inactive ones hidden to preserve state) ── */}
      <div className={cn(tab !== "recommendations" && "hidden")}>
        <AiRecommendations />
      </div>
      <div className={cn(tab !== "eligibility" && "hidden")}>
        <EligibilityCheckerSection initialScholarship={initialScholarship} />
      </div>
      <div className={cn(tab !== "chat" && "hidden")}>
        <Chatbot />
      </div>
    </div>
  );
}
