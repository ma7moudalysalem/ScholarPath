import {
  useQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useMemo, useState } from "react";
import { Link } from "react-router";
import { toast } from "sonner";
import { motion, AnimatePresence } from "motion/react";
import {
  BellRing,
  Check,
  CheckCheck,
  ChevronRight,
  Filter,
  Inbox,
  Loader2,
  Settings,
  Sparkles,
} from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { ar, enUS } from "date-fns/locale";
import {
  notificationsApi,
  type NotificationItem,
  type NotificationsPage,
} from "@/services/api/notifications";
import { cn } from "@/lib/utils";

const PAGE_STEP = 20;

type Tab = "all" | "unread";

export function Notifications() {
  const { t, i18n } = useTranslation(["notifications", "common"]);
  const qc = useQueryClient();
  const [pageSize, setPageSize] = useState(PAGE_STEP);
  const [tab, setTab] = useState<Tab>("all");

  const { data, isLoading, isError } = useQuery<NotificationsPage>({
    queryKey: ["notifications", "list", pageSize],
    queryFn: () => notificationsApi.list(1, pageSize),
  });

  const invalidate = () =>
    void qc.invalidateQueries({ queryKey: ["notifications"] });

  const markReadMut = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: invalidate,
    onError: () => toast.error(t("notifications:markReadError")),
  });

  const markAllMut = useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: () => {
      toast.success(t("notifications:allRead"));
      invalidate();
    },
    onError: () => toast.error(t("notifications:markReadError")),
  });

  const isAr = i18n.language === "ar";
  const dateLocale = isAr ? ar : enUS;
  const fmtRelative = (iso: string) => {
    try {
      return formatDistanceToNow(new Date(iso), {
        addSuffix: true,
        locale: dateLocale,
      });
    } catch {
      return iso;
    }
  };

  const title = (n: NotificationItem) => (isAr ? n.titleAr : n.titleEn);
  const body = (n: NotificationItem) => (isAr ? n.bodyAr : n.bodyEn);

  const allItems = useMemo(() => data?.items ?? [], [data?.items]);
  const unreadItems = useMemo(
    () => allItems.filter((n) => !n.isRead),
    [allItems],
  );
  const items = tab === "unread" ? unreadItems : allItems;

  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-10">
        <div className="skeleton mb-3 h-10 w-48" />
        <div className="skeleton mb-8 h-5 w-72" />
        <div className="space-y-3">
          {[0, 1, 2].map((i) => (
            <div key={i} className="skeleton h-20 w-full rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-10">
        <div className="card-premium border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
          {t("notifications:loadError")}
        </div>
      </div>
    );
  }

  const unreadCount = data.unreadCount;

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <motion.div
        initial={{ opacity: 0, y: -8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
        className="mb-6"
      >
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <span className="flex size-9 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
                <BellRing aria-hidden className="size-5" />
              </span>
              <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
                {t("notifications:title")}
              </h1>
            </div>
            <p className="mt-2 text-sm text-text-secondary">
              {t("notifications:subtitle")}
            </p>
          </div>
          <Link
            to="/notifications/preferences"
            className="btn btn-secondary btn-sm"
          >
            <Settings aria-hidden className="size-3.5" />
            {t("notifications:managePreferences")}
          </Link>
        </div>
      </motion.div>

      {/* Sticky toolbar */}
      <div className="sticky top-16 z-20 mb-4 -mx-4 border-b border-border-subtle bg-bg-canvas/85 px-4 py-3 backdrop-blur-md">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div
            role="tablist"
            aria-label={t("notifications:title")}
            className="inline-flex rounded-lg border border-border-subtle bg-bg-elevated p-0.5 shadow-elevation-1"
          >
            <TabButton
              active={tab === "all"}
              onClick={() => setTab("all")}
              label={t("notifications:tabs.all")}
              count={allItems.length}
            />
            <TabButton
              active={tab === "unread"}
              onClick={() => setTab("unread")}
              label={t("notifications:tabs.unread")}
              count={unreadCount}
              accent
            />
          </div>
          {unreadCount > 0 && (
            <button
              type="button"
              onClick={() => markAllMut.mutate()}
              disabled={markAllMut.isPending}
              className="btn btn-ghost btn-sm"
            >
              {markAllMut.isPending ? (
                <Loader2 aria-hidden className="size-3.5 animate-spin" />
              ) : (
                <CheckCheck aria-hidden className="size-3.5" />
              )}
              {t("notifications:markAllRead")}
            </button>
          )}
        </div>
      </div>

      {items.length === 0 &&
      !(tab === "unread" && unreadCount > 0 && allItems.length < data.total) ? (
        <motion.div
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
          className="card-premium flex flex-col items-center gap-4 py-16 text-center"
        >
          <span className="flex size-14 items-center justify-center rounded-2xl bg-bg-subtle text-text-tertiary">
            {tab === "unread" ? (
              <Sparkles aria-hidden className="size-7" />
            ) : (
              <Inbox aria-hidden className="size-7" />
            )}
          </span>
          <div className="max-w-sm">
            <p className="text-base font-semibold text-text-primary">
              {tab === "unread"
                ? t("notifications:emptyUnreadTitle")
                : t("notifications:emptyTitle")}
            </p>
            <p className="mt-1 text-sm text-text-secondary">
              {tab === "unread"
                ? t("notifications:emptyUnread")
                : t("notifications:empty")}
            </p>
          </div>
          {tab === "unread" && allItems.length > 0 && (
            <button
              type="button"
              onClick={() => setTab("all")}
              className="btn btn-secondary btn-sm"
            >
              <Filter aria-hidden className="size-3.5" />
              {t("notifications:tabs.viewAll")}
            </button>
          )}
        </motion.div>
      ) : (
        <ul className="space-y-2">
          <AnimatePresence initial={false}>
            {items.map((n, i) => (
              <motion.li
                key={n.id}
                layout
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -4 }}
                transition={{
                  duration: 0.24,
                  ease: [0.22, 1, 0.36, 1],
                  delay: Math.min(i * 0.02, 0.2),
                }}
              >
                <NotificationRow
                  notification={n}
                  title={title(n)}
                  body={body(n)}
                  when={fmtRelative(n.createdAt)}
                  onMarkRead={() => markReadMut.mutate(n.id)}
                />
              </motion.li>
            ))}
          </AnimatePresence>
        </ul>
      )}

      {/* Load-more must work on BOTH tabs: the Unread tab derives its rows from
          the loaded page, so gating this on the "all" tab left unread items
          beyond the first page unreachable. Compare the loaded count (allItems)
          — not the filtered `items` — against the whole-feed total. */}
      {allItems.length < data.total && (
        <div className="mt-6 text-center">
          <button
            type="button"
            onClick={() => setPageSize((p) => p + PAGE_STEP)}
            className="btn btn-secondary btn-sm"
          >
            {t("notifications:loadMore")}
          </button>
        </div>
      )}
    </div>
  );
}

function TabButton({
  active,
  onClick,
  label,
  count,
  accent,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  count?: number;
  accent?: boolean;
}) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
        active
          ? "bg-brand-500 text-white shadow-elevation-1"
          : "text-text-secondary hover:text-text-primary",
      )}
    >
      {label}
      {count !== undefined && count > 0 && (
        <span
          className={cn(
            "inline-flex min-w-[1.25rem] items-center justify-center rounded-full px-1.5 text-[10px] font-semibold",
            active
              ? "bg-white/25 text-white"
              : accent
                ? "bg-brand-500/10 text-brand-600"
                : "bg-bg-subtle text-text-tertiary",
          )}
        >
          {count}
        </span>
      )}
    </button>
  );
}

function NotificationRow({
  notification,
  title,
  body,
  when,
  onMarkRead,
}: {
  notification: NotificationItem;
  title: string;
  body: string;
  when: string;
  onMarkRead: () => void;
}) {
  const { t } = useTranslation(["notifications"]);
  const isRead = notification.isRead;
  const initial = title.charAt(0).toUpperCase() || "·";

  const RowInner = (
    <div
      className={cn(
        "group relative flex items-start gap-3 rounded-xl border p-4 transition-colors",
        isRead
          ? "border-border-subtle bg-bg-elevated hover:bg-bg-muted"
          : "border-brand-200 bg-brand-50/60 hover:bg-brand-50",
      )}
    >
      {/* Unread dot */}
      {!isRead && (
        <span
          aria-hidden
          className="absolute start-2 top-1/2 size-1.5 -translate-y-1/2 rounded-full bg-brand-500"
        />
      )}

      {/* Avatar */}
      <span
        aria-hidden
        className={cn(
          "flex size-10 shrink-0 items-center justify-center rounded-full text-sm font-semibold shadow-elevation-1",
          isRead
            ? "bg-bg-subtle text-text-secondary"
            : "bg-gradient-to-br from-brand-500 to-brand-700 text-white",
        )}
      >
        {initial}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-3">
          <p
            className={cn(
              "truncate text-sm",
              isRead
                ? "font-medium text-text-primary"
                : "font-semibold text-text-primary",
            )}
          >
            {title}
          </p>
          <time className="shrink-0 text-xs text-text-tertiary">{when}</time>
        </div>
        <p className="mt-0.5 line-clamp-2 text-sm text-text-secondary">{body}</p>

        {(notification.deepLink || !isRead) && (
          <div className="mt-2 flex flex-wrap items-center gap-3 text-xs">
            {notification.deepLink && (
              <span className="inline-flex items-center gap-1 font-semibold text-brand-600">
                {t("notifications:view")}
                <ChevronRight
                  aria-hidden
                  className="size-3 transition-transform group-hover:translate-x-0.5 rtl:group-hover:-translate-x-0.5 rtl:rotate-180"
                />
              </span>
            )}
            {!isRead && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                  onMarkRead();
                }}
                className="inline-flex items-center gap-1 font-medium text-text-secondary hover:text-text-primary"
              >
                <Check aria-hidden className="size-3" />
                {t("notifications:markRead")}
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );

  if (notification.deepLink) {
    return (
      <Link
        to={notification.deepLink}
        onClick={() => {
          if (!isRead) onMarkRead();
        }}
        className="block"
      >
        {RowInner}
      </Link>
    );
  }

  return RowInner;
}
