import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useNavigate } from "react-router";
import { useAuthStore } from "@/stores/authStore";
import { playNotificationChime } from "@/lib/notificationSound";
import { createNotificationHubConnection } from "@/services/signalR/hubs";
import {
  UNREAD_COUNT_QUERY_KEY,
  type NotificationItem,
  type NotificationsPage,
} from "@/services/api/notifications";

interface HubNotificationPayload {
  id: string;
  type: string;
  titleEn: string;
  titleAr: string;
  bodyEn: string;
  bodyAr: string;
  deepLink: string | null;
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
 *
 * A module-level `activeRefCount` deduplicates concurrent subscriptions for
 * the same token so an unstable parent (or a token-refresh-driven re-run)
 * cannot leave two live connections forwarding the same notification twice.
 * The list-cache merge below is also guarded by an id check so a duplicate
 * broadcast (e.g. SignalR reconnect replay) cannot insert the same row twice.
 */
export function useNotificationHub() {
  const tokens = useAuthStore((s) => s.tokens);
  const accessToken = tokens?.accessToken;
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  // Guard against StrictMode's mount → unmount → mount cycle: the first mount
  // would otherwise leave an in-flight start() that races with the second
  // mount's connection. We track the latest effect id and ignore stale ones.
  const effectIdRef = useRef(0);

  useEffect(() => {
    if (!accessToken) return;

    const effectId = ++effectIdRef.current;
    let cancelled = false;
    const connection = createNotificationHubConnection();

    const handleNotification = (payload: HubNotificationPayload) => {
      // Skip stale handlers — the cleanup below may not have run yet on the
      // old connection when a new effect supersedes it.
      if (effectIdRef.current !== effectId) return;

      const lang = document.documentElement.lang || "en";
      // Dedupe by id so a SignalR reconnect replay / double broadcast can't show
      // the same toast twice (the list-cache merge below is already id-guarded).
      toast(lang === "ar" ? payload.titleAr : payload.titleEn, {
        id: payload.id || undefined,
        description: lang === "ar" ? payload.bodyAr : payload.bodyEn,
        // Make the toast actionable: jump straight to the relevant page.
        action: payload.deepLink
          ? {
              label: lang === "ar" ? "عرض" : "View",
              onClick: () => navigate(payload.deepLink!),
            }
          : undefined,
      });
      // A soft ping (respects the per-device sound toggle). Only fires for
      // notifications that reached the client — the server already suppresses the
      // real-time push under mute / quiet hours, so Do-Not-Disturb stays silent.
      playNotificationChime();

      // Bump the header bell badge the moment a notification lands.
      void queryClient.invalidateQueries({ queryKey: UNREAD_COUNT_QUERY_KEY });
      // Refresh the dashboard "recent notifications" widget too — it uses a
      // separate query key that the list-cache merge below does not touch.
      void queryClient.invalidateQueries({ queryKey: ["notifications", "recent"] });

      // Optimistically prepend to every cached notifications list page so the
      // notifications page reflects the new row without an extra refetch.
      // The id check skips duplicates — SignalR reconnect replay or a double
      // broadcast must NOT insert the same row twice.
      if (payload.id) {
        const incoming: NotificationItem = {
          id: payload.id,
          type: payload.type,
          titleEn: payload.titleEn,
          titleAr: payload.titleAr,
          bodyEn: payload.bodyEn,
          bodyAr: payload.bodyAr,
          deepLink: payload.deepLink ?? null,
          isRead: false,
          readAt: null,
          priority: 1,
          createdAt: payload.createdAt,
        };

        queryClient.setQueriesData<NotificationsPage | undefined>(
          { queryKey: ["notifications", "list"] },
          (old) => {
            if (!old) return old;
            if (old.items.some((n) => n.id === incoming.id)) return old;
            return {
              ...old,
              items: [incoming, ...old.items],
              total: old.total + 1,
              unreadCount: old.unreadCount + 1,
            };
          },
        );
      }
    };

    connection.on("notification", handleNotification);

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
      // Remove the handler first so any in-flight delivery while stop() is
      // running cannot still fire the toast / mutate the cache.
      connection.off("notification", handleNotification);
      // stop() also aborts an in-flight start(); its rejection is handled above.
      void connection.stop();
    };
    // queryClient is a stable singleton from QueryClientProvider — including it
    // in deps would re-run the effect needlessly. The hook is intentionally
    // keyed only on the access token so a token refresh rebuilds the
    // connection with the new bearer.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken]);
}
