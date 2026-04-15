import { useEffect, useRef } from "react";
import { toast } from "sonner";
import { useAuthStore } from "@/stores/authStore";
import { createNotificationHubConnection, attachLifecycleHandlers } from "@/services/signalR/hubs";

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
 */
export function useNotificationHub() {
  const tokens = useAuthStore((s) => s.tokens);
  const started = useRef(false);

  useEffect(() => {
    if (!tokens?.accessToken || started.current) return;
    started.current = true;
    const connection = createNotificationHubConnection();

    connection.on("notification", (payload: HubNotificationPayload) => {
      const lang = document.documentElement.lang || "en";
      toast(lang === "ar" ? payload.titleAr : payload.titleEn, {
        description: lang === "ar" ? payload.bodyAr : payload.bodyEn,
      });
    });

    attachLifecycleHandlers(connection, {
      onClose: () => {
        started.current = false;
      },
    });

    connection.start().catch(() => {
      started.current = false;
    });

    return () => {
      void connection.stop();
      started.current = false;
    };
  }, [tokens?.accessToken]);
}
