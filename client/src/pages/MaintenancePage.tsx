import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Wrench, RefreshCw, Sparkles } from "lucide-react";
import { motion } from "motion/react";
import { apiClient } from "@/services/api/client";

interface StatusResponse {
  maintenanceModeEnabled: boolean;
  version: string;
  serverTime: string;
}

async function fetchStatus(): Promise<StatusResponse> {
  const { data } = await apiClient.get<StatusResponse>("/api/status");
  return data;
}

/**
 * Full-screen maintenance page shown while `maintenance.enabled = "true"` in
 * PlatformSettings.  Polls GET /api/status every 30 s; as soon as maintenance
 * ends, `App.tsx`'s own status gate stops rendering this page and swaps in the
 * router — no page reload required.
 */
export function MaintenancePage() {
  const { t } = useTranslation("errors");

  // Keep the shared status query warm so the App-level gate flips back to the
  // app the moment maintenance ends.
  //
  // We deliberately do NOT call window.location.reload() here. It used to live
  // inside this query's `select`, but `select` must be pure and can run on
  // every render — when the status endpoint briefly reported "off" (e.g. one
  // App Service instance lagging behind another), the reload fired in a loop
  // and the landing page refreshed endlessly. Letting App.tsx re-render into
  // the router on the next poll is loop-proof and needs no reload.
  useQuery({
    queryKey: ["platform", "status"],
    queryFn: fetchStatus,
    refetchInterval: 30_000,
    retry: false,
  });

  return (
    <div className="relative flex min-h-dvh flex-col items-center justify-center gap-8 bg-bg-canvas px-4 text-center overflow-hidden">
      {/* Decorative orbs */}
      <div aria-hidden className="bg-mesh-hero pointer-events-none absolute inset-0 opacity-60" />
      <div aria-hidden className="orb orb-brand orb-animated absolute top-1/4 -start-32 size-72" />
      <div aria-hidden className="orb orb-aurora orb-animated absolute bottom-1/4 -end-32 size-80" style={{ animationDelay: "3s" }} />

      <motion.div
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="relative z-[1] flex flex-col items-center gap-8 max-w-lg"
      >
        {/* Illustration placeholder — animated gradient wrench */}
        <div className="relative">
          <div className="flex size-24 items-center justify-center rounded-3xl bg-gradient-to-br from-warning-500 to-warning-600 text-white shadow-warning-md">
            <motion.div
              animate={{ rotate: [0, -12, 12, 0] }}
              transition={{ duration: 2.5, repeat: Infinity, ease: "easeInOut" }}
            >
              <Wrench className="size-12" aria-hidden />
            </motion.div>
          </div>
          <span
            className="absolute inset-0 animate-ping rounded-3xl bg-warning-500/30"
            aria-hidden
          />
          <div aria-hidden className="absolute inset-0 rounded-3xl bg-warning-500/30 blur-3xl -z-10" />
        </div>

        <div className="space-y-3">
          <span className="badge badge-warning">
            <Sparkles size={11} aria-hidden />
            {t("maintenance.polling")}
          </span>
          <h1 className="text-4xl font-bold text-text-primary tracking-tight">
            {t("maintenance.title")}
          </h1>
          <p className="max-w-md mx-auto text-base text-text-secondary leading-relaxed">
            {t("maintenance.description")}
          </p>
        </div>

        <div className="card-premium px-5 py-3 inline-flex items-center gap-2 text-sm text-text-secondary">
          <RefreshCw className="size-4 animate-spin text-brand-500" aria-hidden />
          <span className="font-medium">{t("maintenance.polling")}</span>
        </div>
      </motion.div>
    </div>
  );
}
