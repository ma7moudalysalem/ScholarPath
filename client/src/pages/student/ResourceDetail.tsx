import type { ReactNode } from "react";
import { useParams, Link } from "react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { expertiseTagLabelByLang } from "@/lib/expertiseTagLabel";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { toast } from "sonner";
import {
  ArrowLeft,
  ArrowRight,
  Calendar,
  ExternalLink,
  Bookmark,
  CheckCircle,
  ListChecks,
  BookOpen,
  FileText,
  Video,
  Star,
  Clock,
} from "lucide-react";
import {
  resourcesApi,
  type ResourceDetail as ResourceDetailDto,
  type ChapterProgressResult,
  type ResourceType,
} from "@/services/api/resources";
import { useAuthStore } from "@/stores/authStore";
import { apiErrorMessage } from "@/services/api/client";
import { SkeletonDetailCard } from "@/components/common/Skeleton";

const TYPE_ICON: Record<ResourceType, typeof BookOpen> = {
  Article: FileText,
  Guide: BookOpen,
  Checklist: ListChecks,
  VideoLink: Video,
};

const TYPE_THEME: Record<ResourceType, string> = {
  Article:   "badge-brand",
  Guide:     "badge-success",
  Checklist: "badge-warning",
  VideoLink: "badge-danger",
};

// ── Minimal Markdown renderer ───────────────────────────────────────────────
// Resource content is light Markdown — headings, bullet / ordered / task lists,
// and bold. It is rendered straight to React elements (no HTML injection).

function renderInline(text: string): ReactNode {
  return text.split(/(\*\*[^*]+\*\*)/g).map((part, i) => {
    const bold = /^\*\*([^*]+)\*\*$/.exec(part);
    return bold ? (
      <strong key={i} className="font-semibold text-text-primary">
        {bold[1]}
      </strong>
    ) : (
      part
    );
  });
}

function Markdown({ source }: { source: string }) {
  const blocks: ReactNode[] = [];
  let items: ReactNode[] = [];
  let ordered = false;
  let n = 0;

  const flush = () => {
    if (items.length === 0) return;
    const rendered = items;
    blocks.push(
      ordered ? (
        <ol
          key={`b${n++}`}
          className="ms-5 list-decimal space-y-1 text-sm text-text-secondary"
        >
          {rendered}
        </ol>
      ) : (
        <ul key={`b${n++}`} className="space-y-1.5 text-sm text-text-secondary">
          {rendered}
        </ul>
      ),
    );
    items = [];
  };

  for (const raw of source.replace(/\r\n/g, "\n").split("\n")) {
    const line = raw.trim();
    if (!line) {
      flush();
      continue;
    }

    const heading = /^(#{1,3})\s+(.+)$/.exec(line);
    if (heading) {
      flush();
      const level = heading[1].length;
      const cls =
        level === 1
          ? "text-base font-semibold text-text-primary"
          : level === 2
            ? "text-sm font-semibold text-text-primary"
            : "text-sm font-medium text-text-primary";
      blocks.push(
        <p key={`b${n++}`} className={cls}>
          {renderInline(heading[2])}
        </p>,
      );
      continue;
    }

    const task = /^[-*]\s+\[([ xX])\]\s+(.+)$/.exec(line);
    if (task) {
      if (ordered) flush();
      ordered = false;
      const checked = task[1].toLowerCase() === "x";
      items.push(
        <li key={`i${n++}`} className="flex items-start gap-2">
          <span aria-hidden className="mt-px text-brand-500">
            {checked ? "☑" : "☐"}
          </span>
          <span>{renderInline(task[2])}</span>
        </li>,
      );
      continue;
    }

    const bullet = /^[-*]\s+(.+)$/.exec(line);
    if (bullet) {
      if (ordered) flush();
      ordered = false;
      items.push(
        <li key={`i${n++}`} className="flex items-start gap-2">
          <span
            aria-hidden
            className="mt-1.5 size-1.5 shrink-0 rounded-full bg-brand-500"
          />
          <span>{renderInline(bullet[1])}</span>
        </li>,
      );
      continue;
    }

    const numbered = /^\d+\.\s+(.+)$/.exec(line);
    if (numbered) {
      if (!ordered) flush();
      ordered = true;
      items.push(<li key={`i${n++}`}>{renderInline(numbered[1])}</li>);
      continue;
    }

    flush();
    blocks.push(
      <p key={`b${n++}`} className="text-sm leading-relaxed text-text-secondary">
        {renderInline(line)}
      </p>,
    );
  }
  flush();

  return <div className="space-y-2">{blocks}</div>;
}

// ── Page ────────────────────────────────────────────────────────────────────

export function ResourceDetail() {
  const { idOrSlug } = useParams<{ idOrSlug: string }>();
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isAr ? ar : undefined;
  const BackIcon = isRtl ? ArrowRight : ArrowLeft;
  const { user } = useAuthStore();
  const qc = useQueryClient();

  const { data, isLoading, isError, error, refetch } = useQuery<ResourceDetailDto>({
    queryKey: ["resources", "detail", idOrSlug],
    queryFn: () => resourcesApi.getDetail(idOrSlug!),
    enabled: !!idOrSlug,
  });

  // ── Bookmark toggle ──────────────────────────────────────────────────────
  const bookmarkMut = useMutation({
    mutationFn: (id: string) => resourcesApi.toggleBookmark(id),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["resources", "bookmarks"] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  // ── Chapter completion ───────────────────────────────────────────────────
  const chapterMut = useMutation<ChapterProgressResult, Error, { resourceId: string; chapterId: string }>({
    mutationFn: ({ resourceId, chapterId }) =>
      resourcesApi.completeChapter(resourceId, chapterId),
    onSuccess: (res) => {
      if (res.isResourceComplete) {
        toast.success(t("resources:detail.resourceComplete"));
      } else {
        toast.success(t("resources:detail.chapterMarked"));
      }
      void qc.invalidateQueries({ queryKey: ["resources", "progress"] });
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("common:status.error"))),
  });

  const backLink = (
    <Link
      to="/student/resources"
      className="inline-flex items-center gap-1.5 text-sm text-text-secondary hover:text-text-primary"
    >
      <BackIcon aria-hidden className="size-4" />
      {t("resources:detail.back")}
    </Link>
  );

  // ── Loading ──
  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl">
        <SkeletonDetailCard />
      </div>
    );
  }

  // ── Error / not found ──
  if (isError || !data) {
    const notFound = (error as { status?: number } | null)?.status === 404;
    return (
      <div className="mx-auto max-w-3xl space-y-4">
        {backLink}
        <div className="rounded-lg border border-danger-200 bg-danger-50 p-4 text-sm text-danger-500">
          {notFound ? t("resources:detail.notFound") : t("resources:detail.loadError")}
          {!notFound && (
            <>
              {" "}
              <button
                type="button"
                onClick={() => void refetch()}
                className="underline"
              >
                {t("resources:detail.retry")}
              </button>
            </>
          )}
        </div>
      </div>
    );
  }

  const title = isAr ? data.titleAr || data.titleEn : data.titleEn || data.titleAr;
  const description = isAr
    ? data.descriptionAr ?? data.descriptionEn
    : data.descriptionEn ?? data.descriptionAr;
  const content = isAr
    ? data.contentMarkdownAr ?? data.contentMarkdownEn
    : data.contentMarkdownEn ?? data.contentMarkdownAr;
  const chapters = [...(data.chapters ?? [])].sort((a, b) => a.sortOrder - b.sortOrder);
  const Icon = TYPE_ICON[data.type];
  const typeBadge = TYPE_THEME[data.type];

  return (
    <div className="mx-auto max-w-6xl">
      <div className="mb-6">{backLink}</div>

      <div className="grid lg:grid-cols-[1fr_280px] gap-8">
        <div className="space-y-6 min-w-0">
          {/* ── Hero ── */}
          <div className="card-premium overflow-hidden">
            {data.coverImageUrl ? (
              <div className="relative">
                <img src={data.coverImageUrl} alt="" className="h-56 w-full object-cover" />
                <div aria-hidden className="absolute inset-0 bg-gradient-to-t from-black/30 via-transparent to-transparent" />
                <div className="absolute top-4 start-4 flex flex-wrap gap-1.5">
                  <span className={`badge ${typeBadge} backdrop-blur-md bg-opacity-90`}>
                    <Icon size={10} aria-hidden />
                    {t(`resources:resourceType.${data.type}`)}
                  </span>
                  {data.isFeatured && (
                    <span className="badge badge-warning backdrop-blur-md bg-opacity-90">
                      <Star size={10} aria-hidden fill="currentColor" />
                      {t("resources:browse.featured")}
                    </span>
                  )}
                </div>
              </div>
            ) : (
              <div className="px-6 pt-6 flex flex-wrap gap-1.5">
                <span className={`badge ${typeBadge}`}>
                  <Icon size={10} aria-hidden />
                  {t(`resources:resourceType.${data.type}`)}
                </span>
                {data.isFeatured && (
                  <span className="badge badge-warning">
                    <Star size={10} aria-hidden fill="currentColor" />
                    {t("resources:browse.featured")}
                  </span>
                )}
              </div>
            )}
            <div className="p-6 sm:p-8">
              <h1 className="text-3xl font-bold tracking-tight text-text-primary leading-tight">{title}</h1>

              <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-text-secondary">
                {data.authorName && (
                  <span className="font-medium">{t("resources:detail.by", { author: data.authorName })}</span>
                )}
                {data.publishedAt && (
                  <span className="inline-flex items-center gap-1.5">
                    <Calendar aria-hidden className="size-4 text-text-tertiary" />
                    {t("resources:detail.publishedOn", {
                      date: format(new Date(data.publishedAt), "dd MMMM yyyy", { locale: dateLocale }),
                    })}
                  </span>
                )}
              </div>

              {description && (
                <p className="mt-5 text-base leading-relaxed text-text-secondary">
                  {description}
                </p>
              )}

              {(data.tags ?? []).length > 0 && (
                <div className="mt-5 flex flex-wrap gap-1.5">
                  {(data.tags ?? []).map((tag) => (
                    <span
                      key={tag}
                      className="rounded-md bg-bg-subtle border border-border-subtle px-2 py-0.5 text-xs font-medium text-text-secondary"
                    >
                      #{expertiseTagLabelByLang(tag, i18n.language)}
                    </span>
                  ))}
                </div>
              )}

              <div className="mt-6 flex flex-wrap items-center gap-3">
                {data.externalLinkUrl && (
                  <a
                    href={data.externalLinkUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="btn btn-primary"
                  >
                    {t("resources:detail.openExternal")}
                    <ExternalLink aria-hidden className="size-4" />
                  </a>
                )}
                {user && (
                  <button
                    type="button"
                    disabled={bookmarkMut.isPending}
                    onClick={() => bookmarkMut.mutate(data.id)}
                    className="btn btn-secondary"
                    aria-label={t("resources:detail.bookmarkToggle")}
                  >
                    <Bookmark aria-hidden className="size-4" />
                    {t("resources:detail.bookmark")}
                  </button>
                )}
              </div>
            </div>
          </div>

          {/* ── Content ── */}
          {content && content.trim() && (
            <div className="card-premium p-6 sm:p-8">
              <Markdown source={content} />
            </div>
          )}

          {/* ── Chapters ── */}
          {chapters.length > 0 && (
            <div className="space-y-4">
              <h2 id="chapters" className="text-xl font-bold text-text-primary tracking-tight scroll-mt-24">
                {t("resources:detail.chapters")}
              </h2>
              {chapters.map((ch, idx) => {
                const chTitle = isAr ? ch.titleAr || ch.titleEn : ch.titleEn || ch.titleAr;
                const chContent = isAr
                  ? ch.contentMarkdownAr ?? ch.contentMarkdownEn
                  : ch.contentMarkdownEn ?? ch.contentMarkdownAr;
                return (
                  <div
                    key={ch.id}
                    id={`chapter-${ch.id}`}
                    className="card-premium p-5 sm:p-6 scroll-mt-24"
                  >
                    <div className="flex items-start justify-between gap-3 flex-wrap">
                      <h3 className="flex items-center gap-3 font-bold text-text-primary tracking-tight text-lg leading-snug">
                        <span className="flex size-8 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-xs font-bold text-white shadow-brand-sm">
                          {idx + 1}
                        </span>
                        {chTitle}
                      </h3>
                      <div className="flex shrink-0 items-center gap-2">
                        {ch.estimatedReadMinutes > 0 && (
                          <span className="inline-flex items-center gap-1 text-xs text-text-tertiary font-medium">
                            <Clock size={11} aria-hidden />
                            {t("resources:detail.readTime", {
                              minutes: ch.estimatedReadMinutes,
                            })}
                          </span>
                        )}
                        {user && (
                          <button
                            type="button"
                            disabled={chapterMut.isPending}
                            onClick={() =>
                              chapterMut.mutate({ resourceId: data.id, chapterId: ch.id })
                            }
                            className="inline-flex items-center gap-1 rounded-full border border-success-200 bg-success-50 px-2.5 py-1 text-xs font-semibold text-success-700 transition hover:bg-success-100 disabled:opacity-60"
                            aria-label={t("resources:detail.markComplete")}
                          >
                            <CheckCircle aria-hidden className="size-3.5" />
                            {t("resources:detail.markComplete")}
                          </button>
                        )}
                      </div>
                    </div>
                    {chContent && chContent.trim() && (
                      <div className="mt-4 ps-11">
                        <Markdown source={chContent} />
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* ── Sticky TOC sidebar (desktop only) ── */}
        {chapters.length > 0 && (
          <aside className="hidden lg:block">
            <div className="sticky top-24">
              <div className="card-premium p-4">
                <div className="flex items-center gap-2 mb-3 px-1">
                  <ListChecks size={14} className="text-brand-500" aria-hidden />
                  <h3 className="text-xs font-bold uppercase tracking-wider text-text-tertiary">
                    {t("resources:detail.chapters")}
                  </h3>
                </div>
                <nav className="space-y-0.5">
                  {chapters.map((ch, idx) => {
                    const chTitle = isAr ? ch.titleAr || ch.titleEn : ch.titleEn || ch.titleAr;
                    return (
                      <a
                        key={ch.id}
                        href={`#chapter-${ch.id}`}
                        className="group flex items-start gap-2.5 px-2 py-2 rounded-lg text-start text-sm text-text-secondary hover:text-text-primary hover:bg-bg-subtle transition-colors"
                      >
                        <span className="inline-flex size-5 shrink-0 items-center justify-center rounded-md bg-brand-50 text-[10px] font-bold text-brand-600 group-hover:bg-brand-100 mt-0.5">
                          {idx + 1}
                        </span>
                        <span className="line-clamp-2 leading-snug font-medium flex-1">{chTitle}</span>
                      </a>
                    );
                  })}
                </nav>
              </div>
            </div>
          </aside>
        )}
      </div>
    </div>
  );
}
