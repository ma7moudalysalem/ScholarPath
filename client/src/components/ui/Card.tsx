import { forwardRef } from "react";
import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

/**
 * Card visual treatment — maps to the design-system utility classes.
 *
 * - `default` — `.card`: flat elevated surface with subtle border.
 * - `premium` — `.card-premium`: lifts on hover, stronger shadow ramp.
 * - `glass`   — `.card-glass`: frosted-glass overlay (nav, modals, peek panels).
 * - `feature` — `.card-feature`: marketing card with brand mesh hover.
 */
export type CardVariant = "default" | "premium" | "glass" | "feature";

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  /** Visual variant — defaults to `default`. */
  variant?: CardVariant;
  /**
   * Renders the card as a different element (e.g. `"article"`, `"section"`).
   * Polymorphism is intentionally limited to native block tags so the API
   * stays narrow.
   */
  as?: "div" | "article" | "section" | "aside";
}

const variantClass: Record<CardVariant, string> = {
  default: "card",
  premium: "card-premium",
  glass: "card-glass",
  feature: "card-feature",
};

/**
 * Card primitive — wraps the `.card-*` design-system utilities. Use `variant`
 * to pick the elevation/treatment, and `as` to swap the underlying tag for
 * semantic correctness (e.g. an `<article>` for a scholarship summary card).
 */
export const Card = forwardRef<HTMLDivElement, CardProps>(function Card(
  { variant = "default", as = "div", className, children, ...rest },
  ref,
) {
  const Tag = as;
  return (
    <Tag
      ref={ref as React.Ref<HTMLDivElement>}
      className={cn(variantClass[variant], className)}
      {...rest}
    >
      {children}
    </Tag>
  );
});

export type CardSlotProps = HTMLAttributes<HTMLDivElement>;

/**
 * Optional header slot — keeps padding + border bottom consistent across
 * cards. Compose it with `<Card>` for a familiar header/body/footer rhythm.
 */
export const CardHeader = forwardRef<HTMLDivElement, CardSlotProps>(
  function CardHeader({ className, ...rest }, ref) {
    return (
      <div
        ref={ref}
        className={cn(
          "flex flex-col gap-1 border-b border-border-subtle p-5",
          className,
        )}
        {...rest}
      />
    );
  },
);

export const CardTitle = forwardRef<
  HTMLHeadingElement,
  HTMLAttributes<HTMLHeadingElement>
>(function CardTitle({ className, ...rest }, ref) {
  return (
    <h3
      ref={ref}
      className={cn(
        "text-base font-semibold tracking-tight text-text-primary",
        className,
      )}
      {...rest}
    />
  );
});

export const CardDescription = forwardRef<
  HTMLParagraphElement,
  HTMLAttributes<HTMLParagraphElement>
>(function CardDescription({ className, ...rest }, ref) {
  return (
    <p
      ref={ref}
      className={cn("text-sm text-text-secondary", className)}
      {...rest}
    />
  );
});

export const CardBody = forwardRef<HTMLDivElement, CardSlotProps>(
  function CardBody({ className, ...rest }, ref) {
    return <div ref={ref} className={cn("p-5", className)} {...rest} />;
  },
);

export const CardFooter = forwardRef<HTMLDivElement, CardSlotProps>(
  function CardFooter({ className, ...rest }, ref) {
    return (
      <div
        ref={ref}
        className={cn(
          "flex items-center justify-end gap-2 border-t border-border-subtle p-5",
          className,
        )}
        {...rest}
      />
    );
  },
);
