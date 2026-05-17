import { useEffect } from "react";
import { toast } from "sonner";
import { useAuthStore } from "@/stores/authStore";
import { createNotificationHubConnection } from "@/services/signalR/hubs";

interface HubNotificationPayload {
  titleEn: string;
  titleAr: string;
  bodyEn: string;
  bodyAr: string;
  createdAt: string;
}

/**
 * Subscribes to the notification hub while the user is authenticated.
 * Pushes incoming notifications as sonner toasts. Teammates extend with
 * bell-counter + list updates.
 *
 * The effect is resilient to React StrictMode's deliberate mount → unmount →
 * mount cycle: a per-effect `cancelled` flag means the connection started by
 * the throwaway first mount is stopped cleanly, and the abort error its
 * pending `start()` raises is swallowed instead of surfacing in the console.
 */
export function useNotificationHub() {
  const tokens = useAuthStore((s) => s.tokens);
  const accessToken = tokens?.accessToken;

  useEffect(() => {
    if (!accessToken) return;

    let cancelled = false;
    const connection = createNotificationHubConnection();

    connection.on("notification", (payload: HubNotificationPayload) => {
      const lang = document.documentElement.lang || "en";
      toast(lang === "ar" ? payload.titleAr : payload.titleEn, {
        description: lang === "ar" ? payload.bodyAr : payload.bodyEn,
      });
    });

    connection
      .start()
      .catch((err: unknown) => {
        // A start() that loses the StrictMode race is expected — the cleanup
        // below already stopped this connection. Only real failures (the
        // effect is still live) are worth logging.
        if (!cancelled) {
          console.warn("Notification hub failed to start", err);
        }
      });

    return () => {
      cancelled = true;
      // stop() also aborts an in-flight start(); its rejection is handled above.
      void connection.stop();
    };
  }, [accessToken]);
}
