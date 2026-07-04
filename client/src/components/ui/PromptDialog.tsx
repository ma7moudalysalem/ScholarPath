import { useState } from "react";
import { useTranslation } from "react-i18next";
import * as Dialog from "@radix-ui/react-dialog";
import { Loader2 } from "lucide-react";

type ConfirmVariant = "default" | "destructive";

export interface PromptDialogProps {
  /** Whether the dialog is open. Controlled — the caller owns the boolean. */
  open: boolean;
  /** Called when the dialog requests to close (overlay click, escape, cancel). */
  onOpenChange: (open: boolean) => void;
  /** Bold headline above the description (e.g. "Reject application?"). */
  title: string;
  /** Optional supporting copy under the title. */
  description?: string;
  /** Label rendered above the text field. */
  inputLabel?: string;
  /** Placeholder shown inside the text field. */
  inputPlaceholder?: string;
  /**
   * When true, the field is a multi-line `<textarea>` (good for reasons /
   * notes). Defaults to a single-line `<input>`.
   */
  inputMultiline?: boolean;
  /** Initial value for the text field. Defaults to an empty string. */
  defaultValue?: string;
  /** Confirm-button label. Defaults to `common.dialog.confirm`. */
  confirmLabel?: string;
  /** Cancel-button label. Defaults to `common.dialog.cancel`. */
  cancelLabel?: string;
  /** Colour treatment for the confirm button — destructive uses red. */
  variant?: ConfirmVariant;
  /**
   * Invoked when the user clicks confirm. The trimmed input is passed in;
   * an empty string is allowed (upstream callers decide whether it's required).
   */
  onConfirm: (value: string) => void | Promise<void>;
  /**
   * When true, both buttons disable and the confirm button shows a spinner.
   * Usually wired to the underlying mutation's `isPending`.
   */
  loading?: boolean;
  /**
   * When true, the confirm button stays disabled until the field contains
   * non-whitespace text. Used for mandatory-reason flows (e.g. FR-APP-30
   * application rejection, where a reason is required).
   */
  requireInput?: boolean;
}

/**
 * Styled replacement for `window.prompt()` — a confirm dialog with a text
 * input baked in. Mirrors {@link ConfirmDialog} so the two share a look-and-
 * feel; the only addition is the controlled text field handed to `onConfirm`.
 *
 * Usage:
 * ```tsx
 * const [open, setOpen] = useState(false);
 * <PromptDialog
 *   open={open}
 *   onOpenChange={setOpen}
 *   title={t("scholarshipProviderReview.decision.rejectPrompt.title")}
 *   inputLabel={t("scholarshipProviderReview.decision.rejectPrompt.label")}
 *   inputMultiline
 *   variant="destructive"
 *   loading={rejectMut.isPending}
 *   onConfirm={(reason) => rejectMut.mutate({ id, reason })}
 * />
 * ```
 */
export function PromptDialog({
  open,
  onOpenChange,
  title,
  description,
  inputLabel,
  inputPlaceholder,
  inputMultiline = false,
  defaultValue = "",
  confirmLabel,
  cancelLabel,
  variant = "default",
  onConfirm,
  loading = false,
  requireInput = false,
}: PromptDialogProps) {
  const { t } = useTranslation("common");

  // Reset the field whenever the dialog opens so stale text doesn't leak in
  // between separate uses of the same instance. Tracking the previous `open`
  // value as state (instead of an effect) lets us synchronise via React's
  // documented "render-phase reset" pattern, avoiding cascading rerenders.
  const [value, setValue] = useState(defaultValue);
  const [wasOpen, setWasOpen] = useState(open);
  if (open && !wasOpen) {
    setWasOpen(true);
    setValue(defaultValue);
  } else if (!open && wasOpen) {
    setWasOpen(false);
  }

  const confirmClasses =
    variant === "destructive"
      ? "bg-danger-500 hover:bg-danger-600 text-white"
      : "bg-brand-500 hover:bg-brand-600 text-text-on-brand";

  // When the field is mandatory, block confirmation until it has real text.
  const confirmDisabled = loading || (requireInput && value.trim().length === 0);

  const handleConfirm = async () => {
    if (requireInput && value.trim().length === 0) return;
    await onConfirm(value.trim());
  };

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-2xl bg-bg-elevated p-6 shadow-xl">
          <Dialog.Title className="text-lg font-semibold text-text-primary">
            {title}
          </Dialog.Title>
          {description && (
            <Dialog.Description className="mt-2 text-sm text-text-secondary">
              {description}
            </Dialog.Description>
          )}

          <div className="mt-4 space-y-1.5">
            {inputLabel && (
              <label
                htmlFor="prompt-dialog-input"
                className="block text-xs font-medium text-text-secondary"
              >
                {inputLabel}
              </label>
            )}
            {inputMultiline ? (
              <textarea
                id="prompt-dialog-input"
                value={value}
                onChange={(e) => setValue(e.target.value)}
                placeholder={inputPlaceholder}
                disabled={loading}
                rows={4}
                className="w-full resize-none rounded-lg border border-border-subtle bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:ring-2 focus:ring-brand-400 disabled:opacity-50"
              />
            ) : (
              <input
                id="prompt-dialog-input"
                type="text"
                value={value}
                onChange={(e) => setValue(e.target.value)}
                placeholder={inputPlaceholder}
                disabled={loading}
                className="w-full rounded-lg border border-border-subtle bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:ring-2 focus:ring-brand-400 disabled:opacity-50"
              />
            )}
          </div>

          <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              disabled={loading}
              className="inline-flex h-10 items-center justify-center rounded-lg border border-border-subtle bg-bg-elevated px-4 text-sm font-medium text-text-primary transition hover:border-border-default disabled:opacity-50"
            >
              {cancelLabel ?? t("dialog.cancel")}
            </button>
            <button
              type="button"
              onClick={handleConfirm}
              disabled={confirmDisabled}
              className={`inline-flex h-10 items-center justify-center gap-2 rounded-lg px-4 text-sm font-medium transition disabled:opacity-50 ${confirmClasses}`}
            >
              {loading && <Loader2 className="size-4 animate-spin" />}
              {loading
                ? t("dialog.processing")
                : (confirmLabel ?? t("dialog.confirm"))}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
