import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { Search } from "lucide-react";
import {
  adminApi,
  type AccountStatus,
  type AdminUserRow,
  type PagedResult,
} from "@/services/api/admin";

const STATUSES: AccountStatus[] = ["PendingApproval", "Active", "Suspended", "Deactivated"];
const ROLES = ["Student", "Company", "Consultant", "Admin", "Moderator"];

function statusBadgeClass(s: AccountStatus): string {
  switch (s) {
    case "Active":
      return "bg-emerald-500/10 text-emerald-500";
    case "PendingApproval":
      return "bg-amber-500/10 text-amber-600";
    case "Suspended":
      return "bg-orange-500/10 text-orange-500";
    case "Deactivated":
      return "bg-rose-500/10 text-rose-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

export function UsersAdmin() {
  const { t } = useTranslation(["admin", "common"]);
  const qc = useQueryClient();

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<AccountStatus | "">("");
  const [role, setRole] = useState("");
  const [includeDeleted, setIncludeDeleted] = useState(false);
  const [page, setPage] = useState(1);

  const params = {
    search: search || undefined,
    status: status || undefined,
    role: role || undefined,
    includeDeleted,
    page,
    pageSize: 25,
  };

  const { data, isLoading } = useQuery<PagedResult<AdminUserRow>>({
    queryKey: ["admin", "users", params],
    queryFn: () => adminApi.searchUsers(params),
    placeholderData: keepPreviousData,
  });

  const statusMut = useMutation({
    mutationFn: ({ id, newStatus, reason }: { id: string; newStatus: AccountStatus; reason?: string }) =>
      adminApi.setUserStatus(id, { status: newStatus, reason }),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "users"] });
    },
    onError: () => toast.error(t("common:status.error")),
  });

  const deleteMut = useMutation({
    mutationFn: (id: string) => adminApi.softDeleteUser(id, "Admin action"),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "users"] });
    },
    onError: () => toast.error(t("common:status.error")),
  });

  const changeStatus = (u: AdminUserRow, newStatus: AccountStatus) => {
    const needsReason = newStatus === "Suspended" || newStatus === "Deactivated";
    const reason = needsReason ? window.prompt(t("admin:users.statusChange.reasonLabel")) : undefined;
    if (needsReason && !reason) return;
    statusMut.mutate({ id: u.id, newStatus, reason: reason ?? undefined });
  };

  const confirmDelete = (u: AdminUserRow) => {
    if (window.confirm(t("admin:users.statusChange.confirmDelete"))) {
      deleteMut.mutate(u.id);
    }
  };

  return (
    <div className="space-y-5">
      <h1 className="text-2xl font-semibold tracking-tight">{t("admin:users.title")}</h1>

      <div className="flex flex-wrap items-center gap-3">
        <label className="relative">
          <Search className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-text-tertiary" aria-hidden />
          <input
            type="search"
            placeholder={t("admin:users.searchPlaceholder")}
            value={search}
            onChange={(e) => { setPage(1); setSearch(e.target.value); }}
            className="h-10 w-72 rounded-md border border-border-subtle bg-bg-elevated ps-10 pe-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
          />
        </label>

        <select
          value={status}
          onChange={(e) => { setPage(1); setStatus(e.target.value as AccountStatus | ""); }}
          className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
        >
          <option value="">{t("admin:users.allStatuses")}</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>{t(`admin:status.${s}`)}</option>
          ))}
        </select>

        <select
          value={role}
          onChange={(e) => { setPage(1); setRole(e.target.value); }}
          className="h-10 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
        >
          <option value="">{t("admin:users.allRoles")}</option>
          {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
        </select>

        <label className="flex items-center gap-2 text-sm text-text-secondary">
          <input
            type="checkbox"
            checked={includeDeleted}
            onChange={(e) => { setPage(1); setIncludeDeleted(e.target.checked); }}
            className="size-4 rounded border-border-subtle"
          />
          {t("admin:users.includeDeleted")}
        </label>
      </div>

      <div className="overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
        <table className="w-full text-sm">
          <thead className="bg-bg-subtle text-start text-xs uppercase tracking-wide text-text-tertiary">
            <tr>
              <th className="px-4 py-3 text-start">{t("admin:users.headers.email")}</th>
              <th className="px-4 py-3 text-start">{t("admin:users.headers.name")}</th>
              <th className="px-4 py-3 text-start">{t("admin:users.headers.status")}</th>
              <th className="px-4 py-3 text-start">{t("admin:users.headers.roles")}</th>
              <th className="px-4 py-3 text-start">{t("admin:users.headers.createdAt")}</th>
              <th className="px-4 py-3 text-start">{t("admin:users.headers.lastLogin")}</th>
              <th className="px-4 py-3 text-end">{t("admin:users.headers.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={7} className="px-4 py-6 text-center text-text-tertiary">{t("admin:common.loading")}</td></tr>
            )}
            {!isLoading && (data?.items.length ?? 0) === 0 && (
              <tr><td colSpan={7} className="px-4 py-6 text-center text-text-tertiary">{t("admin:users.empty")}</td></tr>
            )}
            {data?.items.map((u) => (
              <tr key={u.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                <td className="px-4 py-3 font-medium">{u.email}</td>
                <td className="px-4 py-3">{u.fullName}</td>
                <td className="px-4 py-3">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadgeClass(u.accountStatus)}`}>
                    {t(`admin:status.${u.accountStatus}`)}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs text-text-secondary">{u.roles.join(", ") || "—"}</td>
                <td className="px-4 py-3 text-xs text-text-tertiary">{format(new Date(u.createdAt), "yyyy-MM-dd")}</td>
                <td className="px-4 py-3 text-xs text-text-tertiary">
                  {u.lastLoginAt ? format(new Date(u.lastLoginAt), "yyyy-MM-dd HH:mm") : "—"}
                </td>
                <td className="px-4 py-3 text-end">
                  <div className="inline-flex flex-wrap items-center justify-end gap-1.5">
                    {u.accountStatus !== "Active" && (
                      <button
                        type="button"
                        onClick={() => changeStatus(u, "Active")}
                        className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-brand-500 hover:text-brand-500"
                      >
                        {t("admin:users.actions.activate")}
                      </button>
                    )}
                    {u.accountStatus === "Active" && (
                      <button
                        type="button"
                        onClick={() => changeStatus(u, "Suspended")}
                        className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-amber-500 hover:text-amber-500"
                      >
                        {t("admin:users.actions.suspend")}
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => confirmDelete(u)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-rose-500 hover:text-rose-500"
                    >
                      {t("admin:users.actions.delete")}
                    </button>
                  </div>
                </td>
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
