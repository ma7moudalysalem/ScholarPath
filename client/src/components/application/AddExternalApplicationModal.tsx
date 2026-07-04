import { useState } from "react";
import { useTranslation } from "react-i18next";
import * as Dialog from "@radix-ui/react-dialog";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { X, Loader2 } from "lucide-react";
import { applicationsApi } from "@/services/api/applications";
import { queryKeys } from "@/lib/queryClient";
import { ApiError } from "@/services/api/client";

interface AddExternalApplicationModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * "Add External Application" — lets the student log a scholarship they're
 * pursuing OFF the ScholarPath platform. The form takes free-text fields
 * directly (title, provider, optional URL/deadline/notes) — no catalogue
 * search step, because the whole point is the scholarship is NOT in the
 * platform's catalogue.
 *
 * The form lives in a child component mounted only while the dialog is open,
 * so every open starts from fresh `useState` defaults — no effect-based reset.
 */
export function AddExternalApplicationModal({
  isOpen,
  onOpenChange,
}: AddExternalApplicationModalProps) {
  const { t } = useTranslation("applications");

  return (
    <Dialog.Root open={isOpen} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 flex max-h-[90vh] w-full max-w-lg -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-xl bg-bg-elevated shadow-2xl">
          <div className="flex items-center justify-between border-b border-border-subtle p-6 pb-4">
            <Dialog.Title className="text-xl font-semibold text-text-primary">
              {t("addExternalModal.title")}
            </Dialog.Title>
            <Dialog.Close
              aria-label={t("addExternalModal.close")}
              className="text-text-tertiary transition-colors hover:text-text-secondary"
            >
              <X size={20} />
            </Dialog.Close>
          </div>

          <Dialog.Description className="px-6 pt-4 text-sm text-text-secondary">
            {t("addExternalModal.description")}
          </Dialog.Description>

          {isOpen && <AddExternalApplicationForm onDone={() => onOpenChange(false)} />}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

interface AddExternalApplicationFormProps {
  /** Called to close the parent dialog after a successful submission. */
  onDone: () => void;
}

function AddExternalApplicationForm({ onDone }: AddExternalApplicationFormProps) {
  const { t } = useTranslation("applications");
  const queryClient = useQueryClient();

  // Free-text fields — no catalogue search step.
  const [title, setTitle] = useState("");
  const [provider, setProvider] = useState("");
  const [trackingUrl, setTrackingUrl] = useState("");
  const [deadline, setDeadline] = useState(""); // YYYY-MM-DD from <input type="date">
  const [notes, setNotes] = useState("");

  const createMutation = useMutation({
    mutationFn: () =>
      applicationsApi.createExternal({
        scholarshipId: null,
        title: title.trim(),
        provider: provider.trim() || null,
        externalTrackingUrl: trackingUrl.trim() || null,
        // Send a full ISO timestamp so the server's DateTimeOffset binder parses cleanly.
        deadline: deadline ? `${deadline}T00:00:00Z` : null,
        personalNotes: notes.trim() || null,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.applications.all });
      toast.success(t("addExternalModal.success"));
      onDone();
    },
    onError: (error: unknown) => {
      const message =
        error instanceof ApiError && error.payload.detail
          ? error.payload.detail
          : t("addExternalModal.error");
      toast.error(message);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) {
      toast.error(t("addExternalModal.pleaseEnterTitle"));
      return;
    }
    if (!provider.trim()) {
      toast.error(t("addExternalModal.pleaseEnterProvider"));
      return;
    }
    createMutation.mutate();
  };

  const isSubmitting = createMutation.isPending;

  return (
    <form
      onSubmit={handleSubmit}
      className="flex flex-1 flex-col overflow-y-auto p-6 pt-4"
    >
      <div className="space-y-5">
        {/* ── Scholarship name (required) ── */}
        <div className="space-y-2">
          <label
            htmlFor="external-title"
            className="block text-sm font-medium text-text-secondary"
          >
            {t("addExternalModal.titleLabel")}{" "}
            <span aria-hidden className="text-danger-500">*</span>
          </label>
          <input
            id="external-title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t("addExternalModal.titlePlaceholder")}
            required
            maxLength={300}
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </div>

        {/* ── Provider / organization (required) ── */}
        <div className="space-y-2">
          <label
            htmlFor="external-provider"
            className="block text-sm font-medium text-text-secondary"
          >
            {t("addExternalModal.providerLabel")}{" "}
            <span aria-hidden className="text-danger-500">*</span>
          </label>
          <input
            id="external-provider"
            type="text"
            value={provider}
            onChange={(e) => setProvider(e.target.value)}
            placeholder={t("addExternalModal.providerPlaceholder")}
            required
            maxLength={200}
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </div>

        {/* ── External URL (optional) ── */}
        <div className="space-y-2">
          <label
            htmlFor="external-tracking-url"
            className="block text-sm font-medium text-text-secondary"
          >
            {t("addExternalModal.trackingUrlLabel")}{" "}
            <span className="text-xs font-normal text-text-tertiary">
              ({t("addExternalModal.optional")})
            </span>
          </label>
          <input
            id="external-tracking-url"
            type="url"
            value={trackingUrl}
            onChange={(e) => setTrackingUrl(e.target.value)}
            placeholder={t("addExternalModal.trackingUrlPlaceholder")}
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </div>

        {/* ── Deadline (optional date picker) ── */}
        <div className="space-y-2">
          <label
            htmlFor="external-deadline"
            className="block text-sm font-medium text-text-secondary"
          >
            {t("addExternalModal.deadlineLabel")}{" "}
            <span className="text-xs font-normal text-text-tertiary">
              ({t("addExternalModal.optional")})
            </span>
          </label>
          <input
            id="external-deadline"
            type="date"
            value={deadline}
            onChange={(e) => setDeadline(e.target.value)}
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </div>

        {/* ── Personal notes (optional) ── */}
        <div className="space-y-2">
          <label
            htmlFor="external-notes"
            className="block text-sm font-medium text-text-secondary"
          >
            {t("addExternalModal.notesLabel")}{" "}
            <span className="text-xs font-normal text-text-tertiary">
              ({t("addExternalModal.optional")})
            </span>
          </label>
          <textarea
            id="external-notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder={t("addExternalModal.notesPlaceholder")}
            rows={3}
            maxLength={4000}
            className="w-full rounded-md border border-border-subtle bg-bg-canvas px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </div>
      </div>

      {/* ── Footer ── */}
      <div className="mt-6 flex justify-end gap-3 border-t border-border-subtle pt-4">
        <Dialog.Close asChild>
          <button
            type="button"
            className="rounded-md px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle"
          >
            {t("addExternalModal.cancel")}
          </button>
        </Dialog.Close>
        <button
          type="submit"
          disabled={isSubmitting || !title.trim() || !provider.trim()}
          className="inline-flex items-center gap-2 rounded-md bg-brand-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSubmitting && <Loader2 size={16} className="animate-spin" aria-hidden />}
          {isSubmitting
            ? t("addExternalModal.submitting")
            : t("addExternalModal.submit")}
        </button>
      </div>
    </form>
  );
}
