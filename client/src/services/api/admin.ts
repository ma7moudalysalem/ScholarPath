import { apiClient } from "@/services/api/client";

// ─── enums (mirroring server) ────────────────────────────────────────────
export type AccountStatus = "Unassigned" | "PendingApproval" | "Active" | "Suspended" | "Deactivated";
export type UpgradeTarget = "Company" | "Consultant";
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
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface AdminUserRow {
  id: string;
  email: string;
  fullName: string;
  accountStatus: AccountStatus;
  isOnboardingComplete: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt: string | null;
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
}

export interface UpgradeRequestRow {
  id: string;
  userId: string;
  userEmail: string;
  target: UpgradeTarget;
  status: UpgradeRequestStatus;
  reason: string | null;
  createdAt: string;
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
