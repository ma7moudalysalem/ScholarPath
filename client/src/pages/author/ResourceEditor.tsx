import { useEffect } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useForm, useFieldArray, Controller } from "react-hook-form";
import type { Resolver } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { ArrowLeft, ArrowRight, Plus, Trash2, Loader2 } from "lucide-react";
import {
  resourcesApi,
  type ResourceListItem,
  type ResourceType,
} from "@/services/api/resources";
import { apiErrorMessage } from "@/services/api/client";
import { cn } from "@/lib/utils";

// ── Constants ────────────────────────────────────────────────────────────────

const RESOURCE_TYPES: ResourceType[] = ["Article", "Guide", "Checklist", "VideoLink"];

const fieldClass =
  "w-full rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20";

const labelClass = "mb-1 block text-sm font-medium text-text-secondary";
const errorClass = "mt-1 text-xs text-danger-500";

// ── Schema ───────────────────────────────────────────────────────────────────

function makeSchema(t: TFunction) {
  const required = t("errors:validate.required");
  const tooLong  = t("errors:validate.tooLong");

  const chapterSchema = z.object({
    titleEn: z.string().min(1, required).max(300, tooLong),
    titleAr: z.string().min(1, required).max(300, tooLong),
    contentMarkdownEn: z.string().max(20000),
    contentMarkdownAr: z.string().max(20000),
    estimatedReadMinutes: z.coerce.number().int().min(0).max(600),
  });

  return z.object({
    titleEn:           z.string().min(1, required).max(300, tooLong),
    titleAr:           z.string().min(1, required).max(300, tooLong),
    type:              z.enum(["Article", "Guide", "Checklist", "VideoLink"] as const),
    descriptionEn:     z.string().max(2000, tooLong),
    descriptionAr:     z.string().max(2000, tooLong),
    contentMarkdownEn: z.string().max(50000),
    contentMarkdownAr: z.string().max(50000),
    // Allow an empty string (no link) or a valid http(s) URL
    externalLinkUrl:   z.union([z.string().url().max(2048), z.literal("")]),
    tagsRaw:           z.string().max(500),
    chapters:          z.array(chapterSchema),
  });
}

/** Explicit form-values type (avoids z.infer resolution quirks with zodResolver). */
type FormValues = {
  titleEn: string;
  titleAr: string;
  type: ResourceType;
  descriptionEn: string;
  descriptionAr: string;
  contentMarkdownEn: string;
  contentMarkdownAr: string;
  externalLinkUrl: string;
  tagsRaw: string;
  chapters: Array<{
    titleEn: string;
    titleAr: string;
    contentMarkdownEn: string;
    contentMarkdownAr: string;
    estimatedReadMinutes: number;
  }>;
};

// ── Helper ───────────────────────────────────────────────────────────────────

function parseTags(raw: string): string[] {
  return raw
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean)
    .slice(0, 10);
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function ResourceEditor() {
  const { id } = useParams<{ id?: string }>();
  const isEdit = !!id;
  const navigate = useNavigate();
  const { t, i18n } = useTranslation(["resources", "common", "errors"]);
  const isRtl = i18n.dir() === "rtl";
  const BackIcon = isRtl ? ArrowRight : ArrowLeft;
  const qc = useQueryClient();

  const schema = makeSchema(t as TFunction);

  const {
    register,
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema) as Resolver<FormValues>,
    defaultValues: {
      titleEn: "",
      titleAr: "",
      type: "Article",
      descriptionEn: "",
      descriptionAr: "",
      contentMarkdownEn: "",
      contentMarkdownAr: "",
      externalLinkUrl: "",
      tagsRaw: "",
      chapters: [],
    },
  });

  const { fields: chapters, append, remove } = useFieldArray({ control, name: "chapters" });
  const watchedType = watch("type");

  // ── Load existing resource when editing ──────────────────────────────────

  const { data: existing } = useQuery<ResourceListItem>({
    queryKey: ["resources", "mine", id],
    queryFn: async () => {
      const mine = await resourcesApi.getMine();
      const found = mine.find((r) => r.id === id);
      if (!found) throw new Error("Not found");
      return found;
    },
    enabled: isEdit,
  });

  // Load full detail for chapters/content when editing
  const { data: detail } = useQuery({
    queryKey: ["resources", "detail", id],
    queryFn: () => resourcesApi.getDetail(id!),
    enabled: isEdit,
  });

  useEffect(() => {
    if (!detail) return;
    reset({
      titleEn:           detail.titleEn ?? "",
      titleAr:           detail.titleAr ?? "",
      type:              detail.type,
      descriptionEn:     detail.descriptionEn ?? "",
      descriptionAr:     detail.descriptionAr ?? "",
      contentMarkdownEn: detail.contentMarkdownEn ?? "",
      contentMarkdownAr: detail.contentMarkdownAr ?? "",
      externalLinkUrl:   detail.externalLinkUrl ?? "",
      tagsRaw:           detail.tags.join(", "),
      chapters:          detail.chapters.map((ch) => ({
        titleEn:               ch.titleEn,
        titleAr:               ch.titleAr,
        contentMarkdownEn:     ch.contentMarkdownEn ?? "",
        contentMarkdownAr:     ch.contentMarkdownAr ?? "",
        estimatedReadMinutes:  ch.estimatedReadMinutes,
      })),
    });
  }, [detail, reset]);

  // ── Mutations ─────────────────────────────────────────────────────────────

  const createMut = useMutation({
    mutationFn: (vals: FormValues) =>
      resourcesApi.create({
        titleEn:           vals.titleEn,
        titleAr:           vals.titleAr,
        type:              vals.type,
        descriptionEn:     vals.descriptionEn || undefined,
        descriptionAr:     vals.descriptionAr || undefined,
        contentMarkdownEn: vals.contentMarkdownEn || undefined,
        contentMarkdownAr: vals.contentMarkdownAr || undefined,
        externalLinkUrl:   vals.externalLinkUrl || undefined,
        tags:              parseTags(vals.tagsRaw ?? ""),
        chapters:          (vals.chapters ?? []).map((ch, i) => ({
          titleEn:               ch.titleEn,
          titleAr:               ch.titleAr,
          contentMarkdownEn:     ch.contentMarkdownEn || undefined,
          contentMarkdownAr:     ch.contentMarkdownAr || undefined,
          sortOrder:             i,
          estimatedReadMinutes:  ch.estimatedReadMinutes,
        })),
      }),
    onSuccess: (newId) => {
      toast.success(t("resources:author.createSuccess"));
      void qc.invalidateQueries({ queryKey: ["resources", "mine"] });
      navigate(`/author/resources/${newId}/edit`);
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const updateMut = useMutation({
    mutationFn: (vals: FormValues) =>
      resourcesApi.update(id!, {
        titleEn:           vals.titleEn,
        titleAr:           vals.titleAr,
        type:              vals.type,
        descriptionEn:     vals.descriptionEn || undefined,
        descriptionAr:     vals.descriptionAr || undefined,
        contentMarkdownEn: vals.contentMarkdownEn || undefined,
        contentMarkdownAr: vals.contentMarkdownAr || undefined,
        externalLinkUrl:   vals.externalLinkUrl || undefined,
        tags:              parseTags(vals.tagsRaw ?? ""),
        chapters:          (vals.chapters ?? []).map((ch, i) => ({
          titleEn:               ch.titleEn,
          titleAr:               ch.titleAr,
          contentMarkdownEn:     ch.contentMarkdownEn || undefined,
          contentMarkdownAr:     ch.contentMarkdownAr || undefined,
          sortOrder:             i,
          estimatedReadMinutes:  ch.estimatedReadMinutes,
        })),
      }),
    onSuccess: () => {
      toast.success(t("resources:author.updateSuccess"));
      void qc.invalidateQueries({ queryKey: ["resources", "mine"] });
      void qc.invalidateQueries({ queryKey: ["resources", "detail", id] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const onSubmit = (vals: FormValues) => {
    if (isEdit) updateMut.mutate(vals);
    else createMut.mutate(vals);
  };

  const busy = isSubmitting || createMut.isPending || updateMut.isPending;
  const isDraft = !existing || existing.status === "Draft";

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {/* ── Back ── */}
      <Link
        to="/author/resources"
        className="inline-flex items-center gap-1.5 text-sm text-text-secondary hover:text-text-primary"
      >
        <BackIcon aria-hidden className="size-4" />
        {t("resources:author.backToMine")}
      </Link>

      <h1 className="text-xl font-semibold text-text-primary">
        {isEdit ? t("resources:author.editTitle") : t("resources:author.newTitle")}
      </h1>

      {/* ── Locked banner for non-draft ── */}
      {isEdit && !isDraft && (
        <div className="rounded-lg border border-warning-200 bg-warning-50 px-4 py-3 text-sm text-warning-700">
          {t("resources:author.readOnlyWarning")}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
        {/* ── Type ── */}
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs">
          <label className={labelClass} htmlFor="type">
            {t("resources:author.fields.type")}
          </label>
          <Controller
            control={control}
            name="type"
            render={({ field }) => (
              <select
                {...field}
                id="type"
                disabled={!isDraft}
                className={cn(fieldClass, "bg-bg-canvas")}
              >
                {RESOURCE_TYPES.map((rt) => (
                  <option key={rt} value={rt}>
                    {t(`resources:resourceType.${rt}`)}
                  </option>
                ))}
              </select>
            )}
          />
        </div>

        {/* ── Titles ── */}
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs space-y-4">
          <h2 className="text-sm font-semibold text-text-primary">
            {t("resources:author.section.titles")}
          </h2>
          <div>
            <label className={labelClass} htmlFor="titleEn">
              {t("resources:author.fields.titleEn")}
            </label>
            <input
              id="titleEn"
              {...register("titleEn")}
              disabled={!isDraft}
              className={fieldClass}
              dir="ltr"
            />
            {errors.titleEn && <p className={errorClass}>{errors.titleEn.message}</p>}
          </div>
          <div>
            <label className={labelClass} htmlFor="titleAr">
              {t("resources:author.fields.titleAr")}
            </label>
            <input
              id="titleAr"
              {...register("titleAr")}
              disabled={!isDraft}
              className={fieldClass}
              dir="rtl"
            />
            {errors.titleAr && <p className={errorClass}>{errors.titleAr.message}</p>}
          </div>
        </div>

        {/* ── Descriptions ── */}
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs space-y-4">
          <h2 className="text-sm font-semibold text-text-primary">
            {t("resources:author.section.descriptions")}
          </h2>
          <div>
            <label className={labelClass} htmlFor="descriptionEn">
              {t("resources:author.fields.descriptionEn")}
            </label>
            <textarea
              id="descriptionEn"
              {...register("descriptionEn")}
              disabled={!isDraft}
              rows={3}
              className={cn(fieldClass, "resize-y")}
              dir="ltr"
            />
          </div>
          <div>
            <label className={labelClass} htmlFor="descriptionAr">
              {t("resources:author.fields.descriptionAr")}
            </label>
            <textarea
              id="descriptionAr"
              {...register("descriptionAr")}
              disabled={!isDraft}
              rows={3}
              className={cn(fieldClass, "resize-y")}
              dir="rtl"
            />
          </div>
        </div>

        {/* ── Content / External link ── */}
        {watchedType === "VideoLink" ? (
          <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs">
            <label className={labelClass} htmlFor="externalLinkUrl">
              {t("resources:author.fields.externalLinkUrl")}
            </label>
            <input
              id="externalLinkUrl"
              type="url"
              {...register("externalLinkUrl")}
              disabled={!isDraft}
              placeholder="https://"
              className={fieldClass}
              dir="ltr"
            />
            {errors.externalLinkUrl && (
              <p className={errorClass}>{errors.externalLinkUrl.message}</p>
            )}
          </div>
        ) : (
          <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs space-y-4">
            <h2 className="text-sm font-semibold text-text-primary">
              {t("resources:author.section.content")}
              <span className="ms-2 text-xs font-normal text-text-tertiary">
                {t("resources:author.markdownHint")}
              </span>
            </h2>
            <div>
              <label className={labelClass} htmlFor="contentMarkdownEn">
                {t("resources:author.fields.contentEn")}
              </label>
              <textarea
                id="contentMarkdownEn"
                {...register("contentMarkdownEn")}
                disabled={!isDraft}
                rows={10}
                className={cn(fieldClass, "resize-y font-mono text-xs leading-relaxed")}
                dir="ltr"
                placeholder="# Introduction&#10;&#10;Write your content in **Markdown**…"
              />
            </div>
            <div>
              <label className={labelClass} htmlFor="contentMarkdownAr">
                {t("resources:author.fields.contentAr")}
              </label>
              <textarea
                id="contentMarkdownAr"
                {...register("contentMarkdownAr")}
                disabled={!isDraft}
                rows={10}
                className={cn(fieldClass, "resize-y font-mono text-xs leading-relaxed")}
                dir="rtl"
                placeholder="# مقدمة&#10;&#10;اكتب محتواك بصيغة **Markdown**…"
              />
            </div>
          </div>
        )}

        {/* ── Chapters (Guide only) ── */}
        {(watchedType === "Guide" || watchedType === "Checklist") && (
          <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-semibold text-text-primary">
                {t("resources:author.section.chapters")}
              </h2>
              {isDraft && (
                <button
                  type="button"
                  onClick={() =>
                    append({
                      titleEn: "",
                      titleAr: "",
                      contentMarkdownEn: "",
                      contentMarkdownAr: "",
                      estimatedReadMinutes: 5,
                    })
                  }
                  className="inline-flex items-center gap-1.5 rounded-md border border-border-subtle px-2.5 py-1.5 text-xs font-medium text-text-secondary hover:text-text-primary"
                >
                  <Plus aria-hidden className="size-3.5" />
                  {t("resources:author.addChapter")}
                </button>
              )}
            </div>

            {chapters.length === 0 && (
              <p className="text-sm text-text-tertiary">
                {t("resources:author.noChapters")}
              </p>
            )}

            {chapters.map((ch, idx) => (
              <div
                key={ch.id}
                className="space-y-3 rounded-lg border border-border-subtle bg-bg-subtle p-4"
              >
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-text-secondary">
                    {t("resources:author.chapterN", { n: idx + 1 })}
                  </span>
                  {isDraft && (
                    <button
                      type="button"
                      onClick={() => remove(idx)}
                      className="text-text-tertiary hover:text-danger-500"
                      aria-label={t("resources:author.removeChapter")}
                    >
                      <Trash2 aria-hidden className="size-4" />
                    </button>
                  )}
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div>
                    <label className={labelClass}>
                      {t("resources:author.fields.titleEn")}
                    </label>
                    <input
                      {...register(`chapters.${idx}.titleEn`)}
                      disabled={!isDraft}
                      className={fieldClass}
                      dir="ltr"
                    />
                    {errors.chapters?.[idx]?.titleEn && (
                      <p className={errorClass}>
                        {errors.chapters[idx].titleEn?.message}
                      </p>
                    )}
                  </div>
                  <div>
                    <label className={labelClass}>
                      {t("resources:author.fields.titleAr")}
                    </label>
                    <input
                      {...register(`chapters.${idx}.titleAr`)}
                      disabled={!isDraft}
                      className={fieldClass}
                      dir="rtl"
                    />
                  </div>
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  <div>
                    <label className={labelClass}>
                      {t("resources:author.fields.contentEn")}
                    </label>
                    <textarea
                      {...register(`chapters.${idx}.contentMarkdownEn`)}
                      disabled={!isDraft}
                      rows={4}
                      className={cn(fieldClass, "resize-y font-mono text-xs")}
                      dir="ltr"
                    />
                  </div>
                  <div>
                    <label className={labelClass}>
                      {t("resources:author.fields.contentAr")}
                    </label>
                    <textarea
                      {...register(`chapters.${idx}.contentMarkdownAr`)}
                      disabled={!isDraft}
                      rows={4}
                      className={cn(fieldClass, "resize-y font-mono text-xs")}
                      dir="rtl"
                    />
                  </div>
                </div>
                <div className="w-36">
                  <label className={labelClass}>
                    {t("resources:author.fields.readMinutes")}
                  </label>
                  <input
                    type="number"
                    min={0}
                    max={600}
                    {...register(`chapters.${idx}.estimatedReadMinutes`)}
                    disabled={!isDraft}
                    className={fieldClass}
                  />
                </div>
              </div>
            ))}
          </div>
        )}

        {/* ── Tags ── */}
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs">
          <label className={labelClass} htmlFor="tagsRaw">
            {t("resources:author.fields.tags")}
          </label>
          <input
            id="tagsRaw"
            {...register("tagsRaw")}
            disabled={!isDraft}
            placeholder={t("resources:author.tagsPlaceholder")}
            className={fieldClass}
          />
          <p className="mt-1 text-xs text-text-tertiary">
            {t("resources:author.tagsHint")}
          </p>
        </div>

        {/* ── Actions ── */}
        {isDraft && (
          <div className="flex items-center justify-end gap-3">
            <Link
              to="/author/resources"
              className="rounded-lg border border-border-subtle px-4 py-2 text-sm font-medium text-text-secondary transition hover:bg-bg-subtle"
            >
              {t("common:cta.cancel")}
            </Link>
            <button
              type="submit"
              disabled={busy}
              className="inline-flex items-center gap-2 rounded-lg bg-brand-500 px-5 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-60"
            >
              {busy && <Loader2 aria-hidden className="size-4 animate-spin" />}
              {isEdit ? t("resources:author.saveChanges") : t("resources:author.saveDraft")}
            </button>
          </div>
        )}
      </form>
    </div>
  );
}
