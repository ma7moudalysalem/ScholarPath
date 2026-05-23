import { useState } from "react";
import { useQuery, useMutation, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { Search, X } from "lucide-react";
import {
  adminApi,
  type AccountStatus,
  type AdminUserRow,
  type PagedResult,
  type RoleOp,
} from "@/services/api/admin";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { PromptDialog } from "@/components/ui/PromptDialog";

// "Unassigned" is the status of a just-registered / first-SSO account that has
// not picked a role yet (it holds NO role row, so the Roles column shows "—").
// Listing it lets an admin filter for these users and grant them a role.
const STATUSES: AccountStatus[] = ["Unassigned", "PendingApproval", "Active", "Suspended", "Deactivated"];
// Real, seeded roles only. "Moderator" was never seeded into AspNetRoles (no
// user can hold it, no [Authorize] uses it), so listing it just produced an
// empty filter — drop it.
const ROLES = ["Student", "Company", "Consultant", "Admin"];
// Roles an admin can grant/revoke from the user table (e.g. to give a
// freshly-registered SSO user who never picked a role their Student role).
const ASSIGNABLE_ROLES = ["Student", "Consultant", "Company", "Admin"];

function statusBadgeClass(s: AccountStatus): string {
  switch (s) {
    case "Active":
      return "bg-success-100 text-success-600";
    case "PendingApproval":
      return "bg-warning-50 text-warning-600";
    case "Suspended":
      return "bg-warning-50 text-warning-500";
    case "Deactivated":
      return "bg-danger-50 text-danger-500";
    default:
      return "bg-bg-subtle text-text-tertiary";
  }
}

export function UsersAdmin() {
  const { t, i18n } = useTranslation(["admin", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
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

  // The API returns totalCount only — derive the page count client-side.
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

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

  const reinstateMut = useMutation({
    mutationFn: (id: string) => adminApi.reinstateBookingIntake(id),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "users"] });
    },
    onError: () => toast.error(t("common:status.error")),
  });

  const roleMut = useMutation({
    mutationFn: ({ id, role, op }: { id: string; role: string; op: RoleOp }) =>
      adminApi.changeUserRole(id, role, op),
    onSuccess: () => {
      toast.success(t("common:status.success"));
      void qc.invalidateQueries({ queryKey: ["admin", "users"] });
    },
    onError: () => toast.error(t("common:status.error")),
  });

  // Suspend/Deactivate require a reason — surface that through a PromptDialog;
  // Activate goes directly through the mutation with no extra dialog.
  const [reasonTarget, setReasonTarget] = useState<
    | { id: string; newStatus: AccountStatus }
    | null
  >(null);
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);
  // The role dialog tracks a user id, then reads the LATEST row from the query
  // so it reflects each grant/revoke as the list refetches.
  const [roleTargetId, setRoleTargetId] = useState<string | null>(null);
  const roleUser = data?.items.find((u) => u.id === roleTargetId) ?? null;

  const changeStatus = (u: AdminUserRow, newStatus: AccountStatus) => {
    const needsReason = newStatus === "Suspended" || newStatus === "Deactivated";
    if (needsReason) {
      setReasonTarget({ id: u.id, newStatus });
      return;
    }
    statusMut.mutate({ id: u.id, newStatus });
  };

  const submitReasonedStatus = (reason: string) => {
    if (!reasonTarget) return;
    if (!reason) return; // Reason is required for Suspend/Deactivate
    statusMut.mutate(
      { id: reasonTarget.id, newStatus: reasonTarget.newStatus, reason },
      { onSettled: () => setReasonTarget(null) },
    );
  };

  const confirmDelete = (u: AdminUserRow) => {
    setDeleteTargetId(u.id);
  };

  const submitDelete = () => {
    if (!deleteTargetId) return;
    deleteMut.mutate(deleteTargetId, {
      onSettled: () => setDeleteTargetId(null),
    });
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
          {ROLES.map((r) => <option key={r} value={r}>{t(`common:roles.${r}`, { defaultValue: r })}</option>)}
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
            {data?.items.map((u: AdminUserRow) => (
              <tr key={u.id} className="border-t border-border-subtle hover:bg-bg-subtle/40">
                <td className="px-4 py-3 font-medium">{u.email}</td>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <span>{u.fullName}</span>
                    {u.isAtRisk && (
                      <span
                        title={
                          u.riskScore != null
                            ? t("admin:users.atRiskTooltip", { score: (u.riskScore * 100).toFixed(0) })
                            : t("admin:users.atRiskGeneric")
                        }
                        className="inline-flex items-center gap-1 rounded-full bg-warning-50 px-1.5 py-0.5 text-[10px] font-medium text-warning-600"
                      >
                        <span aria-hidden>⚠</span>
                        {t("admin:users.atRisk")}
                      </span>
                    )}
                  </div>
                </td>
                <td className="px-4 py-3">
                  <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusBadgeClass(u.accountStatus)}`}>
                    {t(`admin:status.${u.accountStatus}`)}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs text-text-secondary">{u.roles.map((r) => t(`common:roles.${r}`, { defaultValue: r })).join("، ") || "—"}</td>
                <td className="px-4 py-3 text-xs text-text-tertiary">{format(new Date(u.createdAt), "yyyy-MM-dd", { locale: dateLocale })}</td>
                <td className="px-4 py-3 text-xs text-text-tertiary">
                  {u.lastLoginAt ? format(new Date(u.lastLoginAt), "yyyy-MM-dd HH:mm", { locale: dateLocale }) : "—"}
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
                        className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-warning-500 hover:text-warning-600"
                      >
                        {t("admin:users.actions.suspend")}
                      </button>
                    )}
                    {u.bookingIntakeSuspended && (
                      <button
                        type="button"
                        onClick={() => reinstateMut.mutate(u.id)}
                        disabled={reinstateMut.isPending}
                        title={t("admin:users.reinstateIntakeTooltip")}
                        className="rounded-md border border-warning-500 px-2 py-1 text-xs text-warning-600 hover:bg-warning-50 disabled:opacity-50"
                      >
                        {t("admin:users.actions.reinstateIntake")}
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => setRoleTargetId(u.id)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-brand-500 hover:text-brand-500"
                    >
                      {t("admin:users.actions.manageRoles")}
                    </button>
                    <button
                      type="button"
                      onClick={() => confirmDelete(u)}
                      className="rounded-md border border-border-subtle px-2 py-1 text-xs hover:border-danger-400 hover:text-danger-500"
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

      {data && (
        <div className="flex items-center justify-between text-sm text-text-secondary">
          <span>
            {t("admin:common.page", { page: data.page, total: totalPages })}
            {" · "}
            {t("admin:common.totalCount", { count: data.totalCount, defaultValue: "{{count}} total" })}
          </span>
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
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={data.page >= totalPages}
              className="rounded-md border border-border-subtle px-3 py-1 disabled:opacity-50"
            >
              {t("admin:common.next")}
            </button>
          </div>
        </div>
      )}

      <PromptDialog
        open={reasonTarget !== null}
        onOpenChange={(open) => {
          if (!open) setReasonTarget(null);
        }}
        title={t("admin:users.statusChange.title")}
        inputLabel={t("admin:users.statusChange.reasonLabel")}
        inputMultiline
        confirmLabel={t("admin:users.statusChange.submit")}
        cancelLabel={t("admin:users.statusChange.cancel")}
        variant={reasonTarget?.newStatus === "Deactivated" ? "destructive" : "default"}
        loading={statusMut.isPending}
        onConfirm={submitReasonedStatus}
      />

      <ConfirmDialog
        open={deleteTargetId !== null}
        onOpenChange={(open) => {
          if (!open) setDeleteTargetId(null);
        }}
        title={t("admin:users.actions.delete")}
        description={t("admin:users.statusChange.confirmDelete")}
        confirmLabel={t("admin:users.actions.delete")}
        variant="destructive"
        loading={deleteMut.isPending}
        onConfirm={submitDelete}
      />

      {/* Manage roles — grant/revoke roles (e.g. give a role-less SSO user
          their Student role). Calls POST /api/admin/users/{id}/roles. */}
      {roleUser && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-text-primary/30 p-4 backdrop-blur-sm"
          onClick={() => setRoleTargetId(null)}
          role="presentation"
        >
          <div
            className="w-full max-w-md rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-elevation-3"
            onClick={(e) => e.stopPropagation()}
            role="dialog"
            aria-modal="true"
          >
            <div className="mb-1 flex items-start justify-between gap-4">
              <h2 className="text-lg font-bold text-text-primary">
                {t("admin:users.rolesDialog.title")}
              </h2>
              <button
                type="button"
                onClick={() => setRoleTargetId(null)}
                aria-label={t("admin:users.rolesDialog.close")}
                className="rounded-md p-1 text-text-tertiary hover:bg-bg-subtle hover:text-text-primary"
              >
                <X className="size-5" />
              </button>
            </div>
            <p className="mb-4 text-sm text-text-secondary">
              {t("admin:users.rolesDialog.subtitle", { email: roleUser.email })}
            </p>
            <div className="space-y-2">
              {ASSIGNABLE_ROLES.map((r) => {
                const assigned = roleUser.roles.includes(r);
                return (
                  <div
                    key={r}
                    className="flex items-center justify-between rounded-lg border border-border-subtle px-3 py-2"
                  >
                    <span className="text-sm font-medium text-text-primary">
                      {t(`common:roles.${r}`, { defaultValue: r })}
                      {assigned && (
                        <span className="ms-2 text-[11px] font-normal text-success-600">
                          • {t("admin:users.rolesDialog.assigned")}
                        </span>
                      )}
                    </span>
                    <button
                      type="button"
                      disabled={roleMut.isPending}
                      onClick={() =>
                        roleMut.mutate({ id: roleUser.id, role: r, op: assigned ? "Remove" : "Add" })
                      }
                      className={
                        assigned
                          ? "rounded-md border border-danger-200 px-3 py-1 text-xs font-medium text-danger-600 transition hover:bg-danger-50 disabled:opacity-50"
                          : "rounded-md border border-brand-200 px-3 py-1 text-xs font-medium text-brand-600 transition hover:bg-brand-50 disabled:opacity-50"
                      }
                    >
                      {assigned
                        ? t("admin:users.rolesDialog.remove")
                        : t("admin:users.rolesDialog.add")}
                    </button>
                  </div>
                );
              })}
            </div>
            {roleUser.roles.length === 0 && (
              <p className="mt-3 text-xs text-text-tertiary">
                {t("admin:users.rolesDialog.none")}
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
