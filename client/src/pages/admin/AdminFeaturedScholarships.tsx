import { useRef, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { GripVertical, Star, Trash2 } from "lucide-react";
import {
  scholarshipsApi,
  type AdminFeaturedScholarship,
  type MyScholarship,
  type PaginatedMyScholarships,
} from "@/services/api/scholarships";
import { apiErrorMessage } from "@/services/api/client";
import { cn } from "@/lib/utils";

const MAX_FEATURED = 12;

// ── Featured list with drag-to-reorder ───────────────────────────────────────

function FeaturedList() {
  const { t, i18n } = useTranslation(["admin", "common", "moderation"]);
  const isAr = i18n.language.startsWith("ar");
  const dateLocale = isAr ? ar : undefined;
  const qc = useQueryClient();

  const { data: items = [], isLoading, isError, refetch } = useQuery<
    AdminFeaturedScholarship[]
  >({
    queryKey: ["admin", "scholarships", "featured"],
    queryFn: () => scholarshipsApi.getAdminFeatured(),
  });

  // ── Local ordering state — nullable array of IDs overrides server order ───
  // null  → display in the server-returned order (no unsaved changes)
  // array → the user has dragged items into a new order; show that instead
  const [orderedIds, setOrderedIds] = useState<string[] | null>(null);
  const isDirty = orderedIds !== null;

  // Merge: user's custom order if present, else server data.
  const ordered: AdminFeaturedScholarship[] = orderedIds
    ? orderedIds.flatMap((id) => {
        const s = items.find((i) => i.id === id);
        return s ? [s] : [];
      })
    : items;

  // ── Drag-and-drop state ───────────────────────────────────────────────────
  const dragIndex = useRef<number | null>(null);
  const dragOverIndex = useRef<number | null>(null);

  const handleDragStart = (index: number) => {
    dragIndex.current = index;
  };

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault();
    dragOverIndex.current = index;
  };

  const handleDrop = () => {
    const from = dragIndex.current;
    const to   = dragOverIndex.current;
    if (from === null || to === null || from === to) return;

    const next = [...ordered];
    const [moved] = next.splice(from, 1);
    next.splice(to, 0, moved);
    setOrderedIds(next.map((s) => s.id));

    dragIndex.current     = null;
    dragOverIndex.current = null;
  };

  // ── Mutations ─────────────────────────────────────────────────────────────
  const reorderMut = useMutation({
    mutationFn: (ids: string[]) => scholarshipsApi.reorderFeatured(ids),
    onSuccess: () => {
      toast.success(t("admin:featured.reorderSaved"));
      setOrderedIds(null);
      void qc.invalidateQueries({ queryKey: ["admin", "scholarships", "featured"] });
    },
    onError: (err: unknown) =>
      toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const unfeatureMut = useMutation({
    mutationFn: (id: string) => scholarshipsApi.setFeatured(id, false),
    onSuccess: () => {
      toast.success(t("admin:featured.unfeatureSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "scholarships", "featured"] });
    },
    onError: (err: unknown) =>
      toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const saveOrder = () => {
    reorderMut.mutate(ordered.map((s) => s.id));
  };

  // ── Render ────────────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="space-y-2">
        {[...Array(4)].map((_, i) => (
          <div
            key={i}
            className="h-12 animate-pulse rounded-lg border border-border-subtle bg-bg-subtle"
          />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
        {t("common:status.error")}{" "}
        <button
          type="button"
          onClick={() => void refetch()}
          className="underline"
        >
          {t("common:actions.retry")}
        </button>
      </div>
    );
  }

  if (ordered.length === 0) {
    return (
      <p className="rounded-lg border border-border-subtle bg-bg-subtle px-4 py-8 text-center text-sm text-text-tertiary">
        {t("admin:featured.empty")}
      </p>
    );
  }

  return (
    <div className="space-y-3">
      {/* Capacity indicator */}
      <div className="flex items-center justify-between text-xs text-text-tertiary">
        <span>
          {t("admin:featured.count", {
            count: ordered.length,
            max: MAX_FEATURED,
          })}
        </span>
        {isDirty && (
          <button
            type="button"
            onClick={saveOrder}
            disabled={reorderMut.isPending}
            className={cn(
              "rounded-md px-3 py-1 text-xs font-medium transition",
              reorderMut.isPending
                ? "bg-bg-subtle text-text-tertiary"
                : "bg-brand-500 text-text-on-brand hover:bg-brand-600",
            )}
          >
            {reorderMut.isPending
              ? t("admin:featured.saving")
              : t("admin:featured.saveOrder")}
          </button>
        )}
      </div>

      {/* Draggable list */}
      <div className="divide-y divide-border-subtle rounded-xl border border-border-subtle bg-bg-elevated">
        {ordered.map((s, i) => {
          const title = isAr ? s.titleAr || s.titleEn : s.titleEn || s.titleAr;
          return (
            <div
              key={s.id}
              draggable
              onDragStart={() => handleDragStart(i)}
              onDragOver={(e) => handleDragOver(e, i)}
              onDrop={handleDrop}
              className="flex cursor-grab items-center gap-3 px-4 py-3 hover:bg-bg-subtle/50 active:cursor-grabbing"
            >
              {/* Drag handle */}
              <GripVertical
                aria-hidden
                className="size-4 shrink-0 text-text-tertiary"
              />

              {/* Position badge */}
              <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-brand-500/10 text-xs font-semibold text-brand-600">
                {i + 1}
              </span>

              {/* Title */}
              <span className="flex-1 truncate text-sm font-medium text-text-primary">
                {title}
              </span>

              {/* Deadline */}
              <span className="hidden shrink-0 text-xs text-text-tertiary sm:block">
                {format(new Date(s.deadline), "dd MMM yyyy", {
                  locale: dateLocale,
                })}
              </span>

              {/* Status badge */}
              <span
                className={cn(
                  "hidden rounded-full px-2 py-0.5 text-xs font-medium sm:block",
                  s.status === "Open"
                    ? "bg-success-100 text-success-600"
                    : "bg-warning-50 text-warning-600",
                )}
              >
                {t(`moderation:scholarshipStatus.${s.status}`, { defaultValue: s.status })}
              </span>

              {/* Un-feature button */}
              <button
                type="button"
                aria-label={t("admin:featured.unfeature")}
                disabled={unfeatureMut.isPending}
                onClick={() => unfeatureMut.mutate(s.id)}
                className="rounded-md p-1 text-text-tertiary transition hover:text-danger-500 disabled:opacity-40"
              >
                <Trash2 aria-hidden className="size-4" />
              </button>
            </div>
          );
        })}
      </div>

      {/* Unsaved-changes banner */}
      {isDirty && (
        <p className="text-xs text-warning-600">
          {t("admin:featured.unsavedHint")}
        </p>
      )}
    </div>
  );
}

// ── "Add scholarship" panel ───────────────────────────────────────────────────

function AddScholarshipPanel() {
  const { t, i18n } = useTranslation(["admin", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  const { data: adminList } = useQuery<PaginatedMyScholarships>({
    queryKey: ["admin", "scholarships", "Open", 1],
    queryFn: () => scholarshipsApi.getForModeration("Open", 1, 50),
  });

  const { data: featured = [] } = useQuery<AdminFeaturedScholarship[]>({
    queryKey: ["admin", "scholarships", "featured"],
    queryFn: () => scholarshipsApi.getAdminFeatured(),
  });

  const featuredIds = new Set(featured.map((f) => f.id));
  const notYetFeatured: MyScholarship[] = (adminList?.items ?? []).filter(
    (s) => !featuredIds.has(s.id),
  );

  const featureMut = useMutation({
    mutationFn: (id: string) => scholarshipsApi.setFeatured(id, true),
    onSuccess: () => {
      toast.success(t("admin:featured.featureSuccess"));
      void qc.invalidateQueries({ queryKey: ["admin", "scholarships"] });
    },
    onError: (err: unknown) =>
      toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const atCap = featured.length >= MAX_FEATURED;

  if (notYetFeatured.length === 0) {
    return (
      <p className="text-sm text-text-tertiary">
        {atCap
          ? t("admin:featured.atCapHint", { max: MAX_FEATURED })
          : t("admin:featured.allFeatured")}
      </p>
    );
  }

  return (
    <div className="divide-y divide-border-subtle rounded-xl border border-border-subtle bg-bg-elevated">
      {notYetFeatured.map((s) => {
        const title = isAr ? s.titleAr || s.titleEn : s.titleEn || s.titleAr;
        return (
          <div
            key={s.id}
            className="flex items-center gap-3 px-4 py-3 hover:bg-bg-subtle/50"
          >
            <span className="flex-1 truncate text-sm text-text-primary">
              {title}
            </span>
            <button
              type="button"
              disabled={featureMut.isPending || atCap}
              onClick={() => featureMut.mutate(s.id)}
              title={
                atCap
                  ? t("admin:featured.atCapHint", { max: MAX_FEATURED })
                  : undefined
              }
              className={cn(
                "flex shrink-0 items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium transition",
                atCap || featureMut.isPending
                  ? "cursor-not-allowed text-text-tertiary opacity-50"
                  : "text-brand-500 hover:bg-brand-500/10",
              )}
            >
              <Star aria-hidden className="size-3.5" />
              {t("admin:featured.feature")}
            </button>
          </div>
        );
      })}
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function AdminFeaturedScholarships() {
  const { t } = useTranslation("admin");

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("admin:featured.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("admin:featured.subtitle", { max: MAX_FEATURED })}
        </p>
      </div>

      {/* Current featured list */}
      <section>
        <h2 className="mb-3 text-sm font-semibold text-text-primary">
          {t("admin:featured.currentSection")}
        </h2>
        <FeaturedList />
      </section>

      {/* Add more */}
      <section>
        <h2 className="mb-3 text-sm font-semibold text-text-primary">
          {t("admin:featured.addSection")}
        </h2>
        <p className="mb-3 text-xs text-text-tertiary">
          {t("admin:featured.addHint")}
        </p>
        <AddScholarshipPanel />
      </section>
    </div>
  );
}
