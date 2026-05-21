import { forwardRef } from "react";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

/**
 * Visual treatment for the button.
 *
 * - `primary`   — brand gradient with colored shadow (default CTA).
 * - `secondary` — neutral surface with subtle border (cancel, alt actions).
 * - `ghost`     — transparent until hovered (toolbar / inline actions).
 * - `danger`    — red gradient for destructive actions.
 */
export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

/**
 * Size of the button. `md` is the default; `sm` is for compact toolbars,
 * `lg` for hero CTAs.
 */
export type ButtonSize = "sm" | "md" | "lg";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Visual variant — defaults to `primary`. */
  variant?: ButtonVariant;
  /** Size token — defaults to `md`. */
  size?: ButtonSize;
  /**
   * When `true`, the button is disabled and a spinner replaces the leading
   * icon. The label remains visible so layout doesn't jump.
   */
  loading?: boolean;
  /** Optional leading icon (rendered before the label). */
  leadingIcon?: ReactNode;
  /** Optional trailing icon (rendered after the label). */
  trailingIcon?: ReactNode;
  /**
   * When `true`, the button stretches to fill its container's inline size.
   * Useful inside narrow side panels and forms.
   */
  fullWidth?: boolean;
}

const variantClass: Record<ButtonVariant, string> = {
  primary: "btn-primary",
  secondary: "btn-secondary",
  ghost: "btn-ghost",
  danger: "btn-danger",
};

const sizeClass: Record<ButtonSize, string> = {
  sm: "btn-sm",
  md: "",
  lg: "btn-lg",
};

/**
 * Premium button primitive — wraps the `.btn` design-system utility so app
 * code never has to remember which Tailwind classes to chain. RTL-safe (uses
 * `gap` for icon spacing instead of margins) and forwards `ref` so it composes
 * cleanly with Radix triggers / focus management.
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    variant = "primary",
    size = "md",
    loading = false,
    leadingIcon,
    trailingIcon,
    fullWidth,
    disabled,
    className,
    children,
    type,
    "aria-label": ariaLabel,
    ...rest
  },
  ref,
) {
  const isDisabled = disabled || loading;

  return (
    <button
      ref={ref}
      // Defaulting `type` matters: inside a `<form>`, an un-typed button submits.
      type={type ?? "button"}
      disabled={isDisabled}
      aria-busy={loading || undefined}
      aria-label={ariaLabel}
      className={cn(
        "btn",
        variantClass[variant],
        sizeClass[size],
        fullWidth && "w-full",
        className,
      )}
      {...rest}
    >
      {loading ? (
        <Loader2 aria-hidden className="size-4 animate-spin" />
      ) : (
        leadingIcon && (
          <span aria-hidden className="inline-flex shrink-0 items-center">
            {leadingIcon}
          </span>
        )
      )}
      {children}
      {!loading && trailingIcon && (
        <span aria-hidden className="inline-flex shrink-0 items-center">
          {trailingIcon}
        </span>
      )}
    </button>
  );
});
