import { useTranslation } from "react-i18next";
import { Sparkles } from "lucide-react";
import { cn } from "@/lib/utils";

export interface EmptyStateProps {
  owner: string;
  module: string;
  specPath?: string;
  className?: string;
  title?: string;
  body?: string;
}

/**
 * Placeholder shown on every module page stub. Teammates replace this with real UI.
 */
export function EmptyState({ owner, module, specPath, className, title, body }: EmptyStateProps) {
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

      <p className="mb-6 max-w-md text-base text-text-secondary">{body ?? t("generic.body")}</p>

      <dl className="grid gap-2 text-sm text-text-tertiary">
        <div>
          <dt className="inline font-medium text-text-secondary">{t("generic.ownerLabel")}: </dt>
          <dd className="inline">{owner}</dd>
        </div>
        <div>
          <dt className="inline font-medium text-text-secondary">{t("generic.moduleLabel")}: </dt>
          <dd className="inline">{module}</dd>
        </div>
      </dl>

      {specPath && (
        <p className="mt-4 font-mono text-xs text-text-tertiary">{specPath}</p>
      )}
    </section>
  );
}
