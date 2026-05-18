import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Bookmark, Search, Trash2 } from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  useBookmarksQuery,
  useToggleBookmarkMutation,
} from "@/hooks/useScholarshipsQuery";
import type { FundingType } from "@/types/domain";
import { SkeletonCardGrid } from "@/components/common/Skeleton";

// ── Helpers ───────────────────────────────────────────────────────────────────

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

// ── Page ──────────────────────────────────────────────────────────────────────

export function BookmarksPage() {
  const { t, i18n } = useTranslation(["scholarships", "common"]);
  const isRtl        = i18n.dir() === "rtl";

  const { data: bookmarks, isLoading, isError } = useBookmarksQuery();
  const bookmarkMut = useToggleBookmarkMutation();

  const handleRemove = (e: React.MouseEvent, scholarshipId: string) => {
    e.preventDefault();
    e.stopPropagation();
    bookmarkMut.mutate(scholarshipId, {
      onSuccess: () => toast.success(t("scholarships:bookmark.removed")),
      onError:   () => toast.error(t("common:status.error")),
    });
  };

  // ── Loading ────────────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="skeleton h-8 w-48" />
        <SkeletonCardGrid count={6} />
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────
  if (isError) {
    return (
      <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
        {t("common:status.error")}
      </div>
    );
  }

  // ── Empty ──────────────────────────────────────────────────────────────────
  if (!bookmarks || bookmarks.length === 0) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("scholarships:bookmarks.title")}
        </h1>
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
      </div>
    );
  }

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="space-y-6">

      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("scholarships:bookmarks.title")}
        </h1>
        <span className="text-sm text-text-tertiary">
          {t("scholarships:bookmarks.count", { count: bookmarks.length })}
        </span>
      </div>

      {/* ── Grid ── */}
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
              {/* Remove bookmark button */}
              <button
                type="button"
                onClick={(e) => handleRemove(e, scholarship.id)}
                aria-label={t("scholarships:bookmark.remove")}
                disabled={bookmarkMut.isPending}
                className="absolute inset-e-4 top-4 text-text-tertiary transition hover:text-danger-500 disabled:opacity-50"
              >
                <Trash2 aria-hidden className="size-4" />
              </button>

              {/* Featured badge */}
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
                      isClosed  ? "text-danger-500"    :
                      isUrgent  ? "text-warning-500"   : "text-text-tertiary",
                    )}
                  >
                    {isClosed
                      ? t("scholarships:card.closed")
                      : isUrgent
                        ? t("scholarships:card.daysLeft", { count: daysLeft })
                        : format(deadlineDate, "dd MMM yyyy")}
                  </span>
                </div>

                <p className="text-[11px] text-text-tertiary">
                  {t("scholarships:bookmarks.savedAt", {
                    date: format(new Date(savedAt), "dd MMM yyyy"),
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
