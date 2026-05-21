import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Wrench, RefreshCw } from "lucide-react";
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
 * PlatformSettings.  Polls GET /api/status every 30 s and automatically
 * navigates to the app root as soon as maintenance ends.
 */
export function MaintenancePage() {
  const { t } = useTranslation("errors");

  useQuery({
    queryKey: ["platform", "status"],
    queryFn: fetchStatus,
    refetchInterval: 30_000,
    retry: false,
    select: (data) => {
      if (!data.maintenanceModeEnabled) {
        // Maintenance is over — hard-reload so the full app bootstraps cleanly.
        window.location.reload();
      }
      return data;
    },
  });

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-8 bg-bg-canvas px-4 text-center">
      {/* Animated wrench */}
      <div className="relative flex size-20 items-center justify-center rounded-full bg-warning-subtle">
        <Wrench className="size-10 text-warning-emphasis" aria-hidden />
        <span
          className="absolute inset-0 animate-ping rounded-full bg-warning-subtle opacity-75"
          aria-hidden
        />
      </div>

      <div className="space-y-3">
        <h1 className="text-2xl font-bold text-text-primary">
          {t("maintenance.title")}
        </h1>
        <p className="max-w-md text-base text-text-secondary">
          {t("maintenance.description")}
        </p>
      </div>

      <div className="flex items-center gap-2 text-xs text-text-tertiary">
        <RefreshCw className="size-3 animate-spin" aria-hidden />
        <span>{t("maintenance.polling")}</span>
      </div>
    </div>
  );
}
