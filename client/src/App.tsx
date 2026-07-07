import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { Toaster } from "sonner";

import { AppRouter } from "@/routes/router";
import { getDirection } from "@/lib/i18n";
import { ErrorBoundary } from "@/components/ErrorBoundary";
import { MaintenancePage } from "@/pages/MaintenancePage";
import { apiClient } from "@/services/api/client";
import { useAuthStore } from "@/stores/authStore";

interface StatusResponse {
  maintenanceModeEnabled: boolean;
  /**
   * Master payments switch. When false the platform runs fully free —
   * fee inputs are hidden, dashboards show a banner, Apply Now / Booking
   * always take the free path. Default true so client code that hasn't
   * loaded the status yet keeps showing paid UI.
   */
  paymentsEnabled: boolean;
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

/**
 * Escape-hatches that keep the app reachable while maintenance mode is on.
 * Without them an admin who flips the toggle ON gets locked out of the very
 * page they need to flip it OFF again.
 *
 * - URL paths the admin needs to disable maintenance from (login + admin
 *   settings + the in-app admin shell). Any path starting with one of these
 *   prefixes skips the maintenance gate.
 * - `?bypass=admin` query flag — for emergency access from any path.
 * - Already-signed-in Admin / SuperAdmin sessions — they always see the app.
 */
const MAINTENANCE_BYPASS_PREFIXES = ["/login", "/admin"];

function shouldBypassMaintenance(roles: readonly string[] | undefined): boolean {
  // Anyone with the bypass query flag (used for emergency access from
  // bookmarked URLs that aren't under the prefix list).
  const params = new URLSearchParams(window.location.search);
  if (params.get("bypass") === "admin") return true;

  // Admin / SuperAdmin sessions can always reach the app even when
  // maintenance is on — otherwise they can't turn it off.
  if (roles?.some((r) => r === "Admin" || r === "SuperAdmin")) return true;

  // The login + admin pages stay live so an admin can sign in and reach the
  // toggle even before they have a session.
  const path = window.location.pathname;
  return MAINTENANCE_BYPASS_PREFIXES.some((p) => path === p || path.startsWith(`${p}/`));
}

export function App() {
  const { i18n } = useTranslation();
  const theme = useHtmlTheme();
  const userRoles = useAuthStore((s) => s.user?.roles);

  const { data: status } = useQuery<StatusResponse>({
    queryKey: ["platform", "status"],
    queryFn: fetchStatus,
    // This is the ONE background poller for platform status. It's mounted once
    // at the app root, so a maintenance window / payments-toggle is detected
    // without a page reload while every other consumer (usePlatformStatus, used
    // by 25+ components) just reads this shared cache. Poll every 60 s — the
    // flags change only on rare admin actions, so a tighter cadence just piles
    // up background requests (a long-open tab was firing hundreds of them).
    // No `staleTime: 0`: it inherits the global 60 s window so remounts serve
    // from cache instead of refetching. Failures are silently ignored — don't
    // break the app if /api/status is transiently unreachable.
    refetchInterval: 60_000,
    retry: false,
  });

  useEffect(() => {
    const dir = getDirection(i18n.language);
    document.documentElement.setAttribute("dir", dir);
    document.documentElement.setAttribute("lang", i18n.language);
  }, [i18n.language]);

  if (status?.maintenanceModeEnabled && !shouldBypassMaintenance(userRoles)) {
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
          style: { fontSize: 14, padding: 16, borderRadius: 16 },
          classNames: {
            toast: [
              "group !gap-3 !rounded-2xl !border !font-sans !text-sm",
              "!shadow-[0_12px_34px_-10px_rgba(2,8,23,0.24)]",
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
