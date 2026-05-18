import { useTranslation } from "react-i18next";
import { Sparkles } from "lucide-react";
import { cn } from "@/lib/utils";

export interface EmptyStateProps {
  /** @deprecated kept for backwards compat — no longer rendered */
  owner?: string;
  /** @deprecated kept for backwards compat — no longer rendered */
  module?: string;
  /** @deprecated kept for backwards compat — no longer rendered */
  specPath?: string;
  className?: string;
  title?: string;
  body?: string;
}

/**
 * Placeholder shown on every module page stub. Teammates replace this with real UI.
 */
export function EmptyState({ className, title, body }: EmptyStateProps) {
  const { t } = useTranslation("emptyStates");

  return (
    <section
      className={cn(
        "mx-auto flex w-full max-w-2xl flex-col items-center justify-center rounded-xl border border-border-subtle bg-bg-elevated px-8 py-16 text-center shadow-xs",
        className,
      )}
    >
      <div className="mb-6 flex size-14 items-center justify-center rounded-full bg-brand-50 text-brand-500">
        <Sparkles aria-hidden className="size-7" />
      </div>

      <h2 className="mb-2 text-2xl font-semibold text-text-primary">
        {title ?? t("generic.title")}
      </h2>

      <p className="max-w-md text-base text-text-secondary">{body ?? t("generic.body")}</p>
    </section>
  );
}
