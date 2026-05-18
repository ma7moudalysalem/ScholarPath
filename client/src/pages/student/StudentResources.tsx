import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { BookOpen, ExternalLink, Search } from "lucide-react";
import {
  resourcesApi,
  type PaginatedResources,
  type ResourceListItem,
  type ResourceType,
} from "@/services/api/resources";

const TYPES: ResourceType[] = ["Article", "Guide", "Checklist", "VideoLink"];
const PAGE_SIZE = 12;

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
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("resources:browse.title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">
          {t("resources:browse.subtitle")}
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <form onSubmit={submitSearch} className="relative flex-1 min-w-[220px]">
          <Search className="pointer-events-none absolute top-1/2 -translate-y-1/2 start-3 size-4 text-text-tertiary" />
          <input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            placeholder={t("resources:browse.searchPlaceholder")}
            className="h-10 w-full rounded-md border border-border-subtle bg-bg-elevated ps-9 pe-3 text-sm text-text-primary focus:border-brand-500 focus:outline-none"
          />
        </form>

        <select
          value={type}
          onChange={(e) => {
            setPage(1);
            setType(e.target.value as ResourceType | "");
          }}
          className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm text-text-primary"
        >
          <option value="">{t("resources:browse.allTypes")}</option>
          {TYPES.map((ty) => (
            <option key={ty} value={ty}>
              {t(`resources:resourceType.${ty}`)}
            </option>
          ))}
        </select>
      </div>

      {isLoading && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("resources:browse.loading")}
        </p>
      )}

      {isError && !isLoading && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("resources:browse.loadError")}{" "}
          <button
            type="button"
            onClick={() => void refetch()}
            className="text-brand-500 underline"
          >
            {t("resources:browse.retry")}
          </button>
        </p>
      )}

      {!isLoading && !isError && data?.items.length === 0 && (
        <p className="py-12 text-center text-sm text-text-tertiary">
          {t("resources:browse.empty")}
        </p>
      )}

      {!isLoading && !isError && (data?.items.length ?? 0) > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {data?.items.map((r) => (
            <ResourceCard key={r.id} resource={r} isAr={isAr} />
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-text-secondary">
          <span>
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
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("resources:pagination.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => p + 1)}
              disabled={currentPage >= totalPages}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("resources:pagination.next")}
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

  return (
    <article className="flex flex-col rounded-lg border border-border-subtle bg-bg-elevated p-4">
      {resource.coverImageUrl ? (
        <img
          src={resource.coverImageUrl}
          alt=""
          className="mb-3 h-32 w-full rounded-md object-cover"
        />
      ) : (
        <div className="mb-3 flex h-32 w-full items-center justify-center rounded-md bg-bg-subtle">
          <BookOpen className="size-8 text-text-tertiary" />
        </div>
      )}

      <div className="mb-2 flex items-center gap-2">
        <span className="rounded-full bg-brand-500/10 px-2 py-0.5 text-xs font-medium text-brand-500">
          {t(`resources:resourceType.${resource.type}`)}
        </span>
        {resource.isFeatured && (
          <span className="rounded-full bg-warning-50 px-2 py-0.5 text-xs font-medium text-warning-600">
            {t("resources:browse.featured")}
          </span>
        )}
      </div>

      <h3 className="font-semibold text-text-primary">{title}</h3>

      {description && (
        <p className="mt-1 line-clamp-3 text-sm text-text-secondary">{description}</p>
      )}

      {resource.tags.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1">
          {resource.tags.slice(0, 4).map((tag) => (
            <span
              key={tag}
              className="rounded bg-bg-subtle px-1.5 py-0.5 text-xs text-text-tertiary"
            >
              {tag}
            </span>
          ))}
        </div>
      )}

      {resource.type === "VideoLink" && (
        <span className="mt-3 inline-flex items-center gap-1 text-xs text-text-tertiary">
          <ExternalLink className="size-3" />
          {t("resources:browse.openLink")}
        </span>
      )}
    </article>
  );
}
