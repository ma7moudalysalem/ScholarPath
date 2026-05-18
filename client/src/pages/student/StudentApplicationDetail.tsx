import { useMemo, useState } from "react";
import { useParams, Link } from "react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { ArrowLeft, ExternalLink, Send, Save, FileText, FolderOpen } from "lucide-react";
import {
  applicationsApi,
  type ApplicationDetail,
  type ApplicationStatus,
} from "@/services/api/applications";
import { scholarshipsApi } from "@/services/api/scholarships";
import { documentsApi } from "@/services/api/documents";
import { ApiError } from "@/services/api/client";

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

export function StudentApplicationDetail() {
  const { t, i18n } = useTranslation(["moderation", "common"]);
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;
  const { id } = useParams<{ id: string }>();

  const { data, isLoading, isError } = useQuery<ApplicationDetail>({
    queryKey: ["application", "detail", id],
    queryFn: () => applicationsApi.getById(id!),
    enabled: !!id,
  });

  // The scholarship carries the application-form schema + required documents.
  const { data: scholarship } = useQuery({
    queryKey: ["scholarship", "detail", data?.scholarshipId],
    queryFn: () => scholarshipsApi.getById(data!.scholarshipId),
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
        <span
          className={`rounded-full px-3 py-1 text-sm font-medium ${statusBadgeClass(data.status)}`}
        >
          {t(`moderation:applicationStatus.${data.status}`)}
        </span>
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
  const [attached, setAttached] = useState<string[]>(() =>
    parseStringArray(application.attachedDocumentsJson),
  );
  const [notes, setNotes] = useState(application.personalNotes ?? "");

  const { data: documents = [] } = useQuery({
    queryKey: ["documents", "all"],
    queryFn: () => documentsApi.list(),
  });

  const buildBody = () => {
    const formData: Record<string, string | number> = {};
    for (const field of schemaFields) {
      const value = (formValues[field.key] ?? "").trim();
      formData[field.key] = field.type === "number" && value !== "" ? Number(value) : value;
    }
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
    onError: () => toast.error(t("moderation:appDetail.form.saveError")),
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
    // Mirror the server completeness guard so the student gets a clear,
    // localised message instead of a raw 409.
    const missingField = schemaFields.some(
      (field) => field.required && !(formValues[field.key] ?? "").trim(),
    );
    const missingDocs = requiredDocuments.length > 0 && attached.length === 0;
    if (missingField || missingDocs) {
      toast.error(t("moderation:appDetail.form.incomplete"));
      return;
    }
    submitMut.mutate();
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
                  onChange={(e) =>
                    setFormValues((v) => ({ ...v, [field.key]: e.target.value }))
                  }
                  className="w-full rounded-md border border-border-subtle bg-bg-canvas p-2.5 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                />
              ) : (
                <input
                  id={`field-${field.key}`}
                  type={field.type === "number" ? "number" : "text"}
                  value={formValues[field.key] ?? ""}
                  onChange={(e) =>
                    setFormValues((v) => ({ ...v, [field.key]: e.target.value }))
                  }
                  className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                />
              )}
            </div>
          ))}
        </div>
      )}

      <div>
        <h3 className="text-sm font-semibold text-text-primary">
          {t("moderation:appDetail.form.documents")}
        </h3>
        {requiredDocuments.length > 0 && (
          <p className="mt-0.5 text-xs text-text-tertiary">
            {t("moderation:appDetail.form.requiredDocsHint", {
              docs: requiredDocuments.join(", "),
            })}
          </p>
        )}
        {documents.length === 0 ? (
          <div className="mt-2 rounded-md border border-border-subtle bg-bg-subtle p-3 text-sm text-text-secondary">
            <p>{t("moderation:appDetail.form.noVaultDocs")}</p>
            <Link
              to="/student/documents"
              className="mt-1 inline-flex items-center gap-1 text-brand-500 underline"
            >
              <FolderOpen className="size-4" />
              {t("moderation:appDetail.form.goToDocuments")}
            </Link>
          </div>
        ) : (
          <div className="mt-2 space-y-1.5">
            <p className="text-xs text-text-tertiary">
              {t("moderation:appDetail.form.attachLabel")}
            </p>
            {documents.map((doc) => (
              <label
                key={doc.id}
                className="flex items-center gap-2 text-sm text-text-secondary"
              >
                <input
                  type="checkbox"
                  checked={attached.includes(doc.fileName)}
                  onChange={(e) =>
                    setAttached((list) =>
                      e.target.checked
                        ? [...list, doc.fileName]
                        : list.filter((name) => name !== doc.fileName),
                    )
                  }
                  className="size-4 accent-brand-500"
                />
                <FileText className="size-4 text-text-tertiary" />
                <span>{doc.fileName}</span>
              </label>
            ))}
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

      <div className="flex flex-wrap gap-3 border-t border-border-subtle pt-4">
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
