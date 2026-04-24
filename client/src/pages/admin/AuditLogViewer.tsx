import { useState } from "react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { format } from "date-fns";
import { Search, FilterX } from "lucide-react";
import {
  adminApi,
  type AuditAction,
  type AuditLogDto,
  type AuditLogParams,
  type PagedResult,
} from "@/services/api/admin";

const ACTIONS: AuditAction[] = [
  "Create",
  "Update",
  "Delete",
  "Login",
  "Logout",
  "LoginFailed",
  "PasswordReset",
  "RoleChanged",
  "Approved",
  "Rejected",
  "Moderated",
  "PaymentCaptured",
  "PaymentRefunded",
  "ConfigChanged",
  "BroadcastSent",
];

function actionBadgeClass(a: AuditAction): string {
  switch (a) {
    case "Delete":
    case "Rejected":
    case "LoginFailed":
    case "PaymentRefunded":
      return "bg-rose-500/10 text-rose-500";
    case "Create":
    case "Approved":
    case "PaymentCaptured":
    case "Login":
      return "bg-emerald-500/10 text-emerald-500";
    case "Update":
    case "RoleChanged":
    case "ConfigChanged":
    case "Moderated":
    case "BroadcastSent":
      return "bg-brand-500/10 text-brand-500";
    default:
      return "bg-bg-subtle text-text-secondary";
  }
}

export function AuditLogViewer() {
  const { t } = useTranslation(["admin", "common"]);

  const [search, setSearch] = useState("");
  const [action, setAction] = useState<AuditAction | "">("");
  const [targetType, setTargetType] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);

  const params: AuditLogParams = {
    page,
    pageSize: 50,
    search: search || undefined,
    action: action || undefined,
    targetType: targetType || undefined,
    from: from ? new Date(from).toISOString() : undefined,
    to: to ? new Date(to).toISOString() : undefined,
  };

  const { data, isLoading } = useQuery<PagedResult<AuditLogDto>>({
    queryKey: ["admin", "audit-log", params],
    queryFn: () => adminApi.getAuditLog(params),
    placeholderData: keepPreviousData,
  });

  const clearFilters = () => {
    setSearch(""); setAction(""); setTargetType(""); setFrom(""); setTo(""); setPage(1);
  };

  const hasFilters = search || action || targetType || from || to;

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("admin:audit.title")}</h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">{t("admin:audit.subtitle")}</p>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <label className="relative">
          <Search className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-text-tertiary" aria-hidden />
          <input
            type="search"
            placeholder={t("admin:audit.filters.search")}
            value={search}
            onChange={(e) => { setPage(1); setSearch(e.target.value); }}
            className="h-10 w-80 rounded-md border border-border-subtle bg-bg-elevated ps-10 pe-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>

        <label className="space-y-1 text-xs">
          <span className="block text-text-secondary">{t("admin:audit.filters.action")}</span>
          <select
            value={action}
            onChange={(e) => { setPage(1); setAction(e.target.value as AuditAction | ""); }}
            className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
          >
            <option value="">{t("admin:audit.filters.allActions")}</option>
            {ACTIONS.map((a) => <option key={a} value={a}>{a}</option>)}
          </select>
        </label>

        <label className="space-y-1 text-xs">
          <span className="block text-text-secondary">{t("admin:audit.filters.targetType")}</span>
          <input
            type="text"
            value={targetType}
            onChange={(e) => { setPage(1); setTargetType(e.target.value); }}
            placeholder="User, Scholarship, …"
            className="h-10 w-40 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
          />
        </label>

        <label className="space-y-1 text-xs">
          <span className="block text-text-secondary">{t("admin:audit.filters.from")}</span>
          <input
            type="datetime-local"
            value={from}
            onChange={(e) => { setPage(1); setFrom(e.target.value); }}
            className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
          />
        </label>

        <label className="space-y-1 text-xs">
          <span className="block text-text-secondary">{t("admin:audit.filters.to")}</span>
          <input
            type="datetime-local"
            value={to}
            onChange={(e) => { setPage(1); setTo(e.target.value); }}
            className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
          />
        </label>

        {hasFilters && (
          <button
            type="button"
            onClick={clearFilters}
            className="inline-flex h-10 items-center gap-1.5 rounded-md border border-border-subtle px-3 text-xs text-text-secondary hover:border-border-default hover:text-text-primary"
          >
            <FilterX aria-hidden className="size-3.5" />
            {t("admin:audit.filters.clear")}
          </button>
        )}
      </div>

      <div className="overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("admin:audit.headers.when")}</th>
              <th className="px-4 py-3 text-start">{t("admin:audit.headers.actor")}</th>
              <th className="px-4 py-3 text-start">{t("admin:audit.headers.action")}</th>
              <th className="px-4 py-3 text-start">{t("admin:audit.headers.target")}</th>
              <th className="px-4 py-3 text-start">{t("admin:audit.headers.summary")}</th>
              <th className="px-4 py-3 text-start">{t("admin:audit.headers.ip")}</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={6} className="px-4 py-6 text-center text-text-tertiary">{t("admin:common.loading")}</td></tr>
            )}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={6} className="px-4 py-6 text-center text-text-tertiary">{t("admin:audit.empty")}</td></tr>
            )}
            {data?.items.map((row: AuditLogDto) => (
              <tr key={row.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                <td className="px-4 py-3 text-xs tabular-nums text-text-tertiary">
                  {format(new Date(row.occurredAt), "yyyy-MM-dd HH:mm:ss")}
                </td>
                <td className="px-4 py-3">
                  {row.actorEmail ? (
                    <span className="font-medium">{row.actorEmail}</span>
                  ) : (
                    <span className="text-text-tertiary">{t("admin:audit.system")}</span>
                  )}
                </td>
                <td className="px-4 py-3">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${actionBadgeClass(row.action)}`}>
                    {row.action}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <div className="text-xs text-text-secondary">{row.targetType}</div>
                  {row.targetId && (
                    <div className="font-mono text-[10px] text-text-tertiary" title={row.targetId}>
                      {row.targetId.slice(0, 8)}…
                    </div>
                  )}
                </td>
                <td className="px-4 py-3 text-xs text-text-secondary">{row.summary ?? "—"}</td>
                <td className="px-4 py-3 font-mono text-[11px] text-text-tertiary">{row.ipAddress ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-text-secondary">
          <span>{t("admin:common.page", { page: data.page, total: data.totalPages })}</span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={data.page <= 1}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("admin:common.prev")}
            </button>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
              disabled={data.page >= data.totalPages}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("admin:common.next")}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
