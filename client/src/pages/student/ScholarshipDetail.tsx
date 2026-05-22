import { useParams, Link, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { expertiseTagLabelByLang } from "@/lib/expertiseTagLabel";
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
  Share2,
  Sparkles,
  AlertCircle,
  Award,
  CheckCircle2,
} from "lucide-react";
import { format, differenceInCalendarDays } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  useScholarshipDetailQuery,
  useToggleBookmarkMutation,
} from "@/hooks/useScholarshipsQuery";
import { companyReviewRequestsApi } from "@/services/api/companyReviewRequests";
import { ApiError, apiErrorMessage } from "@/services/api/client";
import { profileApi, type UserProfile } from "@/services/api/profile";
import type { FundingType } from "@/types/domain";
import { SkeletonDetailCard } from "@/components/common/Skeleton";

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Premium chip for a quick metadata stat (icon + label + value). */
function StatChip({
  icon: Icon,
  label,
  value,
  tone = "neutral",
}: {
  icon: React.ComponentType<{ className?: string; "aria-hidden"?: boolean }>;
  label: string;
  value: React.ReactNode;
  tone?: "neutral" | "brand" | "success" | "warning" | "danger";
}) {
  const tones: Record<string, string> = {
    neutral: "border-border-subtle bg-bg-muted text-text-primary",
    brand:   "border-brand-200 bg-brand-50 text-brand-700",
    success: "border-success-200 bg-success-50 text-success-700",
    warning: "border-warning-50 bg-warning-50 text-warning-600",
    danger:  "border-danger-200 bg-danger-50 text-danger-500",
  };
  return (
    <div className={cn("flex items-center gap-2.5 rounded-xl border px-3 py-2.5", tones[tone])}>
      <Icon aria-hidden className="size-4 shrink-0 opacity-70" />
      <div className="min-w-0">
        <p className="text-[10px] font-semibold uppercase tracking-wider opacity-70">
          {label}
        </p>
        <p className="truncate text-sm font-semibold">{value}</p>
      </div>
    </div>
  );
}

function fundingBadgeClass(type: FundingType): string {
  switch (type) {
    case "FullyFunded":     return "badge-success";
    case "PartiallyFunded": return "badge-brand";
    case "TuitionOnly":     return "badge-brand";
    case "StipendOnly":     return "badge-warning";
    default:                return "badge-neutral";
  }
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

  // Profile pre-flight (same logic, unchanged) ───────────────────────────────
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

  // PB-005: Apply Now starts the paid CompanyReview support request flow.
  // This creates a Stripe PaymentIntent in manual-capture mode (the card is
  // authorised, not charged) and navigates the Student to the request page
  // where Stripe Elements completes the authorisation and the Student waits
  // for the Company to accept. The actual card-confirmation UI is delivered
  // in a follow-up — this branch wires the backend round-trip and routing.
  const applyMut = useMutation({
    mutationFn: (scholarshipId: string) => companyReviewRequestsApi.start(scholarshipId),
    onSuccess: (result) => {
      toast.success(t("scholarships:detail.applyStarted"));
      navigate(`/student/review-requests/${result.requestId}`);
    },
    onError: (err: unknown) => {
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

  const handleShare = async () => {
    try {
      await navigator.clipboard.writeText(window.location.href);
      toast.success(t("scholarships:detail.shareCopied"));
    } catch {
      // Clipboard unavailable — silently no-op; user can copy from URL bar.
    }
  };

  // ── Loading skeleton ───────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="mx-auto max-w-5xl">
        <SkeletonDetailCard />
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────
  if (isError || !data) {
    return (
      <div className="mx-auto max-w-3xl">
        <div className="rounded-2xl border border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
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
  // PB-005: in-app Apply Now needs a configured Review Service Fee. When
  // missing, disable the button and tell the Student why so they don't get
  // a generic "could not apply" error from the server.
  const hasReviewFee = (data.reviewFeeUsd ?? 0) > 0;
  const feeFormatted = data.reviewFeeUsd != null
    ? new Intl.NumberFormat(i18n.language, { style: "currency", currency: "USD" })
        .format(data.reviewFeeUsd)
    : null;

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="mx-auto max-w-6xl space-y-6">

      {/* ── Back link ── */}
      <Link
        to="/student/scholarships"
        className="inline-flex items-center gap-1.5 text-sm font-medium text-text-secondary transition hover:text-brand-600"
      >
        <BackIcon aria-hidden className="size-4" />
        {t("scholarships:detail.back")}
      </Link>

      {/* ── Hero banner ── */}
      <div className="relative overflow-hidden rounded-2xl border border-border-subtle">
        <div className="relative h-44 bg-gradient-to-br from-brand-600 via-brand-500 to-brand-700 sm:h-52">
          {/* Decorative mesh */}
          <div
            className="pointer-events-none absolute inset-0"
            style={{
              backgroundImage:
                "radial-gradient(at 25% 20%, rgba(255,255,255,0.18) 0px, transparent 50%), radial-gradient(at 75% 80%, rgba(255,255,255,0.10) 0px, transparent 55%), radial-gradient(at 50% 50%, rgba(255,255,255,0.06) 0px, transparent 70%)",
            }}
          />

          {/* Featured chip */}
          {data.isFeatured && (
            <span className="absolute start-6 top-6 inline-flex items-center gap-1.5 rounded-full bg-white/90 px-3 py-1 text-xs font-semibold text-brand-700 shadow-md backdrop-blur-md">
              <Sparkles aria-hidden className="size-3.5" />
              {t("scholarships:card.featured")}
            </span>
          )}

          {/* Status pill */}
          <span
            className={cn(
              "absolute end-6 top-6 rounded-full px-3 py-1 text-xs font-semibold backdrop-blur-md",
              isClosed
                ? "bg-danger-500/95 text-white"
                : isUrgent
                  ? "bg-warning-500/95 text-white"
                  : "bg-success-500/95 text-white",
            )}
          >
            {isClosed
              ? t("scholarships:card.closed")
              : isUrgent
                ? t("scholarships:card.daysLeft", { count: daysLeft })
                : t("scholarships:detail.open")}
          </span>

          {/* Award icon — bottom-left */}
          <div className="absolute -bottom-6 start-6 flex size-16 items-center justify-center rounded-2xl border-4 border-bg-canvas bg-bg-elevated text-brand-600 shadow-lg">
            <Award aria-hidden className="size-7" />
          </div>
        </div>

        {/* Hero body — sits below the banner */}
        <div className="bg-bg-elevated px-6 pb-6 pt-10">
          {data.ownerCompanyName && (
            <p className="text-xs font-semibold uppercase tracking-wider text-text-tertiary">
              {data.ownerCompanyName}
            </p>
          )}
          <h1 className="mt-1 text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
            {title}
          </h1>

          {/* Quick stats row */}
          <div className="mt-5 grid gap-2 sm:grid-cols-3">
            <StatChip
              icon={Award}
              label={t("scholarships:detail.funding")}
              value={t(`scholarships:fundingType.${data.fundingType}`)}
              tone="brand"
            />
            <StatChip
              icon={GraduationCap}
              label={t("scholarships:detail.level")}
              value={t(`scholarships:level.${data.targetLevel}`)}
            />
            <StatChip
              icon={Calendar}
              label={t("scholarships:detail.deadline")}
              value={format(deadlineDate, "dd MMM yyyy", { locale: dateLocale })}
              tone={isClosed ? "danger" : isUrgent ? "warning" : "neutral"}
            />
          </div>
        </div>
      </div>

      {/* ── Two-column body ── */}
      <div className="grid gap-6 lg:grid-cols-3">

        {/* ── Main content (left) ── */}
        <div className="space-y-6 lg:col-span-2">

          {/* Overview */}
          <section className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
            <h2 className="mb-3 text-base font-semibold text-text-primary">
              {t("scholarships:detail.overview")}
            </h2>
            <p className="whitespace-pre-line text-sm leading-relaxed text-text-secondary">
              {description}
            </p>

            {/* Fields of study */}
            {data.fieldsOfStudy && data.fieldsOfStudy.length > 0 && (
              <div className="mt-5 border-t border-border-subtle pt-5">
                <p className="mb-2 text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
                  {t("scholarships:detail.fieldsOfStudy")}
                </p>
                <div className="flex flex-wrap gap-1.5">
                  {data.fieldsOfStudy.map((f) => (
                    <span key={f} className="badge badge-neutral">{f}</span>
                  ))}
                </div>
              </div>
            )}

            {data.categoryName && (
              <div className="mt-5 flex items-center gap-2 border-t border-border-subtle pt-5 text-sm">
                <Globe aria-hidden className="size-4 text-text-tertiary" />
                <span className="font-medium text-text-secondary">
                  {t("scholarships:detail.category")}:
                </span>
                <span className="text-text-primary">{data.categoryName}</span>
              </div>
            )}
          </section>

          {/* Eligibility */}
          {data.eligibilityCriteria && (
            <section className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
              <h2 className="mb-3 text-base font-semibold text-text-primary">
                {t("scholarships:detail.eligibility")}
              </h2>
              <p className="whitespace-pre-line text-sm leading-relaxed text-text-secondary">
                {data.eligibilityCriteria}
              </p>
            </section>
          )}

          {/* Required documents */}
          {data.requiredDocuments && data.requiredDocuments.length > 0 && (
            <section className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
              <h2 className="mb-3 text-base font-semibold text-text-primary">
                {t("scholarships:detail.documents")}
              </h2>
              <ul className="space-y-2.5">
                {data.requiredDocuments.map((doc) => (
                  <li key={doc} className="flex items-start gap-3 text-sm">
                    <CheckCircle2
                      aria-hidden
                      className="mt-0.5 size-4 shrink-0 text-brand-500"
                    />
                    <span className="text-text-secondary">{expertiseTagLabelByLang(doc, i18n.language)}</span>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </div>

        {/* ── Sticky CTA sidebar (right) ── */}
        <aside className="lg:col-span-1">
          <div className="sticky top-20 space-y-4">

            {/* Profile-incomplete warning */}
            {!isExternal && profileIncomplete && (
              <div className="flex items-start gap-3 rounded-2xl border border-warning-50 bg-warning-50 p-4 text-sm text-warning-600">
                <AlertCircle aria-hidden className="mt-0.5 size-5 shrink-0" />
                <div className="flex-1 space-y-2">
                  <p className="font-semibold">
                    {t("scholarships:detail.profileIncompleteTitle")}
                  </p>
                  <p>{t("scholarships:detail.profileIncompleteBody")}</p>
                  <Link
                    to="/profile"
                    className="inline-flex items-center gap-1.5 text-sm font-semibold underline"
                  >
                    {t("scholarships:detail.applyGoToProfile")}
                    <ArrowRight aria-hidden className="size-3.5 rtl:rotate-180" />
                  </Link>
                </div>
              </div>
            )}

            {/* Primary action card */}
            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-sm">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
                {t("scholarships:detail.actionsLabel")}
              </p>

              <div className="mt-3 flex flex-col gap-2.5">
                {isExternal ? (
                  <a
                    href={data.externalUrl ?? "#"}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="btn btn-primary w-full"
                  >
                    <ExternalLink aria-hidden className="size-4" />
                    {t("scholarships:detail.applyExternal")}
                  </a>
                ) : (
                  <>
                    <button
                      type="button"
                      onClick={() => applyMut.mutate(data.id)}
                      disabled={
                        isClosed ||
                        applyMut.isPending ||
                        profileIncomplete ||
                        !hasReviewFee
                      }
                      title={
                        !hasReviewFee
                          ? t("scholarships:detail.applyMissingFee")
                          : profileIncomplete
                            ? t("scholarships:detail.profileIncompleteTitle")
                            : undefined
                      }
                      className="btn btn-primary w-full"
                    >
                      {applyMut.isPending
                        ? t("scholarships:detail.applying")
                        : t("scholarships:detail.apply")}
                    </button>
                    {/* PB-005: spec PART 2 — when the fee is missing or invalid,
                        Apply Now must be disabled with a clear message. */}
                    {!hasReviewFee && (
                      <p className="text-xs text-warning-600">
                        {t("scholarships:detail.applyMissingFee")}
                      </p>
                    )}
                  </>
                )}

                <Link
                  to={`/student/ai?tab=eligibility&sid=${data.id}&stitle=${encodeURIComponent(title ?? data.titleEn)}`}
                  className="btn btn-secondary w-full"
                >
                  <ClipboardCheck aria-hidden className="size-4" />
                  {t("scholarships:detail.checkEligibility")}
                </Link>

                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={handleBookmark}
                    aria-pressed={data.isBookmarked}
                    disabled={bookmarkMut.isPending}
                    className={cn(
                      "btn btn-secondary flex-1",
                      data.isBookmarked && "border-brand-500 text-brand-600",
                    )}
                  >
                    <Bookmark
                      aria-hidden
                      className="size-4"
                      fill={data.isBookmarked ? "currentColor" : "none"}
                    />
                    {data.isBookmarked
                      ? t("scholarships:bookmark.remove")
                      : t("scholarships:bookmark.toggle")}
                  </button>

                  <button
                    type="button"
                    onClick={handleShare}
                    className="btn btn-secondary"
                    aria-label={t("scholarships:detail.share")}
                  >
                    <Share2 aria-hidden className="size-4" />
                  </button>
                </div>
              </div>
            </div>

            {/* At-a-glance summary */}
            <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-text-tertiary">
                {t("scholarships:detail.summaryLabel")}
              </p>
              <dl className="mt-4 space-y-3 text-sm">
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-text-tertiary">
                    {t("scholarships:detail.funding")}
                  </dt>
                  <dd>
                    <span className={cn("badge", fundingBadgeClass(data.fundingType))}>
                      {t(`scholarships:fundingType.${data.fundingType}`)}
                    </span>
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-text-tertiary">
                    {t("scholarships:detail.level")}
                  </dt>
                  <dd className="font-medium text-text-primary">
                    {t(`scholarships:level.${data.targetLevel}`)}
                  </dd>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <dt className="text-text-tertiary">
                    {t("scholarships:detail.deadline")}
                  </dt>
                  <dd
                    className={cn(
                      "font-medium",
                      isClosed
                        ? "text-danger-500"
                        : isUrgent
                          ? "text-warning-600"
                          : "text-text-primary",
                    )}
                  >
                    {format(deadlineDate, "dd MMM yyyy", { locale: dateLocale })}
                  </dd>
                </div>
                {data.categoryName && (
                  <div className="flex items-center justify-between gap-3">
                    <dt className="text-text-tertiary">
                      {t("scholarships:detail.category")}
                    </dt>
                    <dd className="truncate font-medium text-text-primary">
                      {data.categoryName}
                    </dd>
                  </div>
                )}
                {/* PB-005: Review Service Fee — surfaced BEFORE Apply Now so
                    the Student sees the price they're authorising. */}
                {!isExternal && (
                  <div className="flex items-center justify-between gap-3">
                    <dt className="text-text-tertiary">
                      {t("scholarships:detail.reviewFee")}
                    </dt>
                    <dd className="font-semibold text-text-primary">
                      {feeFormatted ?? t("scholarships:detail.reviewFeeMissing")}
                    </dd>
                  </div>
                )}
              </dl>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}
