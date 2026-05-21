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
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight flex items-center gap-3">
            <span className="inline-flex size-10 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm">
              <Sparkles aria-hidden className="size-5" />
            </span>
            <span className="text-gradient">{t("ai:title")}</span>
          </h1>
          <p className="mt-2 max-w-2xl text-text-secondary">{t("ai:subtitle")}</p>
        </div>
      </div>

      {/* ── Tab bar ── On narrow viewports the row scrolls horizontally
          so longer Arabic labels never push the layout sideways. */}
      <div className="overflow-x-auto scrollbar-premium">
        <div
          role="tablist"
          aria-label={t("ai:title")}
          className="inline-flex items-center gap-1 rounded-xl border border-border-subtle bg-bg-elevated p-1 shadow-elevation-1"
        >
          {TABS.map(({ id, icon: Icon }) => (
            <button
              key={id}
              type="button"
              role="tab"
              aria-selected={tab === id}
              onClick={() => setTab(id)}
              className={cn(
                "inline-flex shrink-0 items-center gap-2 rounded-lg px-3.5 py-2 text-sm font-semibold transition-all",
                tab === id
                  ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm"
                  : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
              )}
            >
              <Icon aria-hidden className="size-4" />
              {t(`ai:tabs.${id}`)}
            </button>
          ))}
        </div>
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
