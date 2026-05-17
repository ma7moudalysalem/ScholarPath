import { useParams, Link } from "react-router";
import { useTranslation } from "react-i18next";
import {
  ArrowRight,
  ArrowLeft,
  Bookmark,
  Calendar,
  Globe,
  GraduationCap,
  ExternalLink,
} from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  useScholarshipDetailQuery,
  useToggleBookmarkMutation,
} from "@/hooks/useScholarshipsQuery";
import type { FundingType } from "@/types/domain";

// ── Helpers ───────────────────────────────────────────────────────────────────

function DetailRow({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-0.5 py-3 sm:flex-row sm:items-center sm:gap-4">
      <dt className="w-40 shrink-0 text-xs font-semibold uppercase tracking-wide text-text-tertiary">
        {label}
      </dt>
      <dd className="text-sm text-text-primary">{children}</dd>
    </div>
  );
}

function FundingBadge({ type }: { type: FundingType }) {
  const colors: Record<FundingType, string> = {
    FullyFunded:     "bg-emerald-500/10 text-emerald-600",
    PartiallyFunded: "bg-blue-500/10 text-blue-600",
    TuitionOnly:     "bg-purple-500/10 text-purple-600",
    StipendOnly:     "bg-amber-500/10 text-amber-600",
    Other:           "bg-bg-subtle text-text-tertiary",
  };
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium",
        colors[type],
      )}
    >
      {type.replace(/([A-Z])/g, " $1").trim()}
    </span>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function ScholarshipDetail() {
  const { id }       = useParams<{ id: string }>();
  const { t, i18n } = useTranslation(["scholarships", "common"]);
  const isRtl        = i18n.dir() === "rtl";
  const BackIcon     = isRtl ? ArrowRight : ArrowLeft;

  const { data, isLoading, isError } = useScholarshipDetailQuery(id);
  const bookmarkMut = useToggleBookmarkMutation();

  const handleBookmark = () => {
    if (!id) return;
    bookmarkMut.mutate(id, {
      onSuccess: (res) =>
        toast.success(
          res.bookmarked
            ? t("scholarships:bookmark.saved")
            : t("scholarships:bookmark.removed"),
        ),
      onError: () => toast.error(t("common:status.error")),
    });
  };

  // ── Loading skeleton ───────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl space-y-4">
        <div className="h-8 w-48 animate-pulse rounded-md bg-bg-elevated" />
        <div className="h-64 animate-pulse rounded-xl bg-bg-elevated" />
        <div className="h-40 animate-pulse rounded-xl bg-bg-elevated" />
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────
  if (isError || !data) {
    return (
      <div className="mx-auto max-w-3xl">
        <div className="rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-600">
          {t("common:status.error")}
        </div>
      </div>
    );
  }

  // ── Derived values ─────────────────────────────────────────────────────────
  const title       = isRtl ? data.titleAr       : data.titleEn;
  const description = isRtl ? data.descriptionAr : data.descriptionEn;

  const deadlineDate = new Date(data.deadline);
  const daysLeft     = differenceInCalendarDays(deadlineDate, new Date());
  const isUrgent     = daysLeft <= 7 && daysLeft >= 0;
  const isClosed     = daysLeft < 0;
  const isExternal   = data.mode === "ExternalUrl" && !!data.externalUrl;

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="mx-auto max-w-3xl space-y-6">

      {/* ── Back link ── */}
      <Link
        to="/student/scholarships"
        className="inline-flex items-center gap-1.5 text-sm text-text-secondary hover:text-text-primary"
      >
        <BackIcon aria-hidden className="size-4" />
        {t("scholarships:detail.back")}
      </Link>

      {/* ── Hero card ── */}
      <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
        <div className="flex items-start justify-between gap-4">

          {/* Title block */}
          <div className="flex-1">
            {data.isFeatured && (
              <span className="mb-2 inline-flex items-center rounded-full bg-brand-500/10 px-2.5 py-0.5 text-xs font-medium text-brand-500">
                {"★ "}{t("scholarships:card.featured")}
              </span>
            )}
            <h1 className="text-xl font-bold text-text-primary">{title}</h1>
            {data.ownerCompanyName && (
              <p className="mt-1 text-sm text-text-secondary">
                {data.ownerCompanyName}
              </p>
            )}
          </div>

          {/* Bookmark + status badge */}
          <div className="flex shrink-0 flex-col items-end gap-2">
            <button
              type="button"
              onClick={handleBookmark}
              aria-label={t("scholarships:bookmark.toggle")}
              disabled={bookmarkMut.isPending}
              className="rounded-lg border border-border-subtle bg-bg-canvas p-2 text-text-tertiary transition hover:border-brand-500 hover:text-brand-500 disabled:opacity-50"
            >
              <Bookmark aria-hidden className="size-5" />
            </button>

            <span
              className={cn(
                "rounded-full px-2.5 py-0.5 text-xs font-medium",
                isClosed
                  ? "bg-rose-500/10 text-rose-600"
                  : isUrgent
                    ? "bg-amber-500/10 text-amber-600"
                    : "bg-emerald-500/10 text-emerald-600",
              )}
            >
              {isClosed
                ? t("scholarships:card.closed")
                : isUrgent
                  ? t("scholarships:card.daysLeft", { count: daysLeft })
                  : t("scholarships:detail.open")}
            </span>
          </div>
        </div>

        {/* Description */}
        <p className="mt-4 text-sm leading-relaxed text-text-secondary">
          {description}
        </p>
      </div>

      {/* ── Details card ── */}
      <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
        <h2 className="mb-2 text-sm font-semibold text-text-primary">
          {t("scholarships:detail.details")}
        </h2>
        <dl className="divide-y divide-border-subtle">
          <DetailRow label={t("scholarships:detail.funding")}>
            <FundingBadge type={data.fundingType} />
          </DetailRow>

          <DetailRow label={t("scholarships:detail.level")}>
            <span className="inline-flex items-center gap-1.5">
              <GraduationCap aria-hidden className="size-4 text-text-tertiary" />
              {data.targetLevel}
            </span>
          </DetailRow>

          <DetailRow label={t("scholarships:detail.deadline")}>
            <span
              className={cn(
                "inline-flex items-center gap-1.5",
                isUrgent ? "font-medium text-rose-500" : "text-text-primary",
              )}
            >
              <Calendar aria-hidden className="size-4 text-text-tertiary" />
              {format(deadlineDate, "dd MMMM yyyy")}
            </span>
          </DetailRow>

          {data.categoryName && (
            <DetailRow label={t("scholarships:detail.category")}>
              <span className="inline-flex items-center gap-1.5">
                <Globe aria-hidden className="size-4 text-text-tertiary" />
                {data.categoryName}
              </span>
            </DetailRow>
          )}
        </dl>
      </div>

      {/* ── Eligibility ── */}
      {data.eligibilityCriteria && (
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
          <h2 className="mb-3 text-sm font-semibold text-text-primary">
            {t("scholarships:detail.eligibility")}
          </h2>
          <p className="whitespace-pre-line text-sm leading-relaxed text-text-secondary">
            {data.eligibilityCriteria}
          </p>
        </div>
      )}

      {/* ── Required documents ── */}
      {data.requiredDocuments && data.requiredDocuments.length > 0 && (
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
          <h2 className="mb-3 text-sm font-semibold text-text-primary">
            {t("scholarships:detail.documents")}
          </h2>
          <ul className="space-y-2">
            {data.requiredDocuments.map((doc) => (
              <li
                key={doc}
                className="flex items-center gap-2 text-sm text-text-secondary"
              >
                <span
                  aria-hidden
                  className="size-1.5 shrink-0 rounded-full bg-brand-500"
                />
                {doc}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* ── CTA buttons ── */}
      <div className="flex flex-wrap gap-3">
        {isExternal ? (
          <a
            href={data.externalUrl ?? "#"}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 rounded-lg bg-brand-500 px-5 py-2.5 text-sm font-medium text-text-on-brand transition hover:bg-brand-600"
          >
            {t("scholarships:detail.applyExternal")}
            <ExternalLink aria-hidden className="size-4" />
          </a>
        ) : (
          <Link
            to={`/student/applications/new?scholarshipId=${data.id}`}
            className={cn(
              "inline-flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm font-medium transition",
              isClosed
                ? "pointer-events-none bg-bg-subtle text-text-tertiary opacity-60"
                : "bg-brand-500 text-text-on-brand hover:bg-brand-600",
            )}
            aria-disabled={isClosed}
          >
            {t("scholarships:detail.apply")}
          </Link>
        )}

        <Link
          to="/student/scholarships"
          className="inline-flex items-center gap-2 rounded-lg border border-border-subtle bg-bg-elevated px-5 py-2.5 text-sm font-medium text-text-secondary transition hover:border-border-default"
        >
          {t("scholarships:detail.back")}
        </Link>
      </div>

    </div>
  );
}
