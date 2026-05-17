import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import * as Dialog from "@radix-ui/react-dialog";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { X, Search, Check, AlertTriangle, Loader2 } from "lucide-react";
import { applicationsApi } from "@/services/api/applications";
import { scholarshipsApi } from "@/services/api/scholarships";
import { queryKeys } from "@/lib/queryClient";
import { ApiError } from "@/services/api/client";

interface AddExternalApplicationModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * "Add External Application" — registers an application the student is
 * pursuing on an external scholarship listing's own website (a ScholarPath
 * scholarship whose listing mode is `ExternalUrl`). On success the
 * `applications` query key is invalidated so the Kanban board refreshes.
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
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 flex max-h-[90vh] w-full max-w-lg -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-xl bg-bg-elevated shadow-2xl dark:bg-slate-900">
          <div className="flex items-center justify-between border-b border-border-subtle p-6 pb-4 dark:border-slate-800">
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

  const [searchTerm, setSearchTerm] = useState("");
  const [debouncedTerm, setDebouncedTerm] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedTitle, setSelectedTitle] = useState("");
  const [trackingUrl, setTrackingUrl] = useState("");
  const [referenceId, setReferenceId] = useState("");
  const [notes, setNotes] = useState("");

  // Debounce the search box so we don't fire a request on every keystroke.
  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedTerm(searchTerm.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [searchTerm]);

  const { data: results, isFetching: isSearching } = useQuery({
    queryKey: queryKeys.scholarships.list({ externalPicker: debouncedTerm }),
    queryFn: () => scholarshipsApi.search({ query: debouncedTerm, pageSize: 8 }),
    enabled: debouncedTerm.length > 0,
    staleTime: 30_000,
  });

  // Once a scholarship is picked, fetch its detail to confirm it is an
  // external listing — the search DTO does not carry the listing mode.
  const { data: selectedDetail, isFetching: isLoadingDetail } = useQuery({
    queryKey: queryKeys.scholarships.detail(selectedId ?? ""),
    queryFn: () => scholarshipsApi.getById(selectedId ?? ""),
    enabled: !!selectedId,
  });

  const isInAppListing = !!selectedDetail && selectedDetail.mode !== "ExternalUrl";

  const createMutation = useMutation({
    mutationFn: () =>
      applicationsApi.createExternal({
        scholarshipId: selectedId!,
        externalTrackingUrl: trackingUrl.trim() || null,
        externalReferenceId: referenceId.trim() || null,
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
    if (!selectedId) {
      toast.error(t("addExternalModal.pleaseSelectScholarship"));
      return;
    }
    if (isInAppListing) {
      toast.error(t("addExternalModal.notExternalListing"));
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
        {/* ── Scholarship picker ── */}
        <div className="space-y-2">
          <label
            htmlFor="external-scholarship-search"
            className="block text-sm font-medium text-text-secondary"
          >
            {t("addExternalModal.scholarshipLabel")}
          </label>

          {selectedId ? (
            <div className="flex items-start justify-between gap-3 rounded-md border border-brand-500/40 bg-brand-500/5 p-3">
              <div className="flex items-start gap-2">
                <Check
                  size={16}
                  aria-hidden
                  className="mt-0.5 shrink-0 text-brand-600"
                />
                <span className="text-sm font-medium text-text-primary">
                  {selectedTitle}
                </span>
              </div>
              <button
                type="button"
                onClick={() => {
                  setSelectedId(null);
                  setSelectedTitle("");
                }}
                className="shrink-0 text-xs font-medium text-brand-600 hover:underline"
              >
                {t("addExternalModal.changeScholarship")}
              </button>
            </div>
          ) : (
            <>
              <div className="relative">
                <Search
                  aria-hidden
                  className="pointer-events-none absolute inset-s-3 top-1/2 size-4 -translate-y-1/2 text-text-tertiary"
                />
                <input
                  id="external-scholarship-search"
                  type="search"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder={t("addExternalModal.scholarshipPlaceholder")}
                  className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas ps-10 pe-3 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 dark:bg-slate-800"
                />
              </div>

              {debouncedTerm.length > 0 && (
                <div className="max-h-48 overflow-y-auto rounded-md border border-border-subtle dark:border-slate-800">
                  {isSearching ? (
                    <div className="flex items-center justify-center gap-2 p-4 text-sm text-text-tertiary">
                      <Loader2 size={16} className="animate-spin" aria-hidden />
                      {t("addExternalModal.searching")}
                    </div>
                  ) : results && results.items.length > 0 ? (
                    <ul className="divide-y divide-border-subtle dark:divide-slate-800">
                      {results.items.map((s) => (
                        <li key={s.id}>
                          <button
                            type="button"
                            onClick={() => {
                              setSelectedId(s.id);
                              setSelectedTitle(s.titleEn);
                            }}
                            className="block w-full px-3 py-2 text-start text-sm text-text-primary transition-colors hover:bg-bg-subtle dark:hover:bg-slate-800"
                          >
                            {s.titleEn}
                          </button>
                        </li>
                      ))}
                    </ul>
                  ) : (
                    <p className="p-4 text-sm text-text-tertiary">
                      {t("addExternalModal.noResults")}
                    </p>
                  )}
                </div>
              )}
            </>
          )}

          {/* In-app listing warning */}
          {selectedId && isLoadingDetail && (
            <p className="flex items-center gap-1.5 text-xs text-text-tertiary">
              <Loader2 size={12} className="animate-spin" aria-hidden />
              {t("addExternalModal.checkingListing")}
            </p>
          )}
          {isInAppListing && (
            <p className="flex items-start gap-1.5 rounded-md bg-amber-500/10 p-2 text-xs text-amber-600 dark:text-amber-400">
              <AlertTriangle size={14} aria-hidden className="mt-0.5 shrink-0" />
              {t("addExternalModal.notExternalListing")}
            </p>
          )}
        </div>

        {/* ── Tracking URL ── */}
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
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 dark:bg-slate-800"
          />
        </div>

        {/* ── Reference id ── */}
        <div className="space-y-2">
          <label
            htmlFor="external-reference-id"
            className="block text-sm font-medium text-text-secondary"
          >
            {t("addExternalModal.referenceIdLabel")}{" "}
            <span className="text-xs font-normal text-text-tertiary">
              ({t("addExternalModal.optional")})
            </span>
          </label>
          <input
            id="external-reference-id"
            type="text"
            value={referenceId}
            onChange={(e) => setReferenceId(e.target.value)}
            placeholder={t("addExternalModal.referenceIdPlaceholder")}
            maxLength={200}
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 dark:bg-slate-800"
          />
        </div>

        {/* ── Personal notes ── */}
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
            className="w-full rounded-md border border-border-subtle bg-bg-canvas px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 dark:bg-slate-800"
          />
        </div>
      </div>

      {/* ── Footer ── */}
      <div className="mt-6 flex justify-end gap-3 border-t border-border-subtle pt-4 dark:border-slate-800">
        <Dialog.Close asChild>
          <button
            type="button"
            className="rounded-md px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle dark:hover:bg-slate-800"
          >
            {t("addExternalModal.cancel")}
          </button>
        </Dialog.Close>
        <button
          type="submit"
          disabled={isSubmitting || !selectedId || isInAppListing}
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
