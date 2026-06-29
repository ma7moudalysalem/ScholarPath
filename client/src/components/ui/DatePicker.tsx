import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import * as Popover from "@radix-ui/react-popover";
import { DayPicker } from "react-day-picker";
import { ar, enUS } from "date-fns/locale";
import { format } from "date-fns";
import { Calendar as CalendarIcon, X, ChevronLeft, ChevronRight, ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";
import "react-day-picker/dist/style.css";

/**
 * Localised date picker — replaces the native HTML `<input type="date">` so
 * Arabic / RTL users see Arabic month names + weekdays instead of whatever
 * the browser locale forces ("May 2026", "Su Mo Tu We Th Fr Sa", etc.).
 *
 * <para>
 * Built on Radix Popover (positioning, focus, escape-to-close) + react-day-picker
 * (calendar grid) + date-fns locales (already a project dep for the date-locale
 * sweep). Value semantics match the native input so the form code that
 * consumed `<input type="date">` only needs to swap the JSX — the wire format
 * stays `YYYY-MM-DD`.
 * </para>
 */
export interface DatePickerProps {
  /** ISO `YYYY-MM-DD` (or empty string). Matches the native input shape. */
  value: string;
  /** Called with the new `YYYY-MM-DD` value, or `""` when cleared. */
  onChange: (value: string) => void;
  /** ISO `YYYY-MM-DD` lower bound — picks before this date are disabled. */
  min?: string;
  /** ISO `YYYY-MM-DD` upper bound — picks after this date are disabled. */
  max?: string;
  /** Disables the trigger button entirely. */
  disabled?: boolean;
  /** Optional placeholder shown when no value is selected. */
  placeholder?: string;
  /** Optional element id (for `<label htmlFor>`). */
  id?: string;
  /** Forwarded to the trigger button so callers can size / align it. */
  className?: string;
  /** Show a clear (×) button inside the trigger when a value is set. */
  clearable?: boolean;
  /** Accessible label for the trigger when no visible label exists. */
  ariaLabel?: string;
}

/** Parses a `YYYY-MM-DD` string into a `Date`, or `undefined` if invalid. */
function parseIso(value: string | undefined): Date | undefined {
  if (!value) return undefined;
  const parts = value.split("-").map(Number);
  if (parts.length !== 3 || parts.some((n) => Number.isNaN(n))) return undefined;
  const [y, m, d] = parts;
  return new Date(y, m - 1, d);
}

/** Serialises a `Date` to `YYYY-MM-DD` using local-time digits (no UTC shift). */
function toIso(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

export function DatePicker({
  value,
  onChange,
  min,
  max,
  disabled,
  placeholder,
  id,
  className,
  clearable = true,
  ariaLabel,
}: DatePickerProps) {
  const { t, i18n } = useTranslation();
  const isAr = i18n.language.startsWith("ar");
  const locale = isAr ? ar : enUS;
  const [open, setOpen] = useState(false);

  const selected = useMemo(() => parseIso(value), [value]);
  const minDate = useMemo(() => parseIso(min), [min]);
  const maxDate = useMemo(() => parseIso(max), [max]);

  // Bounds for the month + year dropdowns. Default to a wide range (covers a
  // date of birth ~80y back through deadlines ~10y ahead); narrow it when the
  // caller passes a min/max so the year dropdown only offers valid years.
  const currentYear = new Date().getFullYear();
  const startMonth = minDate ?? new Date(currentYear - 80, 0, 1);
  const endMonth = maxDate ?? new Date(currentYear + 10, 11, 31);

  // Default placeholder lives in `common:datePicker.placeholder` so every
  // empty DatePicker across the app reads the same — callers should NOT
  // pass the field's LABEL as `placeholder`, that just duplicates the
  // field's title back into the empty input.
  const placeholderText = placeholder ?? t("common:datePicker.placeholder");
  const display = selected
    ? format(selected, "PP", { locale })
    : placeholderText;

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation();
    onChange("");
  };

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          id={id}
          type="button"
          disabled={disabled}
          aria-label={ariaLabel}
          // Merge (not replace) the base trigger style with the caller's
          // className: callers like the audit-log filter pass only sizing
          // utilities ("h-10 w-44") and must keep the border / background /
          // padding / focus ring. twMerge lets caller utilities win on
          // conflicts (w-44 over w-full) while preserving the rest.
          className={cn(
            "inline-flex h-10 w-full items-center justify-between gap-2 rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 disabled:cursor-not-allowed disabled:opacity-60",
            className,
          )}
        >
          <span className="flex items-center gap-2 truncate">
            <CalendarIcon size={16} className="flex-shrink-0 text-text-tertiary" />
            <span className={selected ? "text-text-primary" : "text-text-tertiary"}>
              {display}
            </span>
          </span>
          {clearable && selected && !disabled && (
            <span
              role="button"
              tabIndex={0}
              onClick={handleClear}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  onChange("");
                }
              }}
              aria-label={t("common:dialog.cancel", "Cancel")}
              className="flex-shrink-0 rounded-full p-0.5 text-text-tertiary hover:bg-bg-subtle hover:text-text-secondary"
            >
              <X size={14} />
            </span>
          )}
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={6}
          className="z-50 rounded-xl border border-border-subtle bg-bg-elevated p-3 shadow-lg outline-none"
          dir={isAr ? "rtl" : "ltr"}
        >
          <DayPicker
            mode="single"
            selected={selected}
            onSelect={(date) => {
              if (date) {
                onChange(toIso(date));
                setOpen(false);
              }
            }}
            locale={locale}
            dir={isAr ? "rtl" : "ltr"}
            // Month + year dropdowns so users can jump directly instead of
            // stepping one month at a time with the arrows (which also can't
            // change the year). The dropdowns are bounded by startMonth/endMonth.
            captionLayout="dropdown"
            startMonth={startMonth}
            endMonth={endMonth}
            components={{
              // Clean lucide chevrons. react-day-picker passes the visually
              // correct orientation per the calendar's dir, so left/right is
              // already RTL-aware here.
              Chevron: ({ orientation }) =>
                orientation === "left" ? (
                  <ChevronLeft className="size-4" />
                ) : orientation === "right" ? (
                  <ChevronRight className="size-4" />
                ) : (
                  <ChevronDown className="size-3.5 opacity-60" />
                ),
            }}
            disabled={[
              ...(minDate ? [{ before: minDate }] : []),
              ...(maxDate ? [{ after: maxDate }] : []),
            ]}
            // Tailwind-driven classNames matching react-day-picker v10's API.
            // v10 renamed the keys: caption→month_caption, head_row→weekdays,
            // head_cell→weekday, row→week, day→day_button, etc.
            classNames={{
              months: "flex flex-col",
              month: "space-y-2",
              month_caption:
                "flex justify-center items-center pt-1 pb-2 px-9 relative h-9",
              caption_label: "hidden",
              dropdowns: "flex items-center justify-center gap-1.5",
              dropdown:
                "cursor-pointer rounded-md border border-border-subtle bg-bg-elevated px-2 py-1 text-sm font-semibold text-text-primary hover:bg-bg-subtle focus:border-brand-500 focus:outline-none",
              nav: "flex items-center justify-between absolute inset-x-1 top-1.5",
              button_previous:
                "inline-flex h-7 w-7 items-center justify-center rounded-md text-text-secondary hover:bg-bg-subtle disabled:opacity-30 z-10",
              button_next:
                "inline-flex h-7 w-7 items-center justify-center rounded-md text-text-secondary hover:bg-bg-subtle disabled:opacity-30 z-10",
              month_grid: "w-full border-collapse",
              weekdays: "flex",
              weekday:
                "text-text-tertiary w-9 font-medium text-xs uppercase tracking-wider",
              week: "flex w-full",
              day: "h-9 w-9 text-center text-sm p-0 relative focus-within:relative focus-within:z-10",
              day_button:
                "inline-flex h-9 w-9 items-center justify-center rounded-md text-sm hover:bg-bg-subtle",
              selected:
                "[&_button]:bg-brand-500 [&_button]:text-white [&_button]:hover:bg-brand-600",
              today: "[&_button]:font-bold [&_button]:text-brand-600 [&_button]:ring-1 [&_button]:ring-brand-300",
              outside: "text-text-tertiary/50",
              disabled: "text-text-tertiary/40 [&_button]:cursor-not-allowed",
            }}
            footer={
              clearable && selected ? (
                <div className="mt-2 flex justify-end border-t border-border-subtle pt-2">
                  <button
                    type="button"
                    onClick={() => {
                      onChange("");
                      setOpen(false);
                    }}
                    className="text-xs text-text-secondary hover:text-text-primary"
                  >
                    {t("common:dialog.cancel", "Clear")}
                  </button>
                </div>
              ) : undefined
            }
          />
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
