import { forwardRef } from "react";
import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/utils";

/**
 * Colour treatment for the badge — maps to the `.badge-*` utility classes.
 *
 * - `brand`   — blue (default, application status / featured chips).
 * - `success` — green (accepted, paid, verified).
 * - `warning` — amber (pending, expiring).
 * - `danger`  — red (rejected, failed, overdue).
 * - `neutral` — grey (archived, draft, "N/A").
 */
export type BadgeVariant =
  | "brand"
  | "success"
  | "warning"
  | "danger"
  | "neutral";

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  /** Colour treatment — defaults to `neutral`. */
  variant?: BadgeVariant;
  /** Optional leading icon (rendered before the label). */
  leadingIcon?: ReactNode;
  /** Optional trailing icon (close button, chevron, etc.). */
  trailingIcon?: ReactNode;
}

const variantClass: Record<BadgeVariant, string> = {
  brand: "badge-brand",
  success: "badge-success",
  warning: "badge-warning",
  danger: "badge-danger",
  neutral: "badge-neutral",
};

/**
 * Pill badge primitive — built on the `.badge` design-system utility. Used for
 * status chips (Kanban columns, payment state, application stage). Icons use
 * `gap` so layout stays consistent in both LTR and RTL.
 */
export const Badge = forwardRef<HTMLSpanElement, BadgeProps>(function Badge(
  { variant = "neutral", leadingIcon, trailingIcon, className, children, ...rest },
  ref,
) {
  return (
    <span
      ref={ref}
      className={cn("badge", variantClass[variant], className)}
      {...rest}
    >
      {leadingIcon && (
        <span aria-hidden className="inline-flex shrink-0 items-center">
          {leadingIcon}
        </span>
      )}
      {children}
      {trailingIcon && (
        <span aria-hidden className="inline-flex shrink-0 items-center">
          {trailingIcon}
        </span>
      )}
    </span>
  );
});
