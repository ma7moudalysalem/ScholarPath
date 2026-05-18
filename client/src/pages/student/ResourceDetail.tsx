import type { ReactNode } from "react";
import { useParams, Link } from "react-router";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { ArrowLeft, ArrowRight, Calendar, ExternalLink } from "lucide-react";
import {
  resourcesApi,
  type ResourceDetail as ResourceDetailDto,
} from "@/services/api/resources";
import { SkeletonDetailCard } from "@/components/common/Skeleton";

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
  const BackIcon = isRtl ? ArrowRight : ArrowLeft;

  const { data, isLoading, isError, error, refetch } = useQuery<ResourceDetailDto>({
    queryKey: ["resources", "detail", idOrSlug],
    queryFn: () => resourcesApi.getDetail(idOrSlug!),
    enabled: !!idOrSlug,
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
  const chapters = [...data.chapters].sort((a, b) => a.sortOrder - b.sortOrder);

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {backLink}

      {/* ── Hero ── */}
      <div className="overflow-hidden rounded-xl border border-border-subtle bg-bg-elevated shadow-xs">
        {data.coverImageUrl && (
          <img src={data.coverImageUrl} alt="" className="h-48 w-full object-cover" />
        )}
        <div className="p-6">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <span className="rounded-full bg-brand-500/10 px-2.5 py-0.5 text-xs font-medium text-brand-500">
              {t(`resources:resourceType.${data.type}`)}
            </span>
            {data.isFeatured && (
              <span className="rounded-full bg-warning-50 px-2.5 py-0.5 text-xs font-medium text-warning-600">
                {t("resources:browse.featured")}
              </span>
            )}
          </div>

          <h1 className="text-xl font-bold text-text-primary">{title}</h1>

          <div className="mt-1.5 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-text-secondary">
            {data.authorName && (
              <span>{t("resources:detail.by", { author: data.authorName })}</span>
            )}
            {data.publishedAt && (
              <span className="inline-flex items-center gap-1.5">
                <Calendar aria-hidden className="size-4 text-text-tertiary" />
                {t("resources:detail.publishedOn", {
                  date: format(new Date(data.publishedAt), "dd MMMM yyyy"),
                })}
              </span>
            )}
          </div>

          {description && (
            <p className="mt-4 text-sm leading-relaxed text-text-secondary">
              {description}
            </p>
          )}

          {data.tags.length > 0 && (
            <div className="mt-4 flex flex-wrap gap-1.5">
              {data.tags.map((tag) => (
                <span
                  key={tag}
                  className="rounded bg-bg-subtle px-2 py-0.5 text-xs text-text-tertiary"
                >
                  {tag}
                </span>
              ))}
            </div>
          )}

          {data.externalLinkUrl && (
            <a
              href={data.externalLinkUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-5 inline-flex items-center gap-2 rounded-lg bg-brand-500 px-5 py-2.5 text-sm font-medium text-text-on-brand transition hover:bg-brand-600"
            >
              {t("resources:detail.openExternal")}
              <ExternalLink aria-hidden className="size-4" />
            </a>
          )}
        </div>
      </div>

      {/* ── Content ── */}
      {content && content.trim() && (
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
          <Markdown source={content} />
        </div>
      )}

      {/* ── Chapters ── */}
      {chapters.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-sm font-semibold text-text-primary">
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
                className="rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs"
              >
                <div className="flex items-start justify-between gap-3">
                  <h3 className="flex items-center gap-2 font-semibold text-text-primary">
                    <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-brand-500/10 text-xs font-semibold text-brand-500">
                      {idx + 1}
                    </span>
                    {chTitle}
                  </h3>
                  {ch.estimatedReadMinutes > 0 && (
                    <span className="shrink-0 text-xs text-text-tertiary">
                      {t("resources:detail.readTime", {
                        minutes: ch.estimatedReadMinutes,
                      })}
                    </span>
                  )}
                </div>
                {chContent && chContent.trim() && (
                  <div className="mt-3">
                    <Markdown source={chContent} />
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
