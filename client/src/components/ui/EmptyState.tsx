import { forwardRef } from "react";
import type { HTMLAttributes, ReactNode } from "react";
import { Sparkles } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "./Button";
import type { ButtonVariant } from "./Button";

/**
 * Action descriptor for the empty state's primary CTA. The component is
 * router-agnostic on purpose — pages that need a `<Link>` should pass their
 * own element via `customAction` instead.
 */
export interface EmptyStateAction {
  /** Button label. */
  label: string;
  /** Click handler. */
  onClick: () => void;
  /** Visual treatment — defaults to `primary`. */
  variant?: ButtonVariant;
  /** Optional leading icon. */
  leadingIcon?: ReactNode;
}

export interface EmptyStateProps extends HTMLAttributes<HTMLElement> {
  /** Lucide icon rendered inside the brand badge. Defaults to `Sparkles`. */
  icon?: LucideIcon;
  /** Bold headline. */
  title: string;
  /** Supporting paragraph below the title. */
  description?: string;
  /** Primary CTA. Renders a `<Button variant="primary">` by default. */
  action?: EmptyStateAction;
  /**
   * Pre-rendered action node — use this when you need a router `<Link>` or a
   * custom button. Takes precedence over `action`.
   */
  customAction?: ReactNode;
  /** Extra content below the action (helper text, secondary links). */
  children?: ReactNode;
}

/**
 * Premium empty-state — centred panel with a soft brand-tinted gradient halo,
 * icon badge, headline, description, and optional CTA. Used for zero-results
 * lists, fresh installs, and feature stubs. RTL-aware (centred layout works
 * the same in both directions).
 */
export const EmptyState = forwardRef<HTMLElement, EmptyStateProps>(
  function EmptyState(
    {
      icon: Icon = Sparkles,
      title,
      description,
      action,
      customAction,
      children,
      className,
      ...rest
    },
    ref,
  ) {
    return (
      <section
        ref={ref}
        aria-labelledby="empty-state-title"
        className={cn(
          // Card-style surface with overflow-hidden so the absolutely-positioned
          // gradient halo doesn't bleed past the rounded corner.
          "relative mx-auto flex w-full max-w-md flex-col items-center justify-center overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated px-8 py-16 text-center shadow-elevation-1",
          className,
        )}
        {...rest}
      >
        {/* Soft brand-tinted halo backdrop */}
        <div
          aria-hidden
          className="bg-mesh-hero pointer-events-none absolute inset-0 opacity-60"
        />

        {/* Content sits above the halo via z-index */}
        <div className="relative z-[1] flex flex-col items-center">
          {/* Icon badge with subtle brand gradient */}
          <div
            aria-hidden
            className="mb-5 flex size-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 ring-1 ring-brand-100 ring-inset"
          >
            <Icon className="size-7" />
          </div>

          <h2
            id="empty-state-title"
            className="mb-2 text-xl font-semibold tracking-tight text-text-primary"
          >
            {title}
          </h2>

          {description && (
            <p className="max-w-xs text-sm leading-relaxed text-text-secondary">
              {description}
            </p>
          )}

          {customAction ? (
            <div className="mt-6">{customAction}</div>
          ) : (
            action && (
              <div className="mt-6">
                <Button
                  variant={action.variant ?? "primary"}
                  leadingIcon={action.leadingIcon}
                  onClick={action.onClick}
                >
                  {action.label}
                </Button>
              </div>
            )
          )}

          {children && <div className="mt-4 w-full">{children}</div>}
        </div>
      </section>
    );
  },
);
