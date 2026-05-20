import { AnalyticsEmbedded } from "@/pages/analytics/AnalyticsEmbedded";

/** PB-015 T-011 — Student Self-Analytics dashboard (Power BI embedded). */
export function StudentAnalytics() {
  return (
    <AnalyticsEmbedded
      reportType="StudentSelfAnalytics"
      titleKey="analytics:studentTitle"
      subtitleKey="analytics:studentSubtitle"
    />
  );
}
