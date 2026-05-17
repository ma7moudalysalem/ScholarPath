import { apiClient } from "@/services/api/client";
import type { FundingType, AcademicLevel, ListingMode } from "@/types/domain";

// ── Frontend view models ──────────────────────────────────────────────────────
//
// The backend ships already-localised, flat DTOs (a single `title` /
// `description` chosen server-side from the `Accept-Language` header — see
// ScholarshipDto / ScholarshipDetailDto). The pages, however, were written
// against a bilingual shape (`titleEn` / `titleAr` …). To avoid rewriting
// every consumer we keep these bilingual view models and map the wire DTO
// into them inside the service: both language fields receive the one
// localised string the server returned, so `isRtl ? titleAr : titleEn`
// still renders correctly.

export interface ScholarshipListItem {
  id: string;
  slug: string | null;
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  deadline: string;
  fundingType: FundingType;
  targetLevel: AcademicLevel;
  categoryName?: string | null;
  ownerCompanyName?: string | null;
  isFeatured: boolean;
  status: ScholarshipStatus;
}

export interface ScholarshipDetail extends ScholarshipListItem {
  mode: ListingMode;
  externalUrl?: string | null;
  eligibilityCriteria?: string | null;
  applicationFormSchemaJson?: string | null;
  requiredDocuments?: string[];
}

export interface SearchScholarshipsRequest {
  query?: string;
  country?: string;
  categoryId?: string;
  deadlineFrom?: string;
  deadlineTo?: string;
  fundingTypes?: FundingType[];
  academicLevels?: AcademicLevel[];
  /** Maps to the server's `FundedOnly` flag (fully- or partially-funded only). */
  fundedOnly?: boolean;
  page?: number;
  pageSize?: number;
}

/** Paged scholarship list — mirrors what `ScholarshipsPage` consumes. */
export interface Paginated<T> {
  items: T[];
  page: number;
  total: number;
  totalPages: number;
}

export type ScholarshipStatus =
  | "Draft"
  | "Open"
  | "Closed"
  | "Archived"
  | "UnderReview";

/** Server MyScholarshipDto shape — company list + admin moderation rows. */
export interface MyScholarship {
  id: string;
  titleEn: string;
  titleAr: string;
  slug: string | null;
  status: ScholarshipStatus;
  mode: ListingMode;
  deadline: string;
  applicantCount: number;
  createdAt: string;
}

/** Server PaginatedList<MyScholarshipDto> shape. */
export interface PaginatedMyScholarships {
  items: MyScholarship[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── Wire DTOs (exactly what the .NET API serialises — camelCase, enum strings) ─

/** Server ScholarshipDto (GET /api/scholarships item). */
interface ScholarshipWireDto {
  id: string;
  title: string;
  description: string;
  categoryName: string | null;
  ownerCompanyName: string | null;
  status: ScholarshipStatus;
  fundingType: FundingType;
  targetLevel: AcademicLevel;
  deadline: string;
  isFeatured: boolean;
  slug: string | null;
}

/** Server ScholarshipChildDto. */
interface ScholarshipChildWireDto {
  childType: string;
  key: string;
  value: string | null;
  sortOrder: number;
}

/** Server ScholarshipDetailDto (GET /api/scholarships/{id}). */
interface ScholarshipDetailWireDto extends ScholarshipWireDto {
  externalApplicationUrl: string | null;
  mode: ListingMode;
  eligibilityRequirements: string | null;
  children: ScholarshipChildWireDto[];
  applicationFormSchemaJson: string | null;
  requiredDocumentsJson: string | null;
}

/** Server PaginatedList<T>. */
interface PaginatedListWireDto<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── Wire → view-model mappers ──────────────────────────────────────────────────

function toListItem(dto: ScholarshipWireDto): ScholarshipListItem {
  return {
    id: dto.id,
    slug: dto.slug,
    // The server already localised `title`/`description`; mirror into both
    // language slots so RTL/LTR consumers both render the right string.
    titleEn: dto.title,
    titleAr: dto.title,
    descriptionEn: dto.description,
    descriptionAr: dto.description,
    deadline: dto.deadline,
    fundingType: dto.fundingType,
    targetLevel: dto.targetLevel,
    categoryName: dto.categoryName,
    ownerCompanyName: dto.ownerCompanyName,
    isFeatured: dto.isFeatured,
    status: dto.status,
  };
}

/** Parses the server's JSON string column into a string[] (tolerant of nulls). */
function parseStringArray(json: string | null): string[] | undefined {
  if (!json) return undefined;
  try {
    const parsed: unknown = JSON.parse(json);
    return Array.isArray(parsed) ? parsed.map(String) : undefined;
  } catch {
    return undefined;
  }
}

function toDetail(dto: ScholarshipDetailWireDto): ScholarshipDetail {
  // RequiredDocuments may be a JSON array, or the generic "RequiredDoc"
  // child rows — fall back to the children when the JSON column is empty.
  const childDocs = dto.children
    .filter((c) => c.childType === "RequiredDoc")
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((c) => c.value ?? c.key);

  return {
    ...toListItem(dto),
    mode: dto.mode,
    externalUrl: dto.externalApplicationUrl,
    eligibilityCriteria: dto.eligibilityRequirements,
    applicationFormSchemaJson: dto.applicationFormSchemaJson,
    requiredDocuments: parseStringArray(dto.requiredDocumentsJson) ?? (childDocs.length > 0 ? childDocs : undefined),
  };
}

// ── API ───────────────────────────────────────────────────────────────────────

export const scholarshipsApi = {
  /**
   * Browse/search scholarships — `GET /api/scholarships`, bound from
   * `GetScholarshipsQuery` ([FromQuery]). The server takes a single funding
   * type / academic level / country, so the multi-select filters are
   * narrowed to their first value here.
   */
  async search(
    req: SearchScholarshipsRequest,
  ): Promise<Paginated<ScholarshipListItem>> {
    const params: Record<string, string | number | boolean> = {
      pageNumber: req.page ?? 1,
      pageSize: req.pageSize ?? 12,
    };
    if (req.query) params.term = req.query;
    if (req.country) params.country = req.country;
    if (req.categoryId) params.categoryId = req.categoryId;
    if (req.deadlineFrom) params.deadlineFrom = req.deadlineFrom;
    if (req.deadlineTo) params.deadlineTo = req.deadlineTo;
    if (req.fundingTypes && req.fundingTypes.length > 0)
      params.fundingType = req.fundingTypes[0];
    if (req.academicLevels && req.academicLevels.length > 0)
      params.academicLevel = req.academicLevels[0];
    if (req.fundedOnly) params.fundedOnly = true;

    const { data } = await apiClient.get<PaginatedListWireDto<ScholarshipWireDto>>(
      "/api/scholarships",
      { params },
    );
    return {
      items: data.items.map(toListItem),
      page: data.pageNumber,
      total: data.totalCount,
      totalPages: data.totalPages,
    };
  },

  async getById(id: string): Promise<ScholarshipDetail> {
    const { data } = await apiClient.get<ScholarshipDetailWireDto>(
      `/api/scholarships/${id}`,
    );
    return toDetail(data);
  },

  /** Toggles the bookmark; the server returns the raw new boolean state. */
  async toggleBookmark(id: string): Promise<{ bookmarked: boolean }> {
    const { data } = await apiClient.post<boolean>(
      `/api/scholarships/${id}/bookmark`,
    );
    return { bookmarked: data };
  },

  // ── Company: own scholarships ────────────────────────────────────────────────

  /** The authenticated company's own scholarships, newest first. */
  async getMine(): Promise<MyScholarship[]> {
    const { data } = await apiClient.get<MyScholarship[]>("/api/scholarships/mine");
    return data;
  },

  // ── Admin moderation ─────────────────────────────────────────────────────────

  /** Admin-only: scholarships filtered by moderation status, paged. */
  async getForModeration(
    status: ScholarshipStatus = "UnderReview",
    page = 1,
    pageSize = 20,
  ): Promise<PaginatedMyScholarships> {
    const { data } = await apiClient.get<PaginatedMyScholarships>(
      "/api/scholarships/admin",
      { params: { status, page, pageSize } },
    );
    return data;
  },

  /** Admin-only: approve an under-review scholarship. */
  async approve(id: string): Promise<void> {
    await apiClient.post(`/api/scholarships/${id}/approve`);
  },

  /** Admin-only: reject an under-review scholarship back to draft with a reason. */
  async reject(id: string, reason: string): Promise<void> {
    await apiClient.post(`/api/scholarships/${id}/reject`, { reason });
  },
};
