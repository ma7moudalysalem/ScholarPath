import { useParams, Link, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  ArrowRight,
  ArrowLeft,
  Bookmark,
  Calendar,
  ClipboardCheck,
  Globe,
  GraduationCap,
  ExternalLink,
} from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  useScholarshipDetailQuery,
  useToggleBookmarkMutation,
} from "@/hooks/useScholarshipsQuery";
import { applicationsApi } from "@/services/api/applications";
import { ApiError, apiErrorMessage } from "@/services/api/client";
import { profileApi, type UserProfile } from "@/services/api/profile";
import { AlertCircle } from "lucide-react";
import type { FundingType } from "@/types/domain";
import { SkeletonDetailCard } from "@/components/common/Skeleton";

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

function FundingBadge({ type }: { type?: FundingType }) {
  const { t } = useTranslation("scholarships");
  if (!type) return null;
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
        "inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium",
        colors[type],
      )}
    >
      {t(`fundingType.${type}`)}
    </span>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function ScholarshipDetail() {
  const { id }       = useParams<{ id: string }>();
  const { t, i18n } = useTranslation(["scholarships", "common"]);
  const isRtl        = i18n.dir() === "rtl";
  const dateLocale   = isRtl ? ar : undefined;
  const BackIcon     = isRtl ? ArrowRight : ArrowLeft;

  const navigate     = useNavigate();
  const { data, isLoading, isError } = useScholarshipDetailQuery(id);
  const bookmarkMut = useToggleBookmarkMutation();

  // Pre-flight check for the apply button — the server rejects with 409 if
  // the student's profile is missing AcademicLevel / FieldOfStudy. We mirror
  // that rule here so the user sees an in-page banner + a disabled button
  // BEFORE they tap "Apply", instead of only learning after a network round-trip.
  const profileQuery = useQuery<UserProfile>({
    queryKey: ["profile", "me"],
    queryFn: () => profileApi.getMine(),
    staleTime: 60_000,
  });
  const profileIncomplete = profileQuery.data
    ? !profileQuery.data.academicLevel ||
      !profileQuery.data.fieldOfStudy ||
      profileQuery.data.fieldOfStudy.trim().length === 0
    : false;

  // In-app apply — creates a Draft application, then opens it so the student
  // can review and submit it. External listings use the external-URL button.
  const applyMut = useMutation({
    mutationFn: (scholarshipId: string) => applicationsApi.start(scholarshipId),
    onSuccess: (result) => {
      toast.success(
        result.alreadyExisted
          ? t("scholarships:detail.applyResumed")
          : t("scholarships:detail.applyStarted"),
      );
      navigate(`/student/applications/${result.applicationId}`);
    },
    onError: (err: unknown) => {
      // Surface the server's actual reason (profile incomplete, scholarship
      // closed, …) — and when it's specifically the "complete your profile"
      // 409, route the user there via an action button on the toast so a
      // dismissed in-page banner still has a path forward (UAT TC-003).
      const status = err instanceof ApiError ? err.status : undefined;
      const detail = err instanceof ApiError
        ? (err.payload.detail ?? err.payload.title)
        : null;

      if (status === 409 && detail && detail.toLowerCase().includes("complete your profile")) {
        toast.error(detail, {
          action: {
            label: t("scholarships:detail.applyGoToProfile"),
            onClick: () => navigate("/profile"),
          },
        });
      } else {
        toast.error(apiErrorMessage(err, t("common:status.error")));
      }
    },
  });

  const handleBookmark = () => {
    if (!id) return;
    bookmarkMut.mutate(id, {
      onSuccess: (res) =>
        toast.success(
          res.bookmarked
            ? t("scholarships:bookmark.saved")
            : t("scholarships:bookmark.removed"),
        ),
      onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
    });
  };

  // ── Loading skeleton ───────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl">
        <SkeletonDetailCard />
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────
  if (isError || !data) {
    return (
      <div className="mx-auto max-w-3xl">
        <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
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
              aria-pressed={data.isBookmarked}
              disabled={bookmarkMut.isPending}
              className={cn(
                "rounded-lg border p-2 transition disabled:opacity-50",
                data.isBookmarked
                  ? "border-brand-500 bg-brand-500/10 text-brand-500"
                  : "border-border-subtle bg-bg-canvas text-text-tertiary hover:border-brand-500 hover:text-brand-500",
              )}
            >
              <Bookmark
                aria-hidden
                className="size-5"
                fill={data.isBookmarked ? "currentColor" : "none"}
              />
            </button>

            <span
              className={cn(
                "rounded-full px-2.5 py-0.5 text-xs font-medium",
                isClosed
                  ? "bg-danger-50 text-danger-500"
                  : isUrgent
                    ? "bg-warning-50 text-warning-600"
                    : "bg-success-100 text-success-600",
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
              {t(`scholarships:level.${data.targetLevel}`)}
            </span>
          </DetailRow>

          <DetailRow label={t("scholarships:detail.deadline")}>
            <span
              className={cn(
                "inline-flex items-center gap-1.5",
                isUrgent ? "font-medium text-danger-500" : "text-text-primary",
              )}
            >
              <Calendar aria-hidden className="size-4 text-text-tertiary" />
              {format(deadlineDate, "dd MMMM yyyy", { locale: dateLocale })}
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

          {data.fieldsOfStudy && data.fieldsOfStudy.length > 0 && (
            <DetailRow label={t("scholarships:detail.fieldsOfStudy")}>
              <div className="flex flex-wrap gap-1.5">
                {data.fieldsOfStudy.map((f) => (
                  <span
                    key={f}
                    className="inline-flex items-center rounded-full border border-border-subtle bg-bg-subtle px-2.5 py-0.5 text-xs font-medium text-text-secondary"
                  >
                    {f}
                  </span>
                ))}
              </div>
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

      {/* Profile-incomplete banner — only shows for IN-APP listings (external
          listings don't run the server-side profile check). A direct Link to
          /profile gives the user a one-tap path to unblock themselves. */}
      {!isExternal && profileIncomplete && (
        <div className="mb-4 flex items-start gap-3 rounded-lg border border-warning-200 bg-warning-50 p-4 text-sm text-warning-700">
          <AlertCircle aria-hidden className="size-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1 space-y-2">
            <p className="font-medium">
              {t("scholarships:detail.profileIncompleteTitle")}
            </p>
            <p className="text-warning-700/90">
              {t("scholarships:detail.profileIncompleteBody")}
            </p>
            <Link
              to="/profile"
              className="inline-flex items-center gap-1.5 text-sm font-medium text-warning-800 underline hover:text-warning-900"
            >
              {t("scholarships:detail.applyGoToProfile")}
              <ArrowRight aria-hidden className="size-3.5 rtl:rotate-180" />
            </Link>
          </div>
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
          <button
            type="button"
            onClick={() => applyMut.mutate(data.id)}
            disabled={isClosed || applyMut.isPending || profileIncomplete}
            title={profileIncomplete
              ? t("scholarships:detail.profileIncompleteTitle")
              : undefined}
            className={cn(
              "inline-flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm font-medium transition",
              isClosed || applyMut.isPending || profileIncomplete
                ? "cursor-not-allowed bg-bg-subtle text-text-tertiary opacity-60"
                : "bg-brand-500 text-text-on-brand hover:bg-brand-600",
            )}
          >
            {applyMut.isPending
              ? t("scholarships:detail.applying")
              : t("scholarships:detail.apply")}
          </button>
        )}

        {/* Check eligibility — deep-links to the AI hub with this scholarship
            pre-selected, so the student skips the manual search step. */}
        <Link
          to={`/student/ai?tab=eligibility&sid=${data.id}&stitle=${encodeURIComponent(title ?? data.titleEn)}`}
          className="inline-flex items-center gap-2 rounded-lg border border-border-subtle bg-bg-elevated px-5 py-2.5 text-sm font-medium text-text-secondary transition hover:border-brand-500 hover:text-brand-500"
        >
          <ClipboardCheck aria-hidden className="size-4" />
          {t("scholarships:detail.checkEligibility")}
        </Link>

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
