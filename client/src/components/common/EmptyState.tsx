import type { ReactNode } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Sparkles } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export interface EmptyStateProps {
  /** @deprecated kept for backwards compat — no longer rendered */
  owner?: string;
  /** @deprecated kept for backwards compat — no longer rendered */
  module?: string;
  /** @deprecated kept for backwards compat — no longer rendered */
  specPath?: string;
  className?: string;
  icon?: LucideIcon;
  title?: string;
  body?: string;
  /** Primary action button */
  action?: {
    label: string;
    to?: string;        // renders a Link
    onClick?: () => void; // renders a button
  };
  /** Optional secondary content below the action */
  children?: ReactNode;
}

/**
 * Universal empty state — used for empty lists, zero-results searches, and
 * module stubs. Accepts an optional icon, title, body, and action.
 */
export function EmptyState({
  className,
  icon: Icon = Sparkles,
  title,
  body,
  action,
  children,
}: EmptyStateProps) {
  const { t } = useTranslation("emptyStates");

  return (
    <section
      className={cn(
        "mx-auto flex w-full max-w-md flex-col items-center justify-center rounded-2xl border border-border-subtle bg-bg-elevated px-8 py-16 text-center shadow-xs",
        className,
      )}
    >
      {/* Icon badge */}
      <div className="mb-5 flex size-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-500">
        <Icon aria-hidden className="size-7" />
      </div>

      <h2 className="mb-2 text-xl font-semibold tracking-tight text-text-primary">
        {title ?? t("generic.title")}
      </h2>

      <p className="max-w-xs text-sm leading-relaxed text-text-secondary">
        {body ?? t("generic.body")}
      </p>

      {/* Action */}
      {action && (
        <div className="mt-6">
          {action.to ? (
            <Link
              to={action.to}
              className="cta-pill btn-brand bg-brand-500 px-6 py-2.5 text-sm text-white"
            >
              {action.label}
            </Link>
          ) : (
            <button
              type="button"
              onClick={action.onClick}
              className="cta-pill btn-brand bg-brand-500 px-6 py-2.5 text-sm text-white"
            >
              {action.label}
            </button>
          )}
        </div>
      )}

      {children && <div className="mt-4 w-full">{children}</div>}
    </section>
  );
}
