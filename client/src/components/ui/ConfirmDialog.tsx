import { useTranslation } from "react-i18next";
import * as Dialog from "@radix-ui/react-dialog";
import { Loader2 } from "lucide-react";

/**
 * Visual style for the confirm action button.
 *
 * - `default`   → blue brand button (e.g. "Submit", "Save", "Continue").
 * - `destructive` → red danger button (e.g. "Archive", "Delete", "Cancel booking").
 */
type ConfirmVariant = "default" | "destructive";

export interface ConfirmDialogProps {
  /** Whether the dialog is open. Controlled — the caller owns the boolean. */
  open: boolean;
  /** Called when the dialog requests to close (overlay click, escape, cancel). */
  onOpenChange: (open: boolean) => void;
  /** Bold headline above the description (e.g. "Cancel booking?"). */
  title: string;
  /** Optional supporting copy (the explanation under the title). */
  description?: string;
  /** Confirm-button label. Defaults to `common.dialog.confirm`. */
  confirmLabel?: string;
  /** Cancel-button label. Defaults to `common.dialog.cancel`. */
  cancelLabel?: string;
  /** Colour treatment for the confirm button — destructive uses red. */
  variant?: ConfirmVariant;
  /** Invoked when the user clicks confirm. May return a promise. */
  onConfirm: () => void | Promise<void>;
  /**
   * When true, both buttons disable and the confirm button shows a spinner.
   * The caller usually wires this to the underlying mutation's `isPending`.
   */
  loading?: boolean;
}

/**
 * Styled replacement for `window.confirm()`. Built on Radix's dialog so it is
 * keyboard-navigable, RTL-aware, and respects the existing design tokens.
 *
 * Usage:
 * ```tsx
 * const [open, setOpen] = useState(false);
 * <ConfirmDialog
 *   open={open}
 *   onOpenChange={setOpen}
 *   title={t("scholarships.archive.title")}
 *   description={t("scholarships.archive.body")}
 *   variant="destructive"
 *   loading={mutation.isPending}
 *   onConfirm={() => mutation.mutate(id)}
 * />
 * ```
 */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel,
  cancelLabel,
  variant = "default",
  onConfirm,
  loading = false,
}: ConfirmDialogProps) {
  const { t } = useTranslation("common");

  const confirmClasses =
    variant === "destructive"
      ? "bg-danger-500 hover:bg-danger-600 text-white"
      : "bg-brand-500 hover:bg-brand-600 text-text-on-brand";

  const handleConfirm = async () => {
    await onConfirm();
  };

  // We intentionally do NOT close the dialog on confirm — the caller decides
  // when to flip `open` to false (typically in the mutation's onSuccess /
  // onError handlers) so the spinner stays visible while the request runs.
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
              disabled={loading}
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
