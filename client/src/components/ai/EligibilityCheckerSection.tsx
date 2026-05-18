import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Search,
  Check,
  X,
  Minus,
  CircleHelp,
  Loader2,
  GraduationCap,
  ArrowUpRight,
  ClipboardCheck,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { scholarshipsApi } from "@/services/api/scholarships";
import { useEligibilityQuery } from "@/hooks/useAiQuery";
import type { EligibilityMatch, EligibilityVerdict } from "@/services/api/ai";
import { AiDisclaimer } from "./AiDisclaimer";

// ── Visual maps ───────────────────────────────────────────────────────────────

const VERDICT_STYLES: Record<EligibilityVerdict, string> = {
  Eligible:          "bg-success-100 text-success-600 ring-success-500/20",
  PartiallyEligible: "bg-warning-50 text-warning-600 ring-warning-500/20",
  NotEligible:       "bg-danger-50 text-danger-500 ring-danger-400/20",
};

const MATCH_BADGE: Record<EligibilityMatch, string> = {
  yes:     "bg-success-100 text-success-600",
  partial: "bg-warning-50 text-warning-600",
  no:      "bg-danger-50 text-danger-500",
  unknown: "bg-bg-subtle text-text-tertiary",
};

function MatchIcon({ m }: { m: EligibilityMatch }) {
  const cls = "size-4 shrink-0";
  switch (m) {
    case "yes":     return <Check aria-hidden className={cn(cls, "text-success-500")} />;
    case "no":      return <X aria-hidden className={cn(cls, "text-danger-500")} />;
    case "partial": return <Minus aria-hidden className={cn(cls, "text-warning-500")} />;
    default:        return <CircleHelp aria-hidden className={cn(cls, "text-text-tertiary")} />;
  }
}

// ── Debounce ──────────────────────────────────────────────────────────────────

function useDebounced(value: string, delay = 300) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(id);
  }, [value, delay]);
  return debounced;
}

// ── Section ───────────────────────────────────────────────────────────────────

interface SelectedScholarship {
  id: string;
  title: string;
}

/**
 * SRS FR-116/117/118 — the standalone eligibility checker. The student searches
 * for any open scholarship, picks one, and the section runs the AI eligibility
 * check: an overall verdict, a per-criterion comparison (their profile vs. the
 * listing), and a "what to improve" digest of the criteria that don't yet match.
 */
export function EligibilityCheckerSection() {
  const { t, i18n } = useTranslation(["ai", "common"]);
  const isAr = i18n.language.startsWith("ar");

  const [term, setTerm] = useState("");
  const [selected, setSelected] = useState<SelectedScholarship | null>(null);
  const debounced = useDebounced(term.trim());
  const canSearch = debounced.length >= 2;

  const search = useQuery({
    queryKey: ["ai", "eligibility-search", debounced],
    queryFn: () => scholarshipsApi.search({ query: debounced, pageSize: 8 }),
    enabled: canSearch && !selected,
    staleTime: 60_000,
  });

  const eligibility = useEligibilityQuery(selected?.id);

  // FR-118 — the "what to improve" digest: every criterion the student does not
  // yet fully meet, derived client-side from the per-criterion verdicts.
  const improvements = useMemo(
    () =>
      eligibility.data?.criteria.filter(
        (c) => c.match === "no" || c.match === "partial",
      ) ?? [],
    [eligibility.data],
  );

  // When every criterion is "unknown" the student's profile has no usable
  // data — a verdict would mislead, so a "complete your profile" prompt is
  // shown in its place.
  const profileIncomplete =
    !!eligibility.data &&
    eligibility.data.criteria.length > 0 &&
    eligibility.data.criteria.every((c) => c.match === "unknown");

  const reset = () => {
    setSelected(null);
    setTerm("");
  };

  const errStatus = (eligibility.error as { status?: number } | null)?.status;

  // The server emits "unknown" (the student's profile field is empty) and
  // "any" (the listing sets no restriction) as sentinels — localise them;
  // every other value is real data shown as-is.
  const localizeValue = (value: string) => {
    const key = value.trim().toLowerCase();
    if (key === "unknown") return t("ai:eligibility.unknownValue");
    if (key === "any") return t("ai:eligibility.anyValue");
    return value;
  };

  return (
    <section className="space-y-4 rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <header className="flex items-center gap-2">
        <ClipboardCheck aria-hidden className="size-5 text-brand-500" />
        <div>
          <h2 className="text-lg font-semibold">{t("ai:eligibility.heading")}</h2>
          <p className="mt-0.5 text-sm text-text-secondary">{t("ai:eligibility.intro")}</p>
        </div>
      </header>

      {/* ── Scholarship picker ── */}
      {!selected && (
        <div className="space-y-3">
          <label className="block">
            <span className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">
              {t("ai:eligibility.searchLabel")}
            </span>
            <div className="relative mt-1">
              <Search
                aria-hidden
                className="pointer-events-none absolute top-1/2 size-4 -translate-y-1/2 text-text-tertiary start-3"
              />
              <input
                type="text"
                value={term}
                onChange={(e) => setTerm(e.target.value)}
                placeholder={t("ai:eligibility.searchPlaceholder")}
                className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas text-sm ps-9 pe-3 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              />
            </div>
          </label>

          {!canSearch && (
            <p className="text-sm text-text-tertiary">{t("ai:eligibility.searchHint")}</p>
          )}

          {canSearch && search.isLoading && (
            <div className="space-y-2">
              {[0, 1, 2].map((i) => (
                <div key={i} className="h-12 animate-pulse rounded-md bg-bg-subtle" />
              ))}
            </div>
          )}

          {canSearch && search.isError && (
            <p className="text-sm text-danger-500">{t("ai:eligibility.searchError")}</p>
          )}

          {canSearch && search.data && search.data.items.length === 0 && (
            <p className="text-sm text-text-secondary">{t("ai:eligibility.searchEmpty")}</p>
          )}

          {canSearch && search.data && search.data.items.length > 0 && (
            <ul className="space-y-1.5">
              {search.data.items.map((s) => {
                const title = (isAr ? s.titleAr : s.titleEn) || s.titleEn;
                return (
                  <li key={s.id}>
                    <button
                      type="button"
                      onClick={() => setSelected({ id: s.id, title })}
                      className="flex w-full items-center gap-3 rounded-md border border-border-subtle bg-bg-canvas p-3 text-start transition hover:border-brand-500/60 hover:bg-bg-subtle/40"
                    >
                      <GraduationCap aria-hidden className="size-4 shrink-0 text-text-tertiary" />
                      <span className="min-w-0 flex-1 truncate text-sm font-medium">{title}</span>
                      <ArrowUpRight aria-hidden className="size-4 shrink-0 text-text-tertiary" />
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      )}

      {/* ── Selected scholarship + verdict ── */}
      {selected && (
        <div className="space-y-4">
          <div className="flex items-start justify-between gap-3 rounded-md border border-border-subtle bg-bg-subtle/40 p-3">
            <div className="min-w-0">
              <div className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                {t("ai:eligibility.selected")}
              </div>
              <div className="mt-0.5 truncate text-sm font-medium">{selected.title}</div>
            </div>
            <button
              type="button"
              onClick={reset}
              className="shrink-0 rounded-md border border-border-subtle px-3 py-1.5 text-xs font-medium transition hover:border-brand-500 hover:text-brand-500"
            >
              {t("ai:eligibility.change")}
            </button>
          </div>

          {eligibility.isLoading && (
            <div className="flex items-center gap-2 text-sm text-text-secondary">
              <Loader2 aria-hidden className="size-4 animate-spin" />
              {t("ai:eligibility.checking")}
            </div>
          )}

          {eligibility.isError && (
            <p className="rounded-md border border-danger-200 bg-danger-50 p-3 text-sm text-danger-500">
              {errStatus === 409
                ? t("ai:errors.budgetExceeded")
                : t("ai:eligibility.error")}
            </p>
          )}

          {eligibility.data && (
            <>
              {/* Overall verdict (FR-117) — or a profile-completion prompt */}
              {profileIncomplete ? (
                <div className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-warning-500/30 bg-warning-50 p-3">
                  <p className="text-sm text-warning-600">
                    {t("ai:eligibility.completeProfile")}
                  </p>
                  <Link
                    to="/profile"
                    className="inline-flex shrink-0 items-center gap-1.5 rounded-md bg-brand-500 px-3 py-1.5 text-xs font-medium text-text-on-brand transition hover:bg-brand-600"
                  >
                    {t("ai:eligibility.goToProfile")}
                  </Link>
                </div>
              ) : (
                <div className="flex flex-wrap items-center gap-3">
                  <span className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                    {t("ai:eligibility.verdictLabel")}
                  </span>
                  <span
                    className={cn(
                      "inline-flex items-center rounded-full px-3 py-1 text-sm font-semibold ring-1",
                      VERDICT_STYLES[eligibility.data.verdict],
                    )}
                  >
                    {t(`ai:eligibility.verdict.${eligibility.data.verdict}`)}
                  </span>
                </div>
              )}

              {/* Summary */}
              <div className="rounded-md border border-border-subtle bg-bg-subtle/40 p-3 text-sm">
                <div className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                  {t("ai:eligibility.summary")}
                </div>
                <p className="mt-1 text-text-primary">
                  {isAr ? eligibility.data.summaryAr : eligibility.data.summaryEn}
                </p>
              </div>

              {/* Per-criterion comparison (FR-116) */}
              <div>
                <div className="mb-2 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                  {t("ai:eligibility.criteria")}
                </div>
                <ul className="space-y-2">
                  {eligibility.data.criteria.map((c) => (
                    <li
                      key={c.nameEn}
                      className="flex items-start gap-3 rounded-md border border-border-subtle p-3"
                    >
                      <MatchIcon m={c.match} />
                      <div className="min-w-0 flex-1">
                        <div className="text-sm font-medium">{isAr ? c.nameAr : c.nameEn}</div>
                        <div className="mt-1 grid gap-1 text-xs text-text-secondary sm:grid-cols-2">
                          <div>
                            <span className="text-text-tertiary">{t("ai:eligibility.you")}:</span>{" "}
                            <span className="text-text-primary">{localizeValue(c.studentValue)}</span>
                          </div>
                          <div>
                            <span className="text-text-tertiary">
                              {t("ai:eligibility.listing")}:
                            </span>{" "}
                            <span className="text-text-primary">
                              {localizeValue(c.listingRequirement)}
                            </span>
                          </div>
                        </div>
                      </div>
                      <span
                        className={cn(
                          "shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
                          MATCH_BADGE[c.match],
                        )}
                      >
                        {t(`ai:eligibility.match.${c.match}`)}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>

              {/* What to improve (FR-118) */}
              <div className="rounded-md border border-border-subtle p-3">
                <div className="text-sm font-semibold">
                  {t("ai:eligibility.improve.heading")}
                </div>
                {improvements.length === 0 ? (
                  <p className="mt-1 text-sm text-text-secondary">
                    {t("ai:eligibility.improve.none")}
                  </p>
                ) : (
                  <>
                    <p className="mt-1 text-xs text-text-secondary">
                      {t("ai:eligibility.improve.intro")}
                    </p>
                    <ul className="mt-2 space-y-1.5">
                      {improvements.map((c) => (
                        <li key={c.nameEn} className="flex items-start gap-2 text-sm">
                          <span
                            aria-hidden
                            className="mt-1.5 size-1.5 shrink-0 rounded-full bg-warning-500"
                          />
                          <span>
                            <span className="font-medium">{isAr ? c.nameAr : c.nameEn}</span>
                            {" — "}
                            <span className="text-text-secondary">
                              {localizeValue(c.listingRequirement)}
                            </span>
                          </span>
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </div>

              <AiDisclaimer />
            </>
          )}
        </div>
      )}
    </section>
  );
}
