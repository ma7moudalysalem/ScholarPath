import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Check, X, CircleHelp, Minus } from "lucide-react";
import { aiApi, type EligibilityDto, type EligibilityMatch } from "@/services/api/ai";
import { AiDisclaimer } from "./AiDisclaimer";

interface Props {
  scholarshipId: string;
  children?: React.ReactNode; // optional trigger renderer
}

function MatchIcon({ m }: { m: EligibilityMatch }) {
  const cls = "size-4 flex-shrink-0";
  switch (m) {
    case "yes": return <Check aria-hidden className={`${cls} text-emerald-500`} />;
    case "no": return <X aria-hidden className={`${cls} text-rose-500`} />;
    case "partial": return <Minus aria-hidden className={`${cls} text-amber-500`} />;
    default: return <CircleHelp aria-hidden className={`${cls} text-text-tertiary`} />;
  }
}

export function EligibilityChecker({ scholarshipId }: Props) {
  const { t } = useTranslation(["ai", "common"]);
  const [open, setOpen] = useState(false);
  const [data, setData] = useState<EligibilityDto | null>(null);

  const mut = useMutation({
    mutationFn: () => aiApi.eligibility(scholarshipId),
    onSuccess: (dto) => { setData(dto); setOpen(true); },
    onError: (err: { status?: number }) => {
      if (err.status === 409) toast.warning(t("ai:errors.budgetExceeded"));
      else toast.error(t("ai:errors.generic"));
    },
  });

  return (
    <>
      <button
        type="button"
        onClick={() => mut.mutate()}
        disabled={mut.isPending}
        className="inline-flex items-center gap-2 rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
      >
        {t("ai:eligibility.open")}
      </button>

      {open && data && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          onClick={() => setOpen(false)}
        >
          <div
            className="max-h-[85vh] w-full max-w-xl overflow-y-auto rounded-lg border border-border-subtle bg-bg-elevated p-6 shadow-lg"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-lg font-semibold">{t("ai:eligibility.heading")}</h3>

            <div className="mt-4 rounded-md border border-border-subtle bg-bg-subtle/40 p-3 text-sm">
              <div className="text-xs font-semibold uppercase tracking-wide text-text-tertiary">
                {t("ai:eligibility.summary")}
              </div>
              <p className="mt-1">{data.summary}</p>
            </div>

            <ul className="mt-4 space-y-2">
              {data.criteria.map((c) => (
                <li key={c.name} className="flex items-start gap-3 rounded-md border border-border-subtle p-3">
                  <MatchIcon m={c.match} />
                  <div className="min-w-0 flex-1">
                    <div className="font-medium">{c.name}</div>
                    <div className="mt-1 grid grid-cols-2 gap-2 text-xs text-text-secondary">
                      <div>
                        <span className="text-text-tertiary">{t("ai:eligibility.you")}:</span>{" "}
                        <span className="text-text-primary">{c.studentValue}</span>
                      </div>
                      <div>
                        <span className="text-text-tertiary">{t("ai:eligibility.listing")}:</span>{" "}
                        <span className="text-text-primary">{c.listingRequirement}</span>
                      </div>
                    </div>
                  </div>
                  <span className="rounded-full bg-bg-subtle px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide">
                    {t(`ai:eligibility.match.${c.match}`)}
                  </span>
                </li>
              ))}
            </ul>

            <div className="mt-4">
              <AiDisclaimer />
            </div>

            <div className="mt-5 flex justify-end">
              <button
                type="button"
                onClick={() => setOpen(false)}
                className="rounded-md border border-border-subtle px-4 py-2 text-sm hover:border-border-default"
              >
                {t("ai:eligibility.close")}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
