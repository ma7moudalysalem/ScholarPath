import { useState } from "react";
import { useTranslation } from "react-i18next";
import * as Dialog from "@radix-ui/react-dialog";
import { Flag, Loader2 } from "lucide-react";
import { FLAG_REASONS, flagReasonLabel } from "@/lib/flagReasons";

export interface FlagPostDialogProps {
  /** Controlled open state — the caller owns the boolean. */
  open: boolean;
  /** Called when the dialog requests to close (overlay, escape, cancel). */
  onOpenChange: (open: boolean) => void;
  /**
   * Invoked on confirm with the chosen reason KEY (a {@link FLAG_REASONS}
   * value, not a localized string) and the trimmed optional details.
   */
  onSubmit: (reason: string, additionalDetails: string) => void | Promise<void>;
  /** When true, buttons disable and the submit button shows a spinner. */
  loading?: boolean;
}

/**
 * Styled replacement for the old `window.prompt()` flag flow. Captures a
 * categorized reason (so the admin moderation queue groups reports
 * consistently) plus optional free-text details. Shares the look-and-feel of
 * {@link PromptDialog}; RTL is inherited from the document `dir`.
 */
export function FlagPostDialog({
  open,
  onOpenChange,
  onSubmit,
  loading = false,
}: FlagPostDialogProps) {
  const { t, i18n } = useTranslation("community");
  const lang = i18n.language;

  const [reason, setReason] = useState("");
  const [details, setDetails] = useState("");

  // Reset the fields whenever the dialog opens so stale input never leaks
  // between separate reports (same render-phase reset pattern as PromptDialog).
  const [wasOpen, setWasOpen] = useState(open);
  if (open && !wasOpen) {
    setWasOpen(true);
    setReason("");
    setDetails("");
  } else if (!open && wasOpen) {
    setWasOpen(false);
  }

  const canSubmit = reason !== "" && !loading;

  const handleSubmit = async () => {
    if (!canSubmit) return;
    await onSubmit(reason, details.trim());
  };

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-2xl bg-bg-elevated p-6 shadow-xl">
          <div className="flex items-start gap-3">
            <span className="flex size-10 flex-shrink-0 items-center justify-center rounded-full bg-danger-500/10 text-danger-500">
              <Flag className="size-5" />
            </span>
            <div className="min-w-0">
              <Dialog.Title className="text-lg font-semibold text-text-primary">
                {t("flagDialog.title")}
              </Dialog.Title>
              <Dialog.Description className="mt-1 text-sm text-text-secondary">
                {t("flagDialog.description")}
              </Dialog.Description>
            </div>
          </div>

          <div className="mt-5 space-y-4">
            <div className="space-y-1.5">
              <label
                htmlFor="flag-reason"
                className="block text-xs font-medium text-text-secondary"
              >
                {t("flagDialog.reasonLabel")}
              </label>
              <select
                id="flag-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                disabled={loading}
                className="w-full rounded-lg border border-border-subtle bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:ring-2 focus:ring-brand-400 disabled:opacity-50"
              >
                <option value="" disabled>
                  {t("flagDialog.reasonPlaceholder")}
                </option>
                {FLAG_REASONS.map((r) => (
                  <option key={r} value={r}>
                    {flagReasonLabel(r, lang)}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label
                htmlFor="flag-details"
                className="flex items-center gap-1 text-xs font-medium text-text-secondary"
              >
                {t("flagDialog.detailsLabel")}
                <span className="font-normal text-text-tertiary">
                  ({t("flagDialog.optional")})
                </span>
              </label>
              <textarea
                id="flag-details"
                value={details}
                onChange={(e) => setDetails(e.target.value)}
                placeholder={t("flagDialog.detailsPlaceholder")}
                disabled={loading}
                rows={3}
                maxLength={1000}
                className="w-full resize-none rounded-lg border border-border-subtle bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:ring-2 focus:ring-brand-400 disabled:opacity-50"
              />
            </div>
          </div>

          <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => onOpenChange(false)}
              disabled={loading}
              className="inline-flex h-10 items-center justify-center rounded-lg border border-border-subtle bg-bg-elevated px-4 text-sm font-medium text-text-primary transition hover:border-border-default disabled:opacity-50"
            >
              {t("flagDialog.cancel")}
            </button>
            <button
              type="button"
              onClick={handleSubmit}
              disabled={!canSubmit}
              className="inline-flex h-10 items-center justify-center gap-2 rounded-lg bg-danger-500 px-4 text-sm font-medium text-white transition hover:bg-danger-600 disabled:opacity-50"
            >
              {loading && <Loader2 className="size-4 animate-spin" />}
              {loading ? t("flagDialog.submitting") : t("flagDialog.submit")}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
