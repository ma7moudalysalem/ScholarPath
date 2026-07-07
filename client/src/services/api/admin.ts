import { apiClient } from "@/services/api/client";
import type { DocumentItem } from "@/services/api/documents";
import type { PagedResult } from "@/types/api";

export type { PagedResult };

// ─── enums (mirroring server) ────────────────────────────────────────────
export type AccountStatus = "Unassigned" | "PendingApproval" | "Active" | "Suspended" | "Deactivated";
export type UpgradeTarget = "ScholarshipProvider" | "Consultant";
export type UpgradeRequestStatus = "Pending" | "Approved" | "Rejected" | "Cancelled";
export type ApplicationStatus =
  | "Draft"
  | "Pending"
  | "UnderReview"
  | "Shortlisted"
  | "Accepted"
  | "Rejected"
  | "Withdrawn"
  | "Intending"
  | "Applied";
export type RoleOp = "Add" | "Remove";

export type AuditAction =
  | "Create"
  | "Update"
  | "Delete"
  | "Login"
  | "Logout"
  | "LoginFailed"
  | "PasswordReset"
  | "RoleChanged"
  | "Approved"
  | "Rejected"
  | "Moderated"
  | "PaymentCaptured"
  | "PaymentRefunded"
  | "ConfigChanged"
  | "BroadcastSent";

export interface AuditLogDto {
  id: string;
  actorUserId: string | null;
  actorEmail: string | null;
  action: AuditAction;
  targetType: string;
  targetId: string | null;
  summary: string | null;
  ipAddress: string | null;
  correlationId: string | null;
  occurredAt: string;
}

export interface AuditLogParams {
  page?: number;
  pageSize?: number;
  action?: AuditAction;
  targetType?: string;
  actorUserId?: string;
  targetId?: string;
  from?: string;
  to?: string;
  search?: string;
}

// ─── DTOs ────────────────────────────────────────────────────────────────

export interface AdminUserRow {
  id: string;
  email: string;
  fullName: string;
  accountStatus: AccountStatus;
  isOnboardingComplete: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt: string | null;
  /** PB-018 FR-270 — reverse-ETL flag from Power BI. */
  isAtRisk: boolean;
  /** Normalised 0..1 churn-risk score. Null when the user hasn't been scored yet. */
  riskScore: number | null;
  /** FR-094 — true when a consultant's booking intake is auto-suspended for low ratings. */
  bookingIntakeSuspended: boolean;
}

export interface AdminUserDetail extends Omit<AdminUserRow, "fullName"> {
  firstName: string;
  lastName: string;
  fullName: string;
  profileImageUrl: string | null;
  activeRole: string | null;
  countryOfResidence: string | null;
  preferredLanguage: string | null;
  isDeleted: boolean;
}

export interface OnboardingRequestRow {
  userId: string;
  email: string;
  fullName: string;
  accountStatus: AccountStatus;
  createdAt: string;
  requestedRole: string | null;
  // ScholarshipProvider snapshot
  organizationLegalName: string | null;
  organizationWebsite: string | null;
  organizationEmail: string | null;
  organizationCountry: string | null;
  scholarshipProviderType: string | null;
  scholarshipProviderDescription: string | null;
  organizationRegistrationNumber: string | null;
  organizationTaxNumber: string | null;
  contactPersonFullName: string | null;
  contactPersonPosition: string | null;
  contactPhoneNumber: string | null;
  // Consultant snapshot
  biography: string | null;
  professionalTitle: string | null;
  highestDegree: string | null;
  fieldOfExpertise: string | null;
  yearsOfExperience: number | null;
  sessionFeeUsd: number | null;
  sessionDurationMinutes: number | null;
  expertiseTagsJson: string | null;
  languagesJson: string | null;
  timezone: string | null;
  linkedInUrl: string | null;
  portfolioUrl: string | null;
  consultantCountry: string | null;
}

export interface UpgradeRequestRow {
  id: string;
  userId: string;
  userEmail: string;
  target: UpgradeTarget;
  status: UpgradeRequestStatus;
  reason: string | null;
  createdAt: string;
  // Proposed consultant profile snapshot — evidence for the reviewer (a
  // consultant upgrade grants a paid, earning role). All optional.
  fullName?: string | null;
  biography?: string | null;
  professionalTitle?: string | null;
  highestDegree?: string | null;
  fieldOfExpertise?: string | null;
  yearsOfExperience?: number | null;
  sessionFeeUsd?: number | null;
  sessionDurationMinutes?: number | null;
  expertiseTagsJson?: string | null;
  languagesJson?: string | null;
  timezone?: string | null;
  linkedInUrl?: string | null;
  portfolioUrl?: string | null;
  consultantCountry?: string | null;
}

/**
 * PB-005R: a ScholarshipProvider currently flagged in the low-rating admin queue.
 * `averageRating` is the snapshot at flag time; `reviewCount` and
 * `accountStatus` are live. `flaggedAt` is sticky — the queue is sorted
 * by it, newest first.
 */
export interface LowRatedScholarshipProviderRow {
  scholarshipProviderId: string;
  email: string;
  scholarshipProviderName: string;
  organizationLegalName: string | null;
  accountStatus: AccountStatus;
  averageRating: number | null;
  reviewCount: number;
  flaggedAt: string;
  lastReviewAt: string | null;
}

/**
 * PB-006R: a no-show report awaiting admin validation. `accusedRole` says which
 * party is accused of not showing up; the admin either validates (applies the
 * block / rating deduction / refund) or rejects it as false (penalises the reporter).
 */
export interface NoShowReportRow {
  reportId: string;
  bookingId: string;
  reporterName: string;
  accusedName: string;
  accusedRole: "Student" | "Consultant";
  scheduledStartAt: string;
  scheduledEndAt: string;
  reporterNote: string | null;
  reportedAt: string;
  /** Hard attendance evidence from the session room — null when that party never joined. */
  studentJoinedAt: string | null;
  consultantJoinedAt: string | null;
}

export interface AnalyticsOverviewDto {
  totalUsers: number;
  activeUsers: number;
  pendingApprovals: number;
  totalScholarships: number;
  openScholarships: number;
  totalApplications: number;
  submittedApplications: number;
  totalBookings: number;
  completedBookings: number;
  revenueCentsCaptured: number;
  profitShareCentsAccumulated: number;
  aiInteractions24h: number;
  // Period-over-period + real activity signals.
  newUsers7d: number;
  newUsersPrev7d: number;
  newApplications7d: number;
  newApplicationsPrev7d: number;
  activeUsers24h: number;
  logins24h: number;
}

export interface GrowthPoint {
  date: string;
  count: number;
}

export interface ApplicationStatusPoint {
  status: ApplicationStatus;
  count: number;
}

export type AiFeature = "Recommendation" | "Eligibility" | "Chatbot";

export interface AiFeatureUsageDto {
  feature: AiFeature;
  interactions: number;
  costUsd: number;
  avgLatencyMs: number | null;
}

export interface AiDailyCostPoint {
  date: string; // ISO date (YYYY-MM-DD)
  costUsd: number;
}

export interface RecommendationCtrDto {
  impressions: number;
  clicks: number;
  ctrPercent: number;
}

export interface AiUsageSummaryDto {
  windowDays: number;
  totalCostUsd: number;
  totalInteractions: number;
  byFeature: AiFeatureUsageDto[];
  dailyCost: AiDailyCostPoint[];
  recommendations: RecommendationCtrDto;
  generatedAt: string;
}

export type RedactionVerdict = "Clean" | "MissedEmail" | "MissedPhone" | "MissedCard";

export interface RedactionAuditSampleRow {
  id: string;
  aiInteractionId: string;
  userId: string;
  userEmail: string | null;
  redactedPrompt: string;
  sampledAt: string;
  verdict: RedactionVerdict | null;
  reviewerUserId: string | null;
  reviewedAt: string | null;
}

// ─── request shapes ──────────────────────────────────────────────────────
export interface SearchUsersParams {
  search?: string;
  status?: AccountStatus;
  role?: string;
  includeDeleted?: boolean;
  page?: number;
  pageSize?: number;
}

export interface SetStatusBody {
  status: AccountStatus;
  reason?: string;
}

export interface ReviewBody {
  approve: boolean;
  notes?: string;
}

export interface BroadcastBody {
  titleEn: string;
  titleAr: string;
  bodyEn: string;
  bodyAr: string;
  targetRole?: string | null;
}

// ─── AI / RAG knowledge base ─────────────────────────────────────────────
export interface KnowledgeBaseStatus {
  totalDocuments: number;
  scholarshipDocuments: number;
  faqDocuments: number;
  embeddedDocuments: number;
  pendingDocuments: number;
  activeEmbeddingModel: string;
  lastIndexedAt: string | null;
}

export interface KnowledgeBaseRebuildResult {
  upserted: number;
  reembedded: number;
  removed: number;
  skipped: number;
  status: KnowledgeBaseStatus;
}

export interface DatasetImportResult {
  datasetName: string;
  totalInDataset: number;
  created: number;
  updated: number;
  skipped: number;
}

export interface DatasetImportWithRebuild {
  import: DatasetImportResult;
  knowledgeBase: KnowledgeBaseRebuildResult;
}

export interface FineTuningDataset {
  fileName: string;
  exampleCount: number;
  jsonl: string;
  generatedAt: string;
}

export interface StartFineTuningJobResult {
  jobId: string;
  fileId: string;
  baseModel: string;
  trainingExamples: number;
}

export interface FineTuningStatusResult {
  jobId: string | null;
  status: string;
  fineTunedModel: string | null;
  error: string | null;
  activeDeploymentName: string | null;
  hasActiveDeployment: boolean;
}

export interface ActivateFineTunedModelResult {
  deploymentName: string;
  replaced: boolean;
}

// ─── API ─────────────────────────────────────────────────────────────────
export const adminApi = {
  // users
  async searchUsers(params: SearchUsersParams = {}): Promise<PagedResult<AdminUserRow>> {
    const { data } = await apiClient.get<PagedResult<AdminUserRow>>("/api/admin/users", { params });
    return data;
  },
  async getUserDetail(userId: string): Promise<AdminUserDetail> {
    const { data } = await apiClient.get<AdminUserDetail>(`/api/admin/users/${userId}`);
    return data;
  },
  async setUserStatus(userId: string, body: SetStatusBody): Promise<void> {
    await apiClient.post(`/api/admin/users/${userId}/status`, body);
  },
  async softDeleteUser(userId: string, reason?: string): Promise<void> {
    await apiClient.delete(`/api/admin/users/${userId}`, { params: { reason } });
  },
  async changeUserRole(userId: string, role: string, operation: RoleOp): Promise<void> {
    await apiClient.post(`/api/admin/users/${userId}/roles`, { role, operation });
  },
  /** FR-094 — clears a consultant's auto-suspended booking intake after review. */
  async reinstateBookingIntake(userId: string): Promise<void> {
    await apiClient.post(`/api/admin/users/${userId}/reinstate-booking-intake`);
  },

  // onboarding / upgrade queues
  async getOnboardingQueue(page = 1, pageSize = 25): Promise<PagedResult<OnboardingRequestRow>> {
    const { data } = await apiClient.get<PagedResult<OnboardingRequestRow>>(
      "/api/admin/onboarding-queue",
      { params: { page, pageSize } },
    );
    return data;
  },
  async reviewOnboarding(userId: string, body: ReviewBody): Promise<void> {
    await apiClient.post(`/api/admin/onboarding-queue/${userId}/review`, body);
  },
  /** UAT TC-001/002 — verification documents a pending applicant uploaded for onboarding review. */
  async getOnboardingDocuments(userId: string): Promise<DocumentItem[]> {
    const { data } = await apiClient.get<DocumentItem[]>(
      `/api/admin/onboarding-queue/${userId}/documents`,
    );
    return data;
  },
  async getUpgradeQueue(
    status: UpgradeRequestStatus | null = "Pending",
    page = 1,
    pageSize = 25,
  ): Promise<PagedResult<UpgradeRequestRow>> {
    const { data } = await apiClient.get<PagedResult<UpgradeRequestRow>>(
      "/api/admin/upgrade-queue",
      { params: { status, page, pageSize } },
    );
    return data;
  },
  async reviewUpgrade(requestId: string, body: ReviewBody): Promise<void> {
    await apiClient.post(`/api/admin/upgrade-queue/${requestId}/review`, body);
  },

  // PB-005R: low-rated companies queue
  async getLowRatedCompanies(
    page = 1,
    pageSize = 25,
  ): Promise<PagedResult<LowRatedScholarshipProviderRow>> {
    const { data } = await apiClient.get<PagedResult<LowRatedScholarshipProviderRow>>(
      "/api/admin/low-rated-companies",
      { params: { page, pageSize } },
    );
    return data;
  },
  async clearScholarshipProviderLowRatingFlag(scholarshipProviderId: string): Promise<void> {
    await apiClient.post(
      `/api/admin/companies/${scholarshipProviderId}/clear-low-rating-flag`,
    );
  },

  // PB-006R: no-show reports awaiting validation
  async getNoShowReports(
    page = 1,
    pageSize = 25,
  ): Promise<PagedResult<NoShowReportRow>> {
    const { data } = await apiClient.get<PagedResult<NoShowReportRow>>(
      "/api/admin/no-show-reports",
      { params: { page, pageSize } },
    );
    return data;
  },
  async resolveNoShowReport(
    reportId: string,
    body: { isValid: boolean; adminNote?: string | null },
  ): Promise<void> {
    await apiClient.post(`/api/admin/no-show-reports/${reportId}/resolve`, body);
  },

  // analytics
  async analyticsOverview(): Promise<AnalyticsOverviewDto> {
    const { data } = await apiClient.get<AnalyticsOverviewDto>("/api/admin/analytics/overview");
    return data;
  },
  async userGrowth(days = 30): Promise<GrowthPoint[]> {
    const { data } = await apiClient.get<GrowthPoint[]>("/api/admin/analytics/user-growth", {
      params: { days },
    });
    return data;
  },
  async applicationFunnel(): Promise<ApplicationStatusPoint[]> {
    const { data } = await apiClient.get<ApplicationStatusPoint[]>(
      "/api/admin/analytics/application-funnel",
    );
    return data;
  },
  async aiUsage(windowDays: 7 | 30 | 90 = 30): Promise<AiUsageSummaryDto> {
    const { data } = await apiClient.get<AiUsageSummaryDto>(
      "/api/admin/analytics/ai-usage",
      { params: { windowDays } },
    );
    return data;
  },

  // AI / RAG knowledge base
  async knowledgeBaseStatus(): Promise<KnowledgeBaseStatus> {
    const { data } = await apiClient.get<KnowledgeBaseStatus>("/api/admin/ai/knowledge-base");
    return data;
  },
  async rebuildKnowledgeBase(force = false): Promise<KnowledgeBaseRebuildResult> {
    const { data } = await apiClient.post<KnowledgeBaseRebuildResult>(
      "/api/admin/ai/knowledge-base/rebuild",
      null,
      { params: { force } },
    );
    return data;
  },
  async importExternalDataset(): Promise<DatasetImportWithRebuild> {
    const { data } = await apiClient.post<DatasetImportWithRebuild>(
      "/api/admin/ai/datasets/import",
    );
    return data;
  },
  async fineTuningDataset(): Promise<FineTuningDataset> {
    const { data } = await apiClient.get<FineTuningDataset>(
      "/api/admin/ai/fine-tuning/dataset",
    );
    return data;
  },
  async startFineTuningJob(
    baseModel = "gpt-4o-mini",
  ): Promise<StartFineTuningJobResult> {
    const { data } = await apiClient.post<StartFineTuningJobResult>(
      "/api/admin/ai/fine-tuning/start",
      null,
      { params: { baseModel } },
    );
    return data;
  },
  async fineTuningStatus(): Promise<FineTuningStatusResult> {
    const { data } = await apiClient.get<FineTuningStatusResult>(
      "/api/admin/ai/fine-tuning/status",
    );
    return data;
  },
  async activateFineTunedModel(deploymentName: string): Promise<ActivateFineTunedModelResult> {
    const { data } = await apiClient.put<ActivateFineTunedModelResult>(
      "/api/admin/ai/fine-tuning/activate",
      null,
      { params: { deploymentName } },
    );
    return data;
  },
  async deactivateFineTunedModel(): Promise<void> {
    await apiClient.delete("/api/admin/ai/fine-tuning/activate");
  },

  // redaction audit (PB-017 US-178)
  async getRedactionSamples(
    pendingOnly = true,
    page = 1,
    pageSize = 25,
  ): Promise<PagedResult<RedactionAuditSampleRow>> {
    const { data } = await apiClient.get<PagedResult<RedactionAuditSampleRow>>(
      "/api/admin/redaction-audit",
      { params: { pendingOnly, page, pageSize } },
    );
    return data;
  },
  async setRedactionVerdict(sampleId: string, verdict: RedactionVerdict): Promise<void> {
    await apiClient.post(`/api/admin/redaction-audit/${sampleId}/verdict`, { verdict });
  },

  // audit log
  async getAuditLog(params: AuditLogParams = {}): Promise<PagedResult<AuditLogDto>> {
    const { data } = await apiClient.get<PagedResult<AuditLogDto>>("/api/admin/audit-log", { params });
    return data;
  },

  // broadcast
  async sendBroadcast(body: BroadcastBody): Promise<{ recipientCount: number }> {
    const { data } = await apiClient.post<{ recipientCount: number }>(
      "/api/admin/broadcasts",
      body,
    );
    return data;
  },
};
