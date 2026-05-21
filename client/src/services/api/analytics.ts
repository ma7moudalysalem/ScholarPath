import { apiClient } from "@/services/api/client";

export interface EmbedTokenDto {
  isConfigured: boolean;
  token: string | null;
  embedUrl: string | null;
  reportId: string | null;
  expiresAt: string | null;
}

export type PowerBiReportType =
  | "ExecutiveDashboard"
  | "StudentSuccessDashboard"
  | "FinancialDashboard"
  | "ConsultantSelfAnalytics"
  | "StudentSelfAnalytics";

export interface ConsultantKpisDto {
  totalBookings: number;
  completedBookings: number;
  cancelledBookings: number;
  rejectedBookings: number;
  consultantNoShows: number;
  studentNoShows: number;
  completedRevenueUsd: number;
  reviewCount: number;
  averageRating: number | null;
}

export interface StudentJourneyDto {
  totalApplications: number;
  submittedApplications: number;
  acceptedApplications: number;
  totalBookings: number;
  completedBookings: number;
  lastApplicationAt: string | null;
  lastBookingAt: string | null;
  onboardingComplete: boolean;
}

// ── Reports — Admin Revenue ─────────────────────────────────────────────────

export interface RevenueMonthDto {
  month: string; // YYYY-MM
  grossUsd: number;
  netUsd: number;
  refundedUsd: number;
}

export interface TopConsultantDto {
  id: string;
  name: string;
  revenueUsd: number;
}

export interface AdminRevenueDto {
  totalGrossUsd: number;
  totalProfitShareUsd: number;
  totalPayeeNetUsd: number;
  totalRefundedUsd: number;
  refundRate: number;
  bookingRevenueUsd: number;
  reviewRevenueUsd: number;
  monthOverMonthGrowth: number;
  refundCount: number;
  successfulPaymentCount: number;
  byMonth: RevenueMonthDto[];
  topConsultants: TopConsultantDto[];
}

// ── Reports — Company Insights ──────────────────────────────────────────────

export interface CountryBreakdownDto {
  country: string;
  count: number;
  acceptanceRate: number;
}

export interface FieldBreakdownDto {
  fieldEn: string;
  fieldAr: string;
  count: number;
  acceptanceRate: number;
}

export interface TopScholarshipDto {
  id: string;
  title: string;
  applications: number;
}

export interface FunnelMonthDto {
  month: string;
  views: number;
  applied: number;
  accepted: number;
}

export interface CompanyInsightsDto {
  totalApplications: number;
  submittedCount: number;
  acceptedCount: number;
  rejectedCount: number;
  acceptanceRate: number;
  averageDaysToDecision: number;
  comparisonToPlatformAvg: number;
  byCountry: CountryBreakdownDto[];
  byField: FieldBreakdownDto[];
  topScholarships: TopScholarshipDto[];
  monthlyFunnel: FunnelMonthDto[];
}

// ── Reports — Consultant Earnings Trend ─────────────────────────────────────

export interface MonthlyEarningDto {
  month: string;
  grossUsd: number;
  netUsd: number;
  bookingCount: number;
}

export interface ConsultantEarningsTrendDto {
  totalGrossUsd: number;
  totalNetUsd: number;
  totalRefundedUsd: number;
  monthlyEarnings: MonthlyEarningDto[];
  projectedNextMonth: number;
  peerAvgNetUsd: number;
  yourPercentile: number;
  upcomingBookingRevenue: number;
}

export interface DateRange {
  from?: string; // YYYY-MM-DD
  to?: string;   // YYYY-MM-DD
}

function rangeQuery(range?: DateRange): string {
  if (!range) return "";
  const params: string[] = [];
  if (range.from) params.push(`from=${encodeURIComponent(range.from)}`);
  if (range.to) params.push(`to=${encodeURIComponent(range.to)}`);
  return params.length ? `?${params.join("&")}` : "";
}

export const analyticsApi = {
  /** Fetch a short-lived Power BI embed token for the given report type.
   *  Returns null (503) when the workspace is not yet provisioned. */
  getEmbedToken: async (reportType: PowerBiReportType): Promise<EmbedTokenDto | null> => {
    try {
      const { data } = await apiClient.get<EmbedTokenDto>(
        `/api/analytics/embed-token?reportType=${encodeURIComponent(reportType)}`,
      );
      return data;
    } catch (err: unknown) {
      // 503 means Power BI workspace not provisioned — return null for graceful fallback
      if (
        err != null &&
        typeof err === "object" &&
        "status" in err &&
        (err as { status: number }).status === 503
      ) {
        return null;
      }
      throw err;
    }
  },

  getConsultantKpis: async (): Promise<ConsultantKpisDto> => {
    const { data } = await apiClient.get<ConsultantKpisDto>("/api/analytics/consultant-kpis");
    return data;
  },

  getStudentJourney: async (): Promise<StudentJourneyDto> => {
    const { data } = await apiClient.get<StudentJourneyDto>("/api/analytics/student-journey");
    return data;
  },

  /** Admin revenue report — gross, net, refund rate, MoM growth + monthly + top consultants. */
  getAdminRevenue: async (range?: DateRange): Promise<AdminRevenueDto> => {
    const { data } = await apiClient.get<AdminRevenueDto>(
      `/api/analytics/admin/revenue${rangeQuery(range)}`,
    );
    return data;
  },

  /** Provider application insights — pipeline + breakdowns by country/field/scholarship. */
  getCompanyInsights: async (
    range?: DateRange,
    companyId?: string,
  ): Promise<CompanyInsightsDto> => {
    const params: string[] = [];
    if (range?.from) params.push(`from=${encodeURIComponent(range.from)}`);
    if (range?.to) params.push(`to=${encodeURIComponent(range.to)}`);
    if (companyId) params.push(`companyId=${encodeURIComponent(companyId)}`);
    const qs = params.length ? `?${params.join("&")}` : "";
    const { data } = await apiClient.get<CompanyInsightsDto>(`/api/analytics/company/insights${qs}`);
    return data;
  },

  /** Consultant earnings trend — monthly net + projection + percentile + upcoming. */
  getConsultantEarningsTrend: async (range?: DateRange): Promise<ConsultantEarningsTrendDto> => {
    const { data } = await apiClient.get<ConsultantEarningsTrendDto>(
      `/api/analytics/consultant/earnings-trend${rangeQuery(range)}`,
    );
    return data;
  },
};
