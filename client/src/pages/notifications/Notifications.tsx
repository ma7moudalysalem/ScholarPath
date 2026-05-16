import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useState } from "react";
import { Link } from "react-router";
import { toast } from "sonner";
import { Bell, Check, CheckCheck } from "lucide-react";
import { format } from "date-fns";
import {
  notificationsApi,
  type NotificationItem,
  type NotificationsPage,
} from "@/services/api/notifications";

const PAGE_STEP = 20;

export function Notifications() {
  const { t, i18n } = useTranslation(["notifications", "common"]);
  const qc = useQueryClient();
  const [pageSize, setPageSize] = useState(PAGE_STEP);

  const { data, isLoading, isError } = useQuery<NotificationsPage>({
    queryKey: ["notifications", "list", pageSize],
    queryFn: () => notificationsApi.list(1, pageSize),
  });

  const invalidate = () => void qc.invalidateQueries({ queryKey: ["notifications"] });

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
  const fmtDate = (iso: string) => {
    try {
      return format(new Date(iso), isAr ? "d MMM yyyy, HH:mm" : "MMM d, yyyy, HH:mm");
    } catch {
      return iso;
    }
  };

  const title = (n: NotificationItem) => (isAr ? n.titleAr : n.titleEn);
  const body = (n: NotificationItem) => (isAr ? n.bodyAr : n.bodyEn);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 text-sm text-text-tertiary">
        {t("notifications:loading")}
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 text-sm text-danger-500">
        {t("notifications:loadError")}
      </div>
    );
  }

  const items = data.items;

  return (
    <div className="mx-auto max-w-2xl px-4 py-10">
      <div className="mb-8 flex items-start justify-between gap-4">
        <div>
          <h1 className="mb-2 text-3xl">{t("notifications:title")}</h1>
          <p className="text-text-secondary">{t("notifications:subtitle")}</p>
          {data.unreadCount > 0 && (
            <p className="mt-1 text-sm text-brand-600">
              {t("notifications:unreadCount", { count: data.unreadCount })}
            </p>
          )}
        </div>
        {data.unreadCount > 0 && (
          <button
            type="button"
            onClick={() => markAllMut.mutate()}
            disabled={markAllMut.isPending}
            className="cta-pill inline-flex shrink-0 items-center gap-2 border border-border-default px-4 py-2 text-sm hover:bg-bg-subtle disabled:opacity-50"
          >
            <CheckCheck aria-hidden className="size-4" />
            {t("notifications:markAllRead")}
          </button>
        )}
      </div>

      {items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-xl border border-border-subtle bg-bg-elevated py-16 text-center">
          <Bell aria-hidden className="size-10 text-text-tertiary" />
          <p className="text-sm text-text-secondary">{t("notifications:empty")}</p>
        </div>
      ) : (
        <ul className="space-y-3">
          {items.map((n) => (
            <li
              key={n.id}
              className={`rounded-xl border p-4 ${
                n.isRead
                  ? "border-border-subtle bg-bg-elevated"
                  : "border-brand-200 bg-brand-50"
              }`}
            >
              <div className="flex items-start gap-3">
                <span
                  aria-hidden
                  className={`mt-1.5 size-2 shrink-0 rounded-full ${
                    n.isRead ? "bg-transparent" : "bg-brand-500"
                  }`}
                />
                <div className="min-w-0 flex-1">
                  <p className="font-medium text-text-primary">{title(n)}</p>
                  <p className="mt-0.5 text-sm text-text-secondary">{body(n)}</p>
                  <p className="mt-1 text-xs text-text-tertiary">{fmtDate(n.createdAt)}</p>

                  {(n.deepLink || !n.isRead) && (
                    <div className="mt-2 flex flex-wrap gap-4 text-xs">
                      {n.deepLink && (
                        <Link
                          to={n.deepLink}
                          onClick={() => {
                            if (!n.isRead) markReadMut.mutate(n.id);
                          }}
                          className="font-medium text-brand-600 hover:underline"
                        >
                          {t("notifications:view")}
                        </Link>
                      )}
                      {!n.isRead && (
                        <button
                          type="button"
                          onClick={() => markReadMut.mutate(n.id)}
                          disabled={markReadMut.isPending}
                          className="inline-flex items-center gap-1 font-medium text-text-secondary hover:text-text-primary disabled:opacity-50"
                        >
                          <Check aria-hidden className="size-3.5" />
                          {t("notifications:markRead")}
                        </button>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}

      {items.length < data.total && (
        <div className="mt-6 text-center">
          <button
            type="button"
            onClick={() => setPageSize((p) => p + PAGE_STEP)}
            className="cta-pill border border-border-default px-5 py-2 text-sm hover:bg-bg-subtle"
          >
            {t("notifications:loadMore")}
          </button>
        </div>
      )}
    </div>
  );
}
