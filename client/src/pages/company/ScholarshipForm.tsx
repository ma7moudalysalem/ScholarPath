import { useEffect, useMemo } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import {
  scholarshipsApi,
  type CreateScholarshipInput,
  type ScholarshipCategory,
  type ScholarshipDetail,
  type UpdateScholarshipInput,
} from "@/services/api/scholarships";
import { apiErrorMessage } from "@/services/api/client";
import type { AcademicLevel, FundingType } from "@/types/domain";

// ── Form schema ──────────────────────────────────────────────────────────────
//
// The server enforces "deadline ≥ now + 7 days" (see CreateScholarshipCommand).
// We mirror it here so the user sees the rule fail in the UI before the
// network round-trip — and so the date picker `min` lines up with what the
// validator will accept.

const FUNDING_TYPES = [
  "FullyFunded",
  "PartiallyFunded",
  "TuitionOnly",
  "StipendOnly",
  "Other",
] as const satisfies readonly FundingType[];

const ACADEMIC_LEVELS = [
  "HighSchool",
  "Undergrad",
  "Masters",
  "PhD",
  "PostDoc",
  "Other",
] as const satisfies readonly AcademicLevel[];

const fieldClass =
  "w-full rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20";

function minDeadlineDate(): Date {
  // 7-day rule is exclusive on the server (`> now + 7 days`). Bump by an
  // extra day so a same-day pick of the boundary date passes that check.
  return new Date(Date.now() + 8 * 86_400_000);
}

function makeSchema(t: TFunction) {
  const required = t("errors:validate.required");
  const tooLong = t("errors:validate.tooLong");
  const deadlineMsg = t("moderation:companyScholarships.form.deadlineHint");

  // Single schema — funding type and target level are always validated, but
  // only sent to the server in create mode. In edit mode the form keeps them
  // around to display the current values; they're filtered out at submit.
  return z.object({
    titleEn: z.string().min(1, required).max(300, tooLong),
    titleAr: z.string().min(1, required).max(300, tooLong),
    descriptionEn: z.string().min(1, required),
    descriptionAr: z.string().min(1, required),
    categoryId: z.string().min(1, required),
    deadline: z
      .string()
      .min(1, required)
      .refine((s) => {
        const picked = new Date(s);
        if (Number.isNaN(picked.getTime())) return false;
        // 7-day rule: pick must be strictly after now + 7 days.
        return picked.getTime() > Date.now() + 7 * 86_400_000;
      }, deadlineMsg),
    fundingType: z.enum(FUNDING_TYPES),
    targetLevel: z.enum(ACADEMIC_LEVELS),
  });
}

type FormValues = z.infer<ReturnType<typeof makeSchema>>;

export function ScholarshipForm() {
  const params = useParams<{ id?: string }>();
  const editingId = params.id;
  const mode: "create" | "edit" = editingId ? "edit" : "create";
  const { t, i18n } = useTranslation(["moderation", "common", "errors"]);
  const isAr = i18n.language.startsWith("ar");
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const schema = useMemo(() => makeSchema(t), [t]);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      titleEn: "",
      titleAr: "",
      descriptionEn: "",
      descriptionAr: "",
      categoryId: "",
      deadline: "",
      fundingType: "FullyFunded",
      targetLevel: "Undergrad",
    },
  });

  const categoriesQuery = useQuery<ScholarshipCategory[]>({
    queryKey: ["scholarships", "categories"],
    queryFn: () => scholarshipsApi.getCategories(),
    staleTime: 5 * 60_000,
  });

  const detailQuery = useQuery<ScholarshipDetail>({
    queryKey: ["scholarships", "detail", editingId],
    queryFn: () => scholarshipsApi.getById(editingId!),
    enabled: Boolean(editingId),
  });

  // Pre-fill the form in edit mode from the detail DTO, which now ships the
  // raw bilingual title/description plus the raw categoryId — so the form
  // edits both languages and the category dropdown comes up pre-selected.
  useEffect(() => {
    if (mode !== "edit" || !detailQuery.data) return;
    const d = detailQuery.data;
    form.reset({
      titleEn: d.titleEn ?? "",
      titleAr: d.titleAr ?? "",
      descriptionEn: d.descriptionEn ?? "",
      descriptionAr: d.descriptionAr ?? "",
      categoryId: d.categoryId ?? "",
      deadline: d.deadline ? d.deadline.slice(0, 10) : "",
      fundingType: d.fundingType,
      targetLevel: d.targetLevel,
    });
  }, [mode, detailQuery.data, form]);

  const createMut = useMutation({
    mutationFn: (input: CreateScholarshipInput) =>
      scholarshipsApi.createScholarship(input),
    onSuccess: () => {
      toast.success(t("moderation:companyScholarships.form.createSuccess"));
      void queryClient.invalidateQueries({
        queryKey: ["company", "scholarships", "mine"],
      });
      navigate("/company/scholarships");
    },
    onError: (err) =>
      toast.error(
        apiErrorMessage(err, t("moderation:companyScholarships.form.error")),
      ),
  });

  const updateMut = useMutation({
    mutationFn: ({ id, input }: { id: string; input: UpdateScholarshipInput }) =>
      scholarshipsApi.updateScholarship(id, input),
    onSuccess: () => {
      toast.success(t("moderation:companyScholarships.form.updateSuccess"));
      void queryClient.invalidateQueries({
        queryKey: ["company", "scholarships", "mine"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["scholarships", "detail", editingId],
      });
      navigate("/company/scholarships");
    },
    onError: (err) =>
      toast.error(
        apiErrorMessage(err, t("moderation:companyScholarships.form.error")),
      ),
  });

  const onSubmit = form.handleSubmit((values) => {
    // The HTML date input emits YYYY-MM-DD; convert to an ISO instant so the
    // .NET DateTimeOffset binder + the "> now + 7d" rule both see a real
    // point in time (midnight UTC of the chosen day).
    const deadlineIso = new Date(`${values.deadline}T00:00:00Z`).toISOString();

    if (mode === "edit" && editingId) {
      const input: UpdateScholarshipInput = {
        titleEn: values.titleEn,
        titleAr: values.titleAr,
        descriptionEn: values.descriptionEn,
        descriptionAr: values.descriptionAr,
        categoryId: values.categoryId,
        deadline: deadlineIso,
      };
      updateMut.mutate({ id: editingId, input });
      return;
    }

    const input: CreateScholarshipInput = {
      titleEn: values.titleEn,
      titleAr: values.titleAr,
      descriptionEn: values.descriptionEn,
      descriptionAr: values.descriptionAr,
      categoryId: values.categoryId,
      deadline: deadlineIso,
      fundingType: values.fundingType,
      targetLevel: values.targetLevel,
    };
    createMut.mutate(input);
  });

  const isSubmitting = createMut.isPending || updateMut.isPending;
  const isLoadingDetail = mode === "edit" && detailQuery.isLoading;
  const minDeadline = minDeadlineDate().toISOString().slice(0, 10);
  const errors = form.formState.errors;

  return (
    <div className="mx-auto w-full max-w-2xl px-4 py-8 sm:px-6">
      <div className="mb-6 flex items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {mode === "edit"
            ? t("moderation:companyScholarships.form.titleEdit")
            : t("moderation:companyScholarships.form.titleCreate")}
        </h1>
        <Link
          to="/company/scholarships"
          className="text-sm text-text-secondary hover:text-text-primary hover:underline"
        >
          {t("moderation:companyScholarships.form.back")}
        </Link>
      </div>

      {isLoadingDetail && (
        <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 text-sm text-text-tertiary">
          {t("moderation:companyScholarships.loading")}
        </div>
      )}

      {!isLoadingDetail && (
        <form
          onSubmit={(e) => void onSubmit(e)}
          className="space-y-5 rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm sm:p-8"
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <Field
              id="titleEn"
              label={t("moderation:companyScholarships.form.titleEn")}
              error={errors.titleEn?.message}
            >
              <input
                id="titleEn"
                type="text"
                maxLength={300}
                className={fieldClass}
                {...form.register("titleEn")}
              />
            </Field>
            <Field
              id="titleAr"
              label={t("moderation:companyScholarships.form.titleAr")}
              error={errors.titleAr?.message}
            >
              <input
                id="titleAr"
                type="text"
                maxLength={300}
                className={fieldClass}
                dir="rtl"
                {...form.register("titleAr")}
              />
            </Field>
          </div>

          <Field
            id="descriptionEn"
            label={t("moderation:companyScholarships.form.descriptionEn")}
            error={errors.descriptionEn?.message}
          >
            <textarea
              id="descriptionEn"
              rows={4}
              className={fieldClass}
              {...form.register("descriptionEn")}
            />
          </Field>

          <Field
            id="descriptionAr"
            label={t("moderation:companyScholarships.form.descriptionAr")}
            error={errors.descriptionAr?.message}
          >
            <textarea
              id="descriptionAr"
              rows={4}
              className={fieldClass}
              dir="rtl"
              {...form.register("descriptionAr")}
            />
          </Field>

          <Field
            id="categoryId"
            label={t("moderation:companyScholarships.form.category")}
            error={errors.categoryId?.message}
          >
            <select
              id="categoryId"
              className={fieldClass}
              {...form.register("categoryId")}
              disabled={categoriesQuery.isLoading}
            >
              <option value="">
                {categoriesQuery.isLoading
                  ? t("moderation:companyScholarships.form.categoryLoading")
                  : t("moderation:companyScholarships.form.categoryPlaceholder")}
              </option>
              {categoriesQuery.data?.map((c) => (
                <option key={c.id} value={c.id}>
                  {isAr ? c.nameAr || c.nameEn : c.nameEn || c.nameAr}
                </option>
              ))}
            </select>
          </Field>

          <Field
            id="deadline"
            label={t("moderation:companyScholarships.form.deadline")}
            hint={t("moderation:companyScholarships.form.deadlineHint")}
            error={errors.deadline?.message}
          >
            <input
              id="deadline"
              type="date"
              dir="ltr"
              min={minDeadline}
              className={fieldClass}
              {...form.register("deadline")}
            />
          </Field>

          {mode === "create" && (
            <div className="grid gap-4 sm:grid-cols-2">
              <Field
                id="fundingType"
                label={t("moderation:companyScholarships.form.fundingType")}
                error={errors.fundingType?.message}
              >
                <select
                  id="fundingType"
                  className={fieldClass}
                  {...form.register("fundingType")}
                >
                  {FUNDING_TYPES.map((v) => (
                    <option key={v} value={v}>
                      {t(`moderation:companyScholarships.form.fundingTypeOptions.${v}`)}
                    </option>
                  ))}
                </select>
              </Field>
              <Field
                id="targetLevel"
                label={t("moderation:companyScholarships.form.targetLevel")}
                error={errors.targetLevel?.message}
              >
                <select
                  id="targetLevel"
                  className={fieldClass}
                  {...form.register("targetLevel")}
                >
                  {ACADEMIC_LEVELS.map((v) => (
                    <option key={v} value={v}>
                      {t(`moderation:companyScholarships.form.targetLevelOptions.${v}`)}
                    </option>
                  ))}
                </select>
              </Field>
            </div>
          )}

          <div className="flex flex-col-reverse gap-3 pt-2 sm:flex-row sm:justify-end">
            <Link
              to="/company/scholarships"
              className="inline-flex h-11 items-center justify-center rounded-lg border border-border-default bg-bg-subtle px-5 text-sm font-medium text-text-primary transition hover:border-border-strong hover:bg-bg-elevated"
            >
              {t("moderation:companyScholarships.form.cancel")}
            </Link>
            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex h-11 items-center justify-center gap-2 rounded-lg bg-brand-500 px-5 text-sm font-medium text-white transition hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting && (
                <Loader2 className="size-4 animate-spin" aria-hidden />
              )}
              {isSubmitting
                ? t("moderation:companyScholarships.form.submitting")
                : mode === "edit"
                  ? t("moderation:companyScholarships.form.submitEdit")
                  : t("moderation:companyScholarships.form.submitCreate")}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

function Field({
  id,
  label,
  hint,
  error,
  children,
}: {
  id: string;
  label: string;
  hint?: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="text-sm font-medium text-text-primary">
        {label}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-text-tertiary">{hint}</p>}
      {error && <p className="text-xs text-danger-500">{error}</p>}
    </div>
  );
}
