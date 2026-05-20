import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { BarChart2, ExternalLink, RefreshCw } from "lucide-react";
import { analyticsApi, type PowerBiReportType } from "@/services/api/analytics";

interface AnalyticsEmbeddedProps {
  reportType: PowerBiReportType;
  /** Page heading i18n key (from nav or a dedicated namespace) */
  titleKey: string;
  subtitleKey: string;
}

/** Refresh the embed token 10 minutes before it expires (token lifetime ≈4 h). */
const TOKEN_REFRESH_BUFFER_MS = 10 * 60 * 1000;

/**
 * Generic Power BI embed component (PB-015 T-015).
 *
 * When the Power BI workspace is provisioned it renders the report in an
 * iframe using the short-lived embed token returned by
 * `GET /api/analytics/embed-token`. When the workspace is not configured yet
 * it shows a friendly "coming soon" placeholder.
 *
 * The token is automatically refreshed before it expires so the user never
 * sees an expired-token error while the page is open.
 */
export function AnalyticsEmbedded({ reportType, titleKey, subtitleKey }: AnalyticsEmbeddedProps) {
  const { t } = useTranslation(["analytics"]);
  const [refreshKey, setRefreshKey] = useState(0);
  const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const { data: tokenData, isLoading, isError, refetch } = useQuery({
    queryKey: ["analytics", "embed-token", reportType, refreshKey],
    queryFn: () => analyticsApi.getEmbedToken(reportType),
    staleTime: Infinity,   // We manage staleness manually via the timer
    retry: 1,
  });

  // Schedule a refresh whenever a valid token arrives so the iframe never
  // shows an expired-token error while the page is open.
  useEffect(() => {
    if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);

    if (tokenData?.expiresAt) {
      const expiresMs = new Date(tokenData.expiresAt).getTime();
      const refreshIn = Math.max(0, expiresMs - Date.now() - TOKEN_REFRESH_BUFFER_MS);
      refreshTimerRef.current = setTimeout(() => setRefreshKey((k) => k + 1), refreshIn);
    }

    return () => {
      if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
    };
  }, [tokenData?.expiresAt]);

  return (
    <div className="flex min-h-[80vh] flex-col">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-semibold tracking-tight">{t(titleKey)}</h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">{t(subtitleKey)}</p>
      </div>

      {/* Content area */}
      {isLoading && (
        <div className="flex flex-1 items-center justify-center">
          <div className="size-8 animate-spin rounded-full border-2 border-border-subtle border-t-brand-500" />
        </div>
      )}

      {isError && !isLoading && (
        <div className="flex flex-1 flex-col items-center justify-center gap-4 text-center">
          <p className="text-sm text-danger-500">{t("analytics:loadError")}</p>
          <button
            type="button"
            onClick={() => refetch()}
            className="flex items-center gap-2 rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand hover:bg-brand-600"
          >
            <RefreshCw className="size-4" />
            {t("analytics:retry")}
          </button>
        </div>
      )}

      {!isLoading && !isError && tokenData === null && (
        /* ── "Not yet configured" placeholder ── */
        <div className="flex flex-1 flex-col items-center justify-center gap-6 rounded-xl border-2 border-dashed border-border-subtle p-12 text-center">
          <div className="flex size-16 items-center justify-center rounded-full bg-brand-500/10">
            <BarChart2 className="size-8 text-brand-500" />
          </div>
          <div>
            <p className="text-lg font-medium">{t("analytics:notConfiguredTitle")}</p>
            <p className="mt-1 max-w-sm text-sm text-text-secondary">
              {t("analytics:notConfiguredBody")}
            </p>
          </div>
          <a
            href="https://app.powerbi.com"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 text-sm text-brand-500 hover:underline"
          >
            Power BI Service <ExternalLink className="size-3.5" />
          </a>
        </div>
      )}

      {!isLoading && !isError && tokenData?.isConfigured && tokenData.embedUrl && tokenData.token && (
        /* ── Embedded Power BI iframe ── */
        <iframe
          key={tokenData.token}  // remount when token changes
          title={t(titleKey)}
          src={`${tokenData.embedUrl}&embedToken=${encodeURIComponent(tokenData.token)}`}
          className="min-h-[70vh] w-full flex-1 rounded-xl border border-border-subtle"
          allowFullScreen
        />
      )}
    </div>
  );
}
