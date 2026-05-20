import { AnalyticsEmbedded } from "@/pages/analytics/AnalyticsEmbedded";

/** PB-015 T-010 — Consultant Self-Analytics dashboard (Power BI embedded). */
export function ConsultantAnalytics() {
  return (
    <AnalyticsEmbedded
      reportType="ConsultantSelfAnalytics"
      titleKey="analytics:consultantTitle"
      subtitleKey="analytics:consultantSubtitle"
    />
  );
}
