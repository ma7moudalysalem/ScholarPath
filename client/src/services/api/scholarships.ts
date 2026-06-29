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
  /** Eligible academic fields — empty means any field is accepted. */
  fieldsOfStudy: string[];
  /** True when the signed-in student has this scholarship bookmarked. */
  isBookmarked: boolean;
}

export interface ScholarshipDetail extends ScholarshipListItem {
  mode: ListingMode;
  externalUrl?: string | null;
  eligibilityCriteria?: string | null;
  applicationFormSchemaJson?: string | null;
  requiredDocuments?: string[];
  /** Raw category id — populated by the server so the company edit form
   *  pre-selects the dropdown instead of starting empty. */
  categoryId?: string | null;
  /** Per-scholarship Review Service Fee (PB-005). Null until the Company
   *  configures one; when null the Apply Now button must be disabled and a
   *  clear message shown. */
  reviewFeeUsd?: number | null;
  /** Owner Company id — exposed so the UI can disable Apply Now when the
   *  signed-in user IS the owning Company (no self-apply). */
  ownerCompanyId?: string | null;
}

/**
 * A saved-scholarship row for the student's bookmarks list — the scholarship
 * itself plus the bookmark metadata. Mirrors the server's
 * `BookmarkedScholarshipDto` (the localised scholarship is mapped into the
 * bilingual `ScholarshipListItem` view model, exactly like `search()`).
 */
export interface BookmarkedScholarship {
  /** Identifier of the bookmark (SavedScholarship) row. */
  id: string;
  scholarshipId: string;
  savedAt: string;
  note?: string | null;
  scholarship: ScholarshipListItem;
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
  fieldOfStudy?: string;
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

/** Server ScholarshipCategoryDto — populates the create/edit form's category dropdown. */
export interface ScholarshipCategory {
  id: string;
  nameEn: string;
  nameAr: string;
  slug: string;
}

/**
 * Body for `POST /api/scholarships` — every field the CreateScholarshipCommand
 * accepts. `deadline` is an ISO-8601 instant (the server binds it to a
 * DateTimeOffset and enforces "≥ 7 days from now").
 */
export interface CreateScholarshipInput {
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  categoryId: string;
  deadline: string;
  fundingType: FundingType;
  targetLevel: AcademicLevel;
  fieldsOfStudy?: string[];
  /** Per-scholarship Review Service Fee in USD (PB-005). Required for in-app
   *  listings — the server rejects null / non-positive values. */
  reviewFeeUsd?: number;
  /** Ordered list of document names the applicant must upload (e.g. "Transcript"). */
  requiredDocuments?: string[];
}

/**
 * Body for `PUT /api/scholarships/{id}` — the update command doesn't accept
 * funding type or target level (those are create-only), so they're omitted here.
 */
export type UpdateScholarshipInput = Omit<
  CreateScholarshipInput,
  "fundingType" | "targetLevel"
>;

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

/** Row returned by `GET /api/scholarships/admin/featured` for the reorder page. */
export interface AdminFeaturedScholarship {
  id: string;
  titleEn: string;
  titleAr: string;
  status: ScholarshipStatus;
  featuredOrder: number;
  deadline: string;
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
  fieldsOfStudy: string[];
  isBookmarked: boolean;
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
  // Raw bilingual + categoryId, shipped by the server alongside the localised
  // title/description so the company edit form can recover both languages.
  titleEn?: string | null;
  titleAr?: string | null;
  descriptionEn?: string | null;
  descriptionAr?: string | null;
  categoryId?: string | null;
  reviewFeeUsd?: number | null;
  ownerCompanyId?: string | null;
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

/** Server BookmarkedScholarshipDto (GET /api/scholarships/bookmarks item). */
interface BookmarkedScholarshipWireDto {
  id: string;
  scholarshipId: string;
  savedAt: string;
  note: string | null;
  scholarship: ScholarshipWireDto;
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
    fieldsOfStudy: dto.fieldsOfStudy ?? [],
    isBookmarked: dto.isBookmarked ?? false,
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

  const base = toListItem(dto);

  // The detail DTO now ships the raw bilingual title/description alongside
  // the localised single-string `title`/`description`. Prefer the raw fields
  // when present so the company edit form can populate both EN+AR.
  return {
    ...base,
    titleEn: dto.titleEn ?? base.titleEn,
    titleAr: dto.titleAr ?? base.titleAr,
    descriptionEn: dto.descriptionEn ?? base.descriptionEn,
    descriptionAr: dto.descriptionAr ?? base.descriptionAr,
    categoryId: dto.categoryId,
    reviewFeeUsd: dto.reviewFeeUsd ?? null,
    ownerCompanyId: dto.ownerCompanyId ?? null,
    mode: dto.mode,
    externalUrl: dto.externalApplicationUrl,
    eligibilityCriteria: dto.eligibilityRequirements,
    applicationFormSchemaJson: dto.applicationFormSchemaJson,
    requiredDocuments: parseStringArray(dto.requiredDocumentsJson) ?? (childDocs.length > 0 ? childDocs : undefined),
  };
}

function toBookmark(dto: BookmarkedScholarshipWireDto): BookmarkedScholarship {
  return {
    id: dto.id,
    scholarshipId: dto.scholarshipId,
    savedAt: dto.savedAt,
    note: dto.note,
    scholarship: toListItem(dto.scholarship),
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
    if (req.fieldOfStudy) params.fieldOfStudy = req.fieldOfStudy;

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

  /**
   * The authenticated student's bookmarked scholarships, newest-saved first —
   * `GET /api/scholarships/bookmarks` (GetMyBookmarkedScholarshipsQuery).
   */
  async getBookmarks(): Promise<BookmarkedScholarship[]> {
    const { data } = await apiClient.get<BookmarkedScholarshipWireDto[]>(
      "/api/scholarships/bookmarks",
    );
    return data.map(toBookmark);
  },

  /**
   * Featured (Open) scholarships for the home page / dashboards, ordered by
   * the curated `FeaturedOrder` — `GET /api/scholarships/featured`
   * (GetFeaturedScholarshipsQuery). Anonymous-accessible.
   */
  async getFeatured(limit?: number): Promise<ScholarshipListItem[]> {
    const { data } = await apiClient.get<ScholarshipWireDto[]>(
      "/api/scholarships/featured",
      limit ? { params: { limit } } : undefined,
    );
    return data.map(toListItem);
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

  // ── Company CRUD ─────────────────────────────────────────────────────────────

  /** Categories list for the create/edit form's dropdown. Public endpoint. */
  async getCategories(): Promise<ScholarshipCategory[]> {
    const { data } = await apiClient.get<ScholarshipCategory[]>(
      "/api/scholarships/categories",
    );
    return data;
  },

  /** Company-only: create a new scholarship listing. */
  async createScholarship(
    input: CreateScholarshipInput,
  ): Promise<{ id: string }> {
    const { data } = await apiClient.post<string>("/api/scholarships", input);
    return { id: data };
  },

  /** Company-only: edit one of the caller's own scholarship listings. */
  async updateScholarship(
    id: string,
    input: UpdateScholarshipInput,
  ): Promise<void> {
    await apiClient.put(`/api/scholarships/${id}`, input);
  },

  /** Company / Admin: soft-delete (archive) a scholarship listing. */
  async archiveScholarship(id: string): Promise<void> {
    await apiClient.delete(`/api/scholarships/${id}`);
  },

  // ── Admin: featured scholarships management ──────────────────────────────────

  /**
   * Admin-only: list all currently-featured scholarships ordered by
   * FeaturedOrder — used by the drag-to-reorder page.
   */
  async getAdminFeatured(): Promise<AdminFeaturedScholarship[]> {
    const { data } = await apiClient.get<AdminFeaturedScholarship[]>(
      "/api/scholarships/admin/featured",
    );
    return data;
  },

  /**
   * Admin-only: feature or un-feature a scholarship (max 12 featured, must be
   * Open to feature). Returns true when the operation succeeds.
   */
  async setFeatured(id: string, featured: boolean): Promise<void> {
    await apiClient.post(`/api/scholarships/${id}/feature`, { featured });
  },

  /**
   * Admin-only: overwrite the FeaturedOrder of ALL currently-featured
   * scholarships. Supply every featured ID in the desired display order.
   */
  async reorderFeatured(ids: string[]): Promise<void> {
    await apiClient.put("/api/scholarships/featured/reorder", { ids });
  },
};
