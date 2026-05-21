import { forwardRef, useState } from "react";
import type { InputHTMLAttributes } from "react";
import { Eye, EyeOff } from "lucide-react";
import { useTranslation } from "react-i18next";
import { cn } from "@/lib/utils";

/**
 * Password input with a show/hide eye toggle. Drop-in replacement for a
 * `<input type="password" />` — same props, forwards ref to the underlying
 * input so react-hook-form's `register()` works unchanged. The toggle button
 * is positioned at the trailing edge (right in LTR, left in RTL) and uses
 * `Eye`/`EyeOff` icons from lucide-react. Click toggles the input type
 * between `password` and `text`.
 */
export interface PasswordInputProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  /** Optional className for the WRAPPER (the input still uses `className`). */
  wrapperClassName?: string;
}

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
  function PasswordInput(
    { className, wrapperClassName, ...inputProps },
    ref,
  ) {
    const { t } = useTranslation("common");
    const [visible, setVisible] = useState(false);

    return (
      <div className={cn("relative", wrapperClassName)}>
        <input
          ref={ref}
          {...inputProps}
          type={visible ? "text" : "password"}
          // Reserve space on the trailing edge for the toggle button
          // (logical padding-end works in both LTR and RTL).
          className={cn(className, "pe-10")}
        />
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          aria-label={
            visible
              ? t("password.hide", "Hide password")
              : t("password.show", "Show password")
          }
          aria-pressed={visible}
          tabIndex={-1}
          className="absolute end-2.5 top-1/2 -translate-y-1/2 inline-flex size-7 items-center justify-center rounded-md text-text-tertiary transition-colors hover:bg-bg-subtle hover:text-text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          {visible ? (
            <EyeOff aria-hidden className="size-4" />
          ) : (
            <Eye aria-hidden className="size-4" />
          )}
        </button>
      </div>
    );
  },
);
