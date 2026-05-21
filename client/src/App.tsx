import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { Toaster } from "sonner";

import { AppRouter } from "@/routes/router";
import { getDirection } from "@/lib/i18n";
import { ErrorBoundary } from "@/components/ErrorBoundary";
import { MaintenancePage } from "@/pages/MaintenancePage";
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

/** Watches html[data-theme] and returns "dark" | "light" */
function useHtmlTheme(): "dark" | "light" {
  const [theme, setTheme] = useState<"dark" | "light">(() => {
    return (document.documentElement.getAttribute("data-theme") as "dark" | "light") ?? "light";
  });

  useEffect(() => {
    const observer = new MutationObserver(() => {
      const t = document.documentElement.getAttribute("data-theme");
      setTheme(t === "dark" ? "dark" : "light");
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });
    return () => observer.disconnect();
  }, []);

  return theme;
}

export function App() {
  const { i18n } = useTranslation();
  const theme = useHtmlTheme();

  const { data: status } = useQuery<StatusResponse>({
    queryKey: ["platform", "status"],
    queryFn: fetchStatus,
    // Poll every 30 s so a maintenance window is detected without a page reload.
    // Failures are silently ignored — don't break the app if /api/status is
    // transiently unreachable.
    refetchInterval: 30_000,
    retry: false,
    staleTime: 0,
  });

  useEffect(() => {
    const dir = getDirection(i18n.language);
    document.documentElement.setAttribute("dir", dir);
    document.documentElement.setAttribute("lang", i18n.language);
  }, [i18n.language]);

  if (status?.maintenanceModeEnabled) {
    return <MaintenancePage />;
  }

  return (
    <ErrorBoundary>
      <>
        <AppRouter />
        <Toaster
        theme={theme}
        position={getDirection(i18n.language) === "rtl" ? "top-left" : "top-right"}
        gap={8}
        richColors
        expand
        closeButton
        duration={4000}
        toastOptions={{
          style: { fontSize: 14, padding: 14, borderRadius: 12 },
          classNames: {
            toast: [
              "group !rounded-xl !border !shadow-lg !font-sans !text-sm",
              "!bg-bg-elevated !border-border-subtle !text-text-primary",
            ].join(" "),
            title:       "!font-semibold !text-text-primary",
            description: "!text-text-secondary",
            closeButton: [
              "!rounded-lg !border !border-border-subtle",
              "!bg-bg-canvas !text-text-tertiary",
              "hover:!bg-bg-subtle hover:!text-text-primary",
            ].join(" "),
            // Semantic variants — keep Sonner's bg but override radius/border
            success: "!border-success-200",
            error:   "!border-danger-200",
            warning: "!border-warning-500/30",
            info:    "!border-brand-200",
            actionButton: [
              "!rounded-lg !bg-brand-500 !text-white !text-xs !font-semibold",
              "hover:!bg-brand-600",
            ].join(" "),
            cancelButton: [
              "!rounded-lg !bg-bg-subtle !text-text-secondary !text-xs",
              "hover:!bg-bg-muted",
            ].join(" "),
          },
        }}
      />
      </>
    </ErrorBoundary>
  );
}
