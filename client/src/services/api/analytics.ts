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
};
