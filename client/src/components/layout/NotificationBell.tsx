import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Bell, CheckCheck } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { ar, enUS } from "date-fns/locale";
import {
  notificationsApi,
  UNREAD_COUNT_QUERY_KEY,
  type NotificationItem,
} from "@/services/api/notifications";
import { cn } from "@/lib/utils";

/**
 * Header notification bell with a dropdown preview of the most recent
 * notifications — mark-as-read + jump to the item without leaving the page.
 * The unread badge + recent list share the same query keys the notification hub
 * invalidates on a live push, so they stay in sync in real time.
 */
export function NotificationBell() {
  const { t, i18n } = useTranslation(["notifications", "nav"]);
  const qc = useQueryClient();
  const navigate = useNavigate();
  const isAr = i18n.language.startsWith("ar");

  const [open, setOpen] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);

  const { data: unreadCount = 0 } = useQuery({
    queryKey: UNREAD_COUNT_QUERY_KEY,
    queryFn: () => notificationsApi.unreadCount(),
    refetchInterval: 60_000,
  });

  const { data: recent } = useQuery({
    queryKey: ["notifications", "recent"],
    queryFn: () => notificationsApi.list(1, 6),
  });
  const items = recent?.items ?? [];

  const refresh = () => void qc.invalidateQueries({ queryKey: ["notifications"] });
  const markRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: refresh,
  });
  const markAll = useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: refresh,
  });

  // Close on outside click / Escape.
  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const title = (n: NotificationItem) => (isAr ? n.titleAr : n.titleEn);
  const body = (n: NotificationItem) => (isAr ? n.bodyAr : n.bodyEn);
  const when = (iso: string) => {
    try {
      return formatDistanceToNow(new Date(iso), { addSuffix: true, locale: isAr ? ar : enUS });
    } catch {
      return "";
    }
  };

  const openItem = (n: NotificationItem) => {
    if (!n.isRead) markRead.mutate(n.id);
    setOpen(false);
    if (n.deepLink) navigate(n.deepLink);
  };

  return (
    <div ref={wrapRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={
          unreadCount > 0
            ? `${t("nav:common.notifications")} (${unreadCount})`
            : t("nav:common.notifications")
        }
        className="relative inline-flex size-9 items-center justify-center rounded-lg border border-border-subtle bg-bg-elevated text-text-primary transition hover:bg-bg-subtle"
      >
        <Bell aria-hidden className="size-4" />
        {unreadCount > 0 && (
          <span className="absolute -end-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-danger-500 px-1 text-[10px] font-bold leading-none text-white">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div
          role="menu"
          className="absolute end-0 z-50 mt-2 w-[21rem] max-w-[calc(100vw-1.5rem)] overflow-hidden rounded-xl border border-border-subtle bg-bg-elevated shadow-lg"
        >
          <div className="flex items-center justify-between border-b border-border-subtle px-4 py-3">
            <p className="text-sm font-semibold text-text-primary">
              {t("nav:common.notifications")}
            </p>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() => markAll.mutate()}
                className="inline-flex items-center gap-1 text-xs font-medium text-brand-600 transition hover:text-brand-700"
              >
                <CheckCheck aria-hidden className="size-3.5" />
                {t("notifications:markAllRead")}
              </button>
            )}
          </div>

          {items.length === 0 ? (
            <p className="px-4 py-10 text-center text-sm text-text-secondary">
              {t("notifications:empty")}
            </p>
          ) : (
            <ul className="max-h-[24rem] overflow-y-auto">
              {items.map((n) => (
                <li key={n.id}>
                  <button
                    type="button"
                    onClick={() => openItem(n)}
                    className={cn(
                      "flex w-full items-start gap-2.5 border-b border-border-subtle px-4 py-3 text-start transition hover:bg-bg-subtle",
                      !n.isRead && "bg-bg-subtle/60",
                    )}
                  >
                    <span
                      aria-hidden
                      className={cn(
                        "mt-1.5 size-2 shrink-0 rounded-full",
                        n.isRead ? "bg-transparent" : "bg-brand-500",
                      )}
                    />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-medium text-text-primary">
                        {title(n)}
                      </span>
                      <span className="mt-0.5 block truncate text-xs text-text-secondary">
                        {body(n)}
                      </span>
                      <span className="mt-1 block text-[11px] text-text-tertiary">
                        {when(n.createdAt)}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}

          <button
            type="button"
            onClick={() => {
              setOpen(false);
              navigate("/notifications");
            }}
            className="block w-full border-t border-border-subtle px-4 py-2.5 text-center text-sm font-medium text-brand-600 transition hover:bg-bg-subtle"
          >
            {t("notifications:tabs.viewAll")}
          </button>
        </div>
      )}
    </div>
  );
}
