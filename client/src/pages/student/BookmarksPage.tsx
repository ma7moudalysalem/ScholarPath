import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Bookmark, Search, Trash2, BookOpen } from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  useBookmarksQuery,
  useToggleBookmarkMutation,
} from "@/hooks/useScholarshipsQuery";
import type { FundingType } from "@/types/domain";
import { SkeletonCardGrid } from "@/components/common/Skeleton";
import { apiErrorMessage } from "@/services/api/client";
import { resourcesApi, type ResourceListItem } from "@/services/api/resources";

type Tab = "scholarships" | "resources";

// ── Funding badge ─────────────────────────────────────────────────────────────

function FundingBadge({ type }: { type: FundingType }) {
  const { t } = useTranslation("scholarships");
  const colors: Record<FundingType, string> = {
    FullyFunded:     "bg-success-100 text-success-600",
    PartiallyFunded: "bg-brand-100 text-brand-600",
    TuitionOnly:     "bg-info-50 text-brand-700",
    StipendOnly:     "bg-warning-50 text-warning-600",
    Other:           "bg-bg-subtle text-text-tertiary",
  };
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        colors[type],
      )}
    >
      {t(`fundingType.${type}`)}
    </span>
  );
}

// ── Scholarship bookmarks tab ─────────────────────────────────────────────────

function ScholarshipsTab() {
  const { t, i18n } = useTranslation(["scholarships", "common"]);
  const isRtl       = i18n.dir() === "rtl";
  const dateLocale  = isRtl ? ar : undefined;

  const { data: bookmarks, isLoading, isError } = useBookmarksQuery();
  const bookmarkMut = useToggleBookmarkMutation();

  const handleRemove = (e: React.MouseEvent, scholarshipId: string) => {
    e.preventDefault();
    e.stopPropagation();
    bookmarkMut.mutate(scholarshipId, {
      onSuccess: () => toast.success(t("scholarships:bookmark.removed")),
      onError:   (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
    });
  };

  if (isLoading) {
    return <SkeletonCardGrid count={6} />;
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
        {t("common:status.error")}
      </div>
    );
  }

  if (!bookmarks || bookmarks.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-xl border border-border-subtle bg-bg-elevated py-20 text-center">
        <Bookmark aria-hidden className="mb-3 size-10 text-text-tertiary" />
        <p className="text-base font-medium text-text-primary">
          {t("scholarships:bookmarks.empty.title")}
        </p>
        <p className="mt-1 text-sm text-text-secondary">
          {t("scholarships:bookmarks.empty.body")}
        </p>
        <Link
          to="/student/scholarships"
          className="mt-4 inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600"
        >
          <Search aria-hidden className="size-4" />
          {t("scholarships:bookmarks.browse")}
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-text-tertiary">
        {t("scholarships:bookmarks.count", { count: bookmarks.length })}
      </p>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {bookmarks.map(({ id, scholarship, savedAt }) => {
          const title        = isRtl ? scholarship.titleAr        : scholarship.titleEn;
          const desc         = isRtl ? scholarship.descriptionAr  : scholarship.descriptionEn;
          const deadlineDate = new Date(scholarship.deadline);
          const daysLeft     = differenceInCalendarDays(deadlineDate, new Date());
          const isUrgent     = daysLeft <= 7 && daysLeft >= 0;
          const isClosed     = daysLeft < 0;

          return (
            <Link
              key={id}
              to={`/student/scholarships/${scholarship.id}`}
              className="group relative flex flex-col rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition hover:border-brand-500/40 hover:shadow-sm"
            >
              <button
                type="button"
                onClick={(e) => handleRemove(e, scholarship.id)}
                aria-label={t("scholarships:bookmark.remove")}
                disabled={bookmarkMut.isPending}
                className="absolute inset-e-4 top-4 text-text-tertiary transition hover:text-danger-500 disabled:opacity-50"
              >
                <Trash2 aria-hidden className="size-4" />
              </button>

              {scholarship.isFeatured && (
                <span className="mb-2 inline-flex w-fit items-center rounded-full bg-brand-500/10 px-2 py-0.5 text-xs font-medium text-brand-500">
                  {"★ "}{t("scholarships:card.featured")}
                </span>
              )}

              <h2 className="mb-1 line-clamp-2 text-sm font-semibold text-text-primary group-hover:text-brand-500">
                {title}
              </h2>

              <p className="mb-3 line-clamp-2 flex-1 text-xs text-text-secondary">
                {desc}
              </p>

              <div className="mt-auto space-y-2">
                <FundingBadge type={scholarship.fundingType} />
                <div className="flex items-center justify-between text-xs text-text-tertiary">
                  <span>{t(`scholarships:level.${scholarship.targetLevel}`)}</span>
                  <span
                    className={cn(
                      "font-medium",
                      isClosed  ? "text-danger-500"  :
                      isUrgent  ? "text-warning-500" : "text-text-tertiary",
                    )}
                  >
                    {isClosed
                      ? t("scholarships:card.closed")
                      : isUrgent
                        ? t("scholarships:card.daysLeft", { count: daysLeft })
                        : format(deadlineDate, "dd MMM yyyy", { locale: dateLocale })}
                  </span>
                </div>
                <p className="text-[11px] text-text-tertiary">
                  {t("scholarships:bookmarks.savedAt", {
                    date: format(new Date(savedAt), "dd MMM yyyy", { locale: dateLocale }),
                  })}
                </p>
              </div>
            </Link>
          );
        })}
      </div>
    </div>
  );
}

// ── Resource bookmarks tab ────────────────────────────────────────────────────

function ResourcesTab() {
  const { t, i18n } = useTranslation(["scholarships", "resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const qc = useQueryClient();

  const { data, isLoading, isError } = useQuery<ResourceListItem[]>({
    queryKey: ["resources", "bookmarks", "mine"],
    queryFn: () => resourcesApi.getMyBookmarks(),
  });

  const toggleMut = useMutation({
    mutationFn: (id: string) => resourcesApi.toggleBookmark(id),
    onSuccess: () => {
      toast.success(t("resources:detail.bookmark"));
      void qc.invalidateQueries({ queryKey: ["resources", "bookmarks", "mine"] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="skeleton h-28 rounded-xl" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
        {t("common:status.error")}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-xl border border-border-subtle bg-bg-elevated py-20 text-center">
        <BookOpen aria-hidden className="mb-3 size-10 text-text-tertiary" />
        <p className="text-base font-medium text-text-primary">
          {t("scholarships:bookmarks.emptyResources.title")}
        </p>
        <p className="mt-1 text-sm text-text-secondary">
          {t("scholarships:bookmarks.emptyResources.body")}
        </p>
        <Link
          to="/student/resources"
          className="mt-4 inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600"
        >
          <Search aria-hidden className="size-4" />
          {t("scholarships:bookmarks.browseResources")}
        </Link>
      </div>
    );
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {data.map((r) => {
        const title = isAr ? r.titleAr || r.titleEn : r.titleEn || r.titleAr;
        const desc  = isAr ? r.descriptionAr || r.descriptionEn : r.descriptionEn || r.descriptionAr;
        return (
          <Link
            key={r.id}
            to={`/student/resources/${r.slug}`}
            className="group relative flex flex-col rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition hover:border-brand-500/40 hover:shadow-sm"
          >
            {/* Unsave button */}
            <button
              type="button"
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                toggleMut.mutate(r.id);
              }}
              aria-label={t("scholarships:bookmark.remove")}
              disabled={toggleMut.isPending}
              className="absolute inset-e-4 top-4 text-text-tertiary transition hover:text-danger-500 disabled:opacity-50"
            >
              <Trash2 aria-hidden className="size-4" />
            </button>

            {/* Type badge */}
            <span className="mb-2 inline-flex w-fit items-center rounded-full bg-brand-500/10 px-2 py-0.5 text-xs font-medium text-brand-500">
              {t(`resources:resourceType.${r.type}`)}
            </span>

            <h2 className="mb-1 line-clamp-2 text-sm font-semibold text-text-primary group-hover:text-brand-500">
              {title}
            </h2>

            {desc && (
              <p className="line-clamp-2 flex-1 text-xs text-text-secondary">{desc}</p>
            )}

            {r.tags.length > 0 && (
              <p className="mt-auto pt-3 text-[11px] text-text-tertiary">
                {r.tags.slice(0, 3).join(" · ")}
              </p>
            )}
          </Link>
        );
      })}
    </div>
  );
}

// ── Page shell ────────────────────────────────────────────────────────────────

export function BookmarksPage() {
  const { t } = useTranslation(["scholarships"]);
  const [tab, setTab] = useState<Tab>("scholarships");

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
        {t("scholarships:bookmarks.title")}
      </h1>

      {/* Tab strip */}
      <div className="flex gap-1 rounded-lg border border-border-subtle bg-bg-subtle p-1 w-fit">
        {(["scholarships", "resources"] as const).map((key) => (
          <button
            key={key}
            type="button"
            onClick={() => setTab(key)}
            className={cn(
              "rounded-md px-4 py-1.5 text-sm font-medium transition",
              tab === key
                ? "bg-bg-elevated text-text-primary shadow-sm"
                : "text-text-secondary hover:text-text-primary",
            )}
          >
            {t(`scholarships:bookmarks.tabs.${key}`)}
          </button>
        ))}
      </div>

      {tab === "scholarships" ? <ScholarshipsTab /> : <ResourcesTab />}
    </div>
  );
}
