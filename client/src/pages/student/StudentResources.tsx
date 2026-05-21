import { useState } from "react";
import { Link } from "react-router";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { expertiseTagLabelByLang } from "@/lib/expertiseTagLabel";
import {
  BookOpen,
  ExternalLink,
  Search,
  FileText,
  ListChecks,
  Video,
  Sparkles,
  Star,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import { motion } from "motion/react";
import {
  resourcesApi,
  type PaginatedResources,
  type ResourceListItem,
  type ResourceType,
} from "@/services/api/resources";
import { EmptyState } from "@/components/ui/EmptyState";

const TYPES: ResourceType[] = ["Article", "Guide", "Checklist", "VideoLink"];
const PAGE_SIZE = 12;

const TYPE_ICON: Record<ResourceType, typeof BookOpen> = {
  Article: FileText,
  Guide: BookOpen,
  Checklist: ListChecks,
  VideoLink: Video,
};

const TYPE_THEME: Record<ResourceType, { badge: string; ring: string; accent: string }> = {
  Article:    { badge: "badge-brand",   ring: "ring-brand-200",   accent: "from-brand-500 to-brand-700" },
  Guide:      { badge: "badge-success", ring: "ring-success-200", accent: "from-success-500 to-success-600" },
  Checklist:  { badge: "badge-warning", ring: "ring-warning-200", accent: "from-warning-500 to-warning-600" },
  VideoLink:  { badge: "badge-danger",  ring: "ring-danger-200",  accent: "from-danger-500 to-danger-600" },
};

export function StudentResources() {
  const { t, i18n } = useTranslation(["resources", "common"]);
  const isAr = i18n.language.startsWith("ar");

  const [term, setTerm] = useState("");
  const [searchInput, setSearchInput] = useState("");
  const [type, setType] = useState<ResourceType | "">("");
  const [page, setPage] = useState(1);

  const params = {
    term: term || undefined,
    type: type || undefined,
    language: isAr ? "ar" : "en",
    page,
    pageSize: PAGE_SIZE,
  };

  const { data, isLoading, isError, refetch } = useQuery<PaginatedResources>({
    queryKey: ["resources", "browse", params],
    queryFn: () => resourcesApi.search(params),
    placeholderData: keepPreviousData,
  });

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    setTerm(searchInput.trim());
  };

  const totalPages = data?.totalPages ?? 1;
  const currentPage = data?.pageNumber ?? page;

  return (
    <div className="space-y-6">
      <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary">
            {t("resources:browse.title")}
          </h1>
          <p className="mt-2 max-w-xl text-text-secondary">
            {t("resources:browse.subtitle")}
          </p>
        </div>
      </div>

      <div className="flex flex-col sm:flex-row sm:items-center gap-3">
        <form onSubmit={submitSearch} className="relative flex-1 min-w-[220px]">
          <Search className="pointer-events-none absolute top-1/2 -translate-y-1/2 start-3 size-4 text-text-tertiary" aria-hidden />
          <input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder={t("resources:browse.searchPlaceholder")}
            className="input-premium h-11 ps-10"
          />
        </form>

        {/* Filter chips */}
        <div className="flex flex-wrap items-center gap-1.5 bg-bg-elevated p-1 rounded-xl border border-border-subtle shadow-elevation-1">
          <button
            type="button"
            onClick={() => {
              setPage(1);
              setType("");
            }}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              type === ""
                ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm"
                : "text-text-secondary hover:text-text-primary hover:bg-bg-subtle"
            }`}
          >
            {t("resources:browse.allTypes")}
          </button>
          {TYPES.map((ty) => {
            const Icon = TYPE_ICON[ty];
            const isActive = type === ty;
            return (
              <button
                key={ty}
                type="button"
                onClick={() => {
                  setPage(1);
                  setType(ty);
                }}
                className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                  isActive
                    ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm"
                    : "text-text-secondary hover:text-text-primary hover:bg-bg-subtle"
                }`}
              >
                <Icon size={12} aria-hidden />
                {t(`resources:resourceType.${ty}`)}
              </button>
            );
          })}
        </div>
      </div>

      {isLoading && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="card-premium p-4 space-y-3">
              <div className="skeleton h-32 w-full" />
              <div className="skeleton h-4 w-3/4" />
              <div className="skeleton h-3 w-full" />
              <div className="skeleton h-3 w-5/6" />
            </div>
          ))}
        </div>
      )}

      {isError && !isLoading && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("resources:browse.loadError")}{" "}
          <button
            type="button"
            onClick={() => void refetch()}
            className="text-brand-500 underline font-medium"
          >
            {t("resources:browse.retry")}
          </button>
        </p>
      )}

      {!isLoading && !isError && data?.items.length === 0 && (
        <EmptyState
          icon={Sparkles}
          title={t("resources:browse.empty")}
          description={t("resources:browse.subtitle")}
        />
      )}

      {!isLoading && !isError && (data?.items.length ?? 0) > 0 && (
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {data?.items.map((r, idx) => (
            <motion.div
              key={r.id}
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.22, delay: Math.min(idx * 0.03, 0.18) }}
            >
              <ResourceCard resource={r} isAr={isAr} />
            </motion.div>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-text-secondary pt-4 border-t border-border-subtle">
          <span className="font-medium">
            {t("resources:pagination.pageOf", {
              page: currentPage,
              total: totalPages,
            })}
          </span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={currentPage <= 1}
              className="btn btn-secondary btn-sm"
            >
              <ChevronLeft size={14} className="rtl:rotate-180" aria-hidden />
              {t("resources:pagination.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => p + 1)}
              disabled={currentPage >= totalPages}
              className="btn btn-secondary btn-sm"
            >
              {t("resources:pagination.next")}
              <ChevronRight size={14} className="rtl:rotate-180" aria-hidden />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function ResourceCard({
  resource,
  isAr,
}: {
  resource: ResourceListItem;
  isAr: boolean;
}) {
  const { t } = useTranslation(["resources"]);
  const title = isAr ? resource.titleAr || resource.titleEn : resource.titleEn || resource.titleAr;
  const description = isAr
    ? resource.descriptionAr ?? resource.descriptionEn
    : resource.descriptionEn ?? resource.descriptionAr;
  const Icon = TYPE_ICON[resource.type];
  const theme = TYPE_THEME[resource.type];

  return (
    <Link
      to={`/student/resources/${resource.slug}`}
      className="group flex flex-col card-premium overflow-hidden focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
    >
      <div className="relative">
        {resource.coverImageUrl ? (
          <img
            src={resource.coverImageUrl}
            alt=""
            className="h-36 w-full object-cover transition-transform duration-500 group-hover:scale-[1.04]"
          />
        ) : (
          <div className={`relative flex h-36 w-full items-center justify-center bg-gradient-to-br ${theme.accent}`}>
            <Icon className="size-10 text-white/90" aria-hidden />
            <div aria-hidden className="absolute inset-0 bg-mesh-hero opacity-30" />
          </div>
        )}

        {/* Type badge overlay */}
        <div className="absolute top-3 start-3 flex flex-wrap items-center gap-1.5">
          <span className={`badge ${theme.badge} backdrop-blur-md bg-opacity-90`}>
            <Icon size={10} aria-hidden />
            {t(`resources:resourceType.${resource.type}`)}
          </span>
          {resource.isFeatured && (
            <span className="badge badge-warning backdrop-blur-md bg-opacity-90">
              <Star size={10} aria-hidden fill="currentColor" />
              {t("resources:browse.featured")}
            </span>
          )}
        </div>
      </div>

      <div className="flex flex-col flex-1 p-4 pt-3">
        <h3 className="font-bold text-text-primary tracking-tight leading-snug group-hover:text-brand-600 transition-colors line-clamp-2">
          {title}
        </h3>

        {description && (
          <p className="mt-1.5 line-clamp-3 text-sm text-text-secondary leading-relaxed">{description}</p>
        )}

        <div className="mt-auto pt-3 space-y-2">
          {resource.tags.length > 0 && (
            <div className="flex flex-wrap gap-1">
              {resource.tags.slice(0, 4).map((tag) => (
                <span
                  key={tag}
                  className="rounded-md bg-bg-subtle border border-border-subtle px-1.5 py-0.5 text-[10px] font-medium text-text-secondary"
                >
                  #{expertiseTagLabelByLang(tag, isAr ? "ar" : "en")}
                </span>
              ))}
            </div>
          )}

          {resource.type === "VideoLink" && (
            <span className="mt-1 inline-flex items-center gap-1 text-xs text-text-tertiary font-medium">
              <ExternalLink className="size-3" aria-hidden />
              {t("resources:browse.openLink")}
            </span>
          )}
        </div>
      </div>
    </Link>
  );
}
