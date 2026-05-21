import { useMemo, useRef, useState } from "react";
import { useParams, Link, useNavigate } from "react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import {
  ArrowLeft,
  ExternalLink,
  Send,
  Save,
  FileText,
  Loader2,
  Upload,
  X,
  Trash2,
} from "lucide-react";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import {
  applicationsApi,
  type ApplicationDetail,
  type ApplicationStatus,
} from "@/services/api/applications";
import { scholarshipsApi } from "@/services/api/scholarships";
import {
  documentsApi,
  documentCategories,
  type DocumentCategory,
} from "@/services/api/documents";
import { ApiError, apiErrorMessage } from "@/services/api/client";

// ── Application-form schema ───────────────────────────────────────────────────
// A scholarship's ApplicationFormSchemaJson is { "fields": [{ key, label, type,
// required }] }; the application's FormDataJson is a flat { key: value } map.

interface FormField {
  key: string;
  label: string;
  type: string;
  required?: boolean;
}

function parseSchemaFields(json: string | null | undefined): FormField[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json) as { fields?: FormField[] };
    return Array.isArray(parsed.fields) ? parsed.fields : [];
  } catch {
    return [];
  }
}

function parseFormValues(json: string | null | undefined): Record<string, string> {
  if (!json) return {};
  try {
    const parsed = JSON.parse(json) as Record<string, unknown>;
    const out: Record<string, string> = {};
    for (const [k, v] of Object.entries(parsed)) out[k] = v == null ? "" : String(v);
    return out;
  } catch {
    return {};
  }
}

function parseStringArray(json: string | null | undefined): string[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json) as unknown;
    return Array.isArray(parsed) ? parsed.map(String) : [];
  } catch {
    return [];
  }
}

function statusBadgeClass(s: ApplicationStatus): string {
  switch (s) {
    case "Accepted":
      return "bg-success-100 text-success-600";
    case "Rejected":
    case "Withdrawn":
      return "bg-danger-50 text-danger-500";
    case "UnderReview":
    case "Shortlisted":
    case "WaitingResult":
      return "bg-brand-50 text-brand-600";
    case "Pending":
    case "Applied":
    case "Intending":
      return "bg-warning-50 text-warning-600";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

const WITHDRAWABLE_STATUSES: ApplicationStatus[] = [
  "Draft", "Pending", "UnderReview", "Shortlisted", "Intending", "Applied", "WaitingResult",
];

export function StudentApplicationDetail() {
  const { t, i18n } = useTranslation(["moderation", "common"]);
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [withdrawOpen, setWithdrawOpen] = useState(false);

  const withdrawMut = useMutation({
    mutationFn: () => applicationsApi.withdraw(id!),
    onSuccess: () => {
      toast.success(t("moderation:appDetail.withdrawSuccess"));
      setWithdrawOpen(false);
      void queryClient.invalidateQueries({ queryKey: ["application", "detail", id] });
      void queryClient.invalidateQueries({ queryKey: ["applications"] });
      void navigate("/student/applications");
    },
    onError: () => {
      toast.error(t("moderation:appDetail.withdrawError"));
      setWithdrawOpen(false);
    },
  });

  const { data, isLoading, isError } = useQuery<ApplicationDetail>({
    queryKey: ["application", "detail", id],
    queryFn: () => applicationsApi.getById(id!),
    enabled: !!id,
  });

  // The scholarship carries the application-form schema + required documents.
  const { data: scholarship } = useQuery({
    queryKey: ["scholarship", "detail", data?.scholarshipId],
    queryFn: () => scholarshipsApi.getById(data!.scholarshipId!),
    enabled: !!data?.scholarshipId,
  });

  const schemaFields = useMemo(
    () => parseSchemaFields(scholarship?.applicationFormSchemaJson),
    [scholarship?.applicationFormSchemaJson],
  );

  if (isLoading) {
    return (
      <p className="py-12 text-center text-sm text-text-tertiary">
        {t("moderation:common.loading")}
      </p>
    );
  }

  if (isError || !data) {
    return (
      <div className="space-y-4 py-12 text-center">
        <p className="text-sm text-text-tertiary">{t("moderation:appDetail.notFound")}</p>
        <Link
          to="/student/applications"
          className="inline-flex items-center gap-1 text-sm text-brand-500 underline"
        >
          <ArrowLeft className="size-4" />
          {t("moderation:appDetail.back")}
        </Link>
      </div>
    );
  }

  const isDraft = data.status === "Draft";
  const scholarshipTitle = isRtl ? data.scholarshipTitleAr : data.scholarshipTitleEn;
  const attachedDocs = parseStringArray(data.attachedDocumentsJson);

  return (
    <div className="space-y-5">
      <Link
        to="/student/applications"
        className="inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary"
      >
        <ArrowLeft className="size-4" />
        {t("moderation:appDetail.back")}
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-text-tertiary">
            {t("moderation:appDetail.scholarship")}
          </p>
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {scholarshipTitle}
          </h1>
          {data.companyName && (
            <p className="mt-1 text-sm text-text-secondary">
              {t("moderation:appDetail.company")}: {data.companyName}
            </p>
          )}
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <span
            className={`rounded-full px-3 py-1 text-sm font-medium ${statusBadgeClass(data.status)}`}
          >
            {t(`moderation:applicationStatus.${data.status}`)}
          </span>
          {WITHDRAWABLE_STATUSES.includes(data.status) && (
            <button
              type="button"
              onClick={() => setWithdrawOpen(true)}
              className="inline-flex items-center gap-1.5 rounded-lg border border-danger-200 px-3 py-1.5 text-sm font-medium text-danger-600 transition hover:border-danger-400 hover:bg-danger-50"
            >
              <Trash2 className="size-4" />
              {t("moderation:appDetail.withdraw")}
            </button>
          )}
        </div>
      </div>

      {isDraft && (
        <p className="rounded-lg border border-warning-500/30 bg-warning-50 p-4 text-sm text-warning-600">
          {t("moderation:appDetail.draftHint")}
        </p>
      )}

      {/* In-app Draft → editable form. Submitted / external → read-only below. */}
      {isDraft && data.mode === "InApp" ? (
        scholarship ? (
          <DraftApplicationForm
            application={data}
            schemaFields={schemaFields}
            requiredDocuments={scholarship.requiredDocuments ?? []}
          />
        ) : (
          <p className="py-6 text-center text-sm text-text-tertiary">
            {t("moderation:common.loading")}
          </p>
        )
      ) : null}

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <h2 className="mb-3 text-lg font-semibold text-text-primary">
          {t("moderation:appDetail.timeline")}
        </h2>
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-2">
          <Field label={t("moderation:appDetail.mode")}>
            {t(`moderation:applicationMode.${data.mode}`)}
          </Field>
          <Field label={t("moderation:appDetail.created")}>
            {format(new Date(data.createdAt), "dd MMM yyyy", { locale: dateLocale })}
          </Field>
          <Field label={t("moderation:appDetail.submitted")}>
            {data.submittedAt
              ? format(new Date(data.submittedAt), "dd MMM yyyy", { locale: dateLocale })
              : t("moderation:appDetail.notProvided")}
          </Field>
          <Field label={t("moderation:appDetail.reviewStarted")}>
            {data.reviewStartedAt
              ? format(new Date(data.reviewStartedAt), "dd MMM yyyy", { locale: dateLocale })
              : t("moderation:appDetail.notProvided")}
          </Field>
          <Field label={t("moderation:appDetail.decision")}>
            {data.decisionAt
              ? format(new Date(data.decisionAt), "dd MMM yyyy", { locale: dateLocale })
              : t("moderation:appDetail.notProvided")}
          </Field>
          <Field label={t("moderation:appDetail.deadline")}>
            {data.deadline
              ? format(new Date(data.deadline), "dd MMM yyyy", { locale: dateLocale })
              : t("moderation:appDetail.notProvided")}
          </Field>
        </dl>
      </section>

      {data.decisionReason && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-2 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.decisionReason")}
          </h2>
          <p className="whitespace-pre-wrap text-sm text-text-secondary">
            {data.decisionReason}
          </p>
        </section>
      )}

      {data.mode === "External" && (data.externalTrackingUrl || data.externalReferenceId) && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-2 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.externalTracking")}
          </h2>
          {data.externalReferenceId && (
            <p className="text-sm text-text-secondary">
              {t("moderation:appDetail.externalRef")}: {data.externalReferenceId}
            </p>
          )}
          {data.externalTrackingUrl && (
            <a
              href={data.externalTrackingUrl}
              target="_blank"
              rel="noreferrer"
              className="mt-2 inline-flex items-center gap-1 text-sm text-brand-500 underline"
            >
              <ExternalLink className="size-4" />
              {t("moderation:appDetail.openExternal")}
            </a>
          )}
        </section>
      )}

      {/* A submitted application is read-only — show the answers as a list. */}
      {!isDraft && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-3 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.formData")}
          </h2>
          <FormDataView
            formDataJson={data.formDataJson}
            schemaFields={schemaFields}
            emptyLabel={t("moderation:appDetail.noFormData")}
          />
        </section>
      )}

      {!isDraft && attachedDocs.length > 0 && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-3 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.documents")}
          </h2>
          <ul className="space-y-1.5">
            {attachedDocs.map((doc) => (
              <li key={doc} className="flex items-center gap-2 text-sm text-text-secondary">
                <FileText className="size-4 text-text-tertiary" />
                {doc}
              </li>
            ))}
          </ul>
        </section>
      )}

      {!isDraft && (
        <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
          <h2 className="mb-2 text-lg font-semibold text-text-primary">
            {t("moderation:appDetail.personalNotes")}
          </h2>
          {data.personalNotes ? (
            <p className="whitespace-pre-wrap text-sm text-text-secondary">
              {data.personalNotes}
            </p>
          ) : (
            <p className="text-sm text-text-tertiary">{t("moderation:appDetail.noNotes")}</p>
          )}
        </section>
      )}

      <ConfirmDialog
        open={withdrawOpen}
        onOpenChange={setWithdrawOpen}
        title={t("moderation:appDetail.withdrawDialogTitle")}
        description={t("moderation:appDetail.withdrawDialogBody")}
        confirmLabel={withdrawMut.isPending ? t("moderation:appDetail.withdrawing") : t("moderation:appDetail.withdraw")}
        variant="destructive"
        loading={withdrawMut.isPending}
        onConfirm={() => withdrawMut.mutate()}
      />
    </div>
  );
}

// ── Editable Draft form ───────────────────────────────────────────────────────

function DraftApplicationForm({
  application,
  schemaFields,
  requiredDocuments,
}: {
  application: ApplicationDetail;
  schemaFields: FormField[];
  requiredDocuments: string[];
}) {
  const { t } = useTranslation(["moderation", "common"]);
  const queryClient = useQueryClient();

  const [formValues, setFormValues] = useState<Record<string, string>>(() =>
    parseFormValues(application.formDataJson),
  );

  // Per-slot upload state: doc name → uploaded file name (or "" when empty).
  // On mount we first-fit any previously saved attachments into the slots so
  // re-editing a draft shows the previous files.
  const [attachedByDoc, setAttachedByDoc] = useState<Record<string, string>>(() => {
    const previous = parseStringArray(application.attachedDocumentsJson);
    const initial: Record<string, string> = {};
    requiredDocuments.forEach((docName, i) => {
      initial[docName] = previous[i] ?? "";
    });
    return initial;
  });
  const [uploadingDoc, setUploadingDoc] = useState<string | null>(null);
  const [notes, setNotes] = useState(application.personalNotes ?? "");
  const [confirmed, setConfirmed] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  // Refs for the hidden file inputs — keyed by required-doc name so the slot's
  // "Upload" button can trigger its own input.
  const fileInputRefs = useRef<Record<string, HTMLInputElement | null>>({});

  // The "Reuse from vault" disclosure keeps the original vault-checkbox UX
  // collapsed by default. The user picks a target slot and then ticks a vault
  // doc to fill it — simpler than the previous free-form multi-attach.
  const [vaultTargetSlot, setVaultTargetSlot] = useState<string>(
    requiredDocuments[0] ?? "",
  );
  const { data: vaultDocs = [] } = useQuery({
    queryKey: ["documents", "all"],
    queryFn: () => documentsApi.list(),
  });

  /** Case-insensitive lookup of the canonical DocumentCategory; falls back to "Other". */
  const categoryFor = (docName: string): DocumentCategory => {
    const lower = docName.toLowerCase();
    return (
      documentCategories.find((c) => c.toLowerCase() === lower) ?? "Other"
    );
  };

  const labelFor = (docName: string): string =>
    t("moderation:appDetail.documents.types." + docName, { defaultValue: docName });

  const clearError = (key: string) =>
    setErrors((es) => {
      if (!es[key]) return es;
      const next = { ...es };
      delete next[key];
      return next;
    });

  const buildBody = () => {
    const formData: Record<string, string | number> = {};
    for (const field of schemaFields) {
      const value = (formValues[field.key] ?? "").trim();
      formData[field.key] = field.type === "number" && value !== "" ? Number(value) : value;
    }
    const attached = Object.values(attachedByDoc).filter(Boolean);
    return {
      formDataJson:
        schemaFields.length > 0 ? JSON.stringify(formData) : application.formDataJson,
      attachedDocumentsJson: attached.length > 0 ? JSON.stringify(attached) : null,
      personalNotes: notes.trim() || null,
    };
  };

  const invalidate = () =>
    void queryClient.invalidateQueries({
      queryKey: ["application", "detail", application.id],
    });

  const saveMut = useMutation({
    mutationFn: () => applicationsApi.saveDraft(application.id, buildBody()),
    onSuccess: () => {
      toast.success(t("moderation:appDetail.form.saved"));
      invalidate();
    },
    onError: (err) =>
      toast.error(apiErrorMessage(err, t("moderation:appDetail.form.saveError"))),
  });

  const submitMut = useMutation({
    mutationFn: async () => {
      // Persist the current answers, then transition the draft to Pending.
      await applicationsApi.saveDraft(application.id, buildBody());
      await applicationsApi.submit(application.id);
    },
    onSuccess: () => {
      toast.success(t("moderation:appDetail.submittedToast"));
      invalidate();
    },
    onError: (err: unknown) => {
      const status = err instanceof ApiError ? err.status : undefined;
      toast.error(
        status === 409
          ? t("moderation:appDetail.submitConflict")
          : t("common:status.error"),
      );
    },
  });

  const handleSubmit = () => {
    // Per-field inline validation — collect everything missing so the student
    // can see all gaps at once instead of fixing them one by one.
    const next: Record<string, string> = {};
    for (const field of schemaFields) {
      if (field.required && !(formValues[field.key] ?? "").trim()) {
        next[field.key] = t("moderation:appDetail.form.fieldRequired");
      }
    }
    for (const docName of requiredDocuments) {
      if (!attachedByDoc[docName]) {
        next["documents." + docName] = t("moderation:appDetail.documents.missing");
      }
    }
    if (!confirmed) {
      next.confirm = t("moderation:appDetail.form.confirmRequired");
    }
    if (Object.keys(next).length > 0) {
      setErrors(next);
      return;
    }
    setErrors({});
    submitMut.mutate();
  };

  const pickFileForSlot = (docName: string) => {
    fileInputRefs.current[docName]?.click();
  };

  const handleFilePicked = async (
    docName: string,
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    const file = event.target.files?.[0];
    // Always reset the input so picking the same file twice still fires onChange.
    event.target.value = "";
    if (!file) return;
    setUploadingDoc(docName);
    try {
      const uploaded = await documentsApi.upload({
        file,
        category: categoryFor(docName),
        applicationTrackerId: application.id,
      });
      setAttachedByDoc((prev) => ({ ...prev, [docName]: uploaded.fileName }));
      clearError("documents." + docName);
      void queryClient.invalidateQueries({ queryKey: ["documents", "all"] });
    } catch (err) {
      toast.error(
        apiErrorMessage(err, t("moderation:appDetail.documents.uploadError")),
      );
    } finally {
      setUploadingDoc(null);
    }
  };

  const removeFromSlot = (docName: string) => {
    setAttachedByDoc((prev) => ({ ...prev, [docName]: "" }));
  };

  const fillSlotFromVault = (docName: string, fileName: string, checked: boolean) => {
    setAttachedByDoc((prev) => ({ ...prev, [docName]: checked ? fileName : "" }));
    if (checked) clearError("documents." + docName);
  };

  const busy = saveMut.isPending || submitMut.isPending;

  return (
    <section className="space-y-5 rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <div>
        <h2 className="text-lg font-semibold text-text-primary">
          {t("moderation:appDetail.form.title")}
        </h2>
        <p className="mt-0.5 text-sm text-text-secondary">
          {t("moderation:appDetail.form.intro")}
        </p>
      </div>

      {schemaFields.length > 0 && (
        <div className="space-y-4">
          {schemaFields.map((field) => (
            <div key={field.key}>
              <label
                htmlFor={`field-${field.key}`}
                className="mb-1 block text-sm font-medium text-text-primary"
              >
                {field.label}
                {field.required && (
                  <span className="ms-1 text-xs font-normal text-danger-500">
                    ({t("moderation:appDetail.form.required")})
                  </span>
                )}
              </label>
              {field.type === "textarea" ? (
                <textarea
                  id={`field-${field.key}`}
                  rows={4}
                  value={formValues[field.key] ?? ""}
                  onChange={(e) => {
                    setFormValues((v) => ({ ...v, [field.key]: e.target.value }));
                    clearError(field.key);
                  }}
                  className="w-full rounded-md border border-border-subtle bg-bg-canvas p-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                />
              ) : (
                <input
                  id={`field-${field.key}`}
                  type={field.type === "number" ? "number" : "text"}
                  value={formValues[field.key] ?? ""}
                  onChange={(e) => {
                    setFormValues((v) => ({ ...v, [field.key]: e.target.value }));
                    clearError(field.key);
                  }}
                  className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                />
              )}
              {errors[field.key] && (
                <p className="mt-1 text-xs text-danger-500">{errors[field.key]}</p>
              )}
            </div>
          ))}
        </div>
      )}

      <div>
        <h3 className="text-sm font-semibold text-text-primary">
          {t("moderation:appDetail.form.documents")}
        </h3>

        {requiredDocuments.length === 0 ? (
          <p className="mt-1 text-xs text-text-tertiary">
            {t("moderation:appDetail.form.requiredDocsHint", { docs: "—" })}
          </p>
        ) : (
          <div className="mt-2 space-y-2">
            {requiredDocuments.map((docName) => {
              const attachedName = attachedByDoc[docName];
              const isUploading = uploadingDoc === docName;
              const errorKey = "documents." + docName;
              return (
                <div
                  key={docName}
                  className="rounded-md border border-border-subtle bg-bg-canvas p-3"
                >
                  <div className="flex flex-wrap items-center gap-3">
                    <FileText className="size-4 text-text-tertiary" />
                    <span className="flex-1 text-sm font-medium text-text-primary">
                      {labelFor(docName)}
                    </span>
                    <input
                      ref={(el) => {
                        fileInputRefs.current[docName] = el;
                      }}
                      type="file"
                      className="hidden"
                      onChange={(e) => handleFilePicked(docName, e)}
                    />
                    {attachedName ? (
                      <span className="flex items-center gap-2 text-sm text-text-secondary">
                        <span className="truncate max-w-[16rem]">{attachedName}</span>
                        <button
                          type="button"
                          onClick={() => removeFromSlot(docName)}
                          disabled={busy || isUploading}
                          className="inline-flex items-center gap-1 rounded-md border border-border-default px-2 py-1 text-xs font-medium text-text-primary transition hover:bg-bg-subtle disabled:opacity-50"
                        >
                          <X className="size-3.5" />
                          {t("moderation:appDetail.documents.remove")}
                        </button>
                      </span>
                    ) : (
                      <button
                        type="button"
                        onClick={() => pickFileForSlot(docName)}
                        disabled={busy || isUploading}
                        className="inline-flex items-center gap-2 rounded-md border border-border-default px-3 py-1.5 text-xs font-medium text-text-primary transition hover:bg-bg-subtle disabled:opacity-50"
                      >
                        {isUploading ? (
                          <>
                            <Loader2 className="size-3.5 animate-spin" />
                            {t("moderation:appDetail.documents.uploading")}
                          </>
                        ) : (
                          <>
                            <Upload className="size-3.5" />
                            {t("moderation:appDetail.documents.uploadCta")}
                          </>
                        )}
                      </button>
                    )}
                  </div>
                  {errors[errorKey] && (
                    <p className="mt-1 text-xs text-danger-500">{errors[errorKey]}</p>
                  )}
                </div>
              );
            })}

            {vaultDocs.length > 0 && (
              <details className="mt-2 rounded-md border border-border-subtle bg-bg-subtle p-3 text-sm text-text-secondary">
                <summary className="cursor-pointer text-text-primary">
                  {t("moderation:appDetail.documents.reuseFromVault")}
                </summary>
                <div className="mt-3 space-y-2">
                  {/* Pick a target slot, then tick a vault doc to fill it. */}
                  <label className="flex flex-wrap items-center gap-2 text-xs text-text-tertiary">
                    <span>{t("moderation:appDetail.form.attachLabel")}</span>
                    <select
                      value={vaultTargetSlot}
                      onChange={(e) => setVaultTargetSlot(e.target.value)}
                      className="h-8 rounded-md border border-border-subtle bg-bg-canvas px-2 text-xs text-text-primary focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                    >
                      {requiredDocuments.map((docName) => (
                        <option key={docName} value={docName}>
                          {labelFor(docName)}
                        </option>
                      ))}
                    </select>
                  </label>
                  <div className="space-y-1.5">
                    {vaultDocs.map((doc) => {
                      const isFilling = attachedByDoc[vaultTargetSlot] === doc.fileName;
                      return (
                        <label
                          key={doc.id}
                          className="flex items-center gap-2 text-sm text-text-secondary"
                        >
                          <input
                            type="checkbox"
                            checked={isFilling}
                            disabled={!vaultTargetSlot}
                            onChange={(e) =>
                              fillSlotFromVault(
                                vaultTargetSlot,
                                doc.fileName,
                                e.target.checked,
                              )
                            }
                            className="size-4 accent-brand-500"
                          />
                          <FileText className="size-4 text-text-tertiary" />
                          <span className="truncate">{doc.fileName}</span>
                        </label>
                      );
                    })}
                  </div>
                </div>
              </details>
            )}
          </div>
        )}
      </div>

      <div>
        <label
          htmlFor="application-notes"
          className="mb-1 block text-sm font-medium text-text-primary"
        >
          {t("moderation:appDetail.personalNotes")}
        </label>
        <textarea
          id="application-notes"
          rows={3}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder={t("moderation:appDetail.form.notesPlaceholder")}
          className="w-full rounded-md border border-border-subtle bg-bg-canvas p-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        />
      </div>

      <div className="space-y-1 border-t border-border-subtle pt-4">
        <label className="flex items-start gap-2 text-sm">
          <input
            type="checkbox"
            checked={confirmed}
            onChange={(e) => {
              setConfirmed(e.target.checked);
              clearError("confirm");
            }}
            className="mt-0.5 size-4 accent-brand-500"
          />
          <span>{t("moderation:appDetail.confirmLabel")}</span>
        </label>
        {errors.confirm && (
          <p className="text-xs text-danger-500">{errors.confirm}</p>
        )}
      </div>

      <div className="flex flex-wrap gap-3">
        <button
          type="button"
          onClick={() => saveMut.mutate()}
          disabled={busy}
          className="inline-flex items-center gap-2 rounded-lg border border-border-default px-4 py-2 text-sm font-medium text-text-primary transition hover:bg-bg-subtle disabled:opacity-50"
        >
          <Save className="size-4" />
          {saveMut.isPending
            ? t("moderation:appDetail.form.saving")
            : t("moderation:appDetail.form.save")}
        </button>
        <button
          type="button"
          onClick={handleSubmit}
          disabled={busy}
          className="inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
        >
          <Send className="size-4" />
          {submitMut.isPending
            ? t("moderation:appDetail.submitting")
            : t("moderation:appDetail.submit")}
        </button>
      </div>
    </section>
  );
}

// ── Read-only form-answer list ────────────────────────────────────────────────

function FormDataView({
  formDataJson,
  schemaFields,
  emptyLabel,
}: {
  formDataJson: string | null;
  schemaFields: FormField[];
  emptyLabel: string;
}) {
  const values = parseFormValues(formDataJson);
  const keys = Object.keys(values);

  if (keys.length === 0) {
    return <p className="text-sm text-text-tertiary">{emptyLabel}</p>;
  }

  return (
    <dl className="space-y-3">
      {keys.map((key) => (
        <div key={key}>
          <dt className="text-xs uppercase tracking-wide text-text-tertiary">
            {schemaFields.find((f) => f.key === key)?.label ?? key}
          </dt>
          <dd className="mt-0.5 whitespace-pre-wrap text-sm text-text-primary">
            {values[key] || "—"}
          </dd>
        </div>
      ))}
    </dl>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs uppercase tracking-wide text-text-tertiary">{label}</dt>
      <dd className="mt-0.5 text-sm text-text-primary">{children}</dd>
    </div>
  );
}
